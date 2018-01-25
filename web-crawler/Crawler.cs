using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Configuration;
using System.Text.RegularExpressions;

namespace web_crawler
{
    class Crawler
    {
        private string pattern;
        private int CONFIG_CRAWL_DEPTH;
        private string CONFIG_SEED_LIST_PATH;
        private bool CONFIG_DOWNLOAD_FILE_ENABLED;

        public Crawler()
        {
            CONFIG_CRAWL_DEPTH = Convert.ToInt32(ConfigurationManager.AppSettings["CONFIG_CRAWL_DEPTH"]);
            pattern = "(?:^|\")(http|https|ftp):(?://)?(\\w+(?:[\\.:@]\\w+)*?)(?:/|@)([^\"\\?]*?)(?:\\?([^\\?\"]*?))?(?:$|\")";
            CONFIG_SEED_LIST_PATH = ConfigurationManager.AppSettings["CONFIG_SEED_LIST_PATH"].ToString();
            CONFIG_DOWNLOAD_FILE_ENABLED = Convert.ToBoolean(ConfigurationManager.AppSettings["CONFIG_DOWNLOAD_FILE_ENABLED"]);
        }

        public void crawlSeedListOnly()
        {
            //Console.WriteLine("Crawl seed list only");
            foreach (var a in getSeedList())
            {
                var u = new Url() { ParentId = Guid.NewGuid(), Id = Guid.NewGuid(), url = a, lastCrawled = null, seedList = null };
                Console.WriteLine(u.ParentId + "|" + u.Id + "|" + u.url + "|" + u.lastCrawled);
                using (var db = new CrawlerDbContext())
                {
                    //db.Database.Log = Console.Write;
                    db.Urls.Add(u);
                    db.SaveChanges();
                }
                processUrls(u);
            }
        }

        public void crawlSeedList()
        {
            insertSeedListIntoDatabase();
            while (true)
            {
                var r = selectNextUrl();
                Console.WriteLine(r.ParentId + "|" + r.Id + "|" + r.url + "|" + r.lastCrawled);
                processUrls(r);
            }
        }

        private List<string> getSeedList()
        {
            var list = new List<string>();
            string line = String.Empty;
            using (var reader = new StreamReader(CONFIG_SEED_LIST_PATH))
            {
                while ((line = reader.ReadLine()) != null)
                {
                    list.Add(line);
                }
            }
            return list;
        }

        private void processUrls(Url url)
        {
            string html = webRequest(url);
            var matchesList = findMatches(html, pattern);
            foreach (var i in matchesList)
            {
                try
                {
                    string temp = i.TrimStart('"').TrimEnd('"');
                    for (int j = 0; j <= CONFIG_CRAWL_DEPTH; j++)
                    {
                        var tempRec = new Url() { ParentId = url.Id, Id = Guid.NewGuid(), url = formatUrl(temp, j), lastCrawled = null, seedList = null };
                        using (var db = new CrawlerDbContext())
                        {
                            var u = new Url() { ParentId = tempRec.ParentId, Id = tempRec.Id, url = tempRec.url.ToString(), lastCrawled = null, seedList = null };
                            db.Urls.Add(u);
                            db.SaveChanges();
                            Console.WriteLine("--> " + u.ParentId.ToString() + "|" + u.Id.ToString() + "|" + u.url + "|" + u.lastCrawled);
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("ERROR: " + e.Message);
                }
            }
            // Update lastCrawled
            using (var db = new CrawlerDbContext())
            {
                var query = from a in db.Urls
                            where a.Id == url.Id
                            select a;

                foreach (var item in query)
                {
                    item.lastCrawled = DateTime.Now;
                    //Console.WriteLine("**** " + item.Id + " | " + item.ParentId + " | " + item.url + " | " + item.lastCrawled + " ****");
                }
                db.SaveChanges();
            }
        }

        private string webRequest(Url u)
        {
            var html = String.Empty;
            try
            {
                html = new WebClient().DownloadString(u.url);
                if (CONFIG_DOWNLOAD_FILE_ENABLED)
                {
                    //Create folder and download webpage
                    if (Directory.Exists("Files/" + u.Id.ToString()))
                    {
                        Directory.Delete("Files/" + u.Id.ToString());
                    }
                    Directory.CreateDirectory("Files/" + u.Id.ToString());
                    // Download webpage
                    new WebClient().DownloadFile(u.url, "Files/" + u.Id.ToString() + "/1");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(u.url + ": " + e.Message);
            }
            return html;
        }

        private List<string> findMatches(string input, string regex)
        {
            var matchesList = new List<string>();
            MatchCollection matches = Regex.Matches(input, regex);
            foreach (Match match in matches)
            {
                matchesList.Add(match.Groups[0].Value.TrimStart('"').TrimEnd('"'));
            }
            return matchesList;
        }

        private string formatUrl(string match, int crawlDepth)
        {
            var builder = new StringBuilder();
            var url = new Uri(match);
            if (!url.IsFile)
            {
                builder.Append(url.Scheme);
                builder.Append("://");
                builder.Append(url.Host);
                for (int i = 0; i <= crawlDepth; i++)
                {
                    builder.Append(url.Segments[i]);
                }
                return builder.ToString();
            }
            else
            {
                return String.Empty;
            }
        }

        private void insertSeedListIntoDatabase()
        {
            var newSeed = Guid.NewGuid();
            foreach (var i in getSeedList())
            {
                var u = new Url() { ParentId = newSeed, Id = Guid.NewGuid(), url = i, lastCrawled = null, seedList = true };
                using (var db = new CrawlerDbContext())
                {
                    db.Urls.Add(u);
                    db.SaveChanges();
                }
            }
        }

        private Url selectNextUrl()
        {
            using (var db = new CrawlerDbContext())
            {
                var query = from a in db.Urls
                            where a.seedList == true
                            orderby a.lastCrawled
                            select a;
                return query.First();
            }
        }
    }
}
