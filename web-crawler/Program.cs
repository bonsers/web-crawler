using System;
using System.Configuration;

namespace web_crawler
{
    class Program
    {
        static void Main(string[] args)
        {
            var CONFIG_CONT_CRAWLING_FROM_DB = Convert.ToBoolean(ConfigurationManager.AppSettings["CONFIG_CONT_CRAWLING_FROM_DB"]);

            var crawler = new Crawler();
            if (CONFIG_CONT_CRAWLING_FROM_DB)
                crawler.crawlSeedList();
            else
                crawler.crawlSeedListOnly();
        }
    }
}
