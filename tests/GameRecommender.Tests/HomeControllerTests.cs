using Microsoft.AspNetCore.Mvc;
using GameRecommender.Controllers;

namespace GameRecommender.Tests;

public class HomeControllerTests
{
    [Fact]
    public void Index_ReturnsViewResult()
    {
        // Arrange
        var controller = new HomeController(null);

        // Act
        var result = controller.Index();

        // Assert
        Assert.IsType<ViewResult>(result);
    }
}