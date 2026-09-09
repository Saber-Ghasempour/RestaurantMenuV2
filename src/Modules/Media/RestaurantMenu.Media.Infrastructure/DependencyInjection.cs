using Amazon.S3;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Media.Application.Abstractions;
using RestaurantMenu.Media.Application.Assets;
using RestaurantMenu.Media.Domain.Assets;
using RestaurantMenu.Media.Infrastructure.Assets;
using RestaurantMenu.Media.Infrastructure.Database;
using RestaurantMenu.Media.Infrastructure.Storage;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Media.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddMediaInfrastructure(this IServiceCollection services,
        string connectionString, ObjectStorageOptions storageOptions)
    {
        services.AddDbContext<MediaDbContext>(options => options.UseNpgsql(connectionString,
            npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "media")));
        services.AddSingleton(storageOptions);
        services.AddSingleton<IAmazonS3>(_ => new AmazonS3Client(storageOptions.AccessKey,
            storageOptions.SecretKey, new AmazonS3Config
            {
                ServiceURL = storageOptions.ServiceUrl,
                ForcePathStyle = storageOptions.ForcePathStyle,
                AuthenticationRegion = "us-east-1",
                SignatureVersion = "4"
            }));
        services.AddSingleton<IObjectStorage, S3ObjectStorage>();
        services.AddScoped<MediaAssetRepository>();
        services.AddScoped<IMediaAssetRepository>(sp => sp.GetRequiredService<MediaAssetRepository>());
        services.AddScoped<IMediaAssetReadService>(sp => sp.GetRequiredService<MediaAssetRepository>());
        services.AddScoped<IMediaUnitOfWork>(sp => sp.GetRequiredService<MediaDbContext>());
        services.AddScoped<ICommandHandler<InitiateUploadCommand, Result<InitiateUploadResponse>>, InitiateUploadCommandHandler>();
        services.AddScoped<ICommandHandler<CompleteUploadCommand, Result<MediaAssetResponse>>, CompleteUploadCommandHandler>();
        services.AddScoped<ICommandHandler<DeleteMediaAssetCommand, Result<MediaAssetId>>, DeleteMediaAssetCommandHandler>();
        services.AddScoped<ICommandHandler<RejectMediaAssetCommand, Result<MediaAssetId>>, RejectMediaAssetCommandHandler>();
        services.AddScoped<IQueryHandler<GetMediaAssetQuery, Result<MediaAssetResponse>>, GetMediaAssetQueryHandler>();
        services.AddScoped<IQueryHandler<ResolvePublicMediaQuery, Result<Uri>>, ResolvePublicMediaQueryHandler>();
        services.AddSingleton(TimeProvider.System);
        services.AddHostedService<PendingMediaCleanupWorker>();
        return services;
    }
}
