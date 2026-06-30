#!/usr/bin/env bash
set -euo pipefail

DOTNET_DIR="${HOME}/.dotnet"
if [ ! -x "${DOTNET_DIR}/dotnet" ]; then
  curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
  bash /tmp/dotnet-install.sh --channel 8.0 --install-dir "${DOTNET_DIR}"
fi

"${DOTNET_DIR}/dotnet" clean HRDocs.Client/HRDocs.Client.csproj \
  --configuration Release

"${DOTNET_DIR}/dotnet" publish HRDocs.Client/HRDocs.Client.csproj \
  --configuration Release \
  --output publish-client
