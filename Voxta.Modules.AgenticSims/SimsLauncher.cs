using System.Diagnostics;
using Voxta.Abstractions.Registration;
using Voxta.Abstractions.Repositories;  // IUnitOfWorkFactory
using Voxta.Abstractions.Services.Launchers;

namespace Voxta.Modules.AgenticSims;

/// <summary>
/// The Sims 4 card on Voxta's Play page: finds the game through Voxta's own Steam/EA detection, shows whether it is
/// running, and launches it. It never closes the game and never starts the Agentic Sims companion itself.
/// </summary>
public sealed class SimsLauncher(IUnitOfWorkFactory database, IInstalledGames games) : ILauncher
{
    private static readonly string[] Names = ["The Sims 4", "The Sims™ 4", "The Sims(TM) 4"];
    public string ServiceName => VoxtaModule.ServiceName;
    public ModuleLogo Cover => ModuleLogo.EmbeddedResource(typeof(VoxtaModule), "Assets.cover.png");
    public string ChatBridgeServiceName => null!;

    private static bool GameRunning()
    {
        foreach (var name in new[] { "TS4_x64", "TS4_DX9_x64" })
        {
            var found = Process.GetProcessesByName(name);
            try { if (found.Length > 0) return true; } finally { foreach (var p in found) p.Dispose(); }
        }
        return false;
    }

    public async Task<IReadOnlyList<LauncherStatus>> GetStatusAsync(Guid userId, CancellationToken cancellationToken)
    {
        Voxta.Abstractions.Model.Module[] mine;
        await using (var work = database.CreateUnitOfWork(userId))
            mine = (await work.Modules.GetManyByServiceNameAsync(VoxtaModule.ServiceName, cancellationToken))
                .Where(m => m.UserId == userId && m.Enabled).ToArray();
        if (mine.Length == 0) return [];
        var detected = games.Find(Names);
        var gameRunning = GameRunning() || (detected != null && games.IsRunning(detected) == true);
        var dashboardRunning = Running("AgenticSims");
        return mine.Select(m =>
        {
            var dashboard = Dashboard(m);
            var gameFound = detected != null || ChosenExecutable(m) != null;
            return new LauncherStatus
            {
                ModuleId = m.Id,
                Title = "Agentic Sims · The Sims 4",
                Running = dashboardRunning,
                NotFound = false,
                CanLaunch = true,
                CoverFile = detected?.CoverFile!,
                Detail = dashboard == null ? "Agentic Sims wasn't found at the runtime folder in this module's settings. Launch opens the free download."
                    : gameRunning ? "The Sims 4 is running. Add Agentic Sims to a private chat to talk about your shared Sims."
                    : gameFound ? "Launch opens Agentic Sims. Play The Sims 4 from there."
                    : "Launch opens Agentic Sims. Show it where The Sims 4 is once, from its Home page.",
                Instructions = dashboard == null
                    ? "Click Launch to download Agentic Sims (free), extract the ZIP and run AgenticSims.Setup.exe with The Sims 4 closed. Already installed somewhere else? Set its .runtime folder in this module's settings."
                    : null!,
            };
        }).ToArray();
    }

    private static bool Running(string name)
    {
        var found = Process.GetProcessesByName(name);
        try { return found.Length > 0; } finally { foreach (var p in found) p.Dispose(); }
    }

    /// <summary>The Agentic Sims dashboard next to the configured runtime folder, or in its default install folder.</summary>
    internal static string? Dashboard(Voxta.Abstractions.Model.Module module)
    {
        var roots = new List<string>();
        if (module.Configuration != null && module.Configuration.TryGetValue(ModuleConfigurationProvider.Runtime.Name, out var runtime)
            && Convert.ToString(runtime)?.Trim().Trim('"') is { Length: > 0 } folder && Path.IsPathFullyQualified(folder))
            roots.Add(Path.GetDirectoryName(folder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))!);
        // Only the folder the player configured: guessing could open an old, forgotten copy.
        foreach (var root in roots)
        {
            var exe = Path.Combine(root, "dist", "desktop", "AgenticSims.exe");
            if (File.Exists(exe) && File.Exists(Path.Combine(root, "edition.json"))) return exe;
        }
        return null;
    }

    public async Task LaunchAsync(Guid userId, Guid moduleId, CancellationToken cancellationToken)
    {
        Voxta.Abstractions.Model.Module? module;
        await using (var work = database.CreateUnitOfWork(userId))
            module = (await work.Modules.GetManyByServiceNameAsync(VoxtaModule.ServiceName, cancellationToken))
                .FirstOrDefault(m => m.Id == moduleId && m.UserId == userId);
        var dashboard = module == null ? null : Dashboard(module);
        if (dashboard == null)
        {
            // Not installed: open the friendly download page (fixed address, never one from configuration).
            Process.Start(new ProcessStartInfo(DownloadPage) { UseShellExecute = true })?.Dispose();
            return;
        }
        // Starting it again brings an open dashboard forward (one per installation).
        Process.Start(new ProcessStartInfo(dashboard) { UseShellExecute = true, WorkingDirectory = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(dashboard)))! })?.Dispose();
    }

    public const string DownloadPage = "https://seeingterra.github.io/agentic-sims/#download";

    /// <summary>The player's own game path: TS4_x64.exe itself, or The Sims 4 folder that contains Game\Bin\TS4_x64.exe.</summary>
    internal static string? ChosenExecutable(Voxta.Abstractions.Model.Module module)
    {
        if (module.Configuration is null || !module.Configuration.TryGetValue(ModuleConfigurationProvider.GamePath.Name, out var value)) return null;
        var path = Convert.ToString(value)?.Trim().Trim('"');
        if (string.IsNullOrEmpty(path) || !Path.IsPathFullyQualified(path)) return null;
        foreach (var candidate in new[] { path, Path.Combine(path, "Game", "Bin", "TS4_x64.exe"), Path.Combine(path, "TS4_x64.exe") })
            if (File.Exists(candidate) && Path.GetFileName(candidate).StartsWith("TS4_", StringComparison.OrdinalIgnoreCase)
                && candidate.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) return candidate;
        return null;
    }
}
