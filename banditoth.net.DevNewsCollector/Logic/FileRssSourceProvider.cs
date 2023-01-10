using System;
using banditoth.net.DevNewsCollector.Entities;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace banditoth.net.DevNewsCollector.Logic
{
	public class FileRssSourceProvider
	{
		public FileRssSourceProvider()
		{

		}

		public IEnumerable<RssSource> GetRssSources(string embeddedResourceName = "banditoth.net.DevNewsCollector.Resources.feeds.json")
		{
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(embeddedResourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                return JsonConvert.DeserializeObject<List<RssSource>>(reader.ReadToEnd());
            }
        }
	}
}

