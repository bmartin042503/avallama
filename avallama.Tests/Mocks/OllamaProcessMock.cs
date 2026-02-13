// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Threading.Tasks;
using avallama.Services.Ollama;

namespace avallama.Tests.Mocks;

public class OllamaProcessMock : IOllamaProcess
{
    public int Id { get; set; } = 1;
    public string ProcessName { get; set; } = "ollama";
    public bool EnableRaisingEvents { get; set; }
    public int ExitCode { get; set; } = 0;
    public IntPtr MainWindowHandle { get; set; } = IntPtr.Zero;
    public event EventHandler? Exited;

    public void Kill() { }
    public Task WaitForExitAsync() => Task.CompletedTask;
    public void Dispose() { }
    public void TriggerExit() => Exited?.Invoke(this, EventArgs.Empty);
}
