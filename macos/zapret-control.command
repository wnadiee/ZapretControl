#!/bin/bash
set -euo pipefail
cd "$(dirname "$0")"
SCRIPT_DIR="$(pwd)"; RUNTIME_DIR="$SCRIPT_DIR/.runtime"

open_zapret() {
  local app; app="$(find "$RUNTIME_DIR" -maxdepth 2 -name '*Zapret*.app' 2>/dev/null | head -1 || true)"
  [ -n "$app" ] && open "$app" || echo "ZapretMac не найден. Запустите ./install.command"
}

menu() {
  while true; do
    echo; echo "=== Zapret Control (macOS) ==="
    echo "1) Открыть ZapretMac  2) TgProxy статус  3) TgProxy старт  4) TgProxy стоп"
    echo "5) Автозапуск TgProxy ВКЛ  6) Автозапуск TgProxy ВЫКЛ  0) Выход"
    read -r -p "> " c || exit 0
    case "$c" in
      1) open_zapret || true ;; 2) "$SCRIPT_DIR/tgproxy.command" status || true ;;
      3) "$SCRIPT_DIR/tgproxy.command" start || true ;; 4) "$SCRIPT_DIR/tgproxy.command" stop || true ;;
      5) "$SCRIPT_DIR/tgproxy.command" autostart on || true ;; 6) "$SCRIPT_DIR/tgproxy.command" autostart off || true ;;
      0) exit 0 ;; *) ;;
    esac
  done
}

case "${1:-}" in
  install) "$SCRIPT_DIR/install.command" ;; zapret) open_zapret ;; tgproxy) shift; "$SCRIPT_DIR/tgproxy.command" "$@" ;; *) menu ;;
esac
