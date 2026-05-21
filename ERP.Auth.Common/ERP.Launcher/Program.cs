using Microsoft.Extensions.Configuration;
using System.Diagnostics;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// ─────────────────────────────────────────────────────────────────────────────
// 設定讀取：優先找 CWD 下的 appsettings.json，再找 exe 目錄
// ─────────────────────────────────────────────────────────────────────────────
var configBase = File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json"))
    ? Directory.GetCurrentDirectory()
    : AppContext.BaseDirectory;

var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

var config = new ConfigurationBuilder()
    .SetBasePath(configBase)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .AddJsonFile($"appsettings.{env}.json", optional: true, reloadOnChange: false)
    .Build();

var launcher = config.GetSection("Launcher").Get<LauncherConfig>()
    ?? throw new InvalidOperationException("appsettings.json 缺少 Launcher 區段");

// ─────────────────────────────────────────────────────────────────────────────
// Ctrl+C 優雅關閉
// ─────────────────────────────────────────────────────────────────────────────
using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    Console.WriteLine("\n[Launcher] 收到 Ctrl+C，正在關閉所有系統...");
    cts.Cancel();
};

// ─────────────────────────────────────────────────────────────────────────────
// 啟動橫幅
// ─────────────────────────────────────────────────────────────────────────────
Console.ForegroundColor = ConsoleColor.White;
Console.WriteLine();
Console.WriteLine("  ╔══════════════════════════════════════════════════╗");
Console.WriteLine("  ║        ERP 主檔系統啟動器 v1.0                  ║");
Console.WriteLine($"  ║  erp_plus    → http://192.168.168.15            ║");
Console.WriteLine($"  ║  mode        → {launcher.Mode,-36}║");
Console.WriteLine($"  ║  config base → {ShortenPath(configBase),-36}║");
Console.WriteLine("  ╚══════════════════════════════════════════════════╝");
Console.ResetColor();
Console.WriteLine();

// ─────────────────────────────────────────────────────────────────────────────
// Pre-build 階段：循序建置，避免並行寫入 obj/ 目錄衝突
// ─────────────────────────────────────────────────────────────────────────────
if (launcher.Mode == "dev")
{
    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.WriteLine("  [Pre-build] 循序建置中（避免共用 obj/ 衝突）...");
    Console.ResetColor();

    bool buildOk = true;
    foreach (var app in launcher.Apps)
    {
        if (string.IsNullOrWhiteSpace(app.ProjectPath)) continue;
        var projectPath = Path.GetFullPath(Path.Combine(configBase, app.ProjectPath));
        if (!Directory.Exists(projectPath)) continue;

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write($"  building {app.Name,-20}...");
        Console.ResetColor();

        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"build \"{projectPath}\" -c Debug --nologo -v q",
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute = false,
            CreateNoWindow  = true,
        };

        using var buildProc = Process.Start(psi)!;
        var buildOut = await buildProc.StandardOutput.ReadToEndAsync();
        var buildErr = await buildProc.StandardError.ReadToEndAsync();
        await buildProc.WaitForExitAsync();

        if (buildProc.ExitCode == 0)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("  ✓");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("  ✗ 建置失敗");
            Console.ResetColor();
            // 輸出錯誤行（只印 error，略過 warning）
            foreach (var line in (buildOut + buildErr).Split('\n'))
            {
                var t = line.Trim();
                if (t.Contains("error") || t.Contains("Error"))
                    Console.WriteLine($"    {t}");
            }
            buildOk = false;
        }
    }

    if (!buildOk)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("\n[Launcher] Pre-build 失敗，無法啟動。請修正上方錯誤後重試。");
        Console.ResetColor();
        return;
    }

    Console.WriteLine();
}

// ─────────────────────────────────────────────────────────────────────────────
// 啟動所有子系統
// ─────────────────────────────────────────────────────────────────────────────
ConsoleColor[] palette =
[
    ConsoleColor.Cyan, ConsoleColor.Green,
    ConsoleColor.Yellow, ConsoleColor.Magenta, ConsoleColor.DarkCyan
];

var processes = new List<(ErpApp App, Process Proc, ConsoleColor Color)>();
var outputLock = new object();

for (int i = 0; i < launcher.Apps.Length; i++)
{
    var app  = launcher.Apps[i];
    var color = palette[i % palette.Length];

    var proc = launcher.Mode == "dev"
        ? StartDevApp(app, configBase)
        : StartDeployedApp(app);

    if (proc is null)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"  ✗ {app.Name,-20} 路徑不存在，請確認 appsettings.json 設定。");
        Console.ResetColor();
        continue;
    }

    processes.Add((app, proc, color));

    var appColor = color; // capture for lambda
    var appName  = app.Name;

    proc.OutputDataReceived += (_, e) =>
    {
        if (e.Data is null) return;
        lock (outputLock)
        {
            Console.ForegroundColor = appColor;
            Console.Write($"[{appName,-18}] ");
            Console.ResetColor();
            Console.WriteLine(e.Data);
        }
    };

    proc.ErrorDataReceived += (_, e) =>
    {
        if (e.Data is null) return;
        lock (outputLock)
        {
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.Write($"[{appName,-18}] ");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(e.Data);
            Console.ResetColor();
        }
    };

    proc.BeginOutputReadLine();
    proc.BeginErrorReadLine();

    Console.ForegroundColor = color;
    Console.Write($"  ✓ {app.Name,-20}");
    Console.ResetColor();
    Console.WriteLine($"→ {app.Url}");
}

