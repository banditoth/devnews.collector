using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using banditoth.net.DevNewsCollector.Entities;
using Microsoft.Extensions.Logging;

namespace banditoth.net.DevNewsCollector.Logic
{
	public class PostContentGenerator : IDisposable
	{
        private StringBuilder _builder;
        private readonly IEnumerable<RssSource> _sources;
        private readonly ILogger _logger;

        public PostContentGenerator(IEnumerable<RssSource> sources, ILogger logger)
		{
            if (sources?.Any() != true)
                throw new ArgumentNullException(nameof(sources));

            this._sources = sources;
            this._logger = logger;
            _builder = new StringBuilder();
        }

        public PostContent GetPostContent(IEnumerable<BlogPost> posts, DateTime onDay)
        {
            if (posts?.Any() != true)
                return null;

            _logger.LogInformation($"Generating post content from {posts.Count()} blogpost.");

            _builder.AppendLine("These articles were posted on this day. I hope you find them useful. Enjoy reading.");
            _builder.AppendLine($"<em>These articles are not written by me, but by authors I follow. Get to know them through their articles. Selected from a total of {_sources.Count()} sources.</em>");
            _builder.AppendLine("</br>");

            List<string> postCategoriesDistinct = new List<string>();

            foreach (RssSource rssSource in _sources)
            {
                if (posts.Any(z => z.SourceId == rssSource.Id) == false)
                    continue;

                _builder.AppendLine($"<h3>{rssSource.Name}</h3>");
                _builder.AppendLine($"<ul>");

                foreach (var item in posts.Where(z => z.SourceId == rssSource.Id))
                {
                    _builder.AppendLine("<li>");
                    _builder.AppendLine($"<a href=\"{item.Url}\" target=\"_blank\">{item.Title}</a> [#{string.Join(" #", item.Categories)}]");
                    _builder.AppendLine("</li>");

                    foreach (var category in item.Categories)
                    {
                        if (postCategoriesDistinct.Any(z => z == category) == false)
                            postCategoriesDistinct.Add(category);
                    }
                }

                _builder.AppendLine($"</ul>");
            }

            _builder.AppendLine("</br>");
            _builder.AppendLine($"<em>Make sure you check on my personal blog @ <a href =\"https://www.banditoth.net/\">banditoth.net</a></em>");
            _builder.AppendLine($"<em>If you would like your blog to be removed from this list, or would like us to add it to this list, please send us a message here: info [@] banditoth.net</em>");
            _builder.AppendLine("Have a great day,</br>");
            _builder.AppendLine("bandi");

            _logger.LogInformation($"Successfully created post.");


            return new PostContent()
            {
                RawPost = _builder.ToString(),
                Title = $".NET related news {onDay.ToShortDateString()}",
                Categories = postCategoriesDistinct,
            };
        }

        public void Dispose()
        {
            _builder = null;
        }
    }
}

