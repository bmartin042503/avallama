# Building from source

## Prerequisites

1. Install the [.NET SDK](https://dotnet.microsoft.com/en-us/download) (version 10.0 or later).
2. Clone the repository:

```bash
git clone https://github.com/4foureyes/avallama.git
```

3. Navigate to the project directory:

```bash
cd avallama
```

4. Restore dependencies:

```bash
dotnet restore
```
## Building and running via .NET

1. Build the project:

```bash
dotnet build -c Release
```

2. Run the application:

```bash
dotnet run -c Release --project avallama/avallama.csproj
```

## Compiling a self-contained executable

To compile the application to a self-contained executable file, run:

```bash
dotnet publish -c Release -r <RID> --self-contained true
```
The output executable will be located in the `avallama/bin/Release/net10.0/<RID>/publish/` directory.

*Replace `<RID>` with your platform's Runtime Identifier, e.g. `win-x64`, `linux-x64`, `osx-arm64`, `osx-x64`*

## Note for macOS

For the application to function correctly on macOS, it is necessary to compile the required native macOS library using Clang:

```bash
clang -dynamiclib -framework Cocoa -arch arm64 -o avallama/bin/Release/net10.0/libFullScreenCheck.dylib native/macos/FullScreenCheck.m
```

Use `-arch x86_64` or `-arch arm64` depending on your CPU architecture.
