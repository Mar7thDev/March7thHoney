#!/usr/bin/env bash
set -euo pipefail
# M7H_RELEASE_RUNNER_VERSION=1

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
CONFIGURATION="${M7H_CONFIGURATION:-Release}"
PROJECT="$ROOT_DIR/Program/Program.csproj"
APP_ARGS=()

while [ "$#" -gt 0 ]; do
  case "$1" in
    -c|--configuration)
      if [ "$#" -lt 2 ]; then
        echo "$1 requires Debug or Release"
        exit 1
      fi
      CONFIGURATION="$2"
      shift 2
      ;;
    -h|--help)
      cat <<EOF
Usage: bash scripts/run-server.sh [--configuration Release|Debug] [-- app args]

Runs the selected published release, or builds from this checkout if none exists.
EOF
      exit 0
      ;;
    --)
      shift
      APP_ARGS+=("$@")
      break
      ;;
    *)
      APP_ARGS+=("$1")
      shift
      ;;
  esac
done

if ! command -v dotnet >/dev/null 2>&1; then
  echo "dotnet was not found. On Debian, run: bash scripts/setup-debian.sh"
  exit 1
fi

cd "$ROOT_DIR"
mkdir -p Config/Database Config/Logs

# The update helper selects a prebuilt immutable release. Keep the checkout root
# as cwd so Config, resources and databases retain their existing locations.
if command -v git >/dev/null 2>&1; then
  GIT_DIR="$(git rev-parse --absolute-git-dir 2>/dev/null || true)"
  if [ -n "$GIT_DIR" ] && [ -L "$GIT_DIR/march7thhoney-deploy/current" ]; then
    RELEASE="$GIT_DIR/march7thhoney-deploy/current"
    if [ ! -s "$RELEASE/March7thHoney.dll" ]; then
      echo "Selected release is incomplete; restore the previous release before starting." >&2
      exit 1
    fi
    exec dotnet "$RELEASE/March7thHoney.dll" "${APP_ARGS[@]}"
  fi
fi

exec dotnet run --project "$PROJECT" -c "$CONFIGURATION" -- "${APP_ARGS[@]}"
