
namespace ReporterService.Models
{
    public class Article
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string Summary { get; set; }
        public DateTime PublishDate { get; set; }
        public int ReporterId { get; set; }
        public Reporter Reporter { get; set; }
    }
}