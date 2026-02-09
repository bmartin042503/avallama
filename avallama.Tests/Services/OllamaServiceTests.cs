// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using avallama.Tests.Fixtures;
using Xunit;

namespace avallama.Tests.Services;

public class OllamaServiceTests : IClassFixture<TestServicesFixture>
{
    private readonly TestServicesFixture _fixture;

    public OllamaServiceTests(TestServicesFixture fixture)
    {
        _fixture = fixture;
    }
}
