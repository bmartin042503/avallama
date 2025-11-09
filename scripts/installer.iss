#ifndef AppVersion
#define AppVersion "0.0.0"
#endif

[Setup]
AppName=avallama
AppVersion={#AppVersion}
AppVerName=avallama
DefaultDirName={localappdata}\avallama
DefaultGroupName=4foureyes
UninstallDisplayIcon={app}\avallama.exe
OutputDir=.
OutputBaseFilename=avallama-setup-{#AppVersion}
PrivilegesRequired=lowest
VersionInfoVersion={#AppVersion}

[Code]
var
  MyPage: TWizardPage;

function IsOllamaInstalled(): Boolean;
begin
  Result := FileExists(ExpandConstant('{localappdata}') + '\Programs\Ollama\ollama.exe');
end;

procedure Button1Click(Sender: TObject);
var
  ResultCode: Integer;
begin
  ShellExec('open', 'https://ollama.com/download', '', '', SW_SHOWNORMAL, ewNoWait, ResultCode);
end;

procedure CreateOllamaWarningPage(Page: TWizardPage);
var
  TitleLabel, InfoLabel1, InfoLabel2, InfoLabel3, InfoLabel4: TLabel;
  DownloadButton: TButton;
begin
  { Title }
  TitleLabel := TLabel.Create(Page);
  TitleLabel.Parent := Page.Surface;
  TitleLabel.Caption := 'No Ollama install was found on your machine.';
  TitleLabel.Font.Style := [fsBold];
  TitleLabel.Font.Size := 12;
  TitleLabel.Left := 10;
  TitleLabel.Top := 10;
  TitleLabel.AutoSize := True;

  { Line 1 }
  InfoLabel1 := TLabel.Create(Page);
  InfoLabel1.Parent := Page.Surface;
  InfoLabel1.Caption :=
    'Avallama can be used with an existing Ollama instance on your network.';
  InfoLabel1.Left := 10;
  InfoLabel1.Top := TitleLabel.Top + TitleLabel.Height + 10;
  InfoLabel1.Width := Page.SurfaceWidth - 20;

  { Line 2 }
  InfoLabel2 := TLabel.Create(Page);
  InfoLabel2.Parent := Page.Surface;
  InfoLabel2.Caption :=
    'In this case, you may safely continue with the installation.';
  InfoLabel2.Left := 10;
  InfoLabel2.Top := InfoLabel1.Top + InfoLabel1.Height + 10;
  InfoLabel2.Width := Page.SurfaceWidth - 20;

  { Line 3 }
  InfoLabel3 := TLabel.Create(Page);
  InfoLabel3.Parent := Page.Surface;
  InfoLabel3.Caption :=
    'Otherwise, please install Ollama before continuing.';
  InfoLabel3.Left := 10;
  InfoLabel3.Top := InfoLabel2.Top + InfoLabel2.Height + 10;
  InfoLabel3.Width := Page.SurfaceWidth - 20;

  { Line 4 }
  InfoLabel4 := TLabel.Create(Page);
  InfoLabel4.Parent := Page.Surface;
  InfoLabel4.Caption :=
    'Click the button below to open the Ollama download page.';
  InfoLabel4.Left := 10;
  InfoLabel4.Top := InfoLabel3.Top + InfoLabel3.Height + 10;
  InfoLabel4.Width := Page.SurfaceWidth - 20;

  { Download button }
  DownloadButton := TButton.Create(Page);
  DownloadButton.Parent := Page.Surface;
  DownloadButton.Caption := 'Open';
  DownloadButton.Top := InfoLabel4.Top + InfoLabel4.Height + 15;
  DownloadButton.Left := 10;
  DownloadButton.OnClick := @Button1Click;
end;

procedure InitializeWizard();
begin
  if not IsOllamaInstalled() then
  begin
    MyPage := CreateCustomPage(wpWelcome, 'Warning', 'No Ollama installation found');
    CreateOllamaWarningPage(MyPage);
  end;
end;

[Files]
Source: "{#SourcePath}\..\win-dist\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\avallama"; Filename: "{app}\avallama.exe"; IconFilename: "{app}\avallama.exe"
Name: "{group}\Uninstall avallama"; Filename: "{uninstallexe}"

[Run]
Filename: "{app}\avallama.exe"; Description: "Launch avallama"; Flags: nowait postinstall

[UninstallDelete]
Type: filesandordirs; Name: "{userappdata}\avallama"
