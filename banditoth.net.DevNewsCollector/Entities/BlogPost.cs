using System;
using System.Collections.Generic;
using System.Text;

namespace banditoth.net.DevNewsCollector.Entities
{
    public class BlogPost
    {
        public List<string> Categories { get; set; }

        public string Title { get; set; }

        public string Url { get; set; }

        public Guid SourceId { get; set; }
    }
}
