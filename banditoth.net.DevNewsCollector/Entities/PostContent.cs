using System;
using System.Collections.Generic;

namespace banditoth.net.DevNewsCollector.Entities
{
	public class PostContent
	{
		public string Title { get; set; }

		public string RawPost { get; set; }

		public IEnumerable<string> Categories { get; set; }
	}
}

