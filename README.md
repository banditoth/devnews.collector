# Daily .NET news collector and publisher


This repository contains the automation behind the website 'https://www.devnews.banditoth.hu/'.

Using a timed Azure function, it collects the latest news based on RSS feeds from the source listed in the feeds.json file, going back a day, and publishes it to a Wordpress blog.

If you would prefer, or don't want to be included in the blog at all, fork the repository and extract it or add yourself to the feeds.json file. Then open a pull request to this repository and wait for approval
