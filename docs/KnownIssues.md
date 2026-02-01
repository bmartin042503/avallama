# Known Issues

This is a list of issues we are aware of that will be fixed in a subsequent release. Please do not open new issues for the following known problems, as they are already being addressed.

**All platforms**:
- When using a thinking model, generated conversation titles may be malformed or missing.
- Markdown and custom tags (e.g. `<think>`) rendering is not yet supported.
- Automatic restarting of the app when changing language settings may not work as expected.
- Some models in the model library (e.g. cloud models) are incorrectly available to download.

**Windows**:
- When receiving a very large response at a high token/sec, memory usage can climb higher than normal.

**Linux**:
- When uninstalling, configuration files in `~/.config/avallama` are not removed automatically.
- When starting the app from a terminal, killing the app prints a stack trace to the terminal.

**Ubuntu**:
- The `.deb` package is not functional on Ubuntu.
