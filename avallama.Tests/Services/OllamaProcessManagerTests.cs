// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System.Threading.Tasks;
using avallama.Constants.States;
using avallama.Services.Ollama;
using avallama.Tests.Fixtures;
using Xunit;

namespace avallama.Tests.Services;

public class OllamaProcessManagerTests : IClassFixture<TestServicesFixture>
{
    private readonly TestServicesFixture _fixture;

    public OllamaProcessManagerTests(TestServicesFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task StartAsync_SetsStartingState()
    {
        var opm = new OllamaProcessManager();

        var stateChanged = false;
        var processState = OllamaProcessState.Stopped;

        opm.StatusChanged += status =>
        {
            if (stateChanged) return;
            processState = status.ProcessState;
            stateChanged = true;
        };

        await opm.StartAsync();

        Assert.Equal(OllamaProcessState.Starting, processState);
    }

    [Fact]
    public async Task StartAsync_WhenProcessIsNull_SetsNotInstalledState()
    {
        var opm = new OllamaProcessManager
        {
            StartProcessFunc = _ => null,
            GetProcessCountFunc = () => 0
        };

        var processState = OllamaProcessState.Stopped;
        opm.StatusChanged += status => processState = status.ProcessState;
        await opm.StartAsync();

        Assert.Equal(OllamaProcessState.NotInstalled, processState);
    }

    [Fact]
    public async Task StartAsync_WithProcessRunning_SetsRunningState()
    {
        var opm = new OllamaProcessManager
        {
            StartProcessFunc = _ => null,
            GetProcessCountFunc = () => 1
        };

        var processState = OllamaProcessState.Stopped;
        opm.StatusChanged += status => processState = status.ProcessState;
        await opm.StartAsync();

        Assert.Equal(OllamaProcessState.Running, processState);
    }


}
