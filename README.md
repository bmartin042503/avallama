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

### v0.1.0-alpha - 2025/03/12
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

#### Debian/Ubuntu (x64)
1. Download the latest .deb package from the [releases](https://github.com/4foureyes/avallama/releases) page.
2. Open a terminal and navigate to the directory where the file was downloaded, for example: 
```bash
cd ~/Downloads
```
3. Install the package using the following command:
```bash
sudo apt install ./avallama_0.1.0-alpha_amd64.deb
```
*Replace `./avallama_0.1.0-alpha_amd64.deb` with the correct filename of the latest package*

4. Once installed, execution permission must be granted manually with the following command:

```bash
sudo chmod +x /usr/bin/avallama
```
5. After that, you can run the application from the application menu or with the `avallama` command

To uninstall, run:
```bash
sudo apt remove avallama
```

#### Arch Linux (x64)

This installation method has not been verified and may run unexpectedly.

1. Download the latest PKGBUILD and .tar.gz files from the [releases](https://github.com/4foureyes/avallama/releases) page.
2. Open a terminal and navigate to the directory where the files were downloaded.
3. Install the package using the following command:
```bash
makepkg -si
```

### Windows

1. Download the latest version of AvallamaSetup.exe from the [releases](https://github.com/4foureyes/avallama/releases) page.
2. Open the installer and go through the setup steps.
3. Once installed, you can run the application from the Start Menu.

To uninstall, remove it through the `Apps > Installed apps` page in Settings, or navigate to the installation directory and run unins000.exe.

**Note**: AvallamaSetup.exe is for installing Avallama on x86-64 based Windows systems, there is currently no support for Arm64.

### macOS

This application is **not signed nor notarized**. MacOS may prevent it from running by default.

1. Download the latest ZIP file from the [releases](https://github.com/4foureyes/avallama/releases) page. (for Intel use *osx_x64*, for Apple Silicon use *osx_arm64*)
2. Double click on the ZIP file to extract it.
3. Double click on Avallama to run it.
4. *(Optional) Move the Avallama.app file to the Applications folder using Finder*.

## License

This project is licensed under the [MIT License](./LICENSE).
