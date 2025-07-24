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
    Write-Error "GitHubAsCodePath is not provided and environment variable GITHUB_AS_CODE_PATH is not set."
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
$newFolderName = "$ManufacturerName-$InstrumentName-gbgdriver"
Write-Host "Creating folder '$newFolderName'..."
$targetPath = Join-Path $GitHubAsCodePath $newFolderName
New-Item -ItemType Directory -Path $targetPath -Force | Out-Null

# Determine source directory (this script lives under Workflows/NewDriverDevelopment/scripts)
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$sourcePath = Split-Path -Parent $scriptDir  # one level up to NewDriverDevelopment
if (-not (Test-Path -Path $sourcePath)) {
    Write-Error "Source path '$sourcePath' not found (expected Workflows/NewDriverDevelopment)."
    Pop-Location
    exit 1
}

# Copy all files
Write-Host "Copying files from $sourcePath to $targetPath..."
Copy-Item -Path "$sourcePath\*" -Destination $targetPath -Recurse -Force

# Replace placeholders in repo.yml
$repoYmlPath = Join-Path $targetPath 'repo.yml'
if (Test-Path -Path $repoYmlPath) {
    Write-Host "Updating placeholders in repo.yml..."
    (Get-Content $repoYmlPath) |
        ForEach-Object {
            $_ -replace '\{instrumentName\}', $InstrumentName -replace '\{manufacturerName\}', $ManufacturerName
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
