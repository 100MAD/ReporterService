namespace ReporterService.Models
{   
    public class CsvArticle
    {
        public int RowNumber { get; set; }
        public string Title { get; set; }
        public string Category { get; set; }
        public string Content { get; set; }
        public DateTime Date { get; set; }
        public string Reporter { get; set; }
        public string Country { get; set; }
        public int PriortyNumber { get; set; }
    }
}