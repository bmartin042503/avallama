// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using avallama.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using avallama.Services;
using HtmlAgilityPack;

namespace avallama.Utilities;

public class LibraryScraper(HttpClient httpClient)
{
    const string OllamaUrl = "https://www.ollama.com";

    public async Task<List<OllamaModel>> GetAllOllamaModelsAsync()
    {
        var families = await GetOllamaFamiliesAsync();

        // max families to process at once
        var throttler = new SemaphoreSlim(10);

        var tasks = families.Select(async family =>
        {
            await throttler.WaitAsync();
            try
            {
                return await GetOllamaModelsFromFamilyAsync(family);
            }
            finally
            {
                throttler.Release();
            }
        }).ToList();

        var result = await Task.WhenAll(tasks);
        return result.SelectMany(list => list).ToList();
    }

    public async Task<List<OllamaModelFamily>> GetOllamaFamiliesAsync()
    {
        HttpResponseMessage response;
        try
        {
            response = await httpClient.GetAsync(OllamaUrl + "/library");
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

        var result = new List<OllamaModelFamily>();

        var nodeSelector = doc.DocumentNode.SelectNodes("//*[@id='repo']/ul/li/a");

        foreach (var aNode in nodeSelector)
        {
            try
            {
                var family = new OllamaModelFamily();

                var nameNode = aNode.SelectSingleNode("div/h2/div/span");
                family.Name = nameNode.InnerText.Trim();

                var descNode = aNode.SelectSingleNode("div/p");
                family.Description = descNode.InnerText.Trim();

                // Pull count: “div:nth-of-type(2) > p > span:first-of-type > span:first-of-type”
                var secondDiv = aNode.SelectSingleNode("div[2]");
                var pInSecondDiv = secondDiv.SelectSingleNode("p");
                var span1 = pInSecondDiv.SelectSingleNode("span");
                var span1Inner = span1.SelectSingleNode("span");
                var pullText = span1Inner.InnerText.Trim();

                family.PullCount = ParsePullCount(pullText);

                var spanTagCount = pInSecondDiv.SelectSingleNode("span[2]/span");
                if (int.TryParse(spanTagCount.InnerText.Trim(), out var tagCount))
                    family.TagCount = tagCount;


                // Last updated: third span etc.
                var lastUpdatedNode = pInSecondDiv.SelectSingleNode("span[3]/span[2]");
                family.LastUpdated = lastUpdatedNode.InnerText.Trim();

                // Popular tags: list of span under div > div > span
                // e.g., tags are shown in some nested span set
                var labelSpanNodes = aNode.SelectNodes("div/div/span");

                var labels = new List<string>();
                foreach (var labelSpan in labelSpanNodes)
                {
                    var t = labelSpan.InnerText.Trim();
                    if (!string.IsNullOrEmpty(t))
                        labels.Add(t);
                }

                family.Labels = labels;
                result.Add(family);
            }
            catch (Exception)
            {
                // skip and move on
            }
        }

        return result;
    }

    private async Task<List<OllamaModel>> GetOllamaModelsFromFamilyAsync(OllamaModelFamily family)
    {
        var result = new List<OllamaModel>();
        var familyUrl = $"{OllamaUrl}/library/{family.Name}/tags";
        try
        {
            var response = await httpClient.GetAsync(familyUrl);
            if (!response.IsSuccessStatusCode) return result;

            var html = await response.Content.ReadAsStringAsync();
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var tagNodes = doc.DocumentNode.SelectNodes("body/div/section/div//div");

            // first one is skipped, because they're column names
            for (var i = 1; i < tagNodes.Count; i++)
            {
                var tagName = tagNodes[i].SelectSingleNode("a/div/div/div/span").InnerText.Trim();

                var modelUrl = $"{OllamaUrl}/library/{tagName}";

                var tagSize = tagNodes[i].SelectSingleNode("div/div[1]/p[1]").InnerText.Trim();
                var tagContext = tagNodes[i].SelectSingleNode("div/div[1]/p[2]").InnerText.Trim();
                var infoDict = new Dictionary<string, string>();

                infoDict.Add(
                    LocalizationService.GetString("CONTEXT_LENGTH"),
                    tagContext
                );

                result.Add(
                    new OllamaModel
                    {
                        Name = tagName,
                        Info = infoDict
                    }
                );
            }

            // every second item contains just the name
            for (var i = 1; i <= tagNodes.Count; i+=2)
            {
                result.Add(
                    new OllamaModel
                    {
                        Name = tagNodes[i].InnerText.Trim(),
                        Family = family
                    }
                );
            }
        }
        catch
        {
            // skip
        }
        return result;
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
