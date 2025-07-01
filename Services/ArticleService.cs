using System.Globalization;
using CsvHelper;
using Microsoft.EntityFrameworkCore;
using ReporterService.Models;
using ReporterService.Metrics;
using ReporterService.Data;

namespace ReporterService.Services
{
    public class ArticleService : IArticleService
    {
        private readonly AppDbContext _context;

        public ArticleService(AppDbContext context) => _context = context;

        public async Task<List<Article>> GetAllArticlesAsync() =>
        await _context.Articles.Include(a => a.Reporter)
            .OrderByDescending(a => a.PublishDate)
            .ToListAsync();

        public async Task<List<Article>> GetArticlesByCountryAndDateAsync(string country, DateTime date) =>
            await _context.Articles
            .Include(a => a.Reporter)
            .Where(a => a.PublishDate.Date == date.Date && a.Content.Contains(country))
            .ToListAsync();
        public async Task<List<Article>> GetRecentArticlesAsync(int days)
        {
            var threshold = DateTime.UtcNow.AddDays(-days).Date;
            return await _context.Articles
                .Where(a => a.PublishDate.Date >= threshold)
                .Include(a => a.Reporter)
                .OrderByDescending(a => a.PublishDate)
                .ToListAsync();
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
                    AppMetrics.IncrementReportersCreated();
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
                AppMetrics.IncrementArticlesCreated();
            }
            await _context.SaveChangesAsync();
            AppMetrics.IncrementCsvImports();
        }
    }
}