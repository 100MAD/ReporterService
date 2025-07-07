using Prometheus;

public static class MetricsRegistry
{
    public static readonly Counter ArticlesCreated = Metrics
        .CreateCounter("total_articles_created", "Total number of articles created");

    public static readonly Counter ReportersCreated = Metrics
        .CreateCounter("total_reporters_created", "Total number of reporters created");

    public static readonly Histogram ImportDuration = Metrics
        .CreateHistogram("csv_import_duration_seconds", "Time taken to import articles from CSV");
}
