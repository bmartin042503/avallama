// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using avallama.Models.Ollama;
using avallama.Utilities;
using HtmlAgilityPack;

namespace avallama.Services.Ollama;

public sealed class OllamaScraperResult
{
    public required IAsyncEnumerable<OllamaModel> Models { get; init; }
    public required IList<OllamaModelFamily> Families { get; init; }
}

public interface IOllamaScraperService
{
    Task<OllamaScraperResult> GetAllOllamaModelsAsync(CancellationToken cancellationToken);
}

public class OllamaScraperService : IOllamaScraperService
{
    private readonly HttpClient _httpClient;

    public OllamaScraperService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<OllamaScraperResult> GetAllOllamaModelsAsync(
        CancellationToken cancellationToken)
    {
        var families = await GetOllamaFamiliesAsync(cancellationToken);

        var channel = Channel.CreateBounded<OllamaModel>(new BoundedChannelOptions(20)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleWriter = false,
            SingleReader = true,
        });

        var producerTask = Task.Run(async () =>
        {
            try
            {
                var parallelOptions = new ParallelOptions
                {
                    CancellationToken = cancellationToken,
                    MaxDegreeOfParallelism = 8
                };

                await Parallel.ForEachAsync(families, parallelOptions, async (family, ct) =>
                {
                    await foreach (var model in GetOllamaModelsFromFamilyAsync(family, ct))
                    {
                        await channel.Writer.WriteAsync(model, ct);
                    }
                });
            }
            catch (OperationCanceledException)
            {
                // TODO: proper logging
            }
            catch (Exception ex)
            {
                channel.Writer.Complete(ex);
                return;
            }

            channel.Writer.Complete();
        }, cancellationToken);

        return new OllamaScraperResult
        {
            Families = families,
            Models = StreamModels()
        };

        async IAsyncEnumerable<OllamaModel> StreamModels()
        {
            await foreach (var model in channel.Reader.ReadAllAsync(cancellationToken))
            {
                yield return model;
            }

            await producerTask;
        }
    }

    private async Task<List<OllamaModelFamily>> GetOllamaFamiliesAsync(CancellationToken ct = default)
    {
        try
        {
            using var response = await _httpClient.GetAsync("/library", ct);

            if (!response.IsSuccessStatusCode) return [];

            var html = await response.Content.ReadAsStringAsync(ct);
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

                    family.PullCount = ConversionHelper.ParseAbbreviatedNumber(pullText);

                    var spanTagCount = pInSecondDiv.SelectSingleNode("span[2]/span");
                    if (int.TryParse(spanTagCount.InnerText.Trim(), out var tagCount))
                        family.TagCount = tagCount;

                    // Last updated: third span etc.
                    var lastUpdatedNode = pInSecondDiv.SelectSingleNode("span[3]");
                    var lastUpdated = lastUpdatedNode.GetAttributeValue("title", string.Empty);
                    family.LastUpdated = ConversionHelper.ParseUtcDate(lastUpdated);

                    // Popular tags: list of span under div > div > span
                    // e.g., tags are shown in some nested span set
                    var labelSpanNodes = aNode.SelectNodes("div/div/span");

                    var labels = labelSpanNodes
                        .Select(labelSpan => labelSpan.InnerText.Trim())
                        .Where(t => !string.IsNullOrEmpty(t))
                        .ToList();

                    family.Labels = labels;
                    result.Add(family);
                }
                catch (Exception)
                {
                    // TODO: proper logging
                }
            }

            return result;
        }
        catch (Exception)
        {
            // TODO: proper logging
        }

        return [];
    }

    private async IAsyncEnumerable<OllamaModel> GetOllamaModelsFromFamilyAsync(
        OllamaModelFamily family,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var familyUrl = $"/library/{family.Name}/tags";
        HtmlNodeCollection? tagNodes = null;

        try
        {
            using var tagsResponse = await _httpClient.GetAsync(familyUrl, ct);

            if (tagsResponse.IsSuccessStatusCode)
            {
                var tagsHtml = await tagsResponse.Content.ReadAsStringAsync(ct);
                var tagsDoc = new HtmlDocument();
                tagsDoc.LoadHtml(tagsHtml);

                // body/div/section/div/div/all divs starting from 2
                tagNodes = tagsDoc.DocumentNode.SelectNodes("//section/div/div/div");
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            // TODO: proper logging
        }

        if (tagNodes is null) yield break;

        // first one is skipped, because they're column names
        for (var i = 1; i < tagNodes.Count; i++)
        {
            if (ct.IsCancellationRequested) yield break;

            // name, size, context (available only from /library/{family_name}/tags) iterating through a list of models (as div elements)
            var tagNameNode = tagNodes[i].SelectSingleNode("a/div/div/div/span");
            var tagName = tagNameNode.InnerText.Trim();

            if (string.IsNullOrEmpty(tagName)) continue;

            var tagSizeNode = tagNodes[i].SelectSingleNode("div/div[1]/p[1]");
            var tagSize = tagSizeNode.InnerText.Trim();

            var model = new OllamaModel
            {
                Name = tagName,
                Size = ConversionHelper.ParseSizeToBytes(tagSize),
                Family = family
            };

            yield return model;
        }
    }
}
