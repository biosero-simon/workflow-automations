<#
.SYNOPSIS
    Automates creation of a new driver repository folder in a local clone of github-as-code.

.DESCRIPTION
    This script:
    1. Updates the github-as-code repo (pull main).
    2. Creates a feature branch named feat/add-repo-<instrument>-gbgdriver.
    3. Adds a new folder named <manufacturer>-<instrument>-gbgdriver.
    4. Copies files from WorkflowAutomations Workflows/NewDriverDevelopment into new folder.
    5. Replaces placeholders in repo.yml.

.PARAMETER InstrumentName
    Name of the instrument. Lowercase, no spaces.

.PARAMETER ManufacturerName
    Name of the manufacturer. Lowercase, no spaces.

.PARAMETER GitHubAsCodePath
    Local path to the cloned github-as-code repository.

.EXAMPLE
    .\New-DriverRepo.ps1 -InstrumentName myinstrument -ManufacturerName mymanufacturer -GitHubAsCodePath C:\repos\github-as-code
#>
[CmdletBinding()]
Param(
    [Parameter(Mandatory=$true)]
    [ValidatePattern('^[a-z0-9]+(-[a-z0-9]+)*$')]
    [string]$InstrumentName,

    [Parameter(Mandatory=$true)]
    [ValidatePattern('^[a-z0-9]+(-[a-z0-9]+)*$')]
    [string]$ManufacturerName,

    [Parameter(Mandatory=$false)]
    [string]$GitHubAsCodePath = $Env:GITHUB_AS_CODE_PATH  # default to environment variable GITHUB_AS_CODE_PATH
)

if (-not $GitHubAsCodePath) {
    Write-Error "GitHubAsCodePath is not provided and environment variable GITHUB_AS_CODE_PATH is not set. Please provide the local path to your repo or set the environment variable to the correct path."
    exit 1
}
Write-Host "Using GitHubAsCodePath: '$GitHubAsCodePath'"

# Ensure path exists
if (-not (Test-Path -Path $GitHubAsCodePath)) {
    Write-Error "Path '$GitHubAsCodePath' does not exist."
    exit 1
}

Push-Location $GitHubAsCodePath

# Update main branch
Write-Host "Updating main branch..."
git checkout main
git pull origin main

# Create feature branch
$branchName = "feat/add-repo-$($InstrumentName)-gbgdriver"
Write-Host "Creating branch $branchName..."
git checkout -b $branchName

# Create new repository folder
# build raw folder name
$rawFolderName = "$ManufacturerName-$InstrumentName-gbgdriver"
# enforce lowercase and replace spaces with hyphens
$newFolderName = $rawFolderName.ToLower() -replace '\s+', '-'
$subfolderName = '\data\repos\'
Write-Host "Creating folder '$newFolderName'..."
$targetPath = Join-Path $GitHubAsCodePath $subfolderName
$targetPath = Join-Path $targetPath $newFolderName
New-Item -ItemType Directory -Path $targetPath -Force | Out-Null

# Determine source directory (this script lives under Workflows/NewDriverDevelopment/scripts)
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$sourcePath = Split-Path -Parent $scriptDir  # one level up to NewDriverDevelopment
if (-not (Test-Path -Path $sourcePath)) {
    Write-Error "Source path '$sourcePath' not found (expected Workflows/NewDriverDevelopment)."
    Pop-Location
    exit 1
}

# Copy required files
Write-Host "Copying CODEOWNERS and repo.yml from $sourcePath to $targetPath..."
foreach ($file in @('CODEOWNERS', 'repo.yml')) {
    $srcFile = Join-Path $sourcePath $file
    if (Test-Path -Path $srcFile) {
        Copy-Item -Path $srcFile -Destination $targetPath -Force
    } else {
        Write-Warning "$file not found in source path. Skipping."
    }
}

# Replace placeholders in repo.yml
$repoYmlPath = Join-Path $targetPath 'repo.yml'
if (Test-Path -Path $repoYmlPath) {
    Write-Host "Updating placeholders in repo.yml..."
    (Get-Content $repoYmlPath) |
        ForEach-Object {
            # replace placeholders
            $line = $_ -replace '\{instrumentName\}', $InstrumentName -replace '\{manufacturerName\}', $ManufacturerName
            # enforce lowercase for the repo 'name' field only
            if ($line -match '^[ \t]*name:') {
                $line = $line.ToLower()
            }
            $line
        } |
        Set-Content $repoYmlPath
} else {
    Write-Warning "repo.yml not found in target folder."
}

# Restore location
Pop-Location

Write-Host "Done. New repo folder created at: $targetPath"
Write-Host "Next steps:"
Write-Host "  cd $GitHubAsCodePath; git push -u origin $branchName"
Write-Host "  Open a pull request for branch $branchName."
