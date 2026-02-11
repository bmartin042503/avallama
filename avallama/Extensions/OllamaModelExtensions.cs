// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System.Collections.Generic;
using avallama.Constants.Keys;
using avallama.Models.Dtos;
using avallama.Models.Ollama;

namespace avallama.Extensions;

public static class OllamaModelExtensions
{
    extension(OllamaModel model)
    {
        public void EnrichWith(OllamaShowResponse? showResponse)
        {
            if (showResponse?.ModelInfo == null) return;

            var workingInfoDirectory = new Dictionary<string, string>(model.Info);

            if (!string.IsNullOrEmpty(showResponse.License))
            {
                workingInfoDirectory[ModelInfoKey.License] = showResponse.License;
            }

            var info = showResponse.ModelInfo;
            if (!info.TryGetValue("general.architecture", out var archElem) ||
                !archElem.TryGetString(out var arch) ||
                string.IsNullOrEmpty(arch))
            {
                return;
            }

            workingInfoDirectory[ModelInfoKey.Architecture] = arch;

            if (info.TryGetValue("general.parameter_count", out var paramElem) &&
                paramElem.TryGetInt64(out var paramCount) && paramCount > 0)
            {
                model.Parameters = paramCount;
            }

            string[] keys = [ModelInfoKey.BlockCount, ModelInfoKey.ContextLength, ModelInfoKey.EmbeddingLength];
            foreach (var key in keys)
            {
                var searchKey = $"{arch}.{key}";
                if (info.TryGetValue(searchKey, out var element) && element.TryGetInt32(out var value) && value > 0)
                {
                    workingInfoDirectory[key] = value.ToString();
                }
            }

            model.Info = workingInfoDirectory;
        }

        public void EnrichWith(OllamaModelDto? modelDto)
        {
            var workingInfoDirectory = new Dictionary<string, string>(model.Info);

            if (modelDto?.Name == null) return;
            model.Name = modelDto.Name;

            if (modelDto.Size.HasValue) model.Size = modelDto.Size.Value;
            if (modelDto.Details == null) return;

            if (modelDto.Details.QuantizationLevel != null)
                workingInfoDirectory[ModelInfoKey.QuantizationLevel] = modelDto.Details.QuantizationLevel;
            if (modelDto.Details.Format != null)
                workingInfoDirectory[ModelInfoKey.Format] = modelDto.Details.Format;

            model.Info = workingInfoDirectory;
        }
    }
}
