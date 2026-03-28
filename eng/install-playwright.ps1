$ErrorActionPreference = "Stop"

$project = Join-Path $PSScriptRoot "..\src\AiBrowserTester\AiBrowserTester.csproj"
dotnet build $project
$playwrightScript = Join-Path $PSScriptRoot "..\src\AiBrowserTester\bin\Debug\net10.0\playwright.ps1"

if (-not (Test-Path $playwrightScript)) {
    throw "Playwright install script was not generated. Build the AiBrowserTester project first."
}

pwsh $playwrightScript install msedge chromium
