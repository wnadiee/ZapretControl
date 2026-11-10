# Zapret Control

Простая панель на базе [zapret-discord-youtube](https://github.com/Flowseal/zapret-discord-youtube) для Windows, а также companion-наборы для Linux и macOS. Помогает открыть YouTube, Discord, Roblox, SoundCloud и другие сервисы, если они не работают из‑за ограничений провайдера.

![Скриншот](docs/screenshot.png)

## Установка

### Windows
1. Скачайте `ZapretControl-windows.zip` (или `ZapretControl.exe`) из [Releases](../../releases).
2. Распакуйте архив в удобную папку (например, `C:\ZapretControl`).
3. Запустите `ZapretControl.exe`.
4. Разрешите запуск **от имени администратора** — это нужно для установки/управления службой zapret.
5. Дождитесь подготовки, выберите стратегию и нажмите **«Применить»**. Если сервис не откроется — попробуйте другую стратегию.

### Linux
1. Скачайте `ZapretControl-linux.tar.gz` из [Releases](../../releases) и распакуйте.
2. Установите зависимости: `git`, `curl`, `nftables` (и права для nfqws).
3. Выполните:
   ```bash
   cd linux
   chmod +x install.sh zapret-control.sh tgproxy.sh
   ./install.sh
   ./zapret-control.sh
   ```
   База zapret разворачивается на основе [Sergeydigl3/zapret-discord-youtube-linux](https://github.com/Sergeydigl3/zapret-discord-youtube-linux), TgProxy — [Flowseal/tg-ws-proxy](https://github.com/Flowseal/tg-ws-proxy).

### macOS
1. Скачайте `ZapretControl-macos.zip` из [Releases](../../releases) и распакуйте.
2. Выполните:
   ```bash
   cd macos
   chmod +x install.command zapret-control.command tgproxy.command
   ./install.command
   ./zapret-control.command
   ```
   Zapret на базе [Flowseal/zapret-mac-discord-youtube](https://github.com/Flowseal/zapret-mac-discord-youtube), TgProxy — [Flowseal/tg-ws-proxy](https://github.com/Flowseal/tg-ws-proxy). При первом запуске подтвердите запросы безопасности macOS.

## Что делают настройки

### Windows (в приложении)
- **Стратегия + «Применить»** — выбирает набор параметров обхода (nfqws/winws). Разные стратегии работают по-разному у разных провайдеров; если не помогает — переключите и примените снова.
- **Служба zapret / Запуск / Остановка / Автозапуск службы** — управляет системной службой `zapret`, которая выполняет обход. «Автозапуск службы» включает запуск службы вместе с системой.
- **Проверки** — быстрые проверки доступности сервисов (YouTube, Discord, Roblox, SoundCloud, Telegram) для оценки, работает ли текущая стратегия.
- **Game Filter** — фильтрация трафика игр (`disabled` / `tcp` / `udp` / `all`) для применения обхода к игровому трафику.
- **IPSet Filter** — использование списков IP (`loaded` / `none` / `any`), влияет на то, к каким адресам применяется обход.
- **Auto-Update** — автообновление компонентов zapret (если доступно в используемой сборке).
- **Автозапуск приложения** — запускать Zapret Control при входе в Windows.
- **Replace fakes** — обновление/замена активных «фейков» (bin/fake-данных) в наборе zapret.
- **Кнопка «Сохранить»** — сохраняет настройки; для zapret-настроек стратегия пере-применяется автоматически при необходимости.
- **Telegram / TgProxy** — локальный прокси для Telegram Desktop: установка/запуск/остановка, порт, «Подключить Telegram» (настройка t.me-ссылок), «Настройки» TgProxy (порт, host, буферы/пул, режимы cfproxy/h2/no_secure и др.).

### Linux / macOS
- **Zapret:** стратегии, списки, gamefilter/IPSet и сервис — настраиваются средствами базовых проектов (upstream `service.sh` / приложение ZapretMac).
- **TgProxy:** запускается скриптами `tgproxy.sh` / `tgproxy.command`, конфиг — `~/.config/TgWsProxy/config.json` (Linux) или `~/Library/Application Support/TgWsProxy/config.json` (macOS). Автозапуск TgProxy: `autostart on|off` (user `systemd` / `LaunchAgent`).

## Сборка (Windows)

```bat
src\build.bat
```

## Компоненты

См. [NOTICE.md](NOTICE.md). Linux/macOS наборы основаны на [Sergeydigl3/zapret-discord-youtube-linux](https://github.com/Sergeydigl3/zapret-discord-youtube-linux) и [Flowseal/zapret-mac-discord-youtube](https://github.com/Flowseal/zapret-mac-discord-youtube); TgProxy — [Flowseal/tg-ws-proxy](https://github.com/Flowseal/tg-ws-proxy).