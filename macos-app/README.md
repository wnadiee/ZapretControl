# Zapret Control (macOS) — исходники

Исходный код-приложения (Swift, AppKit, menu bar) для macOS: управление TgProxy и интеграция с [Flowseal/zapret-mac-discord-youtube](https://github.com/Flowseal/zapret-mac-discord-youtube) (ZapretMac).

> Готовый `.app` в этом окружении не собирался (требуется macOS + Xcode). Это проект с исходниками и инструкцией сборки.

## Возможности

- Статус, запуск/остановка, установка TgProxy ([Flowseal/tg-ws-proxy](https://github.com/Flowseal/tg-ws-proxy))
- Автозапуск TgProxy (LaunchAgent)
- Автозапуск приложения (Login Item через LaunchAgent)
- Открытие/запуск ZapretMac (если установлен) и доступ к его настройкам/спискам
- Быстрые проверки доступности сервисов

## Сборка (на macOS)

Требования: macOS 13+, Xcode Command Line Tools (`xcode-select --install`).

```bash
cd macos-app
chmod +x build.sh
./build.sh
```

Скрипт соберёт `build/ZapretControl.app` (универсальный бинарник arm64+x86_64 при наличии SDK) и подпишет ad-hoc. Готовое приложение: `build/ZapretControl.app`.

## Запуск

1. Переместите `ZapretControl.app` в `/Applications` (или другую папку).
2. При первом запуске разрешите открытие в настройках безопасности macOS.
3. Для обхода DPI используйте ZapretMac (устанавливается/открывается из меню приложения).

## Примечания

- Обход трафика (nfqws/utun, стратегии, IPSet) выполняет ZapretMac; данное управляет TgProxy и удобным запуском/автозапуском.
- TgProxy ставится в `~/Library/Application Support/ZapretControl/tgproxy/`, конфиг — `~/Library/Application Support/TgWsProxy/config.json`.