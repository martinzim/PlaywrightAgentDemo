#!/usr/bin/env bash
set -euo pipefail

PROJECT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)/src/AiBrowserTester/AiBrowserTester.csproj"
dotnet build "$PROJECT"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)/src/AiBrowserTester/bin/Debug/net10.0"
pwsh "$SCRIPT_DIR/playwright.ps1" install --with-deps chromium
