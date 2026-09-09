using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Catalog.Domain.Categories;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Catalog.Application.Categories.SetMenuCategoryImage;

public sealed record SetMenuCategoryImageCommand(Guid RestaurantId, MenuCategoryId CategoryId,
    Guid? ImageMediaId, long ExpectedVersion) : ICommand<Result<long>>;
