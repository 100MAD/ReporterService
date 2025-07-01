using ReporterService.Models;

namespace ReporterService.Services
{
    public interface IArticleService
    {
        Task<List<Article>> GetAllArticlesAsync();
        Task<List<Article>> GetArticlesByCountryAndDateAsync(string country, DateTime date);
        Task<List<Article>> GetRecentArticlesAsync(int days);
        Task<List<(Reporter, int)>> GetTopReportersByArticleCountAsync(int year);
        Task ImportFromCsvAsync(IFormFile file);
    }
}