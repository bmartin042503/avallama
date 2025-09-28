// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using avallama.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace avallama.Utilities;

public class LibraryScraper(HttpClient httpClient)
{
    public async Task<List<LibraryModel>> ListModelsFromLibraryAsync()
    {
        const string url = "https://ollama.com/library";
        HttpResponseMessage response;
        try
        {
            response = await httpClient.GetAsync(url);
        }
        catch (HttpRequestException)
        {
            return [];
        }

        if (!response.IsSuccessStatusCode)
        {
            return [];
        }

        var html = await response.Content.ReadAsStringAsync();
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var results = new List<LibraryModel>();

        var nodeSelector = doc.DocumentNode.SelectNodes("//*[@id='repo']/ul/li/a");

        foreach (var aNode in nodeSelector)
        {
            try
            {
                var info = new LibraryModel();

                var nameNode = aNode.SelectSingleNode("div/h2/div/span");
                info.Name = nameNode.InnerText.Trim();

                var descNode = aNode.SelectSingleNode("div/p");
                info.Description = descNode.InnerText.Trim();

                // Pull count: “div:nth-of-type(2) > p > span:first-of-type > span:first-of-type”
                var secondDiv = aNode.SelectSingleNode("div[2]");
                var pInSecondDiv = secondDiv.SelectSingleNode("p");
                var span1 = pInSecondDiv.SelectSingleNode("span");
                var span1Inner = span1.SelectSingleNode("span");
                var pullText = span1Inner.InnerText.Trim();

                info.PullCount = ParsePullCount(pullText);

                var spanTagCount = pInSecondDiv.SelectSingleNode("span[2]/span");
                if (int.TryParse(spanTagCount.InnerText.Trim(), out var tagCount))
                    info.TagCount = tagCount;


                // Last updated: third span etc.
                var lastUpdatedNode = pInSecondDiv.SelectSingleNode("span[3]/span[2]");
                info.LastUpdated = lastUpdatedNode.InnerText.Trim();

                // Popular tags: list of span under div > div > span
                // e.g., tags are shown in some nested span set
                var tagSpanNodes = aNode.SelectNodes("div/div/span");

                var tags = new List<string>();
                foreach (var tagSpan in tagSpanNodes)
                {
                    var t = tagSpan.InnerText.Trim();
                    if (!string.IsNullOrEmpty(t))
                        tags.Add(t);
                }

                info.PopularTags = tags;


                results.Add(info);
            }
            catch (Exception)
            {
                // skip and move on
            }
        }

        return results;
    }

    private long ParsePullCount(string s)
    {
        // e.g., "1.5M", "120K", "100", "2.3B" etc.
        s = s.Trim().ToUpperInvariant();
        double multiplier = 1;
        if (s.EndsWith('B'))
        {
            multiplier = 1_000_000_000;
            s = s[..^1];
        }
        else if (s.EndsWith('M'))
        {
            multiplier = 1_000_000;
            s = s[..^1];
        }
        else if (s.EndsWith('K'))
        {
            multiplier = 1_000;
            s = s[..^1];
        }

        if (double.TryParse(s, System.Globalization.NumberStyles.AllowDecimalPoint,
                System.Globalization.CultureInfo.InvariantCulture, out double value))
        {
            return (long)(value * multiplier);
        }

        return 0;
    }
}