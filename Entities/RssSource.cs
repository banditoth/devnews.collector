using System;
using System.Collections.Generic;
using System.Text;

namespace Pingbacker.Entities
{
    public class RssSource
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string Name { get; set; }

        public string RssUrl { get; set; }
    }
}
