#!/bin/bash
set -euo pipefail

APP_DIR="$HOME/Library/Application Support/ZapretControl/tgproxy"
CONF_DIR="$HOME/Library/Application Support/TgWsProxy"
CONF_FILE="$CONF_DIR/config.json"
BIN="$APP_DIR/tg-ws-proxy"
PID_FILE="$APP_DIR/tg-ws-proxy.pid"
LOG_FILE="$APP_DIR/tg-ws-proxy.log"
PLIST="$HOME/Library/LaunchAgents/com.flowseal.tg-ws-proxy.plist"
RELEASE_BASE="https://github.com/Flowseal/tg-ws-proxy/releases/latest/download"
ASSET="${TGPROXY_ASSET:-TgWsProxy_macos_universal.dmg}"

mkdir -p "$APP_DIR" "$CONF_DIR" "$HOME/Library/LaunchAgents"

ensure_config() { [ -f "$CONF_FILE" ] || echo '{ "port": 1443, "host": "127.0.0.1" }' > "$CONF_FILE"; }

extract_from_dmg() {
  local dmg="$1" mnt found=""
  mnt="$(mktemp -d /tmp/tgws.XXXXXX)"
  hdiutil attach "$dmg" -nobrowse -quiet -mountpoint "$mnt"
  found="$(find "$mnt" -maxdepth 3 -type f \( -name 'tg-ws-proxy' -o -name 'TgWsProxy' \) 2>/dev/null | head -1 || true)"
  [ -z "$found" ] && found="$(find "$mnt" -type f -perm -111 2>/dev/null | grep -i tg | head -1 || true)"
  [ -z "$found" ] && { hdiutil detach "$mnt" -quiet || true; echo "Не найден бинарник в DMG" >&2; return 1; }
  cp "$found" "$BIN"; chmod +x "$BIN"
  hdiutil detach "$mnt" -quiet || true; rmdir "$mnt" 2>/dev/null || true
}

cmd_install() {
  echo "Скачивание $ASSET..."
  local tmp="$APP_DIR/$ASSET.part"
  curl -fL --retry 3 -o "$tmp" "$RELEASE_BASE/$ASSET"
  if [[ "$ASSET" == *.dmg ]]; then extract_from_dmg "$tmp"; rm -f "$tmp"; else mv "$tmp" "$BIN"; chmod +x "$BIN"; fi
  ensure_config; echo "Установлено: $BIN"
}

is_running() {
  [ -f "$PID_FILE" ] && kill -0 "$(cat "$PID_FILE" 2>/dev/null)" 2>/dev/null && return 0
  pgrep -f "$BIN" >/dev/null 2>&1
}

cmd_start() {
  ensure_config; [ -x "$BIN" ] || { echo "Сначала: $0 install" >&2; exit 1; }
  is_running && { echo "TgProxy уже запущен."; exit 0; }
  nohup "$BIN" >>"$LOG_FILE" 2>&1 & echo $! > "$PID_FILE"; sleep 1
  echo "TgProxy: $(is_running && echo запущен || echo 'проверьте лог')"
}

cmd_stop() { [ -f "$PID_FILE" ] && kill "$(cat "$PID_FILE")" 2>/dev/null || true; rm -f "$PID_FILE"; pkill -f "$BIN" 2>/dev/null || true; echo "TgProxy остановлен."; }

cmd_status() {
  local port; port="$(grep -oE '"port"[[:space:]]*:[[:space:]]*[0-9]+' "$CONF_FILE" 2>/dev/null | grep -oE '[0-9]+' | head -1 || echo 1443)"
  is_running && echo "TgProxy: запущен (127.0.0.1:${port})" || echo "TgProxy: не запущен"
}

cmd_autostart() {
  case "${1:-}" in
    on|enable)
      cat > "$PLIST" <<EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0"><dict>
  <key>Label</key><string>com.flowseal.tg-ws-proxy</string>
  <key>ProgramArguments</key><array><string>$BIN</string></array>
  <key>RunAtLoad</key><true/><key>KeepAlive</key><true/>
  <key>StandardOutPath</key><string>$LOG_FILE</string>
  <key>StandardErrorPath</key><string>$LOG_FILE</string>
</dict></plist>
EOF
      launchctl unload "$PLIST" 2>/dev/null || true; launchctl load "$PLIST"
      echo "Автозапуск TgProxy включён." ;;
    off|disable) launchctl unload "$PLIST" 2>/dev/null || true; rm -f "$PLIST"; echo "Автозапуск TgProxy выключен." ;;
    status) [ -f "$PLIST" ] && echo enabled || echo disabled ;;
    *) echo "Использование: $0 autostart on|off|status" >&2; exit 1 ;;
  esac
}

case "${1:-}" in
  install) cmd_install ;; start) cmd_start ;; stop) cmd_stop ;; restart) cmd_stop; cmd_start ;; status) cmd_status ;;
  autostart) shift; cmd_autostart "${1:-}" ;;
  *) echo "Использование: $0 {install|start|stop|restart|status|autostart on|off|status}"; exit 1 ;;
esac
