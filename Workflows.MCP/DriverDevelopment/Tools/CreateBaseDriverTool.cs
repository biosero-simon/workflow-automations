using System;
using System.Diagnostics;
using System.IO;
using ModelContextProtocol.Server;
using System.ComponentModel;

[McpServerToolType]
public static class CreateBaseDriverTool
{
    [McpServerTool, Description("Creates a new solution and a base implementation for a new driver within a new driver repository. This tool assumes that the working directory is the root of the driver repository and that the GBG_FAST template is installed.")]
    public static string CreateBaseDriver(
        [Description("Name of the manufacturer. Human readable.")] string manufacturerName,
        [Description("Name of the instrument. Human readable.")] string instrumentName)
    {
        // Ensure that we have the driver template installed by checking on the dotnet templates
        // The template short name is 'GBG_FAST', check that it exists

        Console.WriteLine("[LOG] Starting driver template check...");

        Console.WriteLine("[LOG] Checking for GBG_FAST template...");
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "new list",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = false
        };
        Console.WriteLine($"[LOG] Running: dotnet new list");
        var process = Process.Start(psi);
        if (process == null)
        {
            Console.WriteLine("[ERROR] Failed to start dotnet process.");
            return "FAILURE: Failed to start dotnet process to check for templates. Ensure dotnet CLI is installed and accessible.";
        }

        // Start reading output asynchronously BEFORE waiting for exit
        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();

        if (!process.WaitForExit(30000)) // 30 second timeout
        {
            process.Kill();
            Console.WriteLine("[ERROR] dotnet new list timed out.");
            return "FAILURE: Dotnet template check timed out after 30 seconds. The system may be overloaded or dotnet CLI is not responding.";
        }

        // Now get the results of the async reads
        var output = outputTask.Result;
        var error = errorTask.Result;
        Console.WriteLine("[LOG] dotnet new list exited. Reading output...");
        Console.WriteLine($"[LOG] dotnet new list output:\n{output}");
        if (!string.IsNullOrWhiteSpace(error))
            Console.WriteLine($"[LOG] dotnet new list error:\n{error}");

        if (!output.Contains("GBG_FAST"))
        {
            Console.WriteLine("[ERROR] GBG_FAST template not found.");
            return "FAILURE: The GBG_FAST template is not installed. Please install it using 'dotnet new --install GBG_FAST'. For more information on installing the template, see the README at https://github.com/biosero/gbgdriver-project-templates.git";
        }
        Console.WriteLine("[LOG] GBG_FAST template found.");

        // Take the spaces out of the manufacturer and instrument names
        var manufacturerNameWithoutSpace = manufacturerName.Replace(" ", "");
        var instrumentNameWithoutSpace = instrumentName.Replace(" ", "");

        // Create a new solution using dotnet new
        Console.WriteLine("[LOG] Creating new solution...");
        psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"new sln -n \"{manufacturerNameWithoutSpace}.{instrumentNameWithoutSpace}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        Console.WriteLine($"[LOG] Running: dotnet new sln -n {manufacturerNameWithoutSpace}.{instrumentNameWithoutSpace}");
        process = Process.Start(psi);
        if (process == null)
        {
            Console.WriteLine("[ERROR] Failed to start dotnet process for solution creation.");
            return "FAILURE: Failed to start dotnet process to create solution. Ensure dotnet CLI is installed and accessible.";
        }
        process.WaitForExit();
        output = process.StandardOutput.ReadToEnd();
        error = process.StandardError.ReadToEnd();
        Console.WriteLine($"[LOG] dotnet new sln output:\n{output}");
        if (!string.IsNullOrWhiteSpace(error))
            Console.WriteLine($"[LOG] dotnet new sln error:\n{error}");
        if (process.ExitCode != 0)
        {
            Console.WriteLine("[ERROR] Error creating solution.");
            return $"FAILURE: Error creating solution. Dotnet CLI returned exit code {process.ExitCode}. Error: {error}";
        }
        Console.WriteLine("[LOG] Solution created successfully.");

        // Create the new driver repository using the GBG_FAST template within the src folder
        var originalDirectory = Directory.GetCurrentDirectory();
        var srcFolder = Path.Combine(originalDirectory, "src");
        if (!Directory.Exists(srcFolder))
        {
            Console.WriteLine("[LOG] Creating src directory...");
            Directory.CreateDirectory(srcFolder);
        }

