using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Threading.Tasks;
using banditoth.net.DevNewsCollector.Entities;
using Microsoft.Extensions.Logging;
using WordPressPCL;
using WordPressPCL.Models;

namespace banditoth.net.DevNewsCollector.Logic
{
    public class WordPressPublisher : IDisposable
    {
        private WordPressClient _wpClient;
        private string _userName;
        private string _password;
        private readonly ILogger _logger;

        public WordPressPublisher(string url, string userName, string password, ILogger logger)
        {
            _wpClient = new WordPressClient(url);
            _wpClient.Auth.UseBasicAuth(userName, password);
            this._userName = userName;
            this._password = password;
            this._logger = logger;
        }

        public async Task PublishPost(PostContent content)
        {
            _logger.LogInformation($"Authentication started");

            _wpClient.Auth.UseBearerAuth(JWTPlugin.JWTAuthByEnriqueChavez);
            await _wpClient.Auth.RequestJWTokenAsync(_userName, _password);

            if (await _wpClient.Auth.IsValidJWTokenAsync() == false)
                throw new Exception("Could not authenticate with WordPress");

            _logger.LogInformation($"Authentication sucessed for user {_userName}");

            IEnumerable<Tag> allTags = await _wpClient.Tags.GetAllAsync();
            _logger.LogInformation($"Existing tags: {(allTags?.Any() != true ? "No tags yet." : string.Join(",", allTags.Select(z => z.Name)))}");


            List<Tag> tagsShouldBeLinked = new List<Tag>();

            foreach (var postCategory in content.Categories)
            {
                try
                {
                    Tag wpTag = allTags.FirstOrDefault(z => z.Name == postCategory.ToLower());

                    if (wpTag == null)
                    {
                        _logger.LogInformation($"Tag was not existing. Creating : {postCategory}");

                        wpTag = await _wpClient.Tags.CreateAsync(new Tag()
                        {
                            Name = postCategory.ToLower()
                        });

                        allTags = await _wpClient.Tags.GetAllAsync();
                    }

                    tagsShouldBeLinked.Add(wpTag);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Can't check tags, exception occured");
                }
            }

            try
            {
                _logger.LogInformation($"Creating post with title: {content.Title}.");
                Post createdPost = await _wpClient.Posts.CreateAsync(new Post()
                {
                    Title = new Title(content.Title),
                    Content = new Content(content.RawPost),
                    Tags = tagsShouldBeLinked?.Any() != true ? null : tagsShouldBeLinked.Select(z => z.Id).ToList(),
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Can't create post, exception occured");
            }
        }

        public void Dispose()
        {
            _wpClient = null;
            _userName = null;
            _password = null;
        }
    }
}

