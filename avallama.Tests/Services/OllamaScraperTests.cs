// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using avallama.Models;
using avallama.Models.Ollama;
using avallama.Services.Ollama;
using Moq;
using Moq.Protected;
using Xunit;

namespace avallama.Tests.Services;

public class OllamaScraperTests
{
    // Used families:
    // gpt-oss
    // qwen3-vl

    // Used models:
    // gpt-oss:latest
    // gpt-oss:20b
    // gpt-oss:120b
    // qwen3-vl:latest
    // qwen3-vl:235b-cloud
    // qwen3-vl:235b-instruct-cloud

    private const string OllamaUrl = "https://www.ollama.com";
    private const string LibraryHtmlPath = "ollama_library_part.html";
    private const string GptOssTagsHtmlPath = "ollama_gpt_oss_tags_part.html";
    private const string Qwen3VlTagsHtmlPath = "ollama_qwen3_vl_tags_part.html";

    private readonly string _libraryHtml;
    private readonly string _gptOssTagsHtml;
    private readonly string _qwen3VlTagsHtml;

    private readonly OllamaScraper _scraper;

    public OllamaScraperTests()
    {
        _libraryHtml = GetHtmlContent(LibraryHtmlPath);
        _gptOssTagsHtml = GetHtmlContent(GptOssTagsHtmlPath);
        _qwen3VlTagsHtml = GetHtmlContent(Qwen3VlTagsHtmlPath);

        var handlerMock = new Mock<HttpMessageHandler>();

        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .Returns<HttpRequestMessage, CancellationToken>((request, cancellationToken) =>
            {
                var path = request.RequestUri!.AbsolutePath;

                if (path.EndsWith("/library"))
                {
                    return Task.FromResult(new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent(_libraryHtml)
                    });
                }

                if (path.Contains("/tags"))
                {
                    if (path.Contains("gpt-oss"))
                    {
                        return Task.FromResult(new HttpResponseMessage
                        {
                            StatusCode = HttpStatusCode.OK,
                            Content = new StringContent(_gptOssTagsHtml)
                        });
                    }

                    if (path.Contains("qwen3-vl"))
                    {
                        return Task.FromResult(new HttpResponseMessage
                        {
                            StatusCode = HttpStatusCode.OK,
                            Content = new StringContent(_qwen3VlTagsHtml)
                        });
                    }
                }

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
            });

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri(OllamaUrl)
        };

        _scraper = new OllamaScraper(httpClient);
    }

    private string GetHtmlContent(string fileName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourcePath = assembly.GetManifestResourceNames()
            .FirstOrDefault(str => str.EndsWith(fileName));

        if (resourcePath == null)
            throw new FileNotFoundException($"File not found: {fileName}");

        using var stream = assembly.GetManifestResourceStream(resourcePath);
        using var reader = new StreamReader(stream!);
        return reader.ReadToEnd();
    }

    private HttpClient CreateMockHttpClient(
        Func<string, HttpResponseMessage> responseFactory)
    {
        var handlerMock = new Mock<HttpMessageHandler>();

        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .Returns<HttpRequestMessage, CancellationToken>((req, _) =>
            {
                var path = req.RequestUri!.AbsolutePath;
                return Task.FromResult(responseFactory(path));
            });

        return new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri(OllamaUrl)
        };
    }

    [Fact]
    public async Task GetAllOllamaModelsAsync_ShouldParseAllFamiliesAndModelsWithValidData()
    {
        var result = await _scraper.GetAllOllamaModelsAsync(CancellationToken.None);

        var modelsList = new List<OllamaModel>();
        await foreach (var model in result.Models)
        {
            modelsList.Add(model);
        }

        Assert.Equal(2, result.Families.Count);

        Assert.All(result.Families, family =>
        {
            Assert.False(string.IsNullOrWhiteSpace(family.Name), "Family name should not be empty.");
            Assert.False(string.IsNullOrWhiteSpace(family.Description),
                $"Description should not be empty for {family.Name}.");

            Assert.True(family.TagCount >= 0, $"TagCount must be non-negative for {family.Name}.");
            Assert.True(family.PullCount >= 0, $"PullCount must be non-negative for {family.Name}.");

            Assert.NotNull(family.Labels);
        });

        Assert.Equal(6, modelsList.Count);

        Assert.All(modelsList, model =>
        {
            Assert.False(string.IsNullOrWhiteSpace(model.Name), "Model name should not be empty.");
            Assert.NotNull(model.Family);

            if (model.Name.Contains("cloud"))
            {
                Assert.Equal(0, model.Size);
            }
            else
            {
                Assert.True(model.Size > 0, $"Size should be greater than 0 for model {model.Name}.");
            }
        });

        Assert.Contains(result.Families, f => f.Name == "gpt-oss");
        Assert.Contains(result.Families, f => f.Name == "qwen3-vl");

        Assert.Contains(modelsList, m => m.Name == "gpt-oss:latest");
        Assert.Contains(modelsList, m => m.Name == "gpt-oss:20b");
        Assert.Contains(modelsList, m => m.Name == "gpt-oss:120b");
        Assert.Contains(modelsList, m => m.Name == "qwen3-vl:latest");
        Assert.Contains(modelsList, m => m.Name == "qwen3-vl:235b-cloud");
        Assert.Contains(modelsList, m => m.Name == "qwen3-vl:235b-instruct-cloud");
    }

    [Fact]
    public async Task GetAllOllamaModelsAsync_WhenLibraryReturnsError_ShouldReturnEmptyResult()
    {
        var httpClient = CreateMockHttpClient(path =>
        {
            if (path.EndsWith("/library"))
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);

            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var scraper = new OllamaScraper(httpClient);

        var result = await scraper.GetAllOllamaModelsAsync(CancellationToken.None);

        var models = new List<OllamaModel>();
        await foreach (var m in result.Models) models.Add(m);

        Assert.Empty(result.Families);
        Assert.Empty(models);
    }

    [Fact]
    public async Task GetAllOllamaModelsAsync_WhenOneFamilyTagsFail_ShouldStillLoadOthers()
    {
        var httpClient = CreateMockHttpClient(path =>
        {
            if (path.EndsWith("/library"))
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(_libraryHtml) };

            if (path.Contains("/gpt-oss/tags"))
                return new HttpResponseMessage(HttpStatusCode.NotFound);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_qwen3VlTagsHtml)
            };
        });

        var scraper = new OllamaScraper(httpClient);

        var result = await scraper.GetAllOllamaModelsAsync(CancellationToken.None);

        var models = new List<OllamaModel>();
        await foreach (var m in result.Models) models.Add(m);

        Assert.Equal(2, result.Families.Count);
        Assert.Equal(3, models.Count);

        Assert.Contains(models, m => m.Name == "qwen3-vl:latest");
        Assert.Contains(models, m => m.Name == "qwen3-vl:235b-cloud");
        Assert.Contains(models, m => m.Name == "qwen3-vl:235b-instruct-cloud");
    }

    [Fact]
    public async Task GetAllOllamaModelsAsync_WhenCancelled_ShouldThrowOperationCanceledException()
    {
        var httpClient = CreateMockHttpClient(path =>
        {
            Thread.Sleep(500);
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(_libraryHtml) };
        });

        var scraper = new OllamaScraper(httpClient);
        var cts = new CancellationTokenSource();

        cts.CancelAfter(100);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            var result = await scraper.GetAllOllamaModelsAsync(cts.Token);
            await foreach (var model in result.Models.WithCancellation(cts.Token))
            {
                // it won't reach this
            }
        });
    }

    [Fact]
    public async Task GetAllOllamaModelsAsync_WhenNoNodesFound_ShouldReturnEmptyLists()
    {
        const string emptyHtml = "<html><body><div id='repo'><ul></ul></div></body></html>";

        var httpClient = CreateMockHttpClient(_ =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(emptyHtml) });

        var scraper = new OllamaScraper(httpClient);

        var result = await scraper.GetAllOllamaModelsAsync(CancellationToken.None);
        var models = new List<OllamaModel>();
        await foreach (var m in result.Models) models.Add(m);

        Assert.Empty(result.Families);
        Assert.Empty(models);
    }
}
