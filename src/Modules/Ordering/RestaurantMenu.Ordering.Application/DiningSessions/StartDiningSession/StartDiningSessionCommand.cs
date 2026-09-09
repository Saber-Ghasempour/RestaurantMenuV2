using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Ordering.Application.DiningSessions.StartDiningSession;
public sealed record StartDiningSessionCommand(string Code) : ICommand<Result<IssuedDiningSession>>;
