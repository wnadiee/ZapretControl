# Zapret Control

Панель управления обходом на базе [zapret-discord-youtube](https://github.com/Flowseal/zapret-discord-youtube). Помогает открыть YouTube, Discord, Roblox, SoundCloud и другие сервисы, если они не работают из‑за ограничений провайдера.

- **Windows:** готовое приложение (`ZapretControl.exe`)
- **Linux:** приложение (исходники, Python/Tkinter) — `linux-app/`
- **macOS:** приложение (исходники, Swift/AppKit) — `macos-app/`

![Скриншот](docs/screenshot.png)

## Установка

### Windows
1. Скачайте `ZapretControl-windows.zip` из [Releases](../../releases) и распакуйте.
2. Запустите `ZapretControl.exe`.
3. Разрешите запуск **от имени администратора**.
4. Дождитесь подготовки, выберите стратегию и нажмите **«Применить»**. Если не помогает — попробуйте другую стратегию.

### Linux
1. Скачайте `ZapretControl-linux-src.zip` из [Releases](../../releases) и распакуйте (или используйте папку `linux-app/` из репозитория).
2. Установите зависимости: Python 3.8+, `python3-tk`, `git`, `curl`, `nftables`.
3. Запустите:
   ```bash
   cd linux-app
   python3 zapret_control_linux.py
   ```
4. В приложении установите/укажите базу zapret (на основе [Sergeydigl3/zapret-discord-youtube-linux](https://github.com/Sergeydigl3/zapret-discord-youtube-linux)), управляйте сервисом/стратегиями и TgProxy.

Опционально: сборка в бинарник через PyInstaller — см. `linux-app/README.md`.

### macOS
1. Скачайте `ZapretControl-macos-src.zip` из [Releases](../../releases) и распакуйте (или используйте `macos-app/`).
2. На Mac (macOS 13+, Xcode Command Line Tools) соберите приложение:
   ```bash
   cd macos-app
   chmod +x build.sh
   ./build.sh
   ```
3. Запустите `build/ZapretControl.app` (при необходимости перенесите в `/Applications` и разрешите запуск в настройках безопасности).
4. Через меню управляйте TgProxy (установка/запуск/автозапуск) и открывайте [ZapretMac](https://github.com/Flowseal/zapret-mac-discord-youtube) для настройки обхода (стратегии/IPSet).

> Готовые `.app`/бинарники для Linux и macOS в релиз не прикладываются — публикуются исходники, собираемые на целевой ОС.

### Дополнительно (скриптовые пакеты)
В репозитории и релизах также есть `linux/` и `macos/` — скриптовые наборы для быстрого запуска upstream-компонентов и TgProxy.

## Что делают настройки

### Windows
- **Стратегия + «Применить»** — выбирает параметры обхода (nfqws/winws). Если не помогает — попробуйте другую стратегию.
- **Служба zapret** — запуск/остановка/перезапуск системной службы обхода.
- **Автозапуск службы** — запуск службы zapret вместе с Windows.
- **Проверки** — проверка доступности сервисов (YouTube, Discord, Roblox, SoundCloud, Telegram) для текущей стратегии.
- **Game Filter** — область применения обхода для игрового трафика (`disabled` / `tcp` / `udp` / `all`).
- **IPSet Filter** — режим списков IP (`loaded` / `none` / `any`) для обхода.
- **Auto-Update** — автообновление компонентов zapret (если поддерживается сборкой).
- **Автозапуск приложения** — запускать Zapret Control при входе в Windows.
- **Replace fakes** — замена/обновление активных fake/bin-данных.
- **Кнопка «Сохранить»** — сохраняет настройки (zapret-параметры применяются автоматически при необходимости).
- **Telegram / TgProxy** — локальный прокси для Telegram Desktop: установка, запуск/остановка, порт, подключение, настройки (host/port, буферы, пул, режимы cfproxy/h2/no_secure и др.).

### Linux
- **База zapret** — путь/установка базы [zapret-discord-youtube-linux](https://github.com/Sergeydigl3/zapret-discord-youtube-linux), управление сервисом и стратегиями (через `service.sh`/`conf.env`).
- **TgProxy** — установка, запуск/остановка, порт, автозапуск (user `systemd`).
- **Автозапуск приложения** — добавление `.desktop` в автозапуск пользователя.
- **Проверки** — базовая проверка доступности сервисов.

### macOS
- **TgProxy** — установка, запуск/остановка, автозапуск (LaunchAgent).
- **ZapretMac** — открытие [ZapretMac](https://github.com/Flowseal/zapret-mac-discord-youtube), где настраиваются стратегии, списки и IPSet (обход трафика).
- **Автозапуск приложения** — LaunchAgent для запуска приложения при входе.
- **Проверки** — базовая проверка доступности сервисов.

## Сборка (Windows)

```bat
src\build.bat
```

## Компоненты

См. [NOTICE.md](NOTICE.md). Основано на [Flowseal/zapret-discord-youtube](https://github.com/Flowseal/zapret-discord-youtube), [Sergeydigl3/zapret-discord-youtube-linux](https://github.com/Sergeydigl3/zapret-discord-youtube-linux), [Flowseal/zapret-mac-discord-youtube](https://github.com/Flowseal/zapret-mac-discord-youtube), [Flowseal/tg-ws-proxy](https://github.com/Flowseal/tg-ws-proxy).