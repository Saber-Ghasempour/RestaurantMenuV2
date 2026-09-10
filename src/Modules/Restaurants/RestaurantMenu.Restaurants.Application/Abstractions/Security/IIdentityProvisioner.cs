namespace RestaurantMenu.Restaurants.Application.Abstractions.Security;

public interface IIdentityProvisioner { Task<string?> EnsureUserAsync(string email, CancellationToken cancellationToken); }
public interface IInvitationTokenGenerator { (string Raw, string Hash) Generate(); string Hash(string raw); bool IsWellFormed(string raw); }