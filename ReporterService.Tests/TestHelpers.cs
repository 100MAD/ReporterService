using Microsoft.EntityFrameworkCore;
using ReporterService.Data;
using Microsoft.Extensions.Caching.Distributed;
using ReporterService.Services;
using Moq;

public static class TestHelpers
{
    public static AppDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
    public static ArticleService CreateArticleServiceWithCache(AppDbContext context, Mock<IDistributedCache> cacheMock = null)
    {
        return new ArticleService(context, cacheMock?.Object ?? new Mock<IDistributedCache>().Object);
    }
}
