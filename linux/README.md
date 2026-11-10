# Zapret Control (Linux)

Набор файлов для Linux на базе [Sergeydigl3/zapret-discord-youtube-linux](https://github.com/Sergeydigl3/zapret-discord-youtube-linux) с поддержкой [Flowseal/tg-ws-proxy](https://github.com/Flowseal/tg-ws-proxy).

## Установка

```bash
cd linux
chmod +x install.sh zapret-control.sh tgproxy.sh
./install.sh
```

`install.sh` клонирует/обновит базу zapret-linux в `linux/.runtime/zapret-discord-youtube-linux` и установит TgProxy.

## Запуск

```bash
./zapret-control.sh
```

- **Zapret:** через меню/команды передаются в upstream `service.sh` (стратегии, сервис, запуск).
  - `./zapret-control.sh zapret` — интерактивное меню
  - `./zapret-control.sh zapret service install` — установить автозапуск/сервис zapret
- **TgProxy:**
  - `./tgproxy.sh start|stop|status`
  - `./tgproxy.sh autostart on|off` — автозапуск TgProxy (user `systemd`)

## Настройки

- **Zapret (nfqws/стратегии):** настраиваются в базе `zapret-discord-youtube-linux` (`conf.env`, выбор стратегии, gamefilter и т.д. через `service.sh`).
- **TgProxy:** конфиг `~/.config/TgWsProxy/config.json` (порт, host и параметры прокси). По умолчанию `127.0.0.1:1443`.

Требования: `git`, `curl`, `nftables` и права для работы nfqws (см. README upstream).