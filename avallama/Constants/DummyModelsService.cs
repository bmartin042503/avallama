using System.Collections.Generic;
using avallama.Models;
using avallama.Services;

namespace avallama.Constants;

public static class DummyModelsService
{
    public static IEnumerable<OllamaModel> GetDummyOllamaModels()
    {
        return new List<OllamaModel>
        {
            new(
                name: "gpt-oss",
                quantization: 4,
                parameters: 20,
                format: "GGUF/MLX",
                details: new Dictionary<string, string>
                {
                    { LocalizationService.GetString("GENERAL_ARCHITECTURE"), "gpt" },
                    { LocalizationService.GetString("BLOCK_COUNT"), "40" },
                    { LocalizationService.GetString("CONTEXT_LENGTH"), "8192" },
                    { LocalizationService.GetString("EMBEDDING_LENGTH"), "4608" },
                },
                size: 21474836480, // 20 GB
                downloadStatus: ModelDownloadStatus.Ready,
                runsSlow: true
            ),
            new(
                name: "llama3.2",
                quantization: 0,
                parameters: 3,
                format: "GGUF",
                details: new Dictionary<string, string>
                {
                    { LocalizationService.GetString("GENERAL_ARCHITECTURE"), "llama" },
                    { LocalizationService.GetString("BLOCK_COUNT"), "40" },
                    { LocalizationService.GetString("CONTEXT_LENGTH"), "8192" },
                    { LocalizationService.GetString("EMBEDDING_LENGTH"), "4608" },
                },
                size: 3221225472, // 3 GB
                downloadStatus: ModelDownloadStatus.Ready,
                runsSlow: false
            ),
            new(
                name: "deepseek-r1",
                quantization: 4,
                parameters: 15,
                format: "GGUF/MLX",
                details: new Dictionary<string, string>
                {
                    { LocalizationService.GetString("GENERAL_ARCHITECTURE"), "gpt" },
                    { LocalizationService.GetString("BLOCK_COUNT"), "40" },
                    { LocalizationService.GetString("CONTEXT_LENGTH"), "8192" },
                    { LocalizationService.GetString("EMBEDDING_LENGTH"), "4608" },
                },
                size: 10737418240, // 10 GB
                downloadStatus: ModelDownloadStatus.Ready,
                runsSlow: false
            ),
            new(
                name: "gemma3",
                quantization: 1,
                parameters: 25,
                format: "GGUF",
                details: new Dictionary<string, string>
                {
                    { LocalizationService.GetString("GENERAL_ARCHITECTURE"), "gemma" },
                    { LocalizationService.GetString("BLOCK_COUNT"), "60" },
                    { LocalizationService.GetString("CONTEXT_LENGTH"), "16384" },
                    { LocalizationService.GetString("EMBEDDING_LENGTH"), "8192" },
                },
                size: 32212254720, // 30 GB
                downloadStatus: ModelDownloadStatus.Ready,
                runsSlow: true
            ),
            new(
                name: "qwen3",
                quantization: 0,
                parameters: 30,
                format: "GGUF",
                details: new Dictionary<string, string>
                {
                    { LocalizationService.GetString("GENERAL_ARCHITECTURE"), "qwen" },
                    { LocalizationService.GetString("BLOCK_COUNT"), "80" },
                    { LocalizationService.GetString("CONTEXT_LENGTH"), "16384" },
                    { LocalizationService.GetString("EMBEDDING_LENGTH"), "12288" },
                },
                size: 42949672960, // 40 GB
                downloadStatus: ModelDownloadStatus.Ready,
                runsSlow: true
            ),
            new(
                name: "llama3.1",
                quantization: 2,
                parameters: 20,
                format: "GGUF",
                details: new Dictionary<string, string>
                {
                    { LocalizationService.GetString("GENERAL_ARCHITECTURE"), "llama" },
                    { LocalizationService.GetString("BLOCK_COUNT"), "50" },
                    { LocalizationService.GetString("CONTEXT_LENGTH"), "10240" },
                    { LocalizationService.GetString("EMBEDDING_LENGTH"), "5120" },
                },
                size: 5368709120, // 5 GB
                downloadStatus: ModelDownloadStatus.Ready,
                runsSlow: false
            ),
            new(
                name: "mistral",
                quantization: 0,
                parameters: 40,
                format: "GGUF",
                details: new Dictionary<string, string>
                {
                    { LocalizationService.GetString("GENERAL_ARCHITECTURE"), "mistral" },
                    { LocalizationService.GetString("BLOCK_COUNT"), "60" },
                    { LocalizationService.GetString("CONTEXT_LENGTH"), "8192" },
                    { LocalizationService.GetString("EMBEDDING_LENGTH"), "4608" },
                },
                size: 10737418240, // 10 GB
                downloadStatus: ModelDownloadStatus.Ready,
                runsSlow: false
            ),
            new(
                name: "qwen2.5",
                quantization: 1,
                parameters: 28,
                format: "GGUF",
                details: new Dictionary<string, string>
                {
                    { LocalizationService.GetString("GENERAL_ARCHITECTURE"), "qwen" },
                    { LocalizationService.GetString("BLOCK_COUNT"), "70" },
                    { LocalizationService.GetString("CONTEXT_LENGTH"), "10240" },
                    { LocalizationService.GetString("EMBEDDING_LENGTH"), "5120" },
                },
                size: 8589934592, // 8 GB
                downloadStatus: ModelDownloadStatus.Ready,
                runsSlow: false
            ),
            new(
                name: "phi3",
                quantization: 3,
                parameters: 35,
                format: "GGUF",
                details: new Dictionary<string, string>
                {
                    { LocalizationService.GetString("GENERAL_ARCHITECTURE"), "phi" },
                    { LocalizationService.GetString("BLOCK_COUNT"), "90" },
                    { LocalizationService.GetString("CONTEXT_LENGTH"), "16384" },
                    { LocalizationService.GetString("EMBEDDING_LENGTH"), "8192" },
                },
                size: 12884901888, // 12 GB
                downloadStatus: ModelDownloadStatus.Ready,
                runsSlow: false
            ),
            new(
                name: "qwen2.5-coder",
                quantization: 0,
                parameters: 40,
                format: "GGUF",
                details: new Dictionary<string, string>
                {
                    { LocalizationService.GetString("GENERAL_ARCHITECTURE"), "qwen" },
                    { LocalizationService.GetString("BLOCK_COUNT"), "100" },
                    { LocalizationService.GetString("CONTEXT_LENGTH"), "16384" },
                    { LocalizationService.GetString("EMBEDDING_LENGTH"), "12288" },
                },
                size: 16106127360, // 15 GB
                downloadStatus: ModelDownloadStatus.Ready,
                runsSlow: true
            ),
            new(
                name: "gemma",
                quantization: 1,
                parameters: 18,
                format: "GGUF",
                details: new Dictionary<string, string>
                {
                    { LocalizationService.GetString("GENERAL_ARCHITECTURE"), "gemma" },
                    { LocalizationService.GetString("BLOCK_COUNT"), "50" },
                    { LocalizationService.GetString("CONTEXT_LENGTH"), "8192" },
                    { LocalizationService.GetString("EMBEDDING_LENGTH"), "4608" },
                },
                size: 4294967296, // 4 GB
                downloadStatus: ModelDownloadStatus.Ready,
                runsSlow: false
            ),
            new(
                name: "llama2",
                quantization: 0,
                parameters: 15,
                format: "GGUF",
                details: new Dictionary<string, string>
                {
                    { LocalizationService.GetString("GENERAL_ARCHITECTURE"), "llama" },
                    { LocalizationService.GetString("BLOCK_COUNT"), "40" },
                    { LocalizationService.GetString("CONTEXT_LENGTH"), "8192" },
                    { LocalizationService.GetString("EMBEDDING_LENGTH"), "4608" },
                },
                size: 3221225472, // 3 GB
                downloadStatus: ModelDownloadStatus.Ready,
                runsSlow: false
            ),
            new(
                name: "dolphin3",
                quantization: 5,
                parameters: 45,
                format: "GGUF",
                details: new Dictionary<string, string>
                {
                    { LocalizationService.GetString("GENERAL_ARCHITECTURE"), "dolphin" },
                    { LocalizationService.GetString("BLOCK_COUNT"), "60" },
                    { LocalizationService.GetString("CONTEXT_LENGTH"), "16384" },
                    { LocalizationService.GetString("EMBEDDING_LENGTH"), "8192" },
                },
                size: 10737418240, // 10 GB
                downloadStatus: ModelDownloadStatus.Ready,
                runsSlow: true
            ),
            new(
                name: "deepseek-v3",
                quantization: 3,
                parameters: 30,
                format: "GGUF",
                details: new Dictionary<string, string>
                {
                    { LocalizationService.GetString("GENERAL_ARCHITECTURE"), "deepseek" },
                    { LocalizationService.GetString("BLOCK_COUNT"), "70" },
                    { LocalizationService.GetString("CONTEXT_LENGTH"), "8192" },
                    { LocalizationService.GetString("EMBEDDING_LENGTH"), "4608" },
                },
                size: 6442450944, // 6 GB
                downloadStatus: ModelDownloadStatus.Ready,
                runsSlow: false
            ),
            new(
                name: "mistral-small",
                quantization: 0,
                parameters: 12,
                format: "GGUF",
                details: new Dictionary<string, string>
                {
                    { LocalizationService.GetString("GENERAL_ARCHITECTURE"), "mistral" },
                    { LocalizationService.GetString("BLOCK_COUNT"), "30" },
                    { LocalizationService.GetString("CONTEXT_LENGTH"), "4096" },
                    { LocalizationService.GetString("EMBEDDING_LENGTH"), "2048" },
                },
                size: 2147483648, // 2 GB
                downloadStatus: ModelDownloadStatus.Ready,
                runsSlow: false
            )
        };
    }
}