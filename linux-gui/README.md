# Zapret Control GUI (Linux) — исходники

GUI-приложение (Python 3 + Tkinter) для управления zapret на Linux на базе [Sergeydigl3/zapret-discord-youtube-linux](https://github.com/Sergeydigl3/zapret-discord-youtube-linux) и [tg-ws-proxy](https://github.com/Flowseal/tg-ws-proxy).

> Готовые бинарники в этом окружении не собирались. Это исходный код: запустите/соберите на Linux.

## Возможности

- Запуск/остановка, стратегия, установка/управление сервисом (через `service.sh` базы zapret-linux)
- Управление TgProxy: установить, запустить/остановить, порт, автозапуск (user systemd)
- Проверки доступности сервисов
- Базовые настройки (gamefilter, интерфейс через конфиг базы), автозапуск приложения (desktop autostart)

## Зависимости

- Python 3.8+
- Tkinter: `python3-tk` (Debian/Ubuntu) / `tk` (Arch/Fedora)
- `git`, `curl`, `nftables` (для zapret), права sudo при управлении сервисом

## Запуск из исходников

```bash
cd linux-gui
python3 zapret_control_linux.py
```

При первом запуске укажите/установите базу zapret-linux (кнопка в приложении использует `install.sh` логики клонирования в `~/.local/share/zapret-control-linux/base`).

## Сборка в исполняемый файл (опционально)

На Linux с установленным PyInstaller:

```bash
pip install pyinstaller
pyinstaller --onefile --windowed --name ZapretControl zapret_control_linux.py
```

Бинарник появится в `dist/ZapretControl`.