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

            if (!string.IsNullOrEmpty(showResponse.License))
            {
                model.Info.TryAdd(ModelInfoKey.License, showResponse.License);
            }

            var info = showResponse.ModelInfo;
            if (!info.TryGetValue("general.architecture", out var archElem) ||
                !archElem.TryGetString(out var arch) ||
                string.IsNullOrEmpty(arch))
            {
                return;
            }

            model.Info.TryAdd(ModelInfoKey.Architecture, arch);

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
                    model.Info.TryAdd(key, value.ToString());
                }
            }
        }
    }
}
