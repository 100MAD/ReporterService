using Microsoft.AspNetCore.Mvc;
using ReporterService.Services;
using ReporterService.Models;
using ReporterService.Metrics;


[ApiController]
[Route("api/[controller]")]
public class ArticleController : ControllerBase
{
    private readonly IArticleService _service;
    public ArticleController(IArticleService service) => _service = service;

    [HttpGet("all")]
    public async Task<IActionResult> GetAll()
    {
        var articles = await _service.GetAllArticlesAsync();
        var result = articles.Select(a => new
        {
            a.Title,
            a.Summary,
            a.Content,
            PublishDate = a.PublishDate.ToString("yyyy/MM/dd"),
            Reporter = a.Reporter.FirstName + " " + a.Reporter.LastName,
        });
        return Ok(result);
    }

    [HttpGet("by-country-date")]
    public async Task<IActionResult> GetByCountryAndDate([FromQuery] string country, [FromQuery] DateTime date)
    {
        date = DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);
        var articles = await _service.GetArticlesByCountryAndDateAsync(country, date);
        var result = articles.Select(a => new
        {
            a.Title,
            a.Summary,
            a.Content,
            PublishDate = a.PublishDate.ToString("yyyy/MM/dd"),
            Reporter = a.Reporter.FirstName + " " + a.Reporter.LastName,
        });
        return Ok(result);
    }
    [HttpGet("recent")]
    public async Task<IActionResult> GetRecent([FromQuery] int days = 1)
    {
        var articles = await _service.GetRecentArticlesAsync(days);
        var result = articles.Select(a => new 
        {
            a.Title,
            a.Summary,
            a.Content,
            PublishDate = a.PublishDate.ToString("yyyy/MM/dd"),
            Reporter = a.Reporter.FirstName + " " + a.Reporter.LastName,
        });
        return Ok(result);
    }

    [HttpGet("top-reporters")]
    public async Task<IActionResult> GetTopReporters([FromQuery] int year)
    {
        var list = await _service.GetTopReportersByArticleCountAsync(year);
        var result = list.Select(item => new
        {
            Reporter = item.Item1.FirstName + " " + item.Item1.LastName,
            Count = item.Item2,
        });
        return Ok(result);
    }
    [HttpPost("import")]
    public async Task<IActionResult> ImportCsv(IFormFile file)
    {
        if (file == null || file.Length == 0) return BadRequest("Invalid file.");
        await _service.ImportFromCsvAsync(file);
        return Ok("Imported successfully.");
    }

    [HttpGet("metrics")]
    public async Task<IActionResult> GetMetrics()
    {
            var lines = new List<string>
        {
            "# HELP total_articles_created Total number of articles created.",
            "# TYPE total_articles_created counter",
            $"total_articles_created {AppMetrics.TotalArticlesCreated}",

            "# HELP total_reporters_created Total number of reporters created.",
            "# TYPE total_reporters_created counter",
            $"total_reporters_created {AppMetrics.TotalReportersCreated}",

            "# HELP TotalCsvImports Total number of CsvImports.",
            "# TYPE TotalCsvImports counter",
            $"TotalCsvImports {AppMetrics.TotalCsvImports}"
        };

        var content = string.Join("\n", lines) + "\n";
        return Content(content, "text/plain");
    }


}