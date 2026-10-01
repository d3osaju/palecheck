#!/usr/bin/env bash
# Vercel build: install the .NET SDK, publish the Blazor app into dist/, then add a CSP
# that allows exactly the inline scripts the publish step generated.
set -euo pipefail
export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1 DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

if ! command -v dotnet >/dev/null 2>&1; then
  curl -sSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
  bash /tmp/dotnet-install.sh --channel 10.0 --install-dir "$HOME/.dotnet"
  export PATH="$HOME/.dotnet:$PATH"
fi
export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1

dotnet publish src/PaleCheck.Web -c Release -o out
rm -rf dist
cp -r out/wwwroot dist
node tools/add-csp.mjs dist/index.html
