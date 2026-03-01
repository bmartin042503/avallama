# Known Issues

This is a list of issues we are aware of that will be fixed in a subsequent release. Please do not open new issues for the following known problems, as they are already being addressed.

**All platforms**:
- Conversation titles may fail to generate correctly across various models.
- Conversations do not currently support any form of rich text rendering. This includes Markdown, LaTeX, and custom tags (e.g., `<think>`).
- Automatic application restart when changing language settings may not work as expected.
- The chat logic is currently unstable when handling multiple sessions. You may notice shared UI states (e.g., text entered in one chat appearing in another) or background generations stopping unexpectedly when switching views.
- Failed or interrupted generations (e.g., if the Ollama server stops) do not properly transition to a "Failed" state and may remain stuck on "Generating..." indefinitely.
- Extremely small models (e.g., all-minilm:22m) may fail to produce output, remaining stuck in the generation phase.
- Models downloaded outside the app (e.g., using latest tags via CLI) may appear as duplicates or cause errors.
- Image generation and cloud-based models are not yet supported.
- In-app navigation is partially implemented; the back button may not always return to the correct previous page.

**Windows**:
- When receiving a very large response at a high token/sec, memory usage can climb higher than normal.

**Linux**:
- When uninstalling, configuration files in `~/.config/avallama` are not removed automatically.
- When starting the app from a terminal, killing the app prints a stack trace to the terminal.

**Ubuntu**:
- The `.deb` package is not functional on Ubuntu.
- The application icon is rendered at a very low resolution in some desktop environments.
