using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using ReporterService.Models;
using Xunit;

public class ArticleControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ArticleControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAll_ReturnsOkWithArticles()
    {
        var response = await _client.GetAsync("/api/article/all");

        response.EnsureSuccessStatusCode();

        var articles = await response.Content.ReadFromJsonAsync<List<dynamic>>();
        Assert.NotNull(articles);
    }

    [Fact]
    public async Task GetByCountryAndDate_ReturnsFilteredArticles()
    {
        var date = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var url = $"/api/article/by-country-date?country=Iran&date={date}";

        var response = await _client.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var articles = await response.Content.ReadFromJsonAsync<List<dynamic>>();
        Assert.NotNull(articles);
    }

    [Fact]
    public async Task GetRecent_ReturnsRecentArticles()
    {
        var response = await _client.GetAsync("/api/article/recent?days=3");

        response.EnsureSuccessStatusCode();

        var articles = await response.Content.ReadFromJsonAsync<List<dynamic>>();
        Assert.NotNull(articles);
    }

    [Fact]
    public async Task GetTopReporters_ReturnsList()
    {
        var response = await _client.GetAsync("/api/article/top-reporters?year=2025");

        response.EnsureSuccessStatusCode();

        var top = await response.Content.ReadFromJsonAsync<List<dynamic>>();
        Assert.NotNull(top);
    }

    [Fact]
    public async Task ImportCsv_ShouldReturnSuccess()
    {
        var csvContent = "RowNumber,Title,Category,Content,Date,Reporter,Country,PriortyNumber\n" +
                         "1,Test Article,General,Content,2025-06-29,Ali Rezaei,Iran,1";

        var fileContent = new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes(csvContent));
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("text/csv");

        var form = new MultipartFormDataContent();
        form.Add(fileContent, "file", "test.csv");

        var response = await _client.PostAsync("/api/article/import", form);

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadAsStringAsync();
        Assert.Contains("Imported", result);
    }
}
