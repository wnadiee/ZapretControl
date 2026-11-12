#!/usr/bin/env bash
set -euo pipefail
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ZAPRET_DIR="$SCRIPT_DIR/.runtime/zapret-discord-youtube-linux"
SERVICE_SH="$ZAPRET_DIR/service.sh"
run_zapret() {
  [ -f "$SERVICE_SH" ] || { echo "База не установлена. Запустите ./install.sh" >&2; exit 1; }
  (cd "$ZAPRET_DIR" && bash ./service.sh "$@")
}
menu() {
  while true; do
    echo; echo "=== Zapret Control (Linux) ==="
    echo "1) Zapret: интерактивное меню"
    echo "2) Zapret: управление сервисом"
    echo "3) TgProxy: статус / запуск / стоп"
    echo "4) TgProxy: автозапуск ВКЛ"
    echo "5) TgProxy: автозапуск ВЫКЛ"
    echo "0) Выход"
    read -r -p "> " c || exit 0
    case "$c" in
      1) run_zapret || true ;;
      2) run_zapret service || true ;;
      3) "$SCRIPT_DIR/tgproxy.sh" status || true; read -r -p "[s]tart/[x]stop/enter: " a || true; [ "${a:-}" = s ] && "$SCRIPT_DIR/tgproxy.sh" start || true; [ "${a:-}" = x ] && "$SCRIPT_DIR/tgproxy.sh" stop || true ;;
      4) "$SCRIPT_DIR/tgproxy.sh" autostart on || true ;;
      5) "$SCRIPT_DIR/tgproxy.sh" autostart off || true ;;
      0) exit 0 ;; *) ;;
    esac
  done
}
case "${1:-}" in
  "" ) menu ;;
  install) "$SCRIPT_DIR/install.sh" ;;
  zapret) shift; run_zapret "$@" ;;
  tgproxy) shift; "$SCRIPT_DIR/tgproxy.sh" "$@" ;;
  *) echo "Использование: $0 [install|zapret [args]|tgproxy <cmd>]"; exit 1 ;;
esac