        try
        {
            Directory.SetCurrentDirectory(srcFolder);
            Console.WriteLine("[LOG] Creating driver project using GBG_FAST template...");
            psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"new GBG_FAST -n \"{manufacturerNameWithoutSpace}.{instrumentNameWithoutSpace}.Driver\" -I {instrumentNameWithoutSpace} -M {manufacturerNameWithoutSpace}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = srcFolder
            };
            Console.WriteLine($"[LOG] Running: dotnet new GBG_FAST -n {manufacturerNameWithoutSpace}.{instrumentNameWithoutSpace}.Driver -I {instrumentNameWithoutSpace} -M {manufacturerNameWithoutSpace}");
            process = Process.Start(psi);
            if (process == null)
            {
                Console.WriteLine("[ERROR] Failed to start dotnet process for driver project creation.");
                return "FAILURE: Failed to start dotnet process to create driver project. Ensure dotnet CLI is installed and accessible.";
            }
            if (!process.WaitForExit(60000)) // 60 second timeout for template creation
            {
                process.Kill();
                Console.WriteLine("[ERROR] dotnet new GBG_FAST timed out.");
                return "FAILURE: Driver project creation timed out after 60 seconds. The template creation process took too long.";
            }
            Console.WriteLine("[LOG] dotnet new GBG_FAST exited. Reading output...");
            output = process.StandardOutput.ReadToEnd();
            error = process.StandardError.ReadToEnd();
            Console.WriteLine($"[LOG] dotnet new GBG_FAST output:\n{output}");
            if (!string.IsNullOrWhiteSpace(error))
                Console.WriteLine($"[LOG] dotnet new GBG_FAST error:\n{error}");
            if (process.ExitCode != 0)
            {
                Console.WriteLine("[ERROR] Error creating driver repository.");
                return $"FAILURE: Error creating driver project from GBG_FAST template. Dotnet CLI returned exit code {process.ExitCode}. Error: {error}";
            }
            Console.WriteLine("[LOG] Driver project created successfully.");
        }
        finally
        {
            // Always restore the original directory
            Directory.SetCurrentDirectory(originalDirectory);
            Console.WriteLine("[LOG] Restored original directory.");
        }

        // Add the new driver project to the solution (from original directory)
        var projectName = $"{manufacturerNameWithoutSpace}.{instrumentNameWithoutSpace}.Driver";
        var projectPath = Path.Combine("src", $"{projectName}.csproj");
        Console.WriteLine("[LOG] Adding driver project to solution...");
        psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"sln add \"{projectPath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = originalDirectory
        };
        Console.WriteLine($"[LOG] Running: dotnet sln add {projectPath}");
        process = Process.Start(psi);
        if (process == null)
        {
            Console.WriteLine("[ERROR] Failed to start dotnet process for adding project to solution.");
            return "FAILURE: Failed to start dotnet process to add project to solution. Ensure dotnet CLI is installed and accessible.";
        }
        if (!process.WaitForExit(30000)) // 30 second timeout
        {
            process.Kill();
            Console.WriteLine("[ERROR] dotnet sln add timed out.");
            return "FAILURE: Adding project to solution timed out after 30 seconds. The process took too long to complete.";
        }
        Console.WriteLine("[LOG] dotnet sln add exited. Reading output...");
        output = process.StandardOutput.ReadToEnd();
        error = process.StandardError.ReadToEnd();
        Console.WriteLine($"[LOG] dotnet sln add output:\n{output}");
        if (!string.IsNullOrWhiteSpace(error))
            Console.WriteLine($"[LOG] dotnet sln add error:\n{error}");
        if (process.ExitCode != 0)
        {
            Console.WriteLine("[ERROR] Error adding project to solution.");
            return $"FAILURE: Error adding project to solution. Dotnet CLI returned exit code {process.ExitCode}. Error: {error}";
        }
        Console.WriteLine("[LOG] Project added to solution successfully.");
        
        // Return success message
        Console.WriteLine("SUCCESS!");
        var successMessage = $"SUCCESS: Base driver '{manufacturerNameWithoutSpace}.{instrumentNameWithoutSpace}.Driver' has been successfully created! " +
                           $"The solution '{manufacturerNameWithoutSpace}.{instrumentNameWithoutSpace}.sln' has been created with the driver project added. " +
                           $"You can find the driver project in the 'src' folder. The project is ready for development and includes all necessary boilerplate code from the GBG_FAST template.";
        return successMessage;
    }
}
