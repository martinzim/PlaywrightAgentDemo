# DemoPlaywrightAgent

Demo repo pre AI-riadene browser testovanie firemnej webovej stranky cez `Microsoft.Playwright`, `Microsoft Agent Framework` provider abstraction a `.NET Aspire AppHost`.

## Co repo obsahuje

- `src/DemoWeb`: sample firemna ASP.NET Core Razor Pages aplikacia.
- `src/AiBrowserTester`: AI runner, ktory vezme prompt a prevedie ho na bezpecny zoznam browser krokov.
- `src/Demo.AppHost`: .NET Aspire orchestration pre Windows-native aj containerized demo.
- `src/Demo.ServiceDefaults`: shared defaults pre HTTP service projekty.
- `tests/AiBrowserTester.Tests`: unit testy pre validaciu planu a report writer.

## Rezimy behu

### 1. Windows-native demo s Edge

```powershell
$env:DEMO_PROFILE = "local-windows"
$env:AI__Endpoint = "http://localhost:11434"
$env:AI__Model = "llama3.2"
dotnet run --project .\src\Demo.AppHost\Demo.AppHost.csproj
```

### 2. Containerized runner s Chromium

```powershell
$env:DEMO_PROFILE = "local-containerized-runner"
$env:AI__Endpoint = "http://host.docker.internal:11434"
$env:AI__Model = "llama3.2"
dotnet run --project .\src\Demo.AppHost\Demo.AppHost.csproj
```

### 3. CI / Azure DevOps Server agent

```powershell
$env:DEMO_PROFILE = "ci-agent"
$env:AI__Endpoint = "http://ollama.internal:11434"
$env:AI__Model = "llama3.2"
dotnet run --project .\src\Demo.AppHost\Demo.AppHost.csproj
```

## Instalacia Playwright browserov pre native beh

```powershell
pwsh .\eng\install-playwright.ps1
```

Na Linuxe:

```bash
./eng/install-playwright.sh
```

## Demo prompty

- `src/AiBrowserTester/prompts/homepage-smoke.md`
- `src/AiBrowserTester/prompts/contact-form.md`
- `src/AiBrowserTester/prompts/invalid-login.md`

## Kontajnerovy image

Runner Dockerfile pouziva oficialny Playwright .NET image `mcr.microsoft.com/playwright/dotnet:v1.58.0-noble`, aby boli Chromium a system dependencies pripravené hned po builde.
