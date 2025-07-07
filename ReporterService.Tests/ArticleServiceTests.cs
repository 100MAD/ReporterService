using Xunit;
using ReporterService.Models;
using ReporterService.Services;
using System.Threading.Tasks;
using System;
using System.IO;
using Moq;
using Microsoft.AspNetCore.Http;
using System.Text;
using System.Linq;
using Microsoft.Extensions.Caching.Distributed;


public class ArticleServiceTests
{
    [Fact]
    public async Task ImportFromCsvAsync_ShouldImportArticleWithReporter()
    {
        var context = TestHelpers.CreateInMemoryDbContext();
        var service = TestHelpers.CreateArticleServiceWithCache(context);

        string csv = "RowNumber,Title,Category,Content,Date,Reporter,Country,PriortyNumber\n" +
                     "1,News Title,Politics,Some text,2025-06-29,Ali Rezaei,Iran,1";

        var fileMock = new Mock<IFormFile>();
        var ms = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        fileMock.Setup(f => f.OpenReadStream()).Returns(ms);
        fileMock.Setup(f => f.Length).Returns(ms.Length);
        fileMock.Setup(f => f.FileName).Returns("test.csv");

        await service.ImportFromCsvAsync(fileMock.Object);
        Assert.Single(context.Articles);
        Assert.Single(context.Reporters);
    }

    [Fact]
    public async Task ImportFromCsvAsync_ShouldNotFailOnEmptyCsv()
    {
        var context = TestHelpers.CreateInMemoryDbContext();
        var service = TestHelpers.CreateArticleServiceWithCache(context);

        var csv = "RowNumber,Title,Category,Content,Date,Reporter,Country,PriortyNumber\n";
        var fileMock = new Mock<IFormFile>();
        var ms = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        fileMock.Setup(f => f.OpenReadStream()).Returns(ms);
        fileMock.Setup(f => f.Length).Returns(ms.Length);
        fileMock.Setup(f => f.FileName).Returns("empty.csv");

        await service.ImportFromCsvAsync(fileMock.Object);
        Assert.Empty(context.Articles);
        Assert.Empty(context.Reporters);
    }

    [Fact]
    public async Task GetTopReportersByArticleCountAsync_ReturnsCorrectCount()
    {
        var context = TestHelpers.CreateInMemoryDbContext();
        var reporter = new Reporter
        {
            FirstName = "Ali",
            LastName = "Rezaei",
            Email = "test@test.com",
            Phone = "000",
            HireDate = DateTime.Now,
            Bio = "Test"
        };
        context.Reporters.Add(reporter);
        context.SaveChanges();

        context.Articles.Add(new Article
        {
            Title = "A",
            Summary = "S",
            Content = "Iran",
            PublishDate = new DateTime(2025, 6, 29),
            ReporterId = reporter.Id
        });
        await context.SaveChangesAsync();

        var service = TestHelpers.CreateArticleServiceWithCache(context);
        var result = await service.GetTopReportersByArticleCountAsync(2025);
        Assert.Single(result);
        Assert.Equal(1, result.First().Item2);
    }

    [Fact]
    public async Task GetAllArticlesAsync_ReturnsAllArticlesOrderedByDate()
    {
        var context = TestHelpers.CreateInMemoryDbContext();
        var reporter = new Reporter { FirstName = "Ali", LastName = "Rezaei", Bio = "X", Email = "test@gmai.com", Phone = "-" };
        context.Reporters.Add(reporter);
        context.Articles.AddRange(
            new Article { Title = "A", Content = "X", Summary = "S", PublishDate = new DateTime(2025, 6, 28), Reporter = reporter },
            new Article { Title = "B", Content = "Y", Summary = "S", PublishDate = new DateTime(2025, 6, 29), Reporter = reporter }
        );
        await context.SaveChangesAsync();

        var service = TestHelpers.CreateArticleServiceWithCache(context);
        var result = await service.GetAllArticlesAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal("B", result[0].Title);
    }

