#!/bin/bash
set -euo pipefail
cd "$(dirname "$0")"
SCRIPT_DIR="$(pwd)"; RUNTIME_DIR="$SCRIPT_DIR/.runtime"; mkdir -p "$RUNTIME_DIR"

echo "[1/3] TgProxy..."
chmod +x "$SCRIPT_DIR/tgproxy.command" "$SCRIPT_DIR/zapret-control.command" 2>/dev/null || true
"$SCRIPT_DIR/tgproxy.command" install || echo "Установите TgProxy позже: ./tgproxy.command install"

echo "[2/3] ZapretMac..."
ZIP="$RUNTIME_DIR/ZapretMac-macOS-universal.zip"
URL="https://github.com/Flowseal/zapret-mac-discord-youtube/releases/latest/download/ZapretMac-macOS-universal.zip"
if ! find "$RUNTIME_DIR" -maxdepth 2 -name '*Zapret*.app' 2>/dev/null | grep -q .; then
  if curl -fL --retry 3 -o "$ZIP.part" "$URL"; then
    mv "$ZIP.part" "$ZIP"; ditto -xk "$ZIP" "$RUNTIME_DIR" 2>/dev/null || unzip -oq "$ZIP" -d "$RUNTIME_DIR" || true
    echo "ZapretMac распакован в $RUNTIME_DIR"
  else
    echo "Скачайте вручную: $URL"
  fi
else
  echo "ZapretMac уже есть."
fi

echo "Готово. Запуск: ./zapret-control.command"
read -r -p "Enter для выхода..." _ || true
