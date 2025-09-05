using System;
using System.Diagnostics;
using System.IO;
using ModelContextProtocol.Server;
using System.ComponentModel;

[McpServerToolType]
public static class CreateNewDriverRepoTool
{
    [McpServerTool, Description("Creates a new driver repository by invoking the New-DriverRepo.ps1 script. This tool should only be used if a new driver repository is needed. If the repo is named after the instrument, the repo exists already.")]
    public static string CreateNewDriverRepo(string manufacturerName, string instrumentName, string? githubAsCodePath = null)
    {
        // Determine GitHubAsCodePath
        githubAsCodePath ??= Environment.GetEnvironmentVariable("GITHUB_AS_CODE_PATH");
        if (string.IsNullOrEmpty(githubAsCodePath))
        {
            throw new ArgumentException("GitHubAsCodePath is not provided and environment variable GITHUB_AS_CODE_PATH is not set.");
        }

        // Locate PowerShell script relative to repo root
        // Determine project root based on tool assembly location
        var assemblyDirRaw = Path.GetDirectoryName(typeof(CreateNewDriverRepoTool).Assembly.Location);
        if (assemblyDirRaw is null)
            throw new InvalidOperationException("Unable to determine assembly directory.");
        var assemblyDir = assemblyDirRaw;
        var repoRoot = Path.GetFullPath(Path.Combine(assemblyDir, "..", "..", "..", "..", ".."));
        var scriptPath = Path.Combine(repoRoot, "Workflows", "NewDriverDevelopment", "scripts", "New-DriverRepo.ps1");
        if (!File.Exists(scriptPath))
            throw new FileNotFoundException($"Script not found at {scriptPath}");

        // Execute PowerShell script
        var psi = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\" -InstrumentName {instrumentName} -ManufacturerName {manufacturerName} -GitHubAsCodePath \"{githubAsCodePath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        // Start and wait for PowerShell process
        var process = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start PowerShell process.");
        process.WaitForExit();
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        if (process.ExitCode != 0)
        {
            throw new Exception($"Error executing script: {error}");
        }

        return output;
    }
}
