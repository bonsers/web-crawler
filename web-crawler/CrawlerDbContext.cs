using System;
using System.Data.Entity;

namespace web_crawler
{
    class CrawlerDbContext : DbContext
    {
        public DbSet<Url> Urls { get; set; }
    }
}
