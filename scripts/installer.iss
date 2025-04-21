#define AppVersion "0.0.0"

[Setup]
AppName=Avallama
AppVersion={#AppVersion}
DefaultDirName={localappdata}\Avallama
DefaultGroupName=4foureyes
UninstallDisplayIcon={app}\Avallama.exe
OutputDir=installer/Output
OutputBaseFilename=AvallamaSetup
PrivilegesRequired=lowest

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
  Label1, Label2, Label3, Label4: TLabel;
  Button1: TButton;
begin
  Label1 := TLabel.Create(Page);
  Label1.Parent := Page.Surface;
  Label1.Caption := 'No installation of Ollama was found on your system.';
  Label1.Top := 20;
  Label1.Left := 10;
  Label1.AutoSize := True;
  
  Label2 := TLabel.Create(Page);
  Label2.Parent := Page.Surface;
  Label2.Caption := 'An installation of Ollama is required for Avallama to work properly.';
  Label2.Top := 40;
  Label2.Left := 10;
  Label2.AutoSize := True;
  
  Label3 := TLabel.Create(Page);
  Label3.Parent := Page.Surface;
  Label3.Caption := 'Click the button below to open the Ollama download page in your browser.';
  Label3.Top := 60;
  Label3.Left := 10;
  Label3.AutoSize := True;
  
  Label4 := TLabel.Create(Page);
  Label4.Parent := Page.Surface;
  Label4.Caption := 'If Ollama has been installed, please continute with the setup.';
  Label4.Top := 80;
  Label4.Left := 10;
  Label4.AutoSize := True;

  Button1 := TButton.Create(Page);
  Button1.Parent := Page.Surface;
  Button1.Caption := 'Open';
  Button1.Top := Label4.Top + Label4.Height + 10;
  Button1.Left := 10;
  Button1.OnClick := @Button1Click;
end;

procedure InitializeWizard();
begin
  if not IsOllamaInstalled() then
  begin
    MyPage := CreateCustomPage(wpWelcome, 'Warning', 'Avallama requires Ollama to be installed.');
    CreateOllamaWarningPage(MyPage);
  end;
end;

[Files]
Source: "{#SourcePath}\..\win-dist\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\Avallama"; Filename: "{app}\Avallama.exe"; IconFilename: "{app}\Avallama.exe"
Name: "{group}\Uninstall Avallama"; Filename: "{uninstallexe}"

[Run]
Filename: "{app}\Avallama.exe"; Description: "Launch application"; Flags: nowait postinstall
