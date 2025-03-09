<p align="center">
  <img src="avallama/Assets/Svg/avallama-logo.svg" alt="Avallama Logo" width="80">
</p>
<h1 align="center">Avallama</h1>


Avallama is a multi-platform desktop app designed to enable a user-friendly GUI for Ollama using the Avalonia framework.

This project is currently being developed by Márk Csörgő and Martin Bartos.

## Features

-  **Automatic Ollama Process Management** – Ensures Ollama runs efficiently in the background without manual intervention
-  **No Internet Required** - Avallama makes no connection with the internet, using only Ollama's local server API to pull new models
-  **Chat with Llama3.2** – Seamlessly interact with Llama 3.2 in a user-friendly chat interface
-  **Multi-Platform Support** – Runs on Windows, Linux and macOS with Avalonia
-  **Lightweight and Efficient** – Designed to be minimal while providing a smooth experience
-  **Extensible Backend** – Future updates will include support for additional LLMs, and much more customization and QOL features

## Contributions

We are currently not accepting outside contributions, please see our [Contribution Guidelines](./CONTRIBUTING.md) for more information.

## Latest release

### 0.1.0-alpha - TBD
This is the first alpha release of Avallama. It serves as a proof of concept and lays the foundation for future features and improvements.

#### Release notes
- Initial release of Avallama
- Support for interacting with Llama3.2, with a limitation of one conversation at a time.
- Automatic management of the Ollama process to optimize memory usage.
- Llama3.2 can be downloaded through the app if not present on the system
- UI settings for switching between Dark/Light Mode and toggling between English and Hungarian languages.

For more details on all releases, please refer to the full [CHANGELOG](./CHANGELOG.md).

## Installation

**Important notice**: a working installation of Ollama is required for Avallama to work properly. Before installing Avallama on any platform, please download and install Ollama from [this link](https://ollama.com/download).

### Linux
I use arch btw

### Windows

1. **Download AvallamaInstaller.exe from the [releases](https://github.com/4foureyes/avallama/releases) page**
2. **Run the installer**
    - Once you have downloaded the installer, locate the file and open it to start the installation process
3. **Choose the installation directory**
    - When prompted, select the directory where you would like to install Avallama.
    - The default location is `C:\Program Files (x86)\Avallama`. If you prefer a different location, click **Browse...** to select a folder of your choice.
4. **Complete the installation**
    - Proceed through the installer, when prompted, click **Install**
    - Wait for the installer to copy all necessary files, this may take a moment
5. **Launch Avallama**
    - After files are copied, untick **Launch application** to exit the installer, if ticked, Avallama will start after you click **Finish**.

### macOS

idk a mac joke ngl

## License

This project is licensed under the [MIT License](./LICENSE).
