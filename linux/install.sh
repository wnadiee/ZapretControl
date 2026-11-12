#!/usr/bin/env bash
set -euo pipefail
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
RUNTIME_DIR="$SCRIPT_DIR/.runtime"
ZAPRET_DIR="$RUNTIME_DIR/zapret-discord-youtube-linux"
ZAPRET_REPO="https://github.com/Sergeydigl3/zapret-discord-youtube-linux.git"
mkdir -p "$RUNTIME_DIR"
echo "[1/3] Проверка зависимостей..."
for cmd in git curl; do
  command -v "$cmd" >/dev/null 2>&1 || { echo "Требуется: $cmd" >&2; exit 1; }
done
echo "[2/3] База zapret-linux..."
if [ -d "$ZAPRET_DIR/.git" ]; then
  git -C "$ZAPRET_DIR" pull --ff-only || true
else
  git clone --depth 1 "$ZAPRET_REPO" "$ZAPRET_DIR"
fi
echo "[3/3] TgProxy..."
chmod +x "$SCRIPT_DIR/tgproxy.sh" "$SCRIPT_DIR/zapret-control.sh"
"$SCRIPT_DIR/tgproxy.sh" install || echo "TgProxy можно установить позже: ./tgproxy.sh install"
echo "Готово. Запуск: ./zapret-control.sh"
