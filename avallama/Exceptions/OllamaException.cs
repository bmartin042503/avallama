// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Net;

namespace avallama.Exceptions;

public abstract class OllamaException(string message, Exception? inner = null) : AvallamaException(message, inner);

public class OllamaApiException(HttpStatusCode statusCode, Exception? inner = null)
    : OllamaException($"Ollama API error: {statusCode}", inner)
{
    public HttpStatusCode StatusCode { get; } = statusCode;
}

public class OllamaSiteUnreachableException(HttpStatusCode statusCode, Exception? inner = null)
    : OllamaException("Ollama website is unreachable.", inner)
{
    public HttpStatusCode StatusCode { get; } = statusCode;
}

public class OllamaLocalServerUnreachableException(Exception? inner = null)
    : OllamaException("Local Ollama server is unreachable.", inner);

public class OllamaRemoteServerUnreachableException(Exception? inner = null)
    : OllamaException("Remote Ollama server is unreachable.", inner);

