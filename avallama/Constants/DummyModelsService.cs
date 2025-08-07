using System.Collections.Generic;
using avallama.Models;
using avallama.Services;

namespace avallama.Constants;

// TODO: kitörölni ha már jól össze van kötve a model manager a db-vel

public static class DummyModelsService
{
    public static IEnumerable<OllamaModel> GetDummyOllamaModels()
    {
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
                ModelDownloadStatus.Downloaded
            ),
            new(
                name: "llama3.8",
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
                ModelDownloadStatus.ReadyForDownload
            ),
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
                ModelDownloadStatus.ReadyForDownload
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
                ModelDownloadStatus.NotEnoughSpaceForDownload
            ),

            new(
                name: "falcon1b",
                quantization: 2,
                parameters: 1.0,
                details: new Dictionary<string, string>
                {
                    { LocalizationService.GetString("GENERAL_ARCHITECTURE"), "falcon" },
                    { LocalizationService.GetString("BLOCK_COUNT"), "20" },
                    { LocalizationService.GetString("CONTEXT_LENGTH"), "2048" },
                    { LocalizationService.GetString("EMBEDDING_LENGTH"), "2048" },
                    {
                        LocalizationService.GetString("TOKEN_SEC_FORMAT"),
                        string.Format(LocalizationService.GetString("TOKEN_SEC_FORMAT"), 50.2)
                    }
                },
                size: 1073741824,
                ModelDownloadStatus.Downloaded
            ),

            new(
                name: "orca-mini",
                quantization: 4,
                parameters: 2.5,
                details: new Dictionary<string, string>
                {
                    { LocalizationService.GetString("GENERAL_ARCHITECTURE"), "orca" },
                    { LocalizationService.GetString("BLOCK_COUNT"), "30" },
                    { LocalizationService.GetString("CONTEXT_LENGTH"), "8192" },
                    { LocalizationService.GetString("EMBEDDING_LENGTH"), "3072" },
                    {
                        LocalizationService.GetString("TOKEN_SEC_FORMAT"),
                        string.Format(LocalizationService.GetString("TOKEN_SEC_FORMAT"), 33.3)
                    }
                },
                size: 2684354560,
                ModelDownloadStatus.Downloaded
            ),

            new(
                name: "phi-2",
                quantization: 3,
                parameters: 1.5,
                details: new Dictionary<string, string>
                {
                    { LocalizationService.GetString("GENERAL_ARCHITECTURE"), "phi" },
                    { LocalizationService.GetString("BLOCK_COUNT"), "18" },
                    { LocalizationService.GetString("CONTEXT_LENGTH"), "4096" },
                    { LocalizationService.GetString("EMBEDDING_LENGTH"), "2560" }
                },
                size: 1610612736,
                ModelDownloadStatus.ReadyForDownload
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
                ModelDownloadStatus.ReadyForDownload
            ),

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
                ModelDownloadStatus.NotEnoughSpaceForDownload
            ),

            new(
                name: "neural-chat",
                quantization: 5,
                parameters: 4.5,
                details: new Dictionary<string, string>
                {
                    { LocalizationService.GetString("GENERAL_ARCHITECTURE"), "intel" },
                    { LocalizationService.GetString("BLOCK_COUNT"), "36" },
                    { LocalizationService.GetString("CONTEXT_LENGTH"), "8192" },
                    { LocalizationService.GetString("EMBEDDING_LENGTH"), "4352" },
                    {
                        LocalizationService.GetString("TOKEN_SEC_FORMAT"),
                        string.Format(LocalizationService.GetString("TOKEN_SEC_FORMAT"), 36.0)
                    }
                },
                size: 4831838208,
                ModelDownloadStatus.Downloaded
            ),

            new(
                name: "openchat3.5",
                quantization: 4,
                parameters: 6.0,
                details: new Dictionary<string, string>
                {
                    { LocalizationService.GetString("GENERAL_ARCHITECTURE"), "transformer" },
                    { LocalizationService.GetString("BLOCK_COUNT"), "42" },
                    { LocalizationService.GetString("CONTEXT_LENGTH"), "8192" },
                    { LocalizationService.GetString("EMBEDDING_LENGTH"), "4800" }
                },
                size: 6442450944,
                ModelDownloadStatus.NoConnectionForDownload
            )
        };
    }
}