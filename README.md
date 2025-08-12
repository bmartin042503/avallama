<p align="center">
  <img src="avallama/Assets/Svg/avallama-logo.svg" alt="Avallama Logo" width="80">
</p>
<h1 align="center">Avallama</h1>


<p align="center">A cross-platform local AI desktop app powered by Ollama</p>

<p align="center">
    <img style="border-radius: 10px; box-shadow: 0 5px 25px #755085;" src="avallama/Assets/prodimg2.png" alt="Avallama picture">
</p>

This project is currently being developed by Márk Csörgő and Martin Bartos.

## Features

-  **Seamless Local LLMs** - Chat seamlessly with LLMs hosted locally on your machine
-  **Your Data Stays With You** - Avallama runs completely locally, meaning your chats stay with you, and only you
-  **Ollama On The Network** – Connect the app to an Ollama instance on your network to easily use your AI workstation on any device
-  **Multi-Platform Support** – Runs on Windows, Linux and macOS utilizing the Avalonia framework
-  **Lightweight and Efficient** – Designed to be minimal while providing a smooth experience
-  **Automatic Ollama Process Management** – Ensures Ollama runs efficiently in the background without manual intervention

## Contributions

We are currently not accepting outside contributions, however we encourage users to report bugs, crashes or any unexpected behaviour. Please see our [Contribution Guidelines](./CONTRIBUTING.md) for more information.


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
sudo apt install ./avallama_0.1.0_amd64.deb
```
*Replace `./avallama_0.1.0_amd64.deb` with the correct filename of the latest package*

4. After that, you can run the application from the application menu or with the `avallama` command

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

### Windows (x64)

The Windows installer is **not signed with a trusted code-signing certificate**, so Windows SmartScreen may prevent it from running. If you wish to circumvent this, click `More info -> Run anyway`.

1. Download the latest version of AvallamaSetup.exe from the [releases](https://github.com/4foureyes/avallama/releases) page.
2. Open the installer and go through the setup steps.
3. Once installed, you can run the application from the Start Menu.

To uninstall, remove it through the `Apps > Installed apps` page in Settings, or navigate to the installation directory and run unins000.exe.

### Windows (arm64)

Windows on Arm is not currently supported.

### macOS

This application is **not signed nor notarized**. MacOS may prevent it from running by default.

1. Download the latest ZIP file from the [releases](https://github.com/4foureyes/avallama/releases) page. (for Intel use *osx_x64*, for Apple Silicon use *osx_arm64*)
2. Double-click on the ZIP file to extract it.
3. Double-click on Avallama to run it.
4. *(Optional) Move the Avallama.app file to the Applications folder using Finder*.

If the '*app is damaged or can't be opened*' error occurs, make sure you remove the quarantine flag of the ZIP file using the following command in the Terminal, and extract it again:
```bash
xattr -d com.apple.quarantine avallama_0.1.0-alpha_osx_arm64.zip
```

### Building from source

If you are comfortable building the app from source, feel free to clone the repository and build the application to test out the newest features we are actively implementing.

## Known issues

This is a list of issues we are aware of that will be fixed in a subsequent release. Please do not open new issues for the following known problems, as they are already being addressed.

- Arch Linux: Install is not functional

## License

This project is licensed under the [MIT License](./LICENSE).
