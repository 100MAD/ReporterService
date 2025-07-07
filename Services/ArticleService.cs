using System.Globalization;
using CsvHelper;
using Microsoft.EntityFrameworkCore;
using ReporterService.Models;
using ReporterService.Data;
using Prometheus;
using Microsoft.Extensions.Caching.Distributed;

namespace ReporterService.Services
{
    public class ArticleService : IArticleService
    {
        private readonly AppDbContext _context;
        private readonly IDistributedCache _cache;

        public ArticleService(AppDbContext context, IDistributedCache cache)
        {
            _context = context;
            _cache = cache;
        }

        public async Task<List<Article>> GetAllArticlesAsync()
        {
            var cacheKey = "all_articles";
            var cached = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cached))
            {
                MetricsRegistry.CacheHits.Inc();
                return System.Text.Json.JsonSerializer.Deserialize<List<Article>>(cached)!;
            }

            var articles = await _context.Articles
                .Include(a => a.Reporter)
                .OrderByDescending(a => a.PublishDate)
                .ToListAsync();

            var serialized = System.Text.Json.JsonSerializer.Serialize(articles);
            await _cache.SetStringAsync(cacheKey, serialized, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            });

            return articles;
        }

        public async Task<List<Article>> GetArticlesByCountryAndDateAsync(string country, DateTime date) =>
            await _context.Articles
            .Include(a => a.Reporter)
            .Where(a => a.PublishDate.Date == date.Date && a.Content.Contains(country))
            .ToListAsync();
        public async Task<List<Article>> GetRecentArticlesAsync(int days)
        {
            var cacheKey = $"recent_articles_{days}";
            var cached = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cached))
            {
                MetricsRegistry.CacheHits.Inc();
                return System.Text.Json.JsonSerializer.Deserialize<List<Article>>(cached)!;
            }

            var threshold = DateTime.UtcNow.AddDays(-days).Date;

            var articles = await _context.Articles
                .Where(a => a.PublishDate.Date >= threshold)
                .Include(a => a.Reporter)
                .OrderByDescending(a => a.PublishDate)
                .ToListAsync();

            var serialized = System.Text.Json.JsonSerializer.Serialize(articles);
            await _cache.SetStringAsync(cacheKey, serialized, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            });

            return articles;
        }

        public async Task<List<(Reporter, int)>> GetTopReportersByArticleCountAsync(int year)
        {
            var result = await _context.Articles
                .Where(a => a.PublishDate.Year == year)
                .GroupBy(a => a.Reporter)
                .Select(g => new { Reporter = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .ToListAsync();

            return result.Select(r => (r.Reporter, r.Count)).ToList();
        }

        public async Task ImportFromCsvAsync(IFormFile file)
        {
            using var reader = new StreamReader(file.OpenReadStream());
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            var records = csv.GetRecords<CsvArticle>().ToList();
            using (MetricsRegistry.ImportDuration.NewTimer())
            {
                foreach (var rec in records)
                {
                    var nameParts = (rec.Reporter ?? "").Split(' ', 2);
                    string firstName = nameParts.Length > 0 ? nameParts[0] : "Unknown";
                    string lastName = nameParts.Length > 1 ? nameParts[1] : "Reporter";

                    var reporter = await _context.Reporters.FirstOrDefaultAsync(r => r.FirstName == firstName && r.LastName == lastName);
                    if (reporter == null)
                    {
                        reporter = new Reporter
                        {
                            FirstName = firstName,
                            LastName = lastName,
                            HireDate = DateTime.SpecifyKind(DateTime.Now.Date, DateTimeKind.Utc),
                            Email = "unknown@example.com",
                            Phone = "-",
                            Bio = rec.Country ?? "Unknown"
                        };
                        _context.Reporters.Add(reporter);
                        MetricsRegistry.ReportersCreated.Inc();
                        await _context.SaveChangesAsync();
                    }
                    _context.Articles.Add(new Article
                    {
                        Title = rec.Title,
                        Content = (rec.Content + " Country: " + rec.Country).Trim(),
                        Summary = rec.Category,
                        PublishDate = DateTime.SpecifyKind(rec.Date.Date, DateTimeKind.Utc),
                        ReporterId = reporter.Id
                    });
                    MetricsRegistry.ArticlesCreated.Inc();
                }
                await _context.SaveChangesAsync();
            }
            await _cache.RemoveAsync("all_articles");
            await _cache.RemoveAsync($"recent_articles_1");
        }
    }
}