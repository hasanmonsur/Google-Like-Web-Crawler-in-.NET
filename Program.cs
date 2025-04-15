using HtmlAgilityPack;
using System.Collections.Concurrent;

class Program
{
    static readonly HttpClient httpClient = new(new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    });

    static readonly ConcurrentQueue<string> urlQueue = new();
    static readonly HashSet<string> visitedUrls = new();
    static readonly object consoleLock = new();

    static async Task Main(string[] args)
    {
        string startUrl = "https://www.neitsbd.com";
        urlQueue.Enqueue(startUrl);

        while (urlQueue.TryDequeue(out var currentUrl))
        {
            if (visitedUrls.Contains(currentUrl)) continue;

            try
            {
                await CrawlAsync(currentUrl);
                visitedUrls.Add(currentUrl);
            }
            catch (Exception ex)
            {
                lock (consoleLock)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Error crawling {currentUrl}: {ex.Message}");
                    Console.ResetColor();
                }
            }
        }
    }

    static async Task CrawlAsync(string url)
    {
        var html = await httpClient.GetStringAsync(url);
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var links = doc.DocumentNode.SelectNodes("//a[@href]")
            ?.Select(node => node.GetAttributeValue("href", ""))
            .Where(href => href.StartsWith("http"))
            .Distinct();

        lock (consoleLock)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Crawled: {url}");
            Console.ResetColor();
        }

        if (links != null)
        {
            foreach (var link in links)
            {
                if (!visitedUrls.Contains(link))
                {
                    urlQueue.Enqueue(link);
                }
            }
        }
    }
}