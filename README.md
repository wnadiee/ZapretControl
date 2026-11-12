# Zapret Control

Приложение, которое помогает открыть YouTube, Discord, Roblox, SoundCloud и другие сайты, если они не работают у вашего провайдера.

- **Windows** — готовое приложение (`ZapretControl.exe`)
- **Linux** — приложение из исходников (Python/Tkinter), папка `linux-app/`
- **macOS** — приложение из исходников (Swift/AppKit), папка `macos-app/`

![Скриншот](docs/screenshot.png)

## Установка

### Windows
1. Скачайте `ZapretControl-windows.zip` из [Releases](../../releases) и распакуйте.
2. Запустите `ZapretControl.exe` от имени администратора.
3. Выберите стратегию и нажмите **«Применить»**. Если не заработало — попробуйте другую.

### Linux
1. Скачайте `ZapretControl-linux-src.zip` из [Releases](../../releases) и распакуйте (или возьмите папку `linux-app/`).
2. Поставьте зависимости: Python 3.8+, `python3-tk`, `git`, `curl`, `nftables`.
3. Запуск:
   ```bash
   cd linux-app
   python3 zapret_control_linux.py
   ```
   В приложении можно установить базу zapret, выбрать стратегию, управлять сервисом и TgProxy. Сборка в один файл через PyInstaller описана в `linux-app/README.md`.

### macOS
1. Скачайте `ZapretControl-macos-src.zip` из [Releases](../../releases) и распакуйте (или возьмите папку `macos-app/`).
2. На Mac (macOS 13+, Xcode Command Line Tools) соберите:
   ```bash
   cd macos-app
   chmod +x build.sh
   ./build.sh
   ```
3. Откройте `build/ZapretControl.app` (можно перенести в `/Applications`). Если macOS не даст открыть — разрешите в настройках безопасности.
4. Через меню можно поставить/запустить TgProxy и автозапуск, а для самого обхода открыть приложение Zapret для macOS и настроить стратегию/IPSet там.

Готовых бинарников для Linux и macOS в релизе нет — только исходники, их нужно собрать на своей системе. В репозитории также есть скриптовые наборы `linux/` и `macos/`.

## Что делают настройки

### Windows
- **Стратегия + «Применить»** — переключает способ обхода. Если сайт не открылся, обычно помогает другая стратегия.
- **Служба zapret** — запуск, остановка и перезапуск службы, которая делает обход.
- **Автозапуск службы** — чтобы служба поднималась вместе с Windows.
- **Проверки** — быстро показывает, открываются ли YouTube, Discord, Roblox, SoundCloud, Telegram.
- **Game Filter** — применять обход к игровому трафику: `disabled` / `tcp` / `udp` / `all`.
- **IPSet Filter** — какой список IP использовать: `loaded` / `none` / `any`.
- **Auto-Update** — автообновление частей zapret (если это поддерживает сборка).
- **Автозапуск приложения** — запускать сам Zapret Control при входе в систему.
- **Replace fakes** — обновить текущие fake/bin-файлы.
- **«Сохранить»** — сохраняет настройки (нужные параметры применяются сразу).
- **Telegram / TgProxy** — локальный прокси для Telegram Desktop: установить, запустить/остановить, выбрать порт, подключить Telegram и поменять параметры прокси (host/port, буферы, пул, cfproxy/h2/no_secure и т.п.).

### Linux
- **База zapret** — где лежит и откуда устанавливается база zapret, оттуда же берутся стратегии и управление сервисом.
- **TgProxy** — установка, запуск/остановка, порт, автозапуск (user `systemd`).
- **Автозапуск приложения** — добавляет себя в автозапуск пользователя.
- **Проверки** — простая проверка доступности сайтов.

### macOS
- **TgProxy** — установка, запуск/остановка и автозапуск (LaunchAgent).
- **Zapret для macOS** — открывает приложение для настройки стратегий, списков и IPSet.
- **Автозапуск приложения** — автозапуск через LaunchAgent.
- **Проверки** — простая проверка доступности сайтов.

## Сборка (Windows)

```bat
src\build.bat
```

## Компоненты

[NOTICE.md](NOTICE.md)

Используются: [zapret-discord-youtube](https://github.com/Flowseal/zapret-discord-youtube), [zapret-discord-youtube-linux](https://github.com/Sergeydigl3/zapret-discord-youtube-linux), [zapret-mac-discord-youtube](https://github.com/Flowseal/zapret-mac-discord-youtube), [tg-ws-proxy](https://github.com/Flowseal/tg-ws-proxy).
