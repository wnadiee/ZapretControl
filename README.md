**English** | [Русский](#русский)

# Zapret Control

An app that helps open YouTube, Discord, Roblox, SoundCloud, and other sites when they are blocked or unstable with your ISP.

- **Windows** — ready-to-run app (`ZapretControl.exe`)
- **Linux** — app from source (Python/Tkinter), folder `linux-app/`
- **macOS** — app from source (Swift/AppKit), folder `macos-app/`

![Screenshot](docs/screenshot.png)

## Install

### Windows
1. Download `ZapretControl-windows.zip` from [Releases](../../releases) and extract it.
2. Run `ZapretControl.exe` as administrator.
3. Choose a strategy and click **Apply**. If it does not work, try another strategy.

### Linux
1. Download `ZapretControl-linux-src.zip` from [Releases](../../releases) and extract it (or use the `linux-app/` folder).
2. Install dependencies: Python 3.8+, `python3-tk`, `git`, `curl`, `nftables`.
3. Run:
   ```bash
   cd linux-app
   python3 zapret_control_linux.py
   ```
   In the app you can install the zapret base, choose a strategy, manage the service and TgProxy. Building a single file with PyInstaller is described in `linux-app/README.md`.

### macOS
1. Download `ZapretControl-macos-src.zip` from [Releases](../../releases) and extract it (or use the `macos-app/` folder).
2. On a Mac (macOS 13+, Xcode Command Line Tools) build:
   ```bash
   cd macos-app
   chmod +x build.sh
   ./build.sh
   ```
3. Open `build/ZapretControl.app` (you can move it to `/Applications`). If macOS blocks it, allow it in Security settings.
4. Use the app to install/run TgProxy and autostart; for bypass itself open the Zapret app for macOS and configure strategy/IPSet there.

Ready-made binaries for Linux and macOS are not included in releases — only source code that should be built on your system. The repository also contains script bundles in `linux/` and `macos/`.

## Settings

### Windows
- **Strategy + Apply** — switches the bypass method. If a site does not open, another strategy usually helps.
- **zapret service** — start, stop, and restart the service.
- **Service autostart** — start the service with Windows.
- **Access check** — quickly checks whether YouTube, Discord, Roblox, SoundCloud, and Telegram are reachable.
- **Game Filter** — apply bypass to game traffic: `disabled` / `tcp` / `udp` / `all`.
- **IPSet Filter** — which IP list to use: `loaded` / `none` / `any`.
- **App autostart** — launch Zapret Control on sign-in.
- **Replace fakes** — update current fake/bin files.
- **Save** — saves settings (required parameters are applied immediately).
- **Telegram / TgProxy** — local proxy for Telegram Desktop: install, start/stop, choose port, connect Telegram, and change proxy parameters (host/port, buffers, pool, cfproxy/h2/no_secure, etc.).
- **RU/EN toggle** — switch interface language instantly without restarting the app.

### Linux
- **zapret base** — where the zapret base is installed and managed; strategies and service control come from it.
- **TgProxy** — install, start/stop, port, autostart (user `systemd`).
- **App autostart** — adds the app to user autostart.
- **Access check** — simple site availability check.
- **RU/EN toggle** — switch interface language instantly.

### macOS
- **TgProxy** — install, start/stop, and autostart (LaunchAgent).
- **Zapret for macOS** — opens the app to configure strategies, lists, and IPSet.
- **App autostart** — autostart via LaunchAgent.
- **Access check** — simple site availability check.
- **RU/EN toggle** — switch interface language instantly.

## Build (Windows)

```bat
src\build.bat
```

## Components

[NOTICE.md](NOTICE.md)

Uses: [zapret-discord-youtube](https://github.com/Flowseal/zapret-discord-youtube), [zapret-discord-youtube-linux](https://github.com/Sergeydigl3/zapret-discord-youtube-linux), [zapret-mac-discord-youtube](https://github.com/Flowseal/zapret-mac-discord-youtube), [tg-ws-proxy](https://github.com/Flowseal/tg-ws-proxy).

---

## Русский

[English](#zapret-control) | **Русский**

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
4. В приложении можно установить/запустить TgProxy и автозапуск, а для самого обхода открыть приложение Zapret для macOS и настроить стратегию/IPSet там.

Готовых бинарников для Linux и macOS в релизе нет — только исходники, их нужно собрать на своей системе. В репозитории также есть скриптовые наборы `linux/` и `macos/`.

## Настройки

### Windows
- **Стратегия + «Применить»** — переключает способ обхода. Если сайт не открылся, обычно помогает другая стратегия.
- **Служба zapret** — запуск, остановка и перезапуск службы.
- **Автозапуск службы** — запуск службы вместе с Windows.
- **Проверка доступа** — быстро показывает, открываются ли YouTube, Discord, Roblox, SoundCloud, Telegram.
- **Game Filter** — применять обход к игровому трафику: `disabled` / `tcp` / `udp` / `all`.
- **IPSet Filter** — какой список IP использовать: `loaded` / `none` / `any`.
- **Автозапуск приложения** — запускать Zapret Control при входе в систему.
- **Replace fakes** — обновить текущие fake/bin-файлы.
- **Сохранить** — сохраняет настройки (нужные параметры применяются сразу).
- **Telegram / TgProxy** — локальный прокси для Telegram Desktop: установка, запуск/остановка, порт, подключение Telegram и параметры прокси (host/port, буферы, пул, cfproxy/h2/no_secure и т.п.).
- **Переключатель RU/EN** — мгновенная смена языка интерфейса без перезапуска.

### Linux
- **База zapret** — где лежит и откуда устанавливается база zapret, оттуда берутся стратегии и управление сервисом.
- **TgProxy** — установка, запуск/остановка, порт, автозапуск (user `systemd`).
- **Автозапуск приложения** — добавляет приложение в автозапуск пользователя.
- **Проверка доступа** — простая проверка доступности сайтов.
- **Переключатель RU/EN** — мгновенная смена языка.

### macOS
- **TgProxy** — установка, запуск/остановка и автозапуск (LaunchAgent).
- **Zapret для macOS** — открывает приложение для настройки стратегий, списков и IPSet.
- **Автозапуск приложения** — автозапуск через LaunchAgent.
- **Проверка доступа** — простая проверка доступности сайтов.
- **Переключатель RU/EN** — мгновенная смена языка.

## Сборка (Windows)

```bat
src\build.bat
```

## Компоненты

[NOTICE.md](NOTICE.md)

Используются: [zapret-discord-youtube](https://github.com/Flowseal/zapret-discord-youtube), [zapret-discord-youtube-linux](https://github.com/Sergeydigl3/zapret-discord-youtube-linux), [zapret-mac-discord-youtube](https://github.com/Flowseal/zapret-mac-discord-youtube), [tg-ws-proxy](https://github.com/Flowseal/tg-ws-proxy).
