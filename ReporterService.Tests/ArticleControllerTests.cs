using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using ReporterService.Services;
using ReporterService.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Dynamic;
using Microsoft.CSharp.RuntimeBinder;

public class ArticleControllerTests
{
    [Fact]
    public async Task GetAll_ReturnsOkObjectResult()
    {
        var mockService = new Mock<IArticleService>();
        mockService.Setup(s => s.GetAllArticlesAsync()).ReturnsAsync(new List<Article>
        {
            new Article
            {
                Title = "T", Summary = "S", Content = "C",
                PublishDate = new DateTime(2025, 6, 29),
                Reporter = new Reporter { FirstName = "Ali", LastName = "R" }
            }
        });

        var controller = new ArticleController(mockService.Object);
        var result = await controller.GetAll();
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task ImportCsv_NullFile_ReturnsBadRequest()
    {
        var mockService = new Mock<IArticleService>();
        var controller = new ArticleController(mockService.Object);
        var result = await controller.ImportCsv(null);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task ImportCsv_ValidFile_ReturnsOk()
    {
        // Arrange
        var content = "RowNumber,Title,Category,Content,Date,Reporter,Country,PriortyNumber\n" +
                    "1,Title1,Cat,Content here,2025-01-01,Ali,IR,1";
        var fileMock = new Mock<IFormFile>();
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));
        fileMock.Setup(f => f.OpenReadStream()).Returns(stream);
        fileMock.Setup(f => f.FileName).Returns("test.csv");
        fileMock.Setup(f => f.Length).Returns(stream.Length);

        var mockService = new Mock<IArticleService>();
        mockService.Setup(s => s.ImportFromCsvAsync(It.IsAny<IFormFile>())).Returns(Task.CompletedTask);

        var controller = new ArticleController(mockService.Object);

        // Act
        var result = await controller.ImportCsv(fileMock.Object);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }


    [Fact]
    public async Task GetRecent_ReturnsOk_WithArticles()
    {
        // Arrange
        var mockService = new Mock<IArticleService>();
        mockService.Setup(s => s.GetRecentArticlesAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<Article>
            {
                new Article
                {
                    Title = "Sample",
                    Summary = "Summary",
                    Content = "Sample Content",
                    PublishDate = DateTime.UtcNow,
                    Reporter = new Reporter { FirstName = "Ali", LastName = "Rezaei" }
                },
                new Article
                {
                    Title = "Another",
                    Summary = "Another Summary",
                    Content = "Another Content",
                    PublishDate = DateTime.UtcNow,
                    Reporter = new Reporter { FirstName = "Sara", LastName = "Karimi" }
                }
            });

        var controller = new ArticleController(mockService.Object);

        // Act
        var result = await controller.GetRecent(7);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedList = Assert.IsAssignableFrom<IEnumerable<object>>(okResult.Value);
        Assert.Equal(2, returnedList.Count());
    }

    [Fact]
    public async Task GetByCountryAndDate_NoArticles_ReturnsEmptyList()
    {
        // Arrange
        var mockService = new Mock<IArticleService>();
        mockService.Setup(s => s.GetArticlesByCountryAndDateAsync("Nowhere", It.IsAny<DateTime>()))
                .ReturnsAsync(new List<Article>());
        var controller = new ArticleController(mockService.Object);

        // Act
        var result = await controller.GetByCountryAndDate("Nowhere", new DateTime(2025, 1, 1)) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var articles = Assert.IsAssignableFrom<IEnumerable<object>>(result.Value);
        Assert.Empty(articles);
    }

    [Fact]
    public async Task GetTopReporters_ReturnsExpectedReporters()
    {
        // Arrange
        var mockService = new Mock<IArticleService>();
        mockService.Setup(s => s.GetTopReportersByArticleCountAsync(2))
                .ReturnsAsync(new List<(Reporter, int)>
                {
                    (new Reporter { FirstName = "A", LastName = "B" }, 5),
                    (new Reporter { FirstName = "C", LastName = "D" }, 3)
                });
        var controller = new ArticleController(mockService.Object);

        // Act
        var result = await controller.GetTopReporters(2) as OkObjectResult;

        // Assert
        Assert.NotNull(result);
        var reporters = Assert.IsAssignableFrom<IEnumerable<object>>(result.Value);
        Assert.Equal(2, reporters.Count());
    }

}
