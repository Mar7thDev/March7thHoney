#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
CONFIGURATION="${M7H_CONFIGURATION:-Release}"
SERVICE_NAME="${M7H_SERVICE_NAME:-march7thhoney}"
BUILD=1
RESTART_MODE="auto"

usage() {
  cat <<EOF
Usage: bash scripts/update-server.sh [options]

Options:
  --configuration Debug|Release  Build configuration. Default: Release
  --service NAME                 systemd service name. Default: march7thhoney
  --restart                      Restart the service after updating
  --no-restart                   Do not stop/start systemd
  --no-build                     Source-only update; requires --no-restart
  -h, --help                     Show this help
EOF
}

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
    --service)
      if [ "$#" -lt 2 ]; then
        echo "--service requires a name"
        exit 1
      fi
      SERVICE_NAME="$2"
      shift 2
      ;;
    --restart)
      RESTART_MODE="always"
      shift
      ;;
    --no-restart)
      RESTART_MODE="never"
      shift
      ;;
    --no-build)
      BUILD=0
      shift
      ;;
    -h|--help)
      usage
      exit 0
      ;;
    *)
      echo "Unknown option: $1"
      usage
      exit 1
      ;;
  esac
done

case "$CONFIGURATION" in Debug|Release) ;; *) echo "Invalid configuration: $CONFIGURATION" >&2; exit 1;; esac
if [ "$BUILD" -eq 0 ] && [ "$RESTART_MODE" != "never" ]; then
  echo "--no-build requires --no-restart; an unverified build must not restart a service." >&2
  exit 1
fi
for tool in git flock; do
  command -v "$tool" >/dev/null || { echo "$tool was not found." >&2; exit 1; }
done
if [ "$BUILD" -eq 1 ]; then
  command -v dotnet >/dev/null || { echo "dotnet was not found." >&2; exit 1; }
fi
cd "$ROOT_DIR"
STATE_DIR="$(git rev-parse --absolute-git-dir)/march7thhoney-deploy"
mkdir -p "$STATE_DIR/releases"
exec 9>"$STATE_DIR/update.lock"
flock -n 9 || { echo "Another update is in progress." >&2; exit 1; }

# Never overwrite local source/configuration changes or an in-progress Git operation.
if [ -n "$(git status --porcelain --untracked-files=normal)" ]; then
  echo "Commit or stash local changes before updating (ignored runtime data is preserved)." >&2
  exit 1
fi
OLD_HEAD="$(git rev-parse HEAD)"
UPSTREAM="$(git rev-parse --abbrev-ref --symbolic-full-name '@{upstream}')"
git fetch --recurse-submodules=no
NEW_HEAD="$(git rev-parse "$UPSTREAM")"
git merge-base --is-ancestor "$OLD_HEAD" "$NEW_HEAD" || {
  echo "Update is not a fast-forward; resolve branch divergence manually." >&2; exit 1;
}
if [ "$BUILD" -eq 0 ]; then
  git merge --ff-only "$NEW_HEAD"
  git submodule update --init --recursive
  echo "Source updated only. Any selected published release remains unchanged."
  exit 0
fi

# Rollback must never restore a legacy runner which recompiles the failed tree.
# The capability marker is a versioned contract between the two helper scripts.
for commit in "$OLD_HEAD" "$NEW_HEAD"; do
  if ! git show "$commit:scripts/run-server.sh" | grep -qx '# M7H_RELEASE_RUNNER_VERSION=1'; then
    echo "Commit $commit lacks the published-release runner. Update both helper scripts together before managed deployment." >&2
    exit 1
  fi
done

service_active=0
service_known=0
if command -v systemctl >/dev/null && systemctl cat "${SERVICE_NAME}.service" >/dev/null 2>&1; then
  service_known=1
  if systemctl is-active --quiet "${SERVICE_NAME}.service"; then service_active=1; fi
fi
should_restart=0
if [ "$RESTART_MODE" = "always" ] || { [ "$RESTART_MODE" = "auto" ] && [ "$service_active" -eq 1 ]; }; then
  should_restart=1
fi

old_release=""
if [ -L "$STATE_DIR/current" ]; then
  old_release="$(readlink -f "$STATE_DIR/current")"
  for file in March7thHoney.dll March7thHoney.deps.json March7thHoney.runtimeconfig.json; do
    test -s "$old_release/$file" || { echo "Existing rollback release is incomplete: $file" >&2; exit 1; }
  done
fi
STAGING="$(mktemp -d "$STATE_DIR/staging.XXXXXX")"
activated=0
checkout_changed=0
service_stopped=0

