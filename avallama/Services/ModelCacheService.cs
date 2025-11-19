// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using avallama.Models;
using avallama.Utilities;
using Microsoft.Data.Sqlite;

namespace avallama.Services;

public interface IModelCacheService
{
    public Task CacheModelFamilyAsync(IList<OllamaModelFamily> modelFamilies);
    public Task CacheModelsAsync(IList<OllamaModel> models);
    public Task UpdateModelAsync(OllamaModel model);
    public Task<IList<OllamaModel>> GetCachedModelsAsync();
    public Task<IList<OllamaModelFamily>> GetCachedModelFamiliesAsync();
}

public class ModelCacheService : IModelCacheService
{
    private readonly SqliteConnection _connection;

    public ModelCacheService(SqliteConnection? testConnection = null)
    {
        if (testConnection is not null)
        {
            _connection = testConnection;
            _connection.Open();
            return;
        }

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appDir = Path.Combine(appData, "avallama");
        if (!Directory.Exists(appDir))
            Directory.CreateDirectory(appDir);

        var dbPath = Path.Combine(appDir, "avallama.db");
        var connectionString = $"Data Source={dbPath}";
        _connection = new SqliteConnection(connectionString);
        _connection.Open();
    }

    public async Task CacheModelFamilyAsync(IList<OllamaModelFamily> modelFamilies)
    {
        var now = DateTime.Now;

        using (DatabaseLock.Instance.AcquireWriteLock())
        {
            await using var transaction = _connection.BeginTransaction();

            await using var cmd = _connection.CreateCommand();
            cmd.CommandText =
                "SELECT name, description, pull_count, labels, tag_count, last_updated FROM model_families";
            await using var reader = await cmd.ExecuteReaderAsync();
            var existingModelFamilies = new Dictionary<string, OllamaModelFamily>();
            while (await reader.ReadAsync())
            {
                var name = reader.GetString(0);
                var description = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
                var pullCount = reader.GetInt32(2);
                var labels = reader.IsDBNull(3)
                    ? []
                    : JsonSerializer.Deserialize<List<string>>(reader.GetString(3)) ?? [];
                var tagCount = reader.GetInt32(4);
                var lastUpdated = reader.IsDBNull(5) ? DateTime.MinValue : reader.GetDateTime(5);

                var modelFamily = new OllamaModelFamily
                {
                    Name = name,
                    Description = description,
                    PullCount = pullCount,
                    Labels = labels,
                    TagCount = tagCount,
                    LastUpdated = lastUpdated
                };

                existingModelFamilies[name] = modelFamily;
            }

            reader.Close();

            var newFamilies = modelFamilies.ToDictionary(f => f.Name);

            var toInsert = modelFamilies.Where(f => !existingModelFamilies.ContainsKey(f.Name));
            var toDelete = existingModelFamilies.Values.Where(f => !newFamilies.ContainsKey(f.Name));
            var toUpdate = modelFamilies
                .Where(f => existingModelFamilies.ContainsKey(f.Name))
                .Where(f => HasModelFamilyChanged(f, existingModelFamilies[f.Name]));

            var insertList = toInsert.ToList();
            if (insertList.Count != 0)
            {
                await using var insertCmd = _connection.CreateCommand();
                var values = string.Join(", ", insertList.Select((_, i) =>
                    $"(@name{i}, @description{i}, @pullCount{i}, @labels{i}, @tagCount{i}, @lastUpdated{i}, @cachedAt{i})"));

                insertCmd.CommandText = $"""
                                                 INSERT INTO model_families (name, description, pull_count, labels, tag_count, last_updated, cached_at)
                                                 VALUES {values}
                                         """;

                for (var i = 0; i < insertList.Count; i++)
                {
                    var family = insertList[i];
                    insertCmd.Parameters.AddWithValue($"@name{i}", family.Name);
                    insertCmd.Parameters.AddWithValue($"@description{i}", family.Description);
                    insertCmd.Parameters.AddWithValue($"@pullCount{i}", family.PullCount);
                    insertCmd.Parameters.AddWithValue($"@labels{i}", JsonSerializer.Serialize(family.Labels));
                    insertCmd.Parameters.AddWithValue($"@tagCount{i}", family.TagCount);
                    insertCmd.Parameters.AddWithValue($"@lastUpdated{i}", family.LastUpdated);
                    insertCmd.Parameters.AddWithValue($"@cachedAt{i}", now);
                }

                await insertCmd.ExecuteNonQueryAsync();
            }

            var updateList = toUpdate.ToList();
            if (updateList.Count != 0)
            {
                await using var updateCmd = _connection.CreateCommand();
                var nameParams = string.Join(", ", updateList.Select((_, i) => $"@name{i}"));

                updateCmd.CommandText = $"""
                                                 UPDATE model_families
                                                 SET description = CASE name {string.Join(" ", updateList.Select((_, i) => $"WHEN @name{i} THEN @description{i}"))} END,
                                                     pull_count = CASE name {string.Join(" ", updateList.Select((_, i) => $"WHEN @name{i} THEN @pullCount{i}"))} END,
                                                     labels = CASE name {string.Join(" ", updateList.Select((_, i) => $"WHEN @name{i} THEN @labels{i}"))} END,
                                                     tag_count = CASE name {string.Join(" ", updateList.Select((_, i) => $"WHEN @name{i} THEN @tagCount{i}"))} END,
                                                     last_updated = CASE name {string.Join(" ", updateList.Select((_, i) => $"WHEN @name{i} THEN @lastUpdated{i}"))} END,
                                                     cached_at = @cachedAt
                                                 WHERE name IN ({nameParams})
                                         """;

                for (var i = 0; i < updateList.Count; i++)
                {
                    var family = updateList[i];
                    updateCmd.Parameters.AddWithValue($"@name{i}", family.Name);
                    updateCmd.Parameters.AddWithValue($"@description{i}", family.Description);
                    updateCmd.Parameters.AddWithValue($"@pullCount{i}", family.PullCount);
                    updateCmd.Parameters.AddWithValue($"@labels{i}", JsonSerializer.Serialize(family.Labels));
                    updateCmd.Parameters.AddWithValue($"@tagCount{i}", family.TagCount);
                    updateCmd.Parameters.AddWithValue($"@lastUpdated{i}", family.LastUpdated);
                }

                updateCmd.Parameters.AddWithValue("@cachedAt", now);

                await updateCmd.ExecuteNonQueryAsync();
            }

            var deleteList = toDelete.ToList();
            if (deleteList.Count != 0)
            {
                await using var deleteCmd = _connection.CreateCommand();
                var nameParams = string.Join(", ", deleteList.Select((_, i) => $"@name{i}"));
                deleteCmd.CommandText = $"DELETE FROM model_families WHERE name IN ({nameParams})";

                for (var i = 0; i < deleteList.Count; i++)
                {
                    deleteCmd.Parameters.AddWithValue($"@name{i}", deleteList[i].Name);
                }

                await deleteCmd.ExecuteNonQueryAsync();
            }

            transaction.Commit();
        }
    }

