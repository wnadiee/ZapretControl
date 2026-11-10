# Zapret Control (macOS)

Набор файлов для macOS на базе [Flowseal/zapret-mac-discord-youtube](https://github.com/Flowseal/zapret-mac-discord-youtube) с поддержкой [Flowseal/tg-ws-proxy](https://github.com/Flowseal/tg-ws-proxy).

## Установка

```bash
cd macos
chmod +x install.command zapret-control.command tgproxy.command
./install.command
```

`install.command` скачает/распакует ZapretMac (upstream) в `macos/.runtime/` и установит TgProxy.

## Запуск

```bash
./zapret-control.command
```

- **ZapretMac:** открывается трей-приложение ZapretMac (стратегии/запуск — в нём).
- **TgProxy:**
  - `./tgproxy.command start|stop|status`
  - `./tgproxy.command autostart on|off` — автозапуск TgProxy (LaunchAgent)

## Настройки

- **ZapretMac:** стратегии, списки и режимы (в т.ч. IPSet) — в приложении ZapretMac (см. README upstream).
- **TgProxy:** конфиг `~/Library/Application Support/TgWsProxy/config.json` (порт, host и параметры). По умолчанию `127.0.0.1:1443`.

При первом запуске macOS может запросить разрешения — подтвердите их в настройках системы.