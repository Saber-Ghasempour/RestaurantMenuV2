using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Catalog.Domain.Categories;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Catalog.Application.Categories.ChangeMenuCategoryPublication;

public sealed record ChangeMenuCategoryPublicationCommand(
    Guid RestaurantId,
    MenuCategoryId CategoryId,
    bool IsPublished,
    long ExpectedVersion) : ICommand<Result<long>>;
