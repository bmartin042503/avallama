// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.RateLimiting;
using System.Threading.Tasks;

namespace avallama.Utilities.Scraper;

internal sealed class OllamaRateLimitedHandler(
    RateLimiter limiter)
    : DelegatingHandler(new HttpClientHandler()), IAsyncDisposable
{
    private static DateTime? _lastRequestTime;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        using RateLimitLease lease = await limiter.AcquireAsync(
            permitCount: 1,
            cancellationToken
        );

        if (lease.IsAcquired)
        {
            await Task.Delay(Random.Shared.Next(200, 750), cancellationToken);

            var now = DateTime.UtcNow;

            // TODO: proper logging
            if (_lastRequestTime is { } last)
            {
                var diff = now - last;
                // Console.WriteLine($"[HTTP] {request.RequestUri} (+{diff.TotalMilliseconds:F0} ms)");
            }
            else
            {
                // Console.WriteLine($"[HTTP] {request.RequestUri} (first request)");
            }

            _lastRequestTime = now;

            var response = await base.SendAsync(request, cancellationToken);

            // Console.WriteLine($"[DONE] {request.RequestUri} → {response.StatusCode}");
            return response;
        }

        var tooMany = new HttpResponseMessage(HttpStatusCode.TooManyRequests);

        if (lease.TryGetMetadata(
                MetadataName.RetryAfter, out TimeSpan retryAfter))
        {
            tooMany.Headers.Add(
                "Retry-After",
                ((int)retryAfter.TotalSeconds).ToString(
                    NumberFormatInfo.InvariantInfo));
        }

        return tooMany;
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        await limiter.DisposeAsync().ConfigureAwait(false);

        Dispose(disposing: false);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) limiter.Dispose();
    }
}
