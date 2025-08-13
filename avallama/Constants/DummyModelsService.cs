using System.Collections.Generic;
using avallama.Models;
using avallama.Services;

namespace avallama.Constants;

// TODO: kitörölni ha már jól össze van kötve a model manager a db-vel

public static class DummyModelsService
{
    public static IEnumerable<OllamaModel> GetDummyOllamaModels()
    {
        var test1 = new OllamaModel(
            name: "verylongmodelnamellamasomething-coder-pro-ultra-max3.8",
            quantization: 4,
            parameters: 3.8,
            details: new Dictionary<string, string>
            {
                { LocalizationService.GetString("GENERAL_ARCHITECTURE"), "llama" },
                { LocalizationService.GetString("BLOCK_COUNT"), "40" },
                { LocalizationService.GetString("CONTEXT_LENGTH"), "8192" },
                { LocalizationService.GetString("EMBEDDING_LENGTH"), "4608" },
            },
            size: 3825205248,
            downloadStatus: ModelDownloadStatus.Downloading,
            format: "MLX",
            runsSlow: true
        )
        {
            DownloadProgress = 0.6942
        };

        return new List<OllamaModel>
        {
            new(
                name: "llama3.2",
                quantization: 4,
                parameters: 3.2,
                details: new Dictionary<string, string>
                {
                    { LocalizationService.GetString("GENERAL_ARCHITECTURE"), "llama" },
                    { LocalizationService.GetString("BLOCK_COUNT"), "32" },
                    { LocalizationService.GetString("CONTEXT_LENGTH"), "8192" },
                    { LocalizationService.GetString("EMBEDDING_LENGTH"), "4096" },
                    {
                        LocalizationService.GetString("GENERATION_SPEED"), 
                        string.Format(LocalizationService.GetString("TOKEN_SEC_FORMAT"), 35.7)
                    }
                },
                size: 3221225472,
                format: "GGUF",
                downloadStatus: ModelDownloadStatus.Downloaded,
                runsSlow: false
            ),
            test1,
            new(
                name: "mistral7b",
                quantization: 5,
                parameters: 7.0,
                details: new Dictionary<string, string>
                {
                    { LocalizationService.GetString("GENERAL_ARCHITECTURE"), "mistral" },
                    { LocalizationService.GetString("BLOCK_COUNT"), "48" },
                    { LocalizationService.GetString("CONTEXT_LENGTH"), "16384" },
                    { LocalizationService.GetString("EMBEDDING_LENGTH"), "5120" }
                },
                size: 7516192768,
                downloadStatus: ModelDownloadStatus.Downloading,
                format: "GGUF",
                runsSlow: false
                
            ),

            new(
                name: "gemma2.0",
                quantization: 3,
                parameters: 2.0,
                details: new Dictionary<string, string>
                {
                    { LocalizationService.GetString("GENERAL_ARCHITECTURE"), "gemma" },
                    { LocalizationService.GetString("BLOCK_COUNT"), "28" },
                    { LocalizationService.GetString("CONTEXT_LENGTH"), "4096" },
                    { LocalizationService.GetString("EMBEDDING_LENGTH"), "3840" }
                },
                size: 2147483648,
                downloadStatus: ModelDownloadStatus.NotEnoughSpace,
                runsSlow: true,
                format: string.Empty
            ),

            new(
                name: "falcon1b",
                quantization: 0,
                parameters: 0,
                details: new Dictionary<string, string>
                {
                    { LocalizationService.GetString("GENERAL_ARCHITECTURE"), "falcon" },
                    { LocalizationService.GetString("BLOCK_COUNT"), "20" },
                    { LocalizationService.GetString("CONTEXT_LENGTH"), "2048" },
                    { LocalizationService.GetString("EMBEDDING_LENGTH"), "2048" },
                    {
                        LocalizationService.GetString("GENERATION_SPEED"),
                        string.Format(LocalizationService.GetString("TOKEN_SEC_FORMAT"), 50.2)
                    }
                },
                size: 1073741824,
                downloadStatus: ModelDownloadStatus.Downloaded,
                format: string.Empty,
                runsSlow: false
                
            ),

            new(
                name: "orca-mini",
                quantization: 0,
                parameters: 2.5,
                details: new Dictionary<string, string>
                {
                    { LocalizationService.GetString("GENERAL_ARCHITECTURE"), "orca" },
                    { LocalizationService.GetString("BLOCK_COUNT"), "30" },
                    { LocalizationService.GetString("CONTEXT_LENGTH"), "8192" },
                    { LocalizationService.GetString("EMBEDDING_LENGTH"), "3072" },
                    {
                        LocalizationService.GetString("GENERATION_SPEED"),
                        string.Format(LocalizationService.GetString("TOKEN_SEC_FORMAT"), 33.3)
                    }
                },
                size: 2684354560,
                downloadStatus: ModelDownloadStatus.Downloaded,
                format: "Custom",
                runsSlow: true
            ),

            new(
                name: "phi-2",
                quantization: 3,
                parameters: 0,
                details: new Dictionary<string, string>
                {
                    { LocalizationService.GetString("GENERAL_ARCHITECTURE"), "phi" },
                    { LocalizationService.GetString("BLOCK_COUNT"), "18" },
                    { LocalizationService.GetString("CONTEXT_LENGTH"), "4096" },
                    { LocalizationService.GetString("EMBEDDING_LENGTH"), "2560" }
                },
                size: 1610612736,
                downloadStatus: ModelDownloadStatus.Ready,
                runsSlow: true,
                format: string.Empty
            ),

            new(
                name: "codellama-instruct",
                quantization: 6,
                parameters: 7.0,
                details: new Dictionary<string, string>
                {
                    { LocalizationService.GetString("GENERAL_ARCHITECTURE"), "llama" },
                    { LocalizationService.GetString("BLOCK_COUNT"), "50" },
                    { LocalizationService.GetString("CONTEXT_LENGTH"), "16384" },
                    { LocalizationService.GetString("EMBEDDING_LENGTH"), "5120" }
                },
                size: 7516192768,
                downloadStatus: ModelDownloadStatus.Downloading,
                format: "GGUF",
                runsSlow: false
            )
            {
                DownloadProgress = 0.81
            },

            new(
                name: "zephyr-beta",
                quantization: 4,
                parameters: 6.7,
                details: new Dictionary<string, string>
                {
                    { LocalizationService.GetString("GENERAL_ARCHITECTURE"), "zephyr" },
                    { LocalizationService.GetString("BLOCK_COUNT"), "44" },
                    { LocalizationService.GetString("CONTEXT_LENGTH"), "12288" },
                    { LocalizationService.GetString("EMBEDDING_LENGTH"), "4864" }
                },
                size: 7205759400,
                downloadStatus: ModelDownloadStatus.NotEnoughSpace,
                format: "VERY-LONG-FORMAT-NAME-FOR-TESTING",
                runsSlow: false
            ),

            new(
                name: "neural-chat:120b",
                quantization: 5,
                parameters: 120.5,
                details: new Dictionary<string, string>
                {
                    { LocalizationService.GetString("GENERAL_ARCHITECTURE"), "intel" },
                    { LocalizationService.GetString("BLOCK_COUNT"), "36" },
                    { LocalizationService.GetString("CONTEXT_LENGTH"), "8192" },
                    { LocalizationService.GetString("EMBEDDING_LENGTH"), "4352" },
                    {
                        LocalizationService.GetString("GENERATION_SPEED"),
                        string.Format(LocalizationService.GetString("TOKEN_SEC_FORMAT"), 36.0)
                    }
                },
                size: 4831838208,
                downloadStatus: ModelDownloadStatus.Downloaded,
                format: "GGUF",
                runsSlow: true
            ),

            new(
                name: "openchat3.5",
                quantization: 0,
                parameters: 6.0,
                details: new Dictionary<string, string>
                {
                    { LocalizationService.GetString("GENERAL_ARCHITECTURE"), "transformer" },
                    { LocalizationService.GetString("BLOCK_COUNT"), "42" },
                    { LocalizationService.GetString("CONTEXT_LENGTH"), "8192" },
                    { LocalizationService.GetString("EMBEDDING_LENGTH"), "4800" }
                },
                size: 6442450944,
                downloadStatus: ModelDownloadStatus.NoConnection,
                format: string.Empty,
                runsSlow: false
            )
        };
    }
}