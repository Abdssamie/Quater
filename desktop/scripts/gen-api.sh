#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
BACKEND_DLL="$ROOT_DIR/../backend/src/Quater.Backend.Api/bin/Debug/net10.0/Quater.Backend.Api.dll"
OPENAPI_JSON="$ROOT_DIR/openapi.json"
OUTPUT_DIR="$ROOT_DIR/src/Quater.Desktop.Api/Generated"

if [[ ! -f "$BACKEND_DLL" ]]; then
  echo "Backend DLL not found at: $BACKEND_DLL"
  echo "Build the backend first: dotnet build ../backend/src/Quater.Backend.Api"
  exit 1
fi

ASPNETCORE_ENVIRONMENT=Development dotnet swagger tofile \
  --output "$OPENAPI_JSON" \
  "$BACKEND_DLL" v1

 bunx @openapitools/openapi-generator-cli generate \
  -i "$OPENAPI_JSON" \
  -g csharp \
  --additional-properties=packageName=Quater.Desktop.Api,targetFramework=net10.0,netCoreProjectFile=true,library=restsharp \
  -o "$OUTPUT_DIR"

echo "OpenAPI client generated in $OUTPUT_DIR"