if (processes.Count == 0)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("[Launcher] 沒有任何系統成功啟動，請確認設定。");
    Console.ResetColor();
    return;
}

Console.WriteLine();
Console.ForegroundColor = ConsoleColor.DarkGray;
Console.WriteLine("  按 Ctrl+C 停止所有系統。各系統 log 顯示如下：");
Console.ResetColor();
Console.WriteLine();

// ─────────────────────────────────────────────────────────────────────────────
// 等待取消或所有子程序結束
// ─────────────────────────────────────────────────────────────────────────────
var waitTasks = processes.Select(p =>
    p.Proc.WaitForExitAsync(cts.Token)
          .ContinueWith(_ => { }) // 避免 OperationCanceledException 向上傳播
).ToArray();

await Task.WhenAny(
    Task.WhenAll(waitTasks),
    Task.Delay(Timeout.Infinite, cts.Token).ContinueWith(_ => { })
);

// ─────────────────────────────────────────────────────────────────────────────
// 關閉所有子程序
// ─────────────────────────────────────────────────────────────────────────────
foreach (var (app, proc, _) in processes)
{
    try
    {
        if (!proc.HasExited)
        {
            proc.Kill(entireProcessTree: true);
            Console.WriteLine($"[Launcher] 已停止 {app.Name}");
        }
    }
    catch { /* 已停止 */ }
    proc.Dispose();
}

Console.ForegroundColor = ConsoleColor.White;
Console.WriteLine("[Launcher] 所有系統已停止。");
Console.ResetColor();

// ═════════════════════════════════════════════════════════════════════════════
// 本機函式
// ═════════════════════════════════════════════════════════════════════════════

// dev 模式：dotnet run --no-build --project {projectPath}
// --no-build：pre-build 階段已循序建置，此處直接啟動，不重複 build
static Process? StartDevApp(ErpApp app, string configBase)
{
    if (string.IsNullOrWhiteSpace(app.ProjectPath)) return null;

    var projectPath = Path.GetFullPath(Path.Combine(configBase, app.ProjectPath));
    if (!Directory.Exists(projectPath)) return null;

    var psi = new ProcessStartInfo
    {
        FileName = "dotnet",
        Arguments = $"run --no-build --project \"{projectPath}\"",
        RedirectStandardOutput = true,
        RedirectStandardError  = true,
        UseShellExecute = false,
        CreateNoWindow  = true,
    };

    // --urls 透過環境變數傳入，dotnet run 會轉交給 Kestrel
    psi.EnvironmentVariables["ASPNETCORE_URLS"]        = app.Url;
    psi.EnvironmentVariables["ASPNETCORE_ENVIRONMENT"] = app.Environment ?? "Development";

    return Process.Start(psi);
}

// deployed 模式：dotnet {dll}（dotnet publish 後）
static Process? StartDeployedApp(ErpApp app)
{
    if (string.IsNullOrWhiteSpace(app.DllPath)) return null;

    var dllPath = Path.GetFullPath(app.DllPath);
    if (!File.Exists(dllPath)) return null;

    var psi = new ProcessStartInfo
    {
        FileName = "dotnet",
        Arguments = $"\"{dllPath}\"",
        RedirectStandardOutput = true,
        RedirectStandardError  = true,
        UseShellExecute = false,
        CreateNoWindow  = true,
    };

    psi.EnvironmentVariables["ASPNETCORE_URLS"]        = app.Url;
    psi.EnvironmentVariables["ASPNETCORE_ENVIRONMENT"] = app.Environment ?? "Staging";

    return Process.Start(psi);
}

static string ShortenPath(string path)
{
    if (path.Length <= 36) return path;
    return "..." + path[^33..];
}

// ═════════════════════════════════════════════════════════════════════════════
// 設定模型
// ═════════════════════════════════════════════════════════════════════════════

record LauncherConfig
{
    public string    Mode { get; init; } = "dev";
    public ErpApp[]  Apps { get; init; } = [];
}

record ErpApp
{
    /// <summary>顯示名稱</summary>
    public string  Name        { get; init; } = "";

    /// <summary>dev 模式：相對於 appsettings.json 目錄的專案路徑</summary>
    public string? ProjectPath { get; init; }

    /// <summary>deployed 模式：已 publish 的 dll 路徑（絕對或相對）</summary>
    public string? DllPath     { get; init; }

    /// <summary>Kestrel 監聽位址，e.g. http://0.0.0.0:7068</summary>
    public string  Url         { get; init; } = "http://0.0.0.0:5000";

    /// <summary>ASPNETCORE_ENVIRONMENT 值</summary>
    public string? Environment { get; init; }
}