    private static bool HasModelFamilyChanged(OllamaModelFamily newFamily, OllamaModelFamily existingFamily)
    {
        return newFamily.Description != existingFamily.Description ||
               newFamily.PullCount != existingFamily.PullCount ||
               newFamily.TagCount != existingFamily.TagCount ||
               newFamily.LastUpdated != existingFamily.LastUpdated ||
               !newFamily.Labels.SequenceEqual(existingFamily.Labels);
    }

    public async Task<IList<OllamaModelFamily>> GetCachedModelFamiliesAsync()
    {
        var modelFamilies = new List<OllamaModelFamily>();
        using (DatabaseLock.Instance.AcquireReadLock())
        {
            await using var cmd = _connection.CreateCommand();
            cmd.CommandText =
                "SELECT name, description, pull_count, labels, tag_count, last_updated FROM model_families ORDER BY pull_count DESC";
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var name = reader.GetString(0);
                var description = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
                var pullCount = reader.GetInt32(2);
                var labels = reader.IsDBNull(3)
                    ? []
                    : JsonSerializer.Deserialize<List<string>>(reader.GetString(3)) ?? [];
                var tagCount = reader.GetInt32(4);
                var lastUpdated = reader.IsDBNull(5) ? DateTime.MinValue : reader.GetDateTime(5);

                var modelFamily = new OllamaModelFamily
                {
                    Name = name,
                    Description = description,
                    PullCount = pullCount,
                    Labels = labels,
                    TagCount = tagCount,
                    LastUpdated = lastUpdated
                };

                modelFamilies.Add(modelFamily);
            }
        }

