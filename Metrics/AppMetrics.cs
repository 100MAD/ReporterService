namespace ReporterService.Metrics
{
    public static class AppMetrics
    {
        private static int _articlesCreated = 0;
        private static int _reportersCreated = 0;
        private static int _csvImports = 0;

        public static int TotalArticlesCreated => _articlesCreated;
        public static int TotalReportersCreated => _reportersCreated;
        public static int TotalCsvImports => _csvImports;

        public static void IncrementArticlesCreated() => Interlocked.Increment(ref _articlesCreated);
        public static void IncrementReportersCreated() => Interlocked.Increment(ref _reportersCreated);
        public static void IncrementCsvImports() => Interlocked.Increment(ref _csvImports);
    }
}
