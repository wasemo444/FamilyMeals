#!/usr/bin/env bash
# Publishes LinkNest.Web.Client for Cloudflare Pages (Linux / GitHub Actions).
set -euo pipefail

API_BASE_URL="${1:-}"
REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
CLIENT_PROJECT="$REPO_ROOT/src/LinkNest.Web/LinkNest.Web.Client/LinkNest.Web.Client.csproj"
APPSETTINGS="$REPO_ROOT/src/LinkNest.Web/LinkNest.Web.Client/wwwroot/appsettings.json"
OUTPUT_PATH="${OUTPUT_PATH:-$REPO_ROOT/publish/static-web}"
STAGING_PATH="$(mktemp -d)"

cleanup() {
  rm -rf "$STAGING_PATH"
  if [[ -n "${APPSETTINGS_BACKUP:-}" && -f "$APPSETTINGS" ]]; then
    printf '%s' "$APPSETTINGS_BACKUP" > "$APPSETTINGS"
  fi
}
trap cleanup EXIT

if [[ -n "$API_BASE_URL" ]]; then
  API_BASE_URL="${API_BASE_URL%/}"
  if [[ -f "$APPSETTINGS" ]]; then
    APPSETTINGS_BACKUP="$(cat "$APPSETTINGS")"
    python3 - <<PY
import json, pathlib
path = pathlib.Path(r"$APPSETTINGS")
data = json.loads(path.read_text(encoding="utf-8"))
data["ApiBaseUrl"] = r"$API_BASE_URL"
path.write_text(json.dumps(data, indent=2) + "\n", encoding="utf-8")
PY
  fi
fi

dotnet publish "$CLIENT_PROJECT" -c Release -o "$STAGING_PATH"

if [[ ! -f "$STAGING_PATH/wwwroot/index.html" ]]; then
  echo "Publish did not produce wwwroot/index.html" >&2
  exit 1
fi

rm -rf "$OUTPUT_PATH"
mkdir -p "$OUTPUT_PATH"
cp -R "$STAGING_PATH/wwwroot/." "$OUTPUT_PATH/"

echo ""
echo "Static web published to: $OUTPUT_PATH"
echo "Cloudflare Pages: upload that folder (index.html at the root)."
if [[ -n "$API_BASE_URL" ]]; then
  echo "ApiBaseUrl: $API_BASE_URL"
fi