        return modelFamilies;
    }

    public async Task CacheModelsAsync(IList<OllamaModel> models)
    {
        var now = DateTime.Now;

        using (DatabaseLock.Instance.AcquireWriteLock())
        {
            await using var transaction = _connection.BeginTransaction();

            await using var cmd = _connection.CreateCommand();
            cmd.CommandText = """
                                      SELECT name, family_name, parameters, size, format, quantization,
                                             architecture, block_count, context_length, embedding_length,
                                             additional_info, download_status
                                      FROM ollama_models
                              """;

            await using var reader = await cmd.ExecuteReaderAsync();
            var existingModels = new Dictionary<string, OllamaModel>();

            while (await reader.ReadAsync())
            {
                var info = new Dictionary<string, string>();

                if (!reader.IsDBNull(4)) info["format"] = reader.GetString(4);
                if (!reader.IsDBNull(5)) info["quantization"] = reader.GetString(5);
                if (!reader.IsDBNull(6)) info["architecture"] = reader.GetString(6);
                if (!reader.IsDBNull(7)) info["block_count"] = reader.GetInt32(7).ToString();
                if (!reader.IsDBNull(8)) info["context_length"] = reader.GetInt32(8).ToString();
                if (!reader.IsDBNull(9)) info["embedding_length"] = reader.GetInt32(9).ToString();

                if (!reader.IsDBNull(10))
                {
                    var additionalInfo = JsonSerializer.Deserialize<Dictionary<string, string>>(reader.GetString(10));
                    if (additionalInfo != null)
                    {
                        foreach (var kvp in additionalInfo)
                        {
                            info[kvp.Key] = kvp.Value;
                        }
                    }
                }

                var model = new OllamaModel
                {
                    Name = reader.GetString(0),
                    Parameters = reader.IsDBNull(2) ? 0 : reader.GetInt64(2),
                    Size = reader.GetInt64(3),
                    Info = info,
                    DownloadStatus = (ModelDownloadStatus)reader.GetInt32(11)
                };

                existingModels[model.Name] = model;
            }

            reader.Close();

            var newModels = models.ToDictionary(m => m.Name);
            var toInsert = models.Where(m => !existingModels.ContainsKey(m.Name));
            var toDelete = existingModels.Values.Where(m => !newModels.ContainsKey(m.Name));
            var toUpdate = models
                .Where(m => existingModels.ContainsKey(m.Name))
                .Where(m => HasModelChanged(m, existingModels[m.Name]));

            var insertList = toInsert.ToList();
            if (insertList.Count != 0)
            {
                await using var insertCmd = _connection.CreateCommand();
                var values = string.Join(", ", insertList.Select((_, i) =>
                    $"(@name{i}, @familyName{i}, @parameters{i}, @size{i}, @format{i}, @quantization{i}, @architecture{i}, @blockCount{i}, @contextLength{i}, @embeddingLength{i}, @additionalInfo{i}, @downloadStatus{i}, @cachedAt{i})"));

                insertCmd.CommandText = $"""
                                         INSERT INTO ollama_models (name, family_name, parameters, size, format, quantization, architecture, block_count, context_length, embedding_length, additional_info, download_status, cached_at)
                                         VALUES {values}
                                         """;

                for (var i = 0; i < insertList.Count; i++)
                {
                    var model = insertList[i];
                    var (format, quantization, architecture, blockCount, contextLength, embeddingLength, additionalInfo
                            ) =
                        ExtractModelInfo(model.Info);

                    insertCmd.Parameters.AddWithValue($"@name{i}", model.Name);
                    insertCmd.Parameters.AddWithValue($"@familyName{i}", ExtractFamilyName(model.Name));
                    insertCmd.Parameters.AddWithValue($"@parameters{i}", model.Parameters);
                    insertCmd.Parameters.AddWithValue($"@size{i}", model.Size);
                    insertCmd.Parameters.AddWithValue($"@format{i}", format ?? (object)DBNull.Value);
                    insertCmd.Parameters.AddWithValue($"@quantization{i}", quantization ?? (object)DBNull.Value);
                    insertCmd.Parameters.AddWithValue($"@architecture{i}", architecture ?? (object)DBNull.Value);
                    insertCmd.Parameters.AddWithValue($"@blockCount{i}", blockCount ?? (object)DBNull.Value);
                    insertCmd.Parameters.AddWithValue($"@contextLength{i}", contextLength ?? (object)DBNull.Value);
                    insertCmd.Parameters.AddWithValue($"@embeddingLength{i}", embeddingLength ?? (object)DBNull.Value);
                    insertCmd.Parameters.AddWithValue($"@additionalInfo{i}", additionalInfo ?? (object)DBNull.Value);
                    insertCmd.Parameters.AddWithValue($"@downloadStatus{i}", (int)model.DownloadStatus);
                    insertCmd.Parameters.AddWithValue($"@cachedAt{i}", now);
                }

                await insertCmd.ExecuteNonQueryAsync();
            }

            var updateList = toUpdate.ToList();
            if (updateList.Count != 0)
            {
                await using var updateCmd = _connection.CreateCommand();
                var nameParams = string.Join(", ", updateList.Select((_, i) => $"@name{i}"));

                updateCmd.CommandText = $"""
                                         UPDATE ollama_models
                                         SET family_name = CASE name {string.Join(" ", updateList.Select((_, i) => $"WHEN @name{i} THEN @familyName{i}"))} END,
                                             parameters = CASE name {string.Join(" ", updateList.Select((_, i) => $"WHEN @name{i} THEN @parameters{i}"))} END,
                                             size = CASE name {string.Join(" ", updateList.Select((_, i) => $"WHEN @name{i} THEN @size{i}"))} END,
                                             format = CASE name {string.Join(" ", updateList.Select((_, i) => $"WHEN @name{i} THEN @format{i}"))} END,
                                             quantization = CASE name {string.Join(" ", updateList.Select((_, i) => $"WHEN @name{i} THEN @quantization{i}"))} END,
                                             architecture = CASE name {string.Join(" ", updateList.Select((_, i) => $"WHEN @name{i} THEN @architecture{i}"))} END,
                                             block_count = CASE name {string.Join(" ", updateList.Select((_, i) => $"WHEN @name{i} THEN @blockCount{i}"))} END,
                                             context_length = CASE name {string.Join(" ", updateList.Select((_, i) => $"WHEN @name{i} THEN @contextLength{i}"))} END,
                                             embedding_length = CASE name {string.Join(" ", updateList.Select((_, i) => $"WHEN @name{i} THEN @embeddingLength{i}"))} END,
                                             additional_info = CASE name {string.Join(" ", updateList.Select((_, i) => $"WHEN @name{i} THEN @additionalInfo{i}"))} END,
                                             download_status = CASE name {string.Join(" ", updateList.Select((_, i) => $"WHEN @name{i} THEN @downloadStatus{i}"))} END,
                                             cached_at = @cachedAt
                                         WHERE name IN ({nameParams})
                                         """;

                for (var i = 0; i < updateList.Count; i++)
                {
                    var model = updateList[i];

                    var (format, quantization, architecture, blockCount, contextLength, embeddingLength, additionalInfo
                            ) =
                        ExtractModelInfo(model.Info);

                    updateCmd.Parameters.AddWithValue($"@name{i}", model.Name);
                    updateCmd.Parameters.AddWithValue($"@familyName{i}", ExtractFamilyName(model.Name));
                    updateCmd.Parameters.AddWithValue($"@parameters{i}", model.Parameters);
                    updateCmd.Parameters.AddWithValue($"@size{i}", model.Size);
                    updateCmd.Parameters.AddWithValue($"@format{i}", format ?? (object)DBNull.Value);
                    updateCmd.Parameters.AddWithValue($"@quantization{i}", quantization ?? (object)DBNull.Value);
                    updateCmd.Parameters.AddWithValue($"@architecture{i}", architecture ?? (object)DBNull.Value);
                    updateCmd.Parameters.AddWithValue($"@blockCount{i}", blockCount ?? (object)DBNull.Value);
                    updateCmd.Parameters.AddWithValue($"@contextLength{i}", contextLength ?? (object)DBNull.Value);
                    updateCmd.Parameters.AddWithValue($"@embeddingLength{i}", embeddingLength ?? (object)DBNull.Value);
                    updateCmd.Parameters.AddWithValue($"@additionalInfo{i}", additionalInfo ?? (object)DBNull.Value);
                    updateCmd.Parameters.AddWithValue($"@downloadStatus{i}", (int)model.DownloadStatus);
                }

                updateCmd.Parameters.AddWithValue("@cachedAt", now);
                await updateCmd.ExecuteNonQueryAsync();
            }

            var deleteList = toDelete.ToList();
            if (deleteList.Count != 0)
            {
                await using var deleteCmd = _connection.CreateCommand();
                var nameParams = string.Join(", ", deleteList.Select((_, i) => $"@name{i}"));
                deleteCmd.CommandText = $"DELETE FROM ollama_models WHERE name IN ({nameParams})";

                for (var i = 0; i < deleteList.Count; i++)
                {
                    deleteCmd.Parameters.AddWithValue($"@name{i}", deleteList[i].Name);
                }

                await deleteCmd.ExecuteNonQueryAsync();
            }

            transaction.Commit();
        }
    }

    private static (string?, string?, string?, int?, int?, int?, string?) ExtractModelInfo(
        IDictionary<string, string>? info)
    {
        if (info == null) return (null, null, null, null, null, null, null);

        var format = info.TryGetValue("format", out var fmt) ? fmt : null;
        var quantization = info.TryGetValue("quantization", out var qntzn)
            ? qntzn
            : null;
        var architecture = info.TryGetValue("architecture", out var arch)
            ? arch
            : null;
        var blockCount = info.TryGetValue("block_count", out var bc) &&
                         int.TryParse(bc, out var bcVal)
            ? bcVal
            : (int?)null;
        var contextLength = info.TryGetValue("context_length", out var cl) &&
                            int.TryParse(cl, out var clVal)
            ? clVal
            : (int?)null;
        var embeddingLength = info.TryGetValue("embedding_length", out var el) &&
                              int.TryParse(el, out var elVal)
            ? elVal
            : (int?)null;

        var additionalDetails = info
            .Where(kvp => kvp.Key is not ("format" or "quantization" or "architecture" or "block_count"
                or "context_length" or "embedding_length"))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        var additionalInfo = additionalDetails.Count > 0 ? JsonSerializer.Serialize(additionalDetails) : null;

        return (format, quantization, architecture, blockCount, contextLength, embeddingLength, additionalInfo);
    }

    private static string ExtractFamilyName(string modelName)
    {
        var colonIndex = modelName.IndexOf(':');
        return colonIndex > 0 ? modelName[..colonIndex] : modelName;
    }

    private static bool HasModelChanged(OllamaModel newModel, OllamaModel existingModel)
    {
        if (newModel.Parameters != existingModel.Parameters ||
            newModel.Size != existingModel.Size ||
            newModel.Info["format"] !=
            existingModel.Info["format"] ||
            newModel.Info["quantization"] !=
            existingModel.Info["quantization"] ||
            newModel.DownloadStatus != existingModel.DownloadStatus)
        {
            return true;
        }

        if (newModel.Info.Count != existingModel.Info.Count)
            return true;

        if (newModel.Info.Count != existingModel.Info.Count) return true;
        foreach (var kv in newModel.Info)
        {
            if (!existingModel.Info.TryGetValue(kv.Key, out var otherVal) ||
                !string.Equals(kv.Value, otherVal, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }


    public async Task UpdateModelAsync(OllamaModel model)
    {
        var now = DateTime.Now;

        using (DatabaseLock.Instance.AcquireWriteLock())
        {
            await using var transaction = _connection.BeginTransaction();

            await using var cmd = _connection.CreateCommand();

            cmd.CommandText = """
                              UPDATE ollama_models
                              SET parameters = @parameters,
                                  size = @size,
                                  format = @format,
                                  quantization = @quantization,
                                  architecture = @architecture,
                                  block_count = @blockCount,
                                  context_length = @contextLength,
                                  embedding_length = @embeddingLength,
                                  additional_info = @additionalInfo,
                                  download_status = @downloadStatus,
                                  cached_at = @cachedAt
                              WHERE name = @name
                              """;

            var (format, quantization, architecture, blockCount, contextLength, embeddingLength, additionalInfo) =
                ExtractModelInfo(model.Info);

            cmd.Parameters.AddWithValue("@name", model.Name);
            cmd.Parameters.AddWithValue("@parameters", model.Parameters);
            cmd.Parameters.AddWithValue("@size", model.Size);
            cmd.Parameters.AddWithValue("@format", format ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@quantization", quantization ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@architecture", architecture ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@blockCount", blockCount ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@contextLength", contextLength ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@embeddingLength", embeddingLength ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@additionalInfo", additionalInfo ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@downloadStatus", (int)model.DownloadStatus);
            cmd.Parameters.AddWithValue("@cachedAt", now);

            await cmd.ExecuteNonQueryAsync();
            transaction.Commit();
        }
    }

    public async Task<IList<OllamaModel>> GetCachedModelsAsync()
    {
        var models = new List<OllamaModel>();

        using (DatabaseLock.Instance.AcquireReadLock())
        {
            await using var cmd = _connection.CreateCommand();
            cmd.CommandText =
                "SELECT name, family_name, parameters, size, format, quantization, architecture, block_count, context_length, embedding_length, additional_info, download_status FROM ollama_models ORDER BY name NULLS LAST";
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var info = new Dictionary<string, string>();

                if (!reader.IsDBNull(4)) info["format"] = reader.GetString(4);
                if (!reader.IsDBNull(5)) info["quantization"] = reader.GetString(5);
                if (!reader.IsDBNull(6)) info["architecture"] = reader.GetString(6);
                if (!reader.IsDBNull(7)) info["block_count"] = reader.GetInt32(7).ToString();
                if (!reader.IsDBNull(8)) info["context_length"] = reader.GetInt32(8).ToString();
                if (!reader.IsDBNull(9)) info["embedding_length"] = reader.GetInt32(9).ToString();

                if (!reader.IsDBNull(10))
                {
                    var additionalInfo = JsonSerializer.Deserialize<Dictionary<string, string>>(reader.GetString(10));
                    if (additionalInfo != null)
                    {
                        foreach (var kvp in additionalInfo)
                        {
                            info[kvp.Key] = kvp.Value;
                        }
                    }
                }

                var model = new OllamaModel
                {
                    Name = reader.GetString(0),
                    Parameters = reader.IsDBNull(2) ? 0 : reader.GetInt64(2),
                    Size = reader.GetInt64(3),
                    Info = info,
                    DownloadStatus = (ModelDownloadStatus)reader.GetInt32(11)
                };

                models.Add(model);
            }
        }

        return models;
    }
}
