using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using StackExchange.Redis;

using RestaurantMenu.Application.Abstractions.Data;
using RestaurantMenu.Restaurants.Application.Abstractions.Caching;
using RestaurantMenu.Restaurants.Application.Abstractions.Data;
using RestaurantMenu.Restaurants.Infrastructure.Caching;
using RestaurantMenu.Restaurants.Infrastructure.Database;
using RestaurantMenu.Restaurants.Infrastructure.Restaurants;
using RestaurantMenu.Restaurants.Infrastructure.Memberships;
using RestaurantMenu.Restaurants.Application.Restaurants.GetRestaurant;
using RestaurantMenu.Restaurants.Application.Restaurants.ListRestaurants;
using RestaurantMenu.Restaurants.Application.Restaurants.UpdateRestaurant;

using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Restaurants.Application.Restaurants.CreateRestaurant;
using RestaurantMenu.Restaurants.Application.Restaurants.DeleteRestaurant;
using RestaurantMenu.Restaurants.Domain.Restaurants;
using RestaurantMenu.SharedKernel.Results;
using RestaurantMenu.Restaurants.Application.Restaurants.UpdateRestaurantProfile;
using RestaurantMenu.Restaurants.Application.Restaurants.UpdateRestaurantDefaults;
using RestaurantMenu.Restaurants.Application.Branches.ChangeBranchStatus;
using RestaurantMenu.Restaurants.Application.Branches.CreateBranch;
using RestaurantMenu.Restaurants.Application.Branches.DeleteBranch;
using RestaurantMenu.Restaurants.Application.Branches.GetBranch;
using RestaurantMenu.Restaurants.Application.Branches.ListBranches;
using RestaurantMenu.Restaurants.Application.Branches.UpdateBranch;
using RestaurantMenu.Restaurants.Domain.Branches;
using RestaurantMenu.Restaurants.Infrastructure.Branches;
using RestaurantMenu.Restaurants.Application.Abstractions.Security;
using RestaurantMenu.Restaurants.Application.DiningTables;
using RestaurantMenu.Restaurants.Application.DiningTables.ChangeDiningTableStatus;
using RestaurantMenu.Restaurants.Application.DiningTables.CreateDiningTable;
using RestaurantMenu.Restaurants.Application.DiningTables.ListDiningTables;
using RestaurantMenu.Restaurants.Application.DiningTables.UpdateDiningTable;
using RestaurantMenu.Restaurants.Application.PublicMenuCodes;
using RestaurantMenu.Restaurants.Application.PublicMenuCodes.CreatePublicMenuCode;
using RestaurantMenu.Restaurants.Application.PublicMenuCodes.ListPublicMenuCodes;
using RestaurantMenu.Restaurants.Application.PublicMenuCodes.ResolvePublicMenuCode;
using RestaurantMenu.Restaurants.Application.PublicMenuCodes.RevokePublicMenuCode;
using RestaurantMenu.Restaurants.Application.PublicMenuCodes.RotatePublicMenuCode;
using RestaurantMenu.Restaurants.Domain.DiningTables;
using RestaurantMenu.Restaurants.Domain.PublicMenuCodes;
using RestaurantMenu.Restaurants.Infrastructure.DiningTables;
using RestaurantMenu.Restaurants.Infrastructure.PublicMenuCodes;
using RestaurantMenu.Restaurants.Application.Memberships.AssignBranchMembership;
using RestaurantMenu.Restaurants.Application.Memberships;

