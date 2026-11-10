#!/usr/bin/env bash
set -euo pipefail

APP_DIR="${XDG_DATA_HOME:-$HOME/.local/share}/zapret-control/tgproxy"
CONF_DIR="${XDG_CONFIG_HOME:-$HOME/.config}/TgWsProxy"
CONF_FILE="$CONF_DIR/config.json"
BIN="$APP_DIR/tg-ws-proxy"
PID_FILE="$APP_DIR/tg-ws-proxy.pid"
LOG_FILE="$APP_DIR/tg-ws-proxy.log"
SERVICE_DIR="${XDG_CONFIG_HOME:-$HOME/.config}/systemd/user"
SERVICE_FILE="$SERVICE_DIR/tg-ws-proxy.service"
RELEASE_BASE="https://github.com/Flowseal/tg-ws-proxy/releases/latest/download"
ASSET="${TGPROXY_ASSET:-TgWsProxy_linux_amd64}"

mkdir -p "$APP_DIR" "$CONF_DIR"

ensure_config() {
  [ -f "$CONF_FILE" ] || cat > "$CONF_FILE" <<'JSON'
{ "port": 1443, "host": "127.0.0.1" }
JSON
}

is_running() {
  if [ -f "$PID_FILE" ]; then
    local pid; pid="$(cat "$PID_FILE" 2>/dev/null || true)"
    [ -n "${pid:-}" ] && kill -0 "$pid" 2>/dev/null && return 0
  fi
  pgrep -f "$BIN" >/dev/null 2>&1
}

cmd_install() {
  echo "Скачивание $ASSET..."
  curl -fL --retry 3 -o "$BIN.part" "$RELEASE_BASE/$ASSET"
  mv "$BIN.part" "$BIN"; chmod +x "$BIN"; ensure_config
  echo "Установлено: $BIN"
}

cmd_start() {
  ensure_config; [ -x "$BIN" ] || { echo "Сначала: $0 install" >&2; exit 1; }
  is_running && { echo "TgProxy уже запущен."; exit 0; }
  nohup "$BIN" >>"$LOG_FILE" 2>&1 & echo $! > "$PID_FILE"; sleep 1
  is_running && echo "TgProxy запущен." || echo "Проверьте лог: $LOG_FILE"
}

cmd_stop() {
  [ -f "$PID_FILE" ] && { kill "$(cat "$PID_FILE")" 2>/dev/null || true; rm -f "$PID_FILE"; }
  pkill -f "$BIN" 2>/dev/null || true
  echo "TgProxy остановлен."
}

cmd_status() {
  if is_running; then
    local port; port="$(grep -oE '"port"[[:space:]]*:[[:space:]]*[0-9]+' "$CONF_FILE" 2>/dev/null | grep -oE '[0-9]+' | head -1 || echo 1443)"
    echo "TgProxy: запущен (127.0.0.1:${port:-1443})"
  else
    echo "TgProxy: не запущен"
  fi
}

cmd_autostart() {
  local mode="${1:-}"
  command -v systemctl >/dev/null 2>&1 || { echo "Нужен systemd (user)." >&2; exit 1; }
  case "$mode" in
    on|enable)
      mkdir -p "$SERVICE_DIR"
      cat > "$SERVICE_FILE" <<EOF
[Unit]
Description=TG WS Proxy (Zapret Control)
After=network-online.target

[Service]
Type=simple
ExecStart=$BIN
Restart=on-failure
RestartSec=3

[Install]
WantedBy=default.target
EOF
      systemctl --user daemon-reload
      systemctl --user enable --now tg-ws-proxy.service
      echo "Автозапуск TgProxy включён."
      ;;
    off|disable)
      systemctl --user disable --now tg-ws-proxy.service 2>/dev/null || true
      echo "Автозапуск TgProxy выключен."
      ;;
    status) systemctl --user is-enabled tg-ws-proxy.service 2>/dev/null || echo disabled ;;
    *) echo "Использование: $0 autostart on|off|status" >&2; exit 1 ;;
  esac
}

case "${1:-}" in
  install) cmd_install ;; start) cmd_start ;; stop) cmd_stop ;;
  restart) cmd_stop; cmd_start ;; status) cmd_status ;;
  autostart) shift; cmd_autostart "${1:-}" ;;
  *) echo "Использование: $0 {install|start|stop|restart|status|autostart on|off|status}"; exit 1 ;;
esac
