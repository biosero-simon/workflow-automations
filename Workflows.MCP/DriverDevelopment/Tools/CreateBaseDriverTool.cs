using System;
using System.Diagnostics;
using System.IO;
using ModelContextProtocol.Server;
using System.ComponentModel;

[McpServerToolType]
public static class CreateBaseDriverTool
{
    [McpServerTool, Description("Creates the base implementation for a new driver within a new driver repository. This tool assumes that the working directory is the root of the driver repository and that the GBG_FAST template is installed.")]
    public static string CreateBaseDriver(
        [Description("Name of the manufacturer. Human readable.")] string manufacturerName,
        [Description("Name of the instrument. Human readable.")] string instrumentName)
    {
        // Ensure that we have the driver template installed by checking on the dotnet templates
        // The template short name is 'GBG_FAST', check that it exists
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "new --list",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        var process = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start dotnet process.");
        process.WaitForExit();
        var output = process.StandardOutput.ReadToEnd();

        if (!output.Contains("GBG_FAST"))
        {
            throw new InvalidOperationException("The GBG_FAST template is not installed. Please install it using 'dotnet new --install GBG_FAST'. For more information on installing the template, the README within 'https://github.com/biosero/gbgdriver-project-templates.git' can be helpful.");
        }

        // Take the spaces out of the manufacturer and instrument names
        var manufacturerNameWithoutSpace = manufacturerName.Replace(" ", "");
        var instrumentNameWithoutSpace = instrumentName.Replace(" ", "");

        // Create a new solution using dotnet new
        psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"new sln -n \"{manufacturerNameWithoutSpace}.{instrumentNameWithoutSpace}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        process = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start dotnet process.");
        process.WaitForExit();
        output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        if (process.ExitCode != 0)
        {
            throw new Exception($"Error creating solution: {error}");
        }

        // Create the new driver repository using the GBG_FAST template within the src folder
        var srcFolder = Path.Combine(Directory.GetCurrentDirectory(), "src");
        if (!Directory.Exists(srcFolder))
        {
            Directory.CreateDirectory(srcFolder);
        }
        Directory.SetCurrentDirectory(srcFolder);
        psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"new GBG_FAST -n \"{manufacturerNameWithoutSpace}.{instrumentNameWithoutSpace}.Driver\" -I {instrumentNameWithoutSpace} -M {manufacturerNameWithoutSpace}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        process = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start dotnet process.");
        process.WaitForExit();

        output = process.StandardOutput.ReadToEnd();
        error = process.StandardError.ReadToEnd();
        if (process.ExitCode != 0)
        {
            throw new Exception($"Error creating driver repository: {error}");
        }
        // Add the new driver project to the solution
        var projectName = $"{manufacturerNameWithoutSpace}.{instrumentNameWithoutSpace}.Driver";
        psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"sln add \"{projectName}.csproj\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        process = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start dotnet process.");
        process.WaitForExit();
        output = process.StandardOutput.ReadToEnd();
        error = process.StandardError.ReadToEnd();
        if (process.ExitCode != 0)
        {
            throw new Exception($"Error adding project to solution: {error}");
        }
        // Return the output of the process
        return output;
    }
}