namespace RestaurantMenu.Restaurants.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddRestaurantsInfrastructure(
        this IServiceCollection services,
        string connectionString,
        string redisConnectionString,
        TimeSpan cacheTimeToLive,
        int redisConnectTimeoutMilliseconds,
        int redisOperationTimeoutMilliseconds,
        KeycloakAdminOptions? keycloakAdminOptions = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentException.ThrowIfNullOrWhiteSpace(redisConnectionString);

        if (cacheTimeToLive <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(cacheTimeToLive),
                cacheTimeToLive,
                "Cache time-to-live must be positive.");
        }

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(
            redisConnectTimeoutMilliseconds);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(
            redisOperationTimeoutMilliseconds);

        services.AddDbContext<RestaurantsDbContext>(
            options =>
                options.UseNpgsql(
                    connectionString,
                    npgsqlOptions =>
                        npgsqlOptions.MigrationsHistoryTable(
                            "__ef_migrations_history",
                            "restaurants")));

        services.AddScoped<
            IRestaurantRepository,
            RestaurantRepository>();

        services.AddScoped<
            IRestaurantReadService,
            RestaurantReadService>();

        services.AddScoped<IBranchRepository, BranchRepository>();
        services.AddScoped<IBranchReadService, BranchReadService>();
        services.AddScoped<IDiningTableRepository, DiningTableRepository>();
        services.AddScoped<IDiningTableReadService, DiningTableReadService>();
        services.AddScoped<IPublicMenuCodeRepository, PublicMenuCodeRepository>();
        services.AddScoped<IPublicMenuCodeReadService, PublicMenuCodeReadService>();
        services.AddSingleton<IPublicMenuCodeGenerator, CryptographicPublicMenuCodeGenerator>();

        services.AddScoped<RestaurantMembershipRepository>();
        services.AddScoped<IRestaurantMembershipRepository>(
            serviceProvider =>
                serviceProvider.GetRequiredService<
                    RestaurantMembershipRepository>());
        services.AddScoped<IRestaurantMembershipReadService>(
            serviceProvider =>
                serviceProvider.GetRequiredService<
                    RestaurantMembershipRepository>());
        services.AddScoped<BranchMembershipRepository>();
        services.AddScoped<IBranchMembershipRepository>(sp => sp.GetRequiredService<BranchMembershipRepository>());
        services.AddScoped<IBranchMembershipReadService>(sp => sp.GetRequiredService<BranchMembershipRepository>());

        var redisConfiguration =
            ConfigurationOptions.Parse(redisConnectionString);
        redisConfiguration.AbortOnConnectFail = false;
        redisConfiguration.BacklogPolicy = BacklogPolicy.FailFast;
        redisConfiguration.ConnectRetry = 2;
        redisConfiguration.ConnectTimeout =
            redisConnectTimeoutMilliseconds;
        redisConfiguration.AsyncTimeout =
            redisOperationTimeoutMilliseconds;
        redisConfiguration.SyncTimeout =
            redisOperationTimeoutMilliseconds;
        redisConfiguration.ReconnectRetryPolicy =
            new LinearRetry(500);

        services.AddStackExchangeRedisCache(
            options =>
            {
                options.ConfigurationOptions = redisConfiguration;
                options.InstanceName = "restaurant-menu:v1:";
            });
        services.AddSingleton(
            new RestaurantCacheOptions(cacheTimeToLive));
        services.AddSingleton<
            IRestaurantCache,
            RestaurantCache>();

        services.AddScoped<IUnitOfWork>(
            serviceProvider =>
                serviceProvider.GetRequiredService<
                    RestaurantsDbContext>());

        services.AddScoped<
            ICommandHandler<
                CreateRestaurantCommand,
                Result<RestaurantId>>,
            CreateRestaurantCommandHandler>();

        services.AddScoped<
            IQueryHandler<
                GetRestaurantQuery,
                Result<RestaurantResponse>>,
            GetRestaurantQueryHandler>();

        services.AddScoped<
            IQueryHandler<
                ListRestaurantsQuery,
                Result<RestaurantsPage>>,
            ListRestaurantsQueryHandler>();

        services.AddScoped<
            ICommandHandler<
                UpdateRestaurantCommand,
                Result<long>>,
            UpdateRestaurantCommandHandler>();

        services.AddScoped<
            ICommandHandler<
                DeleteRestaurantCommand,
                Result<RestaurantId>>,
            DeleteRestaurantCommandHandler>();

        services.AddScoped<
            ICommandHandler<
                UpdateRestaurantProfileCommand,
                Result<long>>,
            UpdateRestaurantProfileCommandHandler>();

        services.AddScoped<
            ICommandHandler<UpdateRestaurantDefaultsCommand, Result<long>>,
            UpdateRestaurantDefaultsCommandHandler>();
        services.AddScoped<ICommandHandler<
            RestaurantMenu.Restaurants.Application.Restaurants.SetRestaurantBranding.SetRestaurantBrandingCommand,
            Result<long>>,
            RestaurantMenu.Restaurants.Application.Restaurants.SetRestaurantBranding.SetRestaurantBrandingCommandHandler>();

        services.AddScoped<
            ICommandHandler<
                RestaurantMenu.Restaurants.Application.Restaurants.UpdateRestaurantLinks.UpdateRestaurantLinksCommand,
                Result<long>>,
            RestaurantMenu.Restaurants.Application.Restaurants.UpdateRestaurantLinks.UpdateRestaurantLinksCommandHandler>();

        services.AddScoped<IRestaurantSlugLookup, RestaurantSlugLookup>();
        services.AddScoped<
            ICommandHandler<RestaurantMenu.Restaurants.Application.Restaurants.ChangeRestaurantSlug.ChangeRestaurantSlugCommand, Result<long>>,
            RestaurantMenu.Restaurants.Application.Restaurants.ChangeRestaurantSlug.ChangeRestaurantSlugCommandHandler>();

        services.AddScoped<ICommandHandler<CreateBranchCommand, Result<BranchId>>, CreateBranchCommandHandler>();
        services.AddScoped<IQueryHandler<GetBranchQuery, Result<BranchResponse>>, GetBranchQueryHandler>();
        services.AddScoped<IQueryHandler<ListBranchesQuery, Result<BranchesPage>>, ListBranchesQueryHandler>();
        services.AddScoped<ICommandHandler<UpdateBranchCommand, Result<long>>, UpdateBranchCommandHandler>();
        services.AddScoped<ICommandHandler<ChangeBranchStatusCommand, Result<long>>, ChangeBranchStatusCommandHandler>();
        services.AddScoped<ICommandHandler<DeleteBranchCommand, Result<BranchId>>, DeleteBranchCommandHandler>();
        services.AddScoped<ICommandHandler<CreateDiningTableCommand, Result<DiningTableId>>, CreateDiningTableCommandHandler>();
        services.AddScoped<IQueryHandler<ListDiningTablesQuery, Result<IReadOnlyList<DiningTableResponse>>>, ListDiningTablesQueryHandler>();
        services.AddScoped<ICommandHandler<UpdateDiningTableCommand, Result<long>>, UpdateDiningTableCommandHandler>();
        services.AddScoped<ICommandHandler<ChangeDiningTableStatusCommand, Result<long>>, ChangeDiningTableStatusCommandHandler>();
        services.AddScoped<ICommandHandler<CreatePublicMenuCodeCommand, Result<IssuedPublicMenuCode>>, CreatePublicMenuCodeCommandHandler>();
        services.AddScoped<IQueryHandler<ListPublicMenuCodesQuery, Result<IReadOnlyList<PublicMenuCodeResponse>>>, ListPublicMenuCodesQueryHandler>();
        services.AddScoped<ICommandHandler<RotatePublicMenuCodeCommand, Result<IssuedPublicMenuCode>>, RotatePublicMenuCodeCommandHandler>();
        services.AddScoped<ICommandHandler<RevokePublicMenuCodeCommand, Result<long>>, RevokePublicMenuCodeCommandHandler>();
        services.AddScoped<IQueryHandler<ResolvePublicMenuCodeQuery, Result<ResolvedPublicMenuCode>>, ResolvePublicMenuCodeQueryHandler>();
        services.AddScoped<ICommandHandler<AssignBranchMembershipCommand, Result<BranchMembershipResponse>>, AssignBranchMembershipCommandHandler>();
        services.AddScoped<IMembershipInvitationRepository, MembershipInvitationRepository>();
        services.AddSingleton<IInvitationTokenGenerator, CryptographicInvitationTokenGenerator>();
        if (keycloakAdminOptions is null) services.AddScoped<IIdentityProvisioner, UnconfiguredIdentityProvisioner>();
        else { services.AddSingleton(keycloakAdminOptions); services.AddSingleton(new HttpClient()); services.AddScoped<IIdentityProvisioner, KeycloakIdentityProvisioner>(); }
        services.AddSingleton(new InvitationOptions(TimeSpan.FromDays(2)));
        services.AddScoped<ICommandHandler<InviteMemberCommand, Result<InvitationResponse>>, InviteMemberCommandHandler>();
        services.AddScoped<ICommandHandler<AcceptInvitationCommand, Result<MemberResponse>>, AcceptInvitationCommandHandler>();
        services.AddScoped<ICommandHandler<ChangeMemberCommand, Result<MemberResponse>>, ChangeMemberCommandHandler>();
        services.AddScoped<ICommandHandler<RevokeInvitationCommand, Result<InvitationResponse>>, RevokeInvitationCommandHandler>();
        services.AddScoped<IQueryHandler<ListInvitationsQuery, Result<IReadOnlyList<InvitationResponse>>>, ListInvitationsQueryHandler>();
        services.AddScoped<IQueryHandler<ListMembersQuery, Result<IReadOnlyList<MemberResponse>>>, ListMembersQueryHandler>();
        services.AddScoped<IQueryHandler<ListMembershipAuditQuery, Result<IReadOnlyList<AuditResponse>>>, ListMembershipAuditQueryHandler>();

        services.AddSingleton(TimeProvider.System);

        return services;
    }
}