// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System.Collections.Generic;
using avallama.Models.Dtos;
using avallama.Models.Ollama;

namespace avallama.Extensions;

public static class DtoExtensions
{
    extension(OllamaModelDto modelDto)
    {
        public OllamaModel ConvertToOllamaModel()
        {
            var model = new OllamaModel { Name = modelDto.Name ?? string.Empty };

            if (modelDto.Size.HasValue) model.Size = modelDto.Size.Value;
            if (modelDto.Details == null) return model;

            if (modelDto.Details.QuantizationLevel != null)
                model.Info.TryAdd("quantization_level", modelDto.Details.QuantizationLevel);
            if (modelDto.Details.Format != null) model.Info.TryAdd("format", modelDto.Details.Format);

            return model;
        }
    }
}
