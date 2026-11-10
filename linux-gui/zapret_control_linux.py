#!/usr/bin/env python3
import json
import os
import queue
import shutil
import socket
import subprocess
import sys
import threading
import tkinter as tk
from tkinter import ttk, messagebox, filedialog

APP_NAME = "Zapret Control (Linux)"
BASE_REPO = "https://github.com/Sergeydigl3/zapret-discord-youtube-linux.git"
TG_RELEASE = "https://github.com/Flowseal/tg-ws-proxy/releases/latest/download/TgWsProxy_linux_amd64"
DATA_DIR = os.path.join(os.path.expanduser("~"), ".local", "share", "zapret-control-linux")
CONF_DIR = os.path.join(os.path.expanduser("~"), ".config", "zapret-control-linux")
BASE_DIR = os.path.join(DATA_DIR, "base")
TG_DIR = os.path.join(DATA_DIR, "tgproxy")
TG_BIN = os.path.join(TG_DIR, "tg-ws-proxy")
TG_CONF_DIR = os.path.join(os.path.expanduser("~"), ".config", "TgWsProxy")
TG_CONF = os.path.join(TG_CONF_DIR, "config.json")
TG_LOG = os.path.join(TG_DIR, "tg-ws-proxy.log")
STATE_FILE = os.path.join(CONF_DIR, "state.json")
AUTOSTART_DIR = os.path.join(os.path.expanduser("~"), ".config", "autostart")
AUTOSTART_FILE = os.path.join(AUTOSTART_DIR, "zapret-control.desktop")

os.makedirs(DATA_DIR, exist_ok=True)
os.makedirs(CONF_DIR, exist_ok=True)
os.makedirs(TG_DIR, exist_ok=True)
os.makedirs(TG_CONF_DIR, exist_ok=True)


def load_state():
    try:
        with open(STATE_FILE, "r", encoding="utf-8") as f:
            return json.load(f)
    except Exception:
        return {"base_dir": BASE_DIR}


def save_state(st):
    try:
        with open(STATE_FILE, "w", encoding="utf-8") as f:
            json.dump(st, f, ensure_ascii=False, indent=2)
    except Exception:
        pass


def run_cmd(args, cwd=None, capture=True):
    try:
        p = subprocess.run(args, cwd=cwd, text=True, capture_output=capture,
                           stdout=subprocess.PIPE if capture else None,
                           stderr=subprocess.STDOUT if capture else None)
        return p.returncode, (p.stdout or "")
    except Exception as e:
        return 1, str(e)


def port_open(port, host="127.0.0.1", timeout=0.7):
    try:
        with socket.create_connection((host, int(port)), timeout=timeout):
            return True
    except Exception:
        return False


def get_tg_port():
    try:
        with open(TG_CONF, "r", encoding="utf-8") as f:
            d = json.load(f)
        return int(d.get("port", 1443))
    except Exception:
        return 1443


def ensure_tg_conf():
    if not os.path.exists(TG_CONF):
        with open(TG_CONF, "w", encoding="utf-8") as f:
            json.dump({"port": 1443, "host": "127.0.0.1"}, f, indent=2)