    [Fact]
    public async Task GetArticlesByCountryAndDateAsync_ReturnsMatchingArticles()
    {
        var context = TestHelpers.CreateInMemoryDbContext();
        var reporter = new Reporter { FirstName = "Sara", LastName = "Ahmadi", Bio = "X", Email = "test@gmai.com", Phone = "-" };
        context.Reporters.Add(reporter);
        context.Articles.Add(new Article
        {
            Title = "Article1",
            Content = "News from Iran",
            Summary = "S",
            PublishDate = new DateTime(2025, 6, 29),
            Reporter = reporter
        });
        context.Articles.Add(new Article
        {
            Title = "Article2",
            Content = "Something else",
            Summary = "S",
            PublishDate = new DateTime(2025, 6, 29),
            Reporter = reporter
        });
        await context.SaveChangesAsync();

        var service = TestHelpers.CreateArticleServiceWithCache(context);
        var result = await service.GetArticlesByCountryAndDateAsync("Iran", new DateTime(2025, 6, 29));

        Assert.Single(result);
        Assert.Contains("Iran", result.First().Content);
    }

    [Fact]
    public async Task GetRecentArticlesAsync_ReturnsArticlesWithinDays()
    {
        var context = TestHelpers.CreateInMemoryDbContext();
        var reporter = new Reporter { FirstName = "John", LastName = "Smith", Bio = "X", Email = "test@gmai.com", Phone = "-" };
        context.Reporters.Add(reporter);
        var now = DateTime.UtcNow.Date;

        context.Articles.Add(new Article
        {
            Title = "Recent",
            Content = "Today",
            Summary = "S",
            PublishDate = now,
            Reporter = reporter
        });

        context.Articles.Add(new Article
        {
            Title = "Old",
            Content = "Past",
            Summary = "S",
            PublishDate = now.AddDays(-5),
            Reporter = reporter
        });

        await context.SaveChangesAsync();

        var service = TestHelpers.CreateArticleServiceWithCache(context);
        var result = await service.GetRecentArticlesAsync(2);

        Assert.Single(result);
        Assert.Equal("Recent", result[0].Title);
    }
    
    [Fact]
    public async Task GetAllArticlesAsync_ReturnsFromCache_WhenCacheIsHit()
    {
        var context = TestHelpers.CreateInMemoryDbContext();
        var cacheMock = new Mock<IDistributedCache>();

        // Simulate empty cached article list (encoded JSON array)
        string cachedJson = "[]";
        var cacheBytes = Encoding.UTF8.GetBytes(cachedJson);

        cacheMock.Setup(c => c.GetAsync("all_articles", default))
                .ReturnsAsync(cacheBytes);

        var service = TestHelpers.CreateArticleServiceWithCache(context, cacheMock);
        var result = await service.GetAllArticlesAsync();

        Assert.NotNull(result);
        Assert.Empty(result); // Because we cached an empty list
    }

    [Fact]
    public async Task GetRecentArticlesAsync_CacheMiss_StoresResultInCache()
    {
        var context = TestHelpers.CreateInMemoryDbContext();
        var reporter = new Reporter { FirstName = "Ali", LastName = "Rezaei", Bio = "Bio", Email = "email", Phone = "123" };
        context.Reporters.Add(reporter);
        context.Articles.Add(new Article
        {
            Title = "Today",
            Content = "Content",
            Summary = "Summary",
            PublishDate = DateTime.UtcNow.Date,
            Reporter = reporter
        });
        await context.SaveChangesAsync();

        var cacheMock = new Mock<IDistributedCache>();
        cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), default))
                .ReturnsAsync((byte[])null); // Simulate cache miss

        var service = TestHelpers.CreateArticleServiceWithCache(context, cacheMock);
        var result = await service.GetRecentArticlesAsync(1);

        Assert.Single(result);
        Assert.Equal("Today", result[0].Title);
        cacheMock.Verify(c => c.SetAsync(
            It.Is<string>(s => s.StartsWith("recent_articles_")),
            It.IsAny<byte[]>(),
            It.IsAny<DistributedCacheEntryOptions>(),
            default), Times.Once);
    }
}
