// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace avallama.Services.Ollama;

/// <summary>
/// Defines a contract for interacting with an Ollama server process allowing for abstraction and easier unit testing.
/// </summary>
public interface IOllamaProcess : IDisposable
{
    #region Interface

    /// <summary>
    /// Gets the unique identifier for the associated process.
    /// </summary>
    int Id { get; }

    /// <summary>
    /// Gets the window handle of the main window of the associated process.
    /// Returns <see cref="IntPtr.Zero"/> if the process does not have a main window.
    /// </summary>
    IntPtr MainWindowHandle { get; }

    /// <summary>
    /// Gets the name of the process.
    /// </summary>
    string ProcessName { get; }

    /// <summary>
    /// Gets or sets a value indicating whether raising events is enabled (e.g. <see cref="Exited"/>).
    /// </summary>
    bool EnableRaisingEvents { get; set; }

    /// <summary>
    /// Gets the value that the associated process specified when it terminated.
    /// Returns -1 if the exit code cannot be retrieved.
    /// </summary>
    int ExitCode { get; }

    /// <summary>
    /// Occurs when the associated process exits.
    /// </summary>
    event EventHandler? Exited;

    /// <summary>
    /// Immediately stops the associated process.
    /// </summary>
    void Kill();

    /// <summary>
    /// Instructs the process component to wait indefinitely for the associated process to exit.
    /// </summary>
    Task WaitForExitAsync();

    #endregion
}

/// <summary>
/// A wrapper implementation for <see cref="System.Diagnostics.Process"/> that implements <see cref="IOllamaProcess"/>.
/// </summary>
public class OllamaProcess : IOllamaProcess
{
    private readonly Process _process;

    public event EventHandler? Exited;

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="OllamaProcess"/> class wrapping a system process.
    /// </summary>
    /// <param name="process">The underlying system process to wrap.</param>
    public OllamaProcess(Process process)
    {
        _process = process;

        // Forward the internal process Exited event to the wrapper's event,
        // passing 'this' (the wrapper) as the sender.
        _process.Exited += (sender, args) => Exited?.Invoke(this, args);
    }

    #endregion

    #region Properties

    /// <inheritdoc />
    public int Id => _process.Id;

    /// <inheritdoc />
    public string ProcessName => _process.ProcessName;

    /// <inheritdoc />
    public IntPtr MainWindowHandle => _process.MainWindowHandle;

    /// <inheritdoc />
    public bool EnableRaisingEvents
    {
        get => _process.EnableRaisingEvents;
        set => _process.EnableRaisingEvents = value;
    }

    /// <inheritdoc />
    public int ExitCode
    {
        get
        {
            try
            {
                return _process.ExitCode;
            }
            catch
            {
                // Returns -1 if the process is still running or if access is denied.
                return -1;
            }
        }
    }

    #endregion

    #region Public Methods

    /// <inheritdoc />
    public void Kill() => _process.Kill();

    /// <inheritdoc />
    public Task WaitForExitAsync() => _process.WaitForExitAsync();

    #endregion

    #region Dispose

    public void Dispose()
    {
        _process.Dispose();
        GC.SuppressFinalize(this);
    }

    #endregion
}
