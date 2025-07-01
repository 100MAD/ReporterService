using Microsoft.EntityFrameworkCore;
using ReporterService.Models;

namespace ReporterService.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        public DbSet<Reporter> Reporters { get; set; }
        public DbSet<Article> Articles { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Reporter>().ToTable("reporters");
            modelBuilder.Entity<Article>().ToTable("articles");
        }
    }
}