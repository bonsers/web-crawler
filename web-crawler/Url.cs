using System;
using System.ComponentModel.DataAnnotations;

namespace web_crawler
{
    class Url
    {
        public Guid Id { get; set; }
        public Guid ParentId { get; set; }
        [StringLength(250)]
        //[Index("IX_FirstAndSecond", 1, IsUnique = true)]
        public string url { get; set; }
        public DateTime? lastCrawled { get; set; }
        //public DateTime? firstAdded { get; set; }
        public bool? seedList { get; set; }
    }
}
