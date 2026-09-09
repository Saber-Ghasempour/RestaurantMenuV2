using RestaurantMenu.Catalog.Domain.Categories;

namespace RestaurantMenu.Catalog.Domain.UnitTests.Categories;

public sealed class MenuCategoryImageTests
{
    [Fact]
    public void ImageShouldValidateReferenceAndBeIdempotent()
    {
        var category = MenuCategory.Create(MenuCategoryId.New(), Guid.CreateVersion7(), null,
            "Food", 1, DateTimeOffset.UtcNow).Value;
        Assert.Equal(MenuCategoryErrors.InvalidMediaReference, category.SetImage(Guid.Empty).Error);
        var image = Guid.CreateVersion7();
        Assert.True(category.SetImage(image).IsSuccess);
        Assert.Equal(image, category.ImageMediaId);
        Assert.Equal(2, category.Version);
        category.SetImage(image);
        Assert.Equal(2, category.Version);
    }
}
