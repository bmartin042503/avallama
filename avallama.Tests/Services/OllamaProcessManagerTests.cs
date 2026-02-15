// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System.Threading.Tasks;
using avallama.Constants.States;
using avallama.Services.Ollama;
using avallama.Tests.Fixtures;
using avallama.Tests.Mocks;
using Xunit;

namespace avallama.Tests.Services;

public class OllamaProcessManagerTests : IClassFixture<TestServicesFixture>
{
    [Fact]
    public async Task StartAsync_SetsStartingState()
    {
        var opm = new OllamaProcessManager
        {
            StartProcessFunc = _ => new OllamaProcessMock(),
            GetProcessesFunc = () => []
        };

        var stateChanged = false;
        var processState = OllamaProcessLifecycle.Stopped;

        opm.StatusChanged += status =>
        {
            if (stateChanged) return;
            processState = status.ProcessLifecycle;
            stateChanged = true;
        };

        await opm.StartAsync();

        Assert.Equal(OllamaProcessLifecycle.Starting, processState);
    }

    [Fact]
    public async Task StartAsync_WhenProcessIsNull_SetsNotInstalledState()
    {
        var opm = new OllamaProcessManager
        {
            StartProcessFunc = _ => null,
            GetProcessesFunc = () => []
        };

        var processState = OllamaProcessLifecycle.Stopped;
        opm.StatusChanged += status => processState = status.ProcessLifecycle;
        await opm.StartAsync();

        Assert.Equal(OllamaProcessLifecycle.NotInstalled, processState);
    }

    [Fact]
    public async Task StartAsync_WithProcessRunning_SetsRunningState()
    {
        var opm = new OllamaProcessManager
        {
            StartProcessFunc = _ => new OllamaProcessMock(),
            GetProcessesFunc = () => [ new OllamaProcessMock() ]
        };

        var processState = OllamaProcessLifecycle.Stopped;
        opm.StatusChanged += status => processState = status.ProcessLifecycle;
        await opm.StartAsync();

        Assert.Equal(OllamaProcessLifecycle.Running, processState);
    }
}
