# Known Issues

This is a list of issues we are aware of that will be fixed in a subsequent release. Please do not open new issues for the following known problems, as they are already being addressed.

**All platforms**:
- When using some models (e.g. thinking or other specialized models), generated conversation titles may be malformed or missing.
- Markdown, LaTeX and custom tags (e.g. `<think>`) rendering is not yet supported.
- Automatic application restart when changing language settings may not work as expected.
- When performing actions in rapid succession (e.g. changing views / conversations) during message generation, the UI may break or misbehave.
- If a message generation fails, the message may incorrectly display "Generating..." indefinitely.
- Models downloaded outside the app (e.g., using latest tags via CLI) may appear as duplicates or cause errors.

**Windows**:
- When receiving a very large response at a high token/sec, memory usage can climb higher than normal.

**Linux**:
- When uninstalling, configuration files in `~/.config/avallama` are not removed automatically.
- When starting the app from a terminal, killing the app prints a stack trace to the terminal.
