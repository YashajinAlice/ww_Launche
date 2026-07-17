using System.Diagnostics;

namespace WwLauncher.Services;

public sealed class GamePathStatus
{
    public required string Path { get; init; }
    public bool IsConfigured => !string.IsNullOrWhiteSpace(Path);
    public bool FileExists { get; init; }
    public bool IsExecutable { get; init; }
    public bool IsValid => IsConfigured && FileExists && IsExecutable;
    public string? Folder { get; init; }
    public required string Message { get; init; }
}

public interface IGameLaunchService
{
    GamePathStatus GetStatus(string? gamePath = null);
    void Launch(string? gamePath = null);
    Task OpenGameFolderAsync(string? gamePath = null);
}

/// <summary>遊戲路徑驗證、啟動與開啟目錄。</summary>
public sealed class GameLaunchService : IGameLaunchService
{
    public GamePathStatus GetStatus(string? gamePath = null)
    {
        var path = (gamePath ?? App.Current.Settings.GamePath)?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(path))
        {
            return new GamePathStatus
            {
                Path = string.Empty,
                FileExists = false,
                IsExecutable = false,
                Folder = null,
                Message = "尚未設定遊戲路徑，請到「設定」指定主程式（.exe）。",
            };
        }

        var folder = Path.GetDirectoryName(path);
        var isExe = string.Equals(Path.GetExtension(path), ".exe", StringComparison.OrdinalIgnoreCase);

        if (!File.Exists(path))
        {
            return new GamePathStatus
            {
                Path = path,
                FileExists = false,
                IsExecutable = isExe,
                Folder = Directory.Exists(folder) ? folder : null,
                Message = "找不到遊戲主程式，請重新選擇路徑。",
            };
        }

        if (!isExe)
        {
            return new GamePathStatus
            {
                Path = path,
                FileExists = true,
                IsExecutable = false,
                Folder = folder,
                Message = "路徑不是 .exe 執行檔。",
            };
        }

        var hint = IsLikelyWutheringWavesClient(path)
            ? "路徑有效（偵測為鳴潮客戶端）。"
            : "路徑有效。";

        return new GamePathStatus
        {
            Path = path,
            FileExists = true,
            IsExecutable = true,
            Folder = folder,
            Message = hint,
        };
    }

    public void Launch(string? gamePath = null)
    {
        var status = GetStatus(gamePath);
        if (!status.IsValid)
        {
            throw new InvalidOperationException(status.Message);
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = status.Path,
            WorkingDirectory = status.Folder ?? Path.GetDirectoryName(status.Path) ?? string.Empty,
            UseShellExecute = true,
        };

        Process.Start(startInfo);
    }

    public async Task OpenGameFolderAsync(string? gamePath = null)
    {
        var status = GetStatus(gamePath);
        if (!status.IsConfigured)
        {
            throw new InvalidOperationException(status.Message);
        }

        var folder = status.Folder;
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
        {
            throw new DirectoryNotFoundException("遊戲目錄不存在。");
        }

        await Windows.System.Launcher.LaunchFolderPathAsync(folder);
    }

    private static bool IsLikelyWutheringWavesClient(string path)
    {
        var name = Path.GetFileName(path);
        return name.Contains("Client-Win64-Shipping", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Wuthering", StringComparison.OrdinalIgnoreCase)
            || name.Contains("ww_", StringComparison.OrdinalIgnoreCase)
            || name.Contains("鸣潮", StringComparison.OrdinalIgnoreCase)
            || name.Contains("鳴潮", StringComparison.OrdinalIgnoreCase);
    }
}
