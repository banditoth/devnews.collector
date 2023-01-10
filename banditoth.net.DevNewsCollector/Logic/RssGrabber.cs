using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using banditoth.net.DevNewsCollector.Entities;
using Microsoft.Extensions.Logging;
using SimpleFeedReader;
using WordPressPCL.Models;

namespace banditoth.net.DevNewsCollector.Logic
{
    public class RssGrabber
    {
        private readonly IEnumerable<RssSource> _sources;
        private readonly ILogger _logger;

        public RssGrabber(IEnumerable<RssSource> sources, ILogger logger)
        {
            this._sources = sources;
            this._logger = logger;
        }

        public async Task<IEnumerable<BlogPost>> GetPostsAsync(DateTime fromDate, DateTime toDate)
        {
            if (_sources?.Any() != true)
                return null;

            List<BlogPost> results = new List<BlogPost>();
            List<Task> feedReaderTasks = new List<Task>();

            foreach (RssSource rssSource in _sources)
            {
                feedReaderTasks.Add(Task<IEnumerable<BlogPost>>.Run(() =>
                {
                    try
                    {
                        FeedReader reader = new FeedReader();
                        IEnumerable<FeedItem> rssFeed = reader.RetrieveFeed(rssSource.RssUrl);

                        if (rssFeed?.Any() != true)
                            return null;

                        _logger.LogInformation($"Checking: {rssSource.RssUrl}");

                        List<BlogPost> posts = new List<BlogPost>();
                        foreach (var post in rssFeed)
                        {
                            if (post.PublishDate < fromDate || post.PublishDate > toDate)
                                continue;

                            _logger.LogInformation($"Post found. Author blog: '{rssSource.Name}', Title: '{post.Title}'");

                            posts.Add(new BlogPost()
                            {
                                SourceId = rssSource.Id,
                                Categories = post.Categories?.ToList(),
                                Title = post.Title,
                                Url = post.Uri.ToString()
                            });
                        }

                        return posts;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Could not read feed: {rssSource.RssUrl}, exception occured");
                        return null;
                    }
                }).ContinueWith(res =>
                {
                    if (!res.IsCompletedSuccessfully)
                        return;

                    if (res.Result?.Any() != true)
                        return;

                    results.AddRange(res.Result);
                }));
            }

            await Task.WhenAll(feedReaderTasks.ToArray());

            return results;
        }
    }
}

