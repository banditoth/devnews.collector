using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using banditoth.net.DevNewsCollector.Logic;
using System.Collections.Generic;
using banditoth.net.DevNewsCollector.Entities;

namespace banditoth.net.DevNewsCollector
{
    public class DevNewsCollector
    {
        [FunctionName("Collect")]
#if RELEASE
                public static async Task Run([TimerTrigger("0 0 10 * * *")]TimerInfo myTimer, ILogger log)
#elif DEBUG
        public static async Task Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req, ILogger log)
#endif
        {
            FileRssSourceProvider rssSourceProvider = new FileRssSourceProvider();
            IEnumerable<Entities.RssSource> sources = rssSourceProvider.GetRssSources();

            DateTime currentDate = DateTime.Now;

            try
            {
                RssGrabber grabber = new RssGrabber(sources, log);
                IEnumerable<Entities.BlogPost> posts = await grabber.GetPostsAsync(currentDate.AddDays(-1), currentDate.AddDays(1));

                using (PostContentGenerator postGenerator = new PostContentGenerator(sources, log))
                {
                    PostContent postContent = postGenerator.GetPostContent(posts, currentDate);

                    using (WordPressPublisher wp = new WordPressPublisher(Environment.GetEnvironmentVariable("WP_URL"),
                        Environment.GetEnvironmentVariable("WP_USERNAME"),
                        Environment.GetEnvironmentVariable("WP_PASSWORD"), log))
                    {
                        await wp.PublishPost(postContent);
                    }
                }
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Could not generate post, exception occured");
            }
        }
    }
}