class App(tk.Tk):
    def __init__(self):
        super().__init__()
        self.title(APP_NAME)
        self.geometry("900x700")
        self.minsize(820, 640)
        self.state = load_state()
        self.base_dir = self.state.get("base_dir") or BASE_DIR
        self.log_q = queue.Queue()
        self._build()
        self.after(200, self._drain_log)
        self.refresh_all()

    def log(self, msg):
        self.log_q.put(msg)

    def _drain_log(self):
        try:
            while True:
                msg = self.log_q.get_nowait()
                self.log_text.configure(state="normal")
                self.log_text.insert("end", msg + "\n")
                self.log_text.see("end")
                self.log_text.configure(state="disabled")
        except queue.Empty:
            pass
        self.after(200, self._drain_log)

    def _build(self):
        main = ttk.Frame(self, padding=12)
        main.pack(fill="both", expand=True)

        # Service / base
        svc = ttk.LabelFrame(main, text="Zapret (Linux)", padding=10)
        svc.pack(fill="x", pady=(0, 8))
        ttk.Label(svc, text="База:").grid(row=0, column=0, sticky="w")
        self.base_var = tk.StringVar(value=self.base_dir)
        ttk.Entry(svc, textvariable=self.base_var, width=60).grid(row=0, column=1, sticky="we", padx=6)
        ttk.Button(svc, text="Обзор", command=self.pick_base).grid(row=0, column=2)
        ttk.Button(svc, text="Установить/обновить базу", command=self.install_base).grid(row=0, column=3, padx=6)
        svc.columnconfigure(1, weight=1)

        row1 = ttk.Frame(svc)
        row1.grid(row=1, column=0, columnspan=4, sticky="w", pady=(8, 0))
        ttk.Button(row1, text="Открыть меню service.sh", command=self.open_service_menu).pack(side="left")
        ttk.Button(row1, text="Сервис: install", command=lambda: self.run_service(["service", "install"])).pack(side="left", padx=4)
        ttk.Button(row1, text="Сервис: start", command=lambda: self.run_service(["service", "start"])).pack(side="left", padx=4)
        ttk.Button(row1, text="Сервис: stop", command=lambda: self.run_service(["service", "stop"])).pack(side="left", padx=4)
        ttk.Button(row1, text="Сервис: restart", command=lambda: self.run_service(["service", "restart"])).pack(side="left", padx=4)

        # Strategy
        stf = ttk.LabelFrame(main, text="Стратегия / запуск", padding=10)
        stf.pack(fill="x", pady=(0, 8))
        ttk.Label(stf, text="Стратегия:").grid(row=0, column=0, sticky="w")
        self.strategy_var = tk.StringVar()
        self.strategy_combo = ttk.Combobox(stf, textvariable=self.strategy_var, width=50, state="readonly")
        self.strategy_combo.grid(row=0, column=1, sticky="w", padx=6)
        ttk.Button(stf, text="Обновить список", command=self.load_strategies).grid(row=0, column=2, padx=4)
        ttk.Button(stf, text="Применить (config)", command=self.apply_strategy).grid(row=0, column=3, padx=4)

        # TgProxy
        tg = ttk.LabelFrame(main, text="Telegram / TgProxy", padding=10)
        tg.pack(fill="x", pady=(0, 8))
        self.tg_status = tk.StringVar(value="TgProxy: —")
        ttk.Label(tg, textvariable=self.tg_status).grid(row=0, column=0, columnspan=4, sticky="w")
        ttk.Label(tg, text="Порт:").grid(row=1, column=0, sticky="w", pady=(6, 0))
        self.tg_port = tk.StringVar(value=str(get_tg_port()))
        ttk.Entry(tg, textvariable=self.tg_port, width=8).grid(row=1, column=1, sticky="w", padx=6, pady=(6, 0))
        ttk.Button(tg, text="Установить TgProxy", command=self.install_tg).grid(row=1, column=2, padx=4, pady=(6, 0))
        ttk.Button(tg, text="Запустить", command=self.start_tg).grid(row=1, column=3, padx=4, pady=(6, 0))
        ttk.Button(tg, text="Остановить", command=self.stop_tg).grid(row=1, column=4, padx=4, pady=(6, 0))
        self.tg_autostart = tk.BooleanVar(value=False)
        ttk.Checkbutton(tg, text="Автозапуск TgProxy (systemd --user)", variable=self.tg_autostart,
                        command=self.toggle_tg_autostart).grid(row=2, column=0, columnspan=3, sticky="w", pady=(6, 0))

        # App autostart
        appf = ttk.LabelFrame(main, text="Приложение", padding=10)
        appf.pack(fill="x", pady=(0, 8))
        self.app_autostart = tk.BooleanVar(value=os.path.exists(AUTOSTART_FILE))
        ttk.Checkbutton(appf, text="Автозапуск приложения при входе", variable=self.app_autostart,
                        command=self.toggle_app_autostart).pack(side="left")
        ttk.Button(appf, text="Проверить доступность сервисов", command=self.run_checks).pack(side="left", padx=12)

        # Log
        logf = ttk.LabelFrame(main, text="Журнал", padding=10)
        logf.pack(fill="both", expand=True)
        self.log_text = tk.Text(logf, height=14, state="disabled", wrap="word")
        self.log_text.pack(fill="both", expand=True)

    def pick_base(self):
        d = filedialog.askdirectory(initialdir=os.path.expanduser("~"))
        if d:
            self.base_var.set(d)
            self.base_dir = d
            self.state["base_dir"] = d
            save_state(self.state)
            self.load_strategies()

    def install_base(self):
        d = self.base_var.get().strip() or BASE_DIR
        self.base_dir = d
        self.state["base_dir"] = d
        save_state(self.state)

        def work():
            if os.path.isdir(os.path.join(d, ".git")):
                self.log("Обновление базы...")
                c, o = run_cmd(["git", "-C", d, "pull", "--ff-only"])
            else:
                os.makedirs(os.path.dirname(d), exist_ok=True)
                self.log("Клонирование базы...")
                c, o = run_cmd(["git", "clone", "--depth", "1", BASE_REPO, d])
            self.log(o.strip() or ("Готово" if c == 0 else f"Ошибка ({c})"))
            self.after(0, self.load_strategies)
        threading.Thread(target=work, daemon=True).start()

    def service_sh(self):
        return os.path.join(self.base_var.get().strip() or self.base_dir, "service.sh")

    def run_service(self, args):
        sh = self.service_sh()
        if not os.path.exists(sh):
            messagebox.showwarning(APP_NAME, "База не найдена. Установите/укажите путь к zapret-discord-youtube-linux.")
            return

        def work():
            # service management usually needs sudo
            cmd = ["bash", sh] + args
            self.log("$ " + " ".join(cmd))
            c, o = run_cmd(cmd, cwd=os.path.dirname(sh))
            self.log(o.strip() or f"Код: {c}")
        threading.Thread(target=work, daemon=True).start()

    def open_service_menu(self):
        sh = self.service_sh()
        if not os.path.exists(sh):
            messagebox.showwarning(APP_NAME, "База не найдена.")
            return
        # open in terminal emulator
        term = None
        for t in ["x-terminal-emulator", "gnome-terminal", "konsole", "xfce4-terminal", "xterm"]:
            if shutil.which(t):
                term = t; break
        if not term:
            messagebox.showinfo(APP_NAME, f"Запустите вручную в терминале:\ncd {os.path.dirname(sh)} && bash service.sh")
            return
        try:
            if term == "gnome-terminal":
                subprocess.Popen([term, "--", "bash", "-lc", f"cd '{os.path.dirname(sh)}' && bash service.sh; exec bash"])
            elif term == "konsole":
                subprocess.Popen([term, "-e", "bash", "-lc", f"cd '{os.path.dirname(sh)}' && bash service.sh; exec bash"])
            else:
                subprocess.Popen([term, "-e", "bash", "-lc", f"cd '{os.path.dirname(sh)}' && bash service.sh; exec bash"])
        except Exception as e:
            messagebox.showerror(APP_NAME, str(e))

    def load_strategies(self):
        base = self.base_var.get().strip() or self.base_dir
        candidates = []
        for root in [os.path.join(base, "zapret-latest"), os.path.join(base, "custom-strategies"), base]:
            if not os.path.isdir(root):
                continue
            try:
                for name in os.listdir(root):
                    if name.startswith("general") and name.endswith(".bat"):
                        candidates.append(os.path.join(root, name))
            except Exception:
                pass
        # unique by basename
        seen = set(); items = []
        for p in sorted(candidates):
            b = os.path.basename(p)
            if b in seen: continue
            seen.add(b); items.append(b)
        self.strategy_combo["values"] = items
        if items and not self.strategy_var.get():
            self.strategy_var.set(items[0])
        self.log(f"Стратегий найдено: {len(items)}")

    def apply_strategy(self):
        base = self.base_var.get().strip() or self.base_dir
        conf = os.path.join(base, "conf.env")
        strat = self.strategy_var.get().strip()
        if not strat:
            return
        try:
            lines = []
            if os.path.exists(conf):
                with open(conf, "r", encoding="utf-8", errors="ignore") as f:
                    lines = f.read().splitlines()
            out = []; found = False
            for ln in lines:
                if ln.startswith("strategy="):
                    out.append(f"strategy={strat}"); found = True
                else:
                    out.append(ln)
            if not found:
                out.append(f"strategy={strat}")
            with open(conf, "w", encoding="utf-8") as f:
                f.write("\n".join(out) + "\n")
            self.log(f"Стратегия сохранена в conf.env: {strat}. Перезапустите сервис для применения.")
        except Exception as e:
            messagebox.showerror(APP_NAME, str(e))

    def install_tg(self):
        def work():
            ensure_tg_conf()
            self.log("Скачивание TgProxy...")
            c, o = run_cmd(["curl", "-fL", "--retry", "3", "-o", TG_BIN + ".part", TG_RELEASE])
            if c != 0:
                self.log("Ошибка загрузки: " + o); return
            os.replace(TG_BIN + ".part", TG_BIN)
            os.chmod(TG_BIN, 0o755)
            self.log("TgProxy установлен.")
            self.after(0, self.refresh_tg)
        threading.Thread(target=work, daemon=True).start()

    def start_tg(self):
        ensure_tg_conf()
        if not os.path.exists(TG_BIN):
            messagebox.showinfo(APP_NAME, "Сначала установите TgProxy."); return
        if port_open(get_tg_port()):
            self.log("TgProxy уже запущен."); self.refresh_tg(); return
        try:
            with open(TG_LOG, "a", encoding="utf-8") as logf:
                subprocess.Popen([TG_BIN], cwd=TG_DIR, stdout=logf, stderr=logf, start_new_session=True)
            self.log("Запуск TgProxy...")
        except Exception as e:
            self.log("Ошибка запуска: " + str(e))
        self.after(800, self.refresh_tg)

    def stop_tg(self):
        run_cmd(["pkill", "-f", TG_BIN])
        self.log("TgProxy остановлен.")
        self.after(300, self.refresh_tg)

    def refresh_tg(self):
        p = get_tg_port()
        if port_open(p):
            self.tg_status.set(f"TgProxy: работает (127.0.0.1:{p})")
        else:
            self.tg_status.set("TgProxy: не запущен")
        self.refresh_tg_autostart()

    def refresh_tg_autostart(self):
        c, o = run_cmd(["systemctl", "--user", "is-enabled", "tg-ws-proxy.service"])
        self.tg_autostart.set(c == 0 and "enabled" in o)

    def toggle_tg_autostart(self):
        svc_dir = os.path.join(os.path.expanduser("~"), ".config", "systemd", "user")
        svc = os.path.join(svc_dir, "tg-ws-proxy.service")
        os.makedirs(svc_dir, exist_ok=True)
        if self.tg_autostart.get():
            unit = f"""[Unit]
Description=TG WS Proxy (Zapret Control)
After=network-online.target

[Service]
Type=simple
ExecStart={TG_BIN}
Restart=on-failure
RestartSec=3

[Install]
WantedBy=default.target
"""
            with open(svc, "w", encoding="utf-8") as f:
                f.write(unit)
            run_cmd(["systemctl", "--user", "daemon-reload"])
            run_cmd(["systemctl", "--user", "enable", "--now", "tg-ws-proxy.service"])
            self.log("Автозапуск TgProxy включён.")
        else:
            run_cmd(["systemctl", "--user", "disable", "--now", "tg-ws-proxy.service"])
            self.log("Автозапуск TgProxy выключен.")

    def toggle_app_autostart(self):
        os.makedirs(AUTOSTART_DIR, exist_ok=True)
        if self.app_autostart.get():
            exe = os.path.abspath(sys.argv[0])
            desktop = f"""[Desktop Entry]
Type=Application
Name=Zapret Control
Exec={sys.executable} {exe}
X-GNOME-Autostart-enabled=true
"""
            with open(AUTOSTART_FILE, "w", encoding="utf-8") as f:
                f.write(desktop)
            self.log("Автозапуск приложения включён.")
        else:
            try: os.remove(AUTOSTART_FILE)
            except Exception: pass
            self.log("Автозапуск приложения выключен.")

    def run_checks(self):
        targets = [("YouTube", "www.youtube.com", 443), ("Discord", "discord.com", 443),
                   ("Telegram", "telegram.org", 443)]
        def work():
            for name, host, port in targets:
                try:
                    with socket.create_connection((host, port), timeout=4):
                        self.log(f"{name}: доступен")
                except Exception as e:
                    self.log(f"{name}: недоступен ({e.__class__.__name__})")
        threading.Thread(target=work, daemon=True).start()

    def refresh_all(self):
        self.load_strategies()
        self.refresh_tg()


if __name__ == "__main__":
    App().mainloop()
