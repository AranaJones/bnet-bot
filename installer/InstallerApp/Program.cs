using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;
using LibGit2Sharp;

namespace BNetDiscordBridge.Installer;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new InstallerForm());
    }
}

internal sealed class InstallerForm : Form
{
    private const string RepositoryUrl = "https://github.com/AranaJones/bnet-bot.git";
    private static readonly string InstallDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
        "BNetDiscordBridge");

    private readonly Label _titleLabel = new()
    {
        AutoSize = false,
        Dock = DockStyle.Top,
        Height = 56,
        Font = new Font("Segoe UI", 16, FontStyle.Bold),
        Text = "BNetDiscordBridge Setup",
        TextAlign = ContentAlignment.MiddleLeft
    };

    private readonly Label _bodyLabel = new()
    {
        AutoSize = false,
        Dock = DockStyle.Top,
        Height = 120,
        Font = new Font("Segoe UI", 10),
        Text = "This installer will clone the bot from GitHub, build it with .NET 8, install it to C:\\Program Files\\BNetDiscordBridge, and create Start Menu plus Desktop shortcuts.",
        TextAlign = ContentAlignment.TopLeft
    };

    private readonly Label _statusLabel = new()
    {
        AutoSize = false,
        Dock = DockStyle.Top,
        Height = 28,
        Font = new Font("Segoe UI", 10, FontStyle.Bold),
        Text = "Ready to install"
    };

    private readonly ProgressBar _progressBar = new()
    {
        Dock = DockStyle.Top,
        Height = 24,
        Minimum = 0,
        Maximum = 5,
        Visible = false
    };

    private readonly TextBox _logTextBox = new()
    {
        Dock = DockStyle.Fill,
        Multiline = true,
        ReadOnly = true,
        ScrollBars = ScrollBars.Vertical,
        Visible = false
    };

    private readonly Button _installButton = new()
    {
        Text = "Install",
        Width = 96,
        Height = 32
    };

    private readonly Button _cancelButton = new()
    {
        Text = "Cancel",
        Width = 96,
        Height = 32
    };

    private readonly Button _finishButton = new()
    {
        Text = "Close",
        Width = 96,
        Height = 32,
        Visible = false
    };

    public InstallerForm()
    {
        Text = "BNetDiscordBridge Setup";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(720, 480);
        Size = new Size(720, 480);

        var contentPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16)
        };

        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 48,
            FlowDirection = FlowDirection.RightToLeft
        };

        _installButton.Click += async (_, _) => await BeginInstallAsync();
        _cancelButton.Click += (_, _) => Close();
        _finishButton.Click += (_, _) => Close();

        buttonPanel.Controls.Add(_finishButton);
        buttonPanel.Controls.Add(_cancelButton);
        buttonPanel.Controls.Add(_installButton);

        contentPanel.Controls.Add(_logTextBox);
        contentPanel.Controls.Add(_progressBar);
        contentPanel.Controls.Add(_statusLabel);
        contentPanel.Controls.Add(_bodyLabel);
        contentPanel.Controls.Add(_titleLabel);

        Controls.Add(contentPanel);
        Controls.Add(buttonPanel);
    }

    private async Task BeginInstallAsync()
    {
        if (!IsAdministrator())
        {
            MessageBox.Show(
                this,
                "Please run install.exe as an administrator.",
                "Administrator access required",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        _installButton.Enabled = false;
        _cancelButton.Enabled = false;
        _progressBar.Visible = true;
        _logTextBox.Visible = true;
        _bodyLabel.Visible = false;

        try
        {
            await Task.Run(InstallAsync);
            ShowCompletedState();
        }
        catch (Exception ex)
        {
            AppendLog($"ERROR: {ex.Message}");
            MessageBox.Show(
                this,
                ex.Message,
                "Installation failed",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            _installButton.Enabled = true;
            _cancelButton.Enabled = true;
        }
    }

    private void InstallAsync()
    {
        string? cloneDirectory = null;

        try
        {
            UpdateStep(1, "Checking for .NET 8.0 SDK...");
            EnsureDotNet8Sdk();

            UpdateStep(2, "Cloning repository from GitHub...");
            cloneDirectory = CloneRepository();

            UpdateStep(3, "Building the bot...");
            PublishBot(cloneDirectory);

            UpdateStep(4, "Preparing installed files...");
            PrepareInstalledFiles(cloneDirectory);

            UpdateStep(5, "Creating shortcuts...");
            CreateShortcuts();
        }
        finally
        {
            if (!string.IsNullOrWhiteSpace(cloneDirectory) && Directory.Exists(cloneDirectory))
            {
                try
                {
                    Directory.Delete(cloneDirectory, recursive: true);
                }
                catch
                {
                }
            }
        }
    }

    private static bool IsAdministrator()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    private void EnsureDotNet8Sdk()
    {
        var output = RunProcess("dotnet", new[] { "--list-sdks" }, Environment.CurrentDirectory);
        var hasDotNet8Sdk = output
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Any(line => line.TrimStart().StartsWith("8.", StringComparison.Ordinal));

        if (!hasDotNet8Sdk)
        {
            throw new InvalidOperationException(
                ".NET 8.0 SDK is required. Please install it from https://dotnet.microsoft.com/download/dotnet/8.0 and run install.exe again.");
        }
    }

    private string CloneRepository()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "BNetDiscordBridge-Installer");
        Directory.CreateDirectory(tempRoot);

        var cloneDirectory = Path.Combine(tempRoot, Guid.NewGuid().ToString("N"));
        Repository.Clone(RepositoryUrl, cloneDirectory);
        AppendLog($"Cloned repository to {cloneDirectory}");
        return cloneDirectory;
    }

    private void PublishBot(string cloneDirectory)
    {
        Directory.CreateDirectory(InstallDirectory);

        var projectPath = Path.Combine(cloneDirectory, "bnet-bot.csproj");
        RunProcess(
            "dotnet",
            new[]
            {
                "publish",
                projectPath,
                "-c", "Release",
                "-o", InstallDirectory,
                "-r", "win-x64",
                "--self-contained", "true",
                "/p:PublishSingleFile=true",
                "/p:PublishReadyToRun=true",
                "/p:DebugType=None",
                "/p:DebugSymbols=false"
            },
            cloneDirectory);
    }

    private void PrepareInstalledFiles(string cloneDirectory)
    {
        var exampleConfigPath = Path.Combine(cloneDirectory, "appsettings.example.json");
        var installedExampleConfigPath = Path.Combine(InstallDirectory, "appsettings.example.json");
        var installedConfigPath = Path.Combine(InstallDirectory, "appsettings.json");
        var readmeSourcePath = Path.Combine(cloneDirectory, "README.md");
        var readmeTargetPath = Path.Combine(InstallDirectory, "README.md");

        File.Copy(exampleConfigPath, installedExampleConfigPath, overwrite: true);
        File.Copy(readmeSourcePath, readmeTargetPath, overwrite: true);

        if (!File.Exists(installedConfigPath))
        {
            File.Copy(exampleConfigPath, installedConfigPath);
            AppendLog("Created appsettings.json from appsettings.example.json");
        }
    }

    private void CreateShortcuts()
    {
        var executablePath = Path.Combine(InstallDirectory, "bnet-bot.exe");
        var startMenuDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms),
            "BNetDiscordBridge");
        var desktopShortcutPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory),
            "BNetDiscordBridge.lnk");

        Directory.CreateDirectory(startMenuDirectory);

        CreateShortcut(
            Path.Combine(startMenuDirectory, "BNetDiscordBridge.lnk"),
            executablePath,
            InstallDirectory,
            "Run BNetDiscordBridge");

        CreateShortcut(
            desktopShortcutPath,
            executablePath,
            InstallDirectory,
            "Run BNetDiscordBridge");
    }

    private static void CreateShortcut(string shortcutPath, string targetPath, string workingDirectory, string description)
    {
        var shellType = Type.GetTypeFromProgID("WScript.Shell")
            ?? throw new InvalidOperationException("Windows shortcut support is unavailable on this machine.");

        var shell = Activator.CreateInstance(shellType)
            ?? throw new InvalidOperationException("Unable to initialize the Windows shortcut shell.");

        object? shortcut = null;

        try
        {
            shortcut = shellType.InvokeMember("CreateShortcut", System.Reflection.BindingFlags.InvokeMethod, null, shell, new object[] { shortcutPath });
            if (shortcut == null)
            {
                throw new InvalidOperationException($"Unable to create shortcut at {shortcutPath}.");
            }

            var shortcutType = shortcut.GetType();
            shortcutType.InvokeMember("TargetPath", System.Reflection.BindingFlags.SetProperty, null, shortcut, new object[] { targetPath });
            shortcutType.InvokeMember("WorkingDirectory", System.Reflection.BindingFlags.SetProperty, null, shortcut, new object[] { workingDirectory });
            shortcutType.InvokeMember("Description", System.Reflection.BindingFlags.SetProperty, null, shortcut, new object[] { description });
            shortcutType.InvokeMember("Save", System.Reflection.BindingFlags.InvokeMethod, null, shortcut, null);
        }
        finally
        {
            if (shortcut != null && Marshal.IsComObject(shortcut))
            {
                Marshal.FinalReleaseComObject(shortcut);
            }

            if (Marshal.IsComObject(shell))
            {
                Marshal.FinalReleaseComObject(shell);
            }
        }
    }

    private string RunProcess(string fileName, IEnumerable<string> arguments, string workingDirectory)
    {
        var output = new System.Text.StringBuilder();
        var startInfo = new ProcessStartInfo(fileName)
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = new Process { StartInfo = startInfo };
        process.OutputDataReceived += (_, args) =>
        {
            if (!string.IsNullOrWhiteSpace(args.Data))
            {
                output.AppendLine(args.Data);
                AppendLog(args.Data);
            }
        };
        process.ErrorDataReceived += (_, args) =>
        {
            if (!string.IsNullOrWhiteSpace(args.Data))
            {
                output.AppendLine(args.Data);
                AppendLog(args.Data);
            }
        };

        if (!process.Start())
        {
            throw new InvalidOperationException($"Failed to start {fileName}.");
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"{fileName} exited with code {process.ExitCode}.");
        }

        return output.ToString();
    }

    private void UpdateStep(int step, string message)
    {
        InvokeOnUiThread(() =>
        {
            _progressBar.Value = Math.Min(step, _progressBar.Maximum);
            _statusLabel.Text = message;
            AppendLog(message);
        });
    }

    private void ShowCompletedState()
    {
        InvokeOnUiThread(() =>
        {
            _statusLabel.Text = "Installation complete";
            _titleLabel.Text = "BNetDiscordBridge is installed";
            _bodyLabel.Text = $"Installed to {InstallDirectory}{Environment.NewLine}{Environment.NewLine}Edit appsettings.json before starting the bot.";
            _bodyLabel.Visible = true;
            _cancelButton.Visible = false;
            _finishButton.Visible = true;
            AppendLog("Installation complete.");
        });
    }

    private void AppendLog(string message)
    {
        InvokeOnUiThread(() =>
        {
            _logTextBox.AppendText(message + Environment.NewLine);
        });
    }

    private void InvokeOnUiThread(Action action)
    {
        if (InvokeRequired)
        {
            Invoke(action);
            return;
        }

        action();
    }
}