select_release() {
  ln -s "$1" "$STATE_DIR/current.next"
  mv -Tf "$STATE_DIR/current.next" "$STATE_DIR/current"
}

finish() {
  result=$?
  trap - EXIT INT TERM
  set +e
  if [ "$result" -ne 0 ] && { [ "$activated" -eq 1 ] || [ "$checkout_changed" -eq 1 ] || [ "$service_stopped" -eq 1 ]; }; then
    echo "Update failed; restoring the previous code and published release." >&2
    can_restore=1
    if [ "$should_restart" -eq 1 ] && [ "$service_known" -eq 1 ]; then
      # Stop a partially started candidate before changing any source/configuration.
      if ! sudo systemctl stop "${SERVICE_NAME}.service"; then
        echo "ERROR: service could not be stopped; rollback needs manual attention." >&2
        can_restore=0
      fi
    fi
    if [ "$can_restore" -eq 1 ]; then
      # --keep refuses to overwrite new local modifications; never reset --hard or git clean runtime data.
      if [ "$checkout_changed" -eq 1 ]; then
        git reset --keep "$OLD_HEAD" || echo "WARNING: source rollback needs manual attention." >&2
        git submodule update --init --recursive || echo "WARNING: submodule rollback needs manual attention." >&2
      fi
      if [ -n "$old_release" ]; then
        rm -f "$STATE_DIR/current.next"
        select_release "$old_release" || echo "ERROR: release rollback needs manual attention." >&2
      fi
      if [ "$should_restart" -eq 1 ] && [ "$service_known" -eq 1 ] && [ "$service_active" -eq 1 ]; then
        sudo systemctl start "${SERVICE_NAME}.service" || echo "ERROR: could not restart the old service." >&2
      fi
    fi
  fi
  if [ -d "$STAGING/source" ]; then git worktree remove --force "$STAGING/source" >/dev/null 2>&1; fi
  rm -rf "$STAGING"
  exit "$result"
}
trap finish EXIT
trap 'exit 130' INT
trap 'exit 143' TERM

publish_snapshot() {
  local commit="$1" output="$2"
  git worktree add --detach "$STAGING/source" "$commit" >/dev/null
  git -C "$STAGING/source" submodule update --init --recursive
  dotnet publish "$STAGING/source/Program/Program.csproj" -c "$CONFIGURATION" \
    --self-contained false -o "$output"
  for file in March7thHoney.dll March7thHoney.deps.json March7thHoney.runtimeconfig.json; do
    test -s "$output/$file" || { echo "Missing publish output: $file" >&2; return 1; }
  done
  git worktree remove --force "$STAGING/source" >/dev/null
}

# All restore/build failures happen before stopping or changing the running checkout.
echo "Publishing candidate $NEW_HEAD in an isolated worktree..."
publish_snapshot "$NEW_HEAD" "$STAGING/candidate"
if [ -z "$old_release" ]; then
  echo "Preparing a rollback release of $OLD_HEAD..."
  publish_snapshot "$OLD_HEAD" "$STAGING/previous"
  old_release="$(mktemp -d "$STATE_DIR/releases/previous.XXXXXX")"
  mv "$STAGING/previous"/* "$old_release/"
fi
new_release="$(mktemp -d "$STATE_DIR/releases/candidate.XXXXXX")"
mv "$STAGING/candidate"/* "$new_release/"
printf '%s\n' "$NEW_HEAD" > "$new_release/source-commit"

# Recheck for edits while staging. Do not silently discard work created during a long build.
if [ "$(git rev-parse HEAD)" != "$OLD_HEAD" ] || [ -n "$(git status --porcelain --untracked-files=normal)" ]; then
  echo "Checkout changed during staging; aborting before deployment." >&2
  exit 1
fi
if [ "$should_restart" -eq 1 ] && [ "$service_known" -eq 1 ] && [ "$service_active" -eq 1 ]; then
  service_stopped=1
  sudo systemctl stop "${SERVICE_NAME}.service"
fi
checkout_changed=1
git merge --ff-only "$NEW_HEAD"
git submodule update --init --recursive
select_release "$new_release"
activated=1
if [ "$should_restart" -eq 1 ] && [ "$service_known" -eq 1 ]; then
  sudo systemctl start "${SERVICE_NAME}.service"
  systemctl is-active --quiet "${SERVICE_NAME}.service"
fi
# systemd active is not an application health or data-migration test.
echo "Update complete: $NEW_HEAD. Previous release retained at $old_release."
