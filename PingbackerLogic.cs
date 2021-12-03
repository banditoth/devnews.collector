using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Pingbacker.Entities;
using SimpleFeedReader;
using WordPressPCL;
using WordPressPCL.Models;

namespace Pingbacker
{
    public static class PingbackerLogic
    {
        private static List<RssSource> _sources;

        [FunctionName("PingbackerLogic")]
#if RELEASE
        public static void Run([TimerTrigger("1.00:00:00")] TimerInfo myTimer, ILogger log)
#else
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req, ILogger log)
#endif
        {
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Pingbacker.feeds.json"))
            using (StreamReader reader = new StreamReader(stream))
            {
                _sources = JsonConvert.DeserializeObject<List<RssSource>>(reader.ReadToEnd());
            }

            for (int i = -1; i < 1; i++)
            {
                DateTime targetDate = DateTime.Now.AddDays(i);
                log.LogInformation("Current day: " + targetDate.ToShortDateString());

                try
                {
                    FeedReader reader = new FeedReader();
                    List<PostResult> results = new List<PostResult>();

                    foreach (RssSource rssSource in _sources)
                    {
                        try
                        {
                            foreach (var post in reader.RetrieveFeed(rssSource.RssUrl))
                            {
                                if (post.PublishDate < targetDate || post.PublishDate > targetDate.AddDays(1))
                                    continue;

                                log.LogInformation($"[New result] Author blog: '{rssSource.Name}', Title: '{post.Title}'");

                                results.Add(new PostResult()
                                {
                                    SourceId = rssSource.Id,
                                    Categories = post.Categories?.ToList(),
                                    Title = post.Title,
                                    Url = post.Uri?.ToString()
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            log.LogError($"Failed to read feed: '{rssSource.RssUrl}', exception occured. " + ex.ToString()); ;
                            continue;
                        }
                    }

                    if (results.Count == 0)
                        continue;

                    log.LogInformation($"[Processing] Making blog post content");


                    List<string> postCategoriesDistinct = new List<string>();
                    StringBuilder builder = new StringBuilder();
                    builder.AppendLine("These articles were posted on this day. I hope you find them useful. Enjoy reading.");
                    builder.AppendLine($"<em>These articles are not written by me, but by authors I follow. Get to know them through their articles. Selected from a total of {_sources.Count} sources.</em>");
                    builder.AppendLine("</br>");


                    foreach (RssSource rssSource in _sources)
                    {
                        if (results.Any(z => z.SourceId == rssSource.Id) == false)
                            continue;

                        builder.AppendLine($"<h3>{rssSource.Name}</h3>");
                        builder.AppendLine($"<ul>");

                        foreach (var item in results.Where(z => z.SourceId == rssSource.Id))
                        {
                            builder.AppendLine("<li>");
                            builder.AppendLine($"<a href=\"{item.Url}\" target=\"_blank\">{item.Title}</a> [#{string.Join(" #", item.Categories)}]");
                            builder.AppendLine("</li>");

                            foreach (var category in item.Categories)
                            {
                                if (postCategoriesDistinct.Any(z => z == category) == false)
                                    postCategoriesDistinct.Add(category);
                            }
                        }

                        builder.AppendLine($"</ul>");
                    }

                    builder.AppendLine("</br>");
                    builder.AppendLine($"<em>Make sure you check on my personal blog @ <a href =\"https://www.banditoth.hu/\">banditoth.hu</a></em>");
                    builder.AppendLine($"<em>If you would like your blog to be removed from this list, or would like us to add it to this list, please send us a message here: info [@] banditoth.hu</em>");
                    builder.AppendLine("Have a great day,</br>");
                    builder.AppendLine("bandi");

                    log.LogInformation($"[Processing] Making blog post content finished");

                    log.LogInformation($"[Processing] Authenticating with wordpress");


                    var client = new WordPressClient("https://www.devnews.banditoth.hu/wp-json/")
                    {
                        AuthMethod = AuthMethod.JWT
                    };

                    await client.RequestJWToken(Environment.GetEnvironmentVariable("WORDPRESS_USERNAME"), Environment.GetEnvironmentVariable("WORDPRESS_PASSWORD"));

                    log.LogInformation($"[Processing] Authenticating with wordpress sucessed");


                    if (await client.IsValidJWToken())
                    {
                        IEnumerable<Tag> allTags = await client.Tags.GetAll();
                        List<Tag> tagsShouldBeLinked = new List<Tag>();
                        foreach (var postCategory in postCategoriesDistinct)
                        {
                            try
                            {
                                Tag wpTag = allTags.FirstOrDefault(z => z.Name == postCategory.ToLower());
                                if (wpTag == null)
                                {
                                    log.LogInformation($"[Processing] Creating tag: '{postCategory.ToLower()}'");
                                    wpTag = await client.Tags.Create(new Tag()
                                    {
                                        Name = postCategory.ToLower()
                                    });

                                    allTags = await client.Tags.GetAll();
                                }

                                tagsShouldBeLinked.Add(wpTag);
                            }
                            catch (Exception ex)
                            {
                                log.LogError("Cannot create tag, exception occured. " + ex.ToString()); ;
                            }
                        }

                        try
                        {
                            string title = $".NET related news on {targetDate.ToShortDateString()}";
                            log.LogInformation($"[Processing] Creating post with title: '{title}'");
                            Post createdPost = await client.Posts.Create(new Post()
                            {
                                Title = new Title(title),
                                Content = new Content(builder.ToString()),
                                Tags = tagsShouldBeLinked.Count == 0 ? null : tagsShouldBeLinked.Select(z => z.Id).ToArray()
                            });
                        }
                        catch (Exception ex)
                        {
                            log.LogError("Cannot create post, exception occured. " + ex.ToString()); ;
                        }
                    }


                    log.LogInformation("Day finished: " + targetDate.ToShortDateString());
                }
                catch (Exception ex)
                {
                    log.LogError("GLOBAL Exception occured: " + ex.ToString());
                }
            }

            return new OkObjectResult(null);
        }
    }
}
