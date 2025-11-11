using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using avallama.Models;
using avallama.Services;
using Microsoft.Data.Sqlite;
using Xunit;

namespace avallama.Tests.Services;

[Collection("Database Tests")]
public class ModelCacheServiceTests : IAsyncDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ModelCacheService _modelCacheService;

    public ModelCacheServiceTests()
    {

        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        InitializeTestSchema(_connection);

        _modelCacheService = new ModelCacheService(_connection);
    }

    private static void InitializeTestSchema(SqliteConnection connection)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """

                          CREATE TABLE IF NOT EXISTS model_families (
                          name TEXT PRIMARY KEY,
                          description TEXT NOT NULL,
                          pull_count INTEGER NOT NULL DEFAULT 0,
                          labels TEXT,
                          tag_count INTEGER NOT NULL DEFAULT 0,
                          last_updated TEXT,
                          cached_at TEXT NOT NULL,
                          UNIQUE(name)
                          );

                          CREATE TABLE IF NOT EXISTS ollama_models (
                          name TEXT PRIMARY KEY,
                          family_name TEXT NOT NULL,
                          parameters REAL,
                          size INTEGER NOT NULL,
                          format TEXT,
                          quantization TEXT,
                          architecture TEXT,
                          block_count INTEGER,
                          context_length INTEGER,
                          embedding_length INTEGER,
                          additional_info TEXT,
                          download_status INTEGER NOT NULL DEFAULT 0,
                          cached_at TEXT NOT NULL,
                          FOREIGN KEY (family_name) REFERENCES model_families(name) ON DELETE CASCADE
                          );
                          """;
        cmd.ExecuteNonQuery();
    }

    [Fact]
    public async Task CacheOllamaModelFamilyAsync_WithNothingInDb_InsertsModelFamilies()
    {
        var modelFamilies = new List<OllamaModelFamily>
        {
            new(
                "model-family-1",
                "Test Model Family 1",
                100,
                new List<string> { "tag1", "tag2" },
                2,
                DateTime.UtcNow
            ),
            new(
                "model-family-2",
                "Test Model Family 2",
                200,
                new List<string> { "tag3", "tag4" },
                2,
                DateTime.UtcNow
            )
        };

        await _modelCacheService.CacheOllamaModelFamilyAsync(modelFamilies);

        await using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM model_families";
        var count = (long)(await cmd.ExecuteScalarAsync() ?? 0);

        Assert.Equal(2, count);
    }

    [Fact]
    public async Task CacheOllamaModelFamilyAsync_WithExistingDb_UpdatesModelFamilies()
    {
        var initialModelFamilies = new List<OllamaModelFamily>
        {
            new(
                "model-family-1",
                "Initial Description",
                100,
                new List<string> { "tag1" },
                1,
                DateTime.UtcNow.AddDays(-1)
            )
        };

        await _modelCacheService.CacheOllamaModelFamilyAsync(initialModelFamilies);

        var updatedModelFamilies = new List<OllamaModelFamily>
        {
            new(
                "model-family-1",
                "Updated Description",
                150,
                new List<string> { "tag1", "tag2" },
                2,
                DateTime.UtcNow
            )
        };

        await _modelCacheService.CacheOllamaModelFamilyAsync(updatedModelFamilies);

        await using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT description, pull_count, labels, tag_count FROM model_families WHERE name = 'model-family-1'";
        await using var reader = await cmd.ExecuteReaderAsync();

        Assert.True(await reader.ReadAsync());
        Assert.Equal("Updated Description", reader.GetString(0));
        Assert.Equal(150, reader.GetInt32(1));
        var labels = reader.GetString(2);
        Assert.Contains("tag1", labels);
        Assert.Contains("tag2", labels);
        Assert.Equal(2, reader.GetInt32(3));
    }

    [Fact]
    public async Task CacheOllamaModelFamilyAsync_WithDelete_RemovesModelFamilies()
    {
        var initialModelFamilies = new List<OllamaModelFamily>
        {
            new(
                "model-family-1",
                "Test Model Family 1",
                100,
                new List<string> { "tag1", "tag2" },
                2,
                DateTime.UtcNow
            ),
            new(
                "model-family-2",
                "Test Model Family 2",
                200,
                new List<string> { "tag3", "tag4" },
                2,
                DateTime.UtcNow
            )
        };
        await _modelCacheService.CacheOllamaModelFamilyAsync(initialModelFamilies);

        var updatedModelFamilies = new List<OllamaModelFamily>
        {
            new(
                "model-family-1",
                "Test Model Family 1",
                100,
                new List<string> { "tag1", "tag2" },
                2,
                DateTime.UtcNow
            )
        };
        await _modelCacheService.CacheOllamaModelFamilyAsync(updatedModelFamilies);
        await using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM model_families";
        var count = (long)(await cmd.ExecuteScalarAsync() ?? 0);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task CacheOllamaModelAsync_WithNothingInDb_InsertsModels()
    {
        var modelFamily = new OllamaModelFamily(
            "model-1",
            "Test Model Family 1",
            100,
            new List<string> { "tag1", "tag2" },
            2,
            DateTime.UtcNow
        );
        await _modelCacheService.CacheOllamaModelFamilyAsync(new List<OllamaModelFamily> { modelFamily });

        var models = new List<OllamaModel>
        {
            new(
                "model-1:7b",
                4096,
                1.01,
                "Test Format",
                new Dictionary<string, string>(),
                2048,
                ModelDownloadStatus.Downloaded,
                false
            )
        };

        await _modelCacheService.CacheOllamaModelAsync(models);

        await using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM ollama_models";
        var count = (long)(await cmd.ExecuteScalarAsync() ?? 0);

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task CacheOllamaModelAsync_WithExistingDb_UpdatesModels()
    {
        var modelFamily = new OllamaModelFamily(
            "model-1",
            "Test Model Family 1",
            100,
            new List<string> { "tag1", "tag2" },
            2,
            DateTime.UtcNow
        );
        await _modelCacheService.CacheOllamaModelFamilyAsync(new List<OllamaModelFamily> { modelFamily });

        var initialModel = new List<OllamaModel>
        {
            new(
                "model-1:7b",
                4096,
                1.01,
                "Test Format",
                new Dictionary<string, string>(),
                2048,
                ModelDownloadStatus.Downloading,
                false
            )
        };
        await _modelCacheService.CacheOllamaModelAsync(initialModel);
        var updatedModel = new List<OllamaModel>
        {
            new(
                "model-1:7b",
                8192,
                1.02,
                "Updated Format",
                new Dictionary<string, string>(),
                4096,
                ModelDownloadStatus.Downloaded,
                true
            )
        };
        await _modelCacheService.CacheOllamaModelAsync(updatedModel);
        await using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT quantization, parameters, format, download_status FROM ollama_models WHERE name = 'model-1:7b'";
        await using var reader = await cmd.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        Assert.Equal(8192, reader.GetInt32(0));
        Assert.Equal(1.02, reader.GetDouble(1));
        Assert.Equal("Updated Format", reader.GetString(2));
        Assert.Equal((int)ModelDownloadStatus.Downloaded, reader.GetInt32(3));
    }

    [Fact]
    public async Task CacheOllamaModelAsync_WithDelete_RemovesModels()
    {
        var modelFamily = new OllamaModelFamily(
            "model-1",
            "Test Model Family 1",
            100,
            new List<string> { "tag1", "tag2" },
            2,
            DateTime.UtcNow
        );
        await _modelCacheService.CacheOllamaModelFamilyAsync(new List<OllamaModelFamily> { modelFamily });
        var initialModels = new List<OllamaModel>
        {
            new(
                "model-1:7b",
                4096,
                1.01,
                "Test Format",
                new Dictionary<string, string>(),
                2048,
                ModelDownloadStatus.Downloaded,
                false
            ),
            new(
                "model-1:13b",
                8192,
                1.02,
                "Test Format",
                new Dictionary<string, string>(),
                4096,
                ModelDownloadStatus.Downloaded,
                false
            )
        };
        await _modelCacheService.CacheOllamaModelAsync(initialModels);
        var updatedModels = new List<OllamaModel>
        {
            new(
                "model-1:7b",
                4096,
                1.01,
                "Test Format",
                new Dictionary<string, string>(),
                2048,
                ModelDownloadStatus.Downloaded,
                false
            )
        };
        await _modelCacheService.CacheOllamaModelAsync(updatedModels);
        await using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM ollama_models";
        var count = (long)(await cmd.ExecuteScalarAsync() ?? 0);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task UpdateOllamaModel_UpdatesModel()
    {
        var modelFamily = new OllamaModelFamily(
            "model-1",
            "Test Model Family 1",
            100,
            new List<string> { "tag1", "tag2" },
            2,
            DateTime.UtcNow
        );
        await _modelCacheService.CacheOllamaModelFamilyAsync(new List<OllamaModelFamily> { modelFamily });

        var initialModel = new List<OllamaModel>
        {
            new(
                "model-1:7b",
                4096,
                1.01,
                "Test Format",
                new Dictionary<string, string>(),
                2048,
                ModelDownloadStatus.Downloading,
                false
            )
        };
        await _modelCacheService.CacheOllamaModelAsync(initialModel);

        var details = new Dictionary<string, string>
        {
            { "architecture", "llama" },
            { "context_length", "8192" },
            { "block_count", "32" },
            { "embedding_length", "4096" }
        };

        var modelToUpdate = new OllamaModel
        {
            Name = "model-1:7b",
            Quantization = 4096,
            Parameters = 1.01,
            Format = "Test Format",
            Details = details,
            Size = 2048,
            DownloadStatus = ModelDownloadStatus.Downloaded,
            DownloadProgress = 1.0,
            RunsSlow = false
        };

        await _modelCacheService.UpdateOllamaModelAsync(modelToUpdate);

        await using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT architecture, context_length, block_count, embedding_length, download_status FROM ollama_models WHERE name = 'model-1:7b'";
        await using var reader = await cmd.ExecuteReaderAsync();

        Assert.True(await reader.ReadAsync());
        Assert.Equal("llama", reader.GetString(0));
        Assert.Equal(8192, reader.GetInt32(1));
        Assert.Equal(32, reader.GetInt32(2));
        Assert.Equal(4096, reader.GetInt32(3));
        Assert.Equal((int)ModelDownloadStatus.Downloaded, reader.GetInt32(4));
    }

    [Fact]
    public async Task GetCachedOllamaModelFamiliesAsync_ReturnsCachedFamilies()
    {
        var modelFamilies = new List<OllamaModelFamily>
        {
            new(
                "model-family-1",
                "Test Model Family 1",
                100,
                new List<string> { "tag1", "tag2" },
                2,
                DateTime.UtcNow
            ),
            new(
                "model-family-2",
                "Test Model Family 2",
                200,
                new List<string> { "tag3", "tag4" },
                2,
                DateTime.UtcNow
            )
        };

        await _modelCacheService.CacheOllamaModelFamilyAsync(modelFamilies);

        var cachedFamilies = await _modelCacheService.GetCachedOllamaModelFamiliesAsync();

        Assert.Equal(2, cachedFamilies.Count);
        Assert.Contains(cachedFamilies, mf => mf.Name == "model-family-1");
        Assert.Contains(cachedFamilies, mf => mf.Name == "model-family-2");
    }

    [Fact]
    public async Task GetCachedOllamaModelsAsync_ReturnsCachedModels()
    {
        var modelFamily = new OllamaModelFamily(
            "model-1",
            "Test Model Family 1",
            100,
            new List<string> { "tag1", "tag2" },
            2,
            DateTime.UtcNow
        );
        await _modelCacheService.CacheOllamaModelFamilyAsync(new List<OllamaModelFamily> { modelFamily });

        var models = new List<OllamaModel>
        {
            new(
                "model-1:7b",
                4096,
                1.01,
                "Test Format",
                new Dictionary<string, string>(),
                2048,
                ModelDownloadStatus.Downloaded,
                false
            ),
            new(
                "model-1:13b",
                8192,
                1.02,
                "Test Format",
                new Dictionary<string, string>(),
                4096,
                ModelDownloadStatus.Downloaded,
                false
            )
        };

        await _modelCacheService.CacheOllamaModelAsync(models);

        var cachedModels = await _modelCacheService.GetCachedOllamaModelsAsync();

        Assert.Equal(2, cachedModels.Count);
        Assert.Contains(cachedModels, m => m.Name == "model-1:7b");
        Assert.Contains(cachedModels, m => m.Name == "model-1:13b");
    }

    // Integration tests, ModelCacheService + DatabaseLock

[Fact]
    public async Task ConcurrentReads_DoNotBlock()
    {
        var modelFamilies = new List<OllamaModelFamily>
        {
            new("model-family-1", "Test", 100, new List<string> { "tag1" }, 1, DateTime.UtcNow)
        };
        await _modelCacheService.CacheOllamaModelFamilyAsync(modelFamilies);

        var tasks = Enumerable.Range(0, 50)
            .Select(_ => Task.Run(async () => await _modelCacheService.GetCachedOllamaModelFamiliesAsync()))
            .ToArray();

        await Task.WhenAll(tasks);

        Assert.All(tasks, t => Assert.True(t.IsCompletedSuccessfully));
        Assert.All(tasks, t => Assert.Single((IEnumerable)t.Result));
    }

    [Fact]
    public async Task MixedReadWriteOperations_AreThreadSafe()
    {
        var initialFamily = new OllamaModelFamily("model-1", "Initial", 100,
            new List<string> { "tag1" }, 1, DateTime.UtcNow);
        await _modelCacheService.CacheOllamaModelFamilyAsync(new List<OllamaModelFamily> { initialFamily });

        var readTasks = Enumerable.Range(0, 30)
            .Select(_ => Task.Run(async () => await _modelCacheService.GetCachedOllamaModelFamiliesAsync()));

        var writeTasks = Enumerable.Range(0, 20)
            .Select(i => Task.Run(async () =>
            {
                var updated = new OllamaModelFamily("model-1", $"Updated {i}", 100 + i,
                    new List<string> { "tag1" }, 1, DateTime.UtcNow);
                await _modelCacheService.CacheOllamaModelFamilyAsync(new List<OllamaModelFamily> { updated });
            }));

        await Task.WhenAll(readTasks.Concat(writeTasks));

        var final = await _modelCacheService.GetCachedOllamaModelFamiliesAsync();
        Assert.Single(final);
    }

    [Fact]
    public async Task HighVolumeUpdates_MaintainConsistency()
    {
        var modelFamily = new OllamaModelFamily("model-1", "Test", 100,
            new List<string> { "tag1" }, 1, DateTime.UtcNow);
        await _modelCacheService.CacheOllamaModelFamilyAsync(new List<OllamaModelFamily> { modelFamily });

        var model = new OllamaModel("model-1:7b", 4096, 1.0, "gguf",
            new Dictionary<string, string>(), 2048, ModelDownloadStatus.Downloaded, false);
        await _modelCacheService.CacheOllamaModelAsync(new List<OllamaModel> { model });

        var tasks = Enumerable.Range(0, 100)
            .Select(i => Task.Run(async () =>
            {
                var updated = new OllamaModel
                {
                    Name = "model-1:7b",
                    Quantization = 4096 + i,
                    Parameters = 1.0 + i,
                    Format = "gguf",
                    Details = new Dictionary<string, string> { ["iteration"] = i.ToString() },
                    Size = 2048 + i,
                    DownloadStatus = ModelDownloadStatus.Downloaded,
                    DownloadProgress = 1.0,
                    RunsSlow = false
                };
                await _modelCacheService.UpdateOllamaModelAsync(updated);
            }))
            .ToArray();

        await Task.WhenAll(tasks);

        var cached = await _modelCacheService.GetCachedOllamaModelsAsync();
        Assert.Single(cached);
        Assert.Equal("model-1:7b", cached[0].Name);
    }

    [Fact]
    public async Task ConcurrentReadsDuringBulkWrite_Succeed()
    {
        var modelFamilies = Enumerable.Range(0, 100)
            .Select(i => new OllamaModelFamily($"family-{i}", $"Desc {i}", i,
                new List<string> { $"tag{i}" }, 1, DateTime.UtcNow))
            .ToList();

        var writeTask = Task.Run(async () =>
            await _modelCacheService.CacheOllamaModelFamilyAsync(modelFamilies));

        var readTasks = Enumerable.Range(0, 50)
            .Select(_ => Task.Run(async () => await _modelCacheService.GetCachedOllamaModelFamiliesAsync()))
            .ToArray();

        await Task.WhenAll(readTasks.Append(writeTask));

        var final = await _modelCacheService.GetCachedOllamaModelFamiliesAsync();
        Assert.Equal(100, final.Count);
    }


    public async ValueTask DisposeAsync()
    {
        await Task.Delay(100);

        await _connection.CloseAsync();
        await _connection.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
