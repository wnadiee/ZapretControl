#!/usr/bin/env python3
import json
import os
import queue
import shutil
import socket
import subprocess
import sys
import threading
import time
import tkinter as tk
from tkinter import ttk, messagebox, filedialog

APP = "Zapret Control"
BASE_REPO = "https://github.com/Sergeydigl3/zapret-discord-youtube-linux.git"
TG_RELEASE = "https://github.com/Flowseal/tg-ws-proxy/releases/latest/download/TgWsProxy_linux_amd64"
DATA = os.path.join(os.path.expanduser("~"), ".local", "share", "zapret-control-linux")
CONF = os.path.join(os.path.expanduser("~"), ".config", "zapret-control-linux")
STATE = os.path.join(CONF, "state.json")
TG_DIR = os.path.join(DATA, "tgproxy")
TG_BIN = os.path.join(TG_DIR, "tg-ws-proxy")
TG_CONF = os.path.join(os.path.expanduser("~"), ".config", "TgWsProxy", "config.json")
TG_LOG = os.path.join(TG_DIR, "tg-ws-proxy.log")
AUTOSTART = os.path.join(os.path.expanduser("~"), ".config", "autostart", "zapret-control.desktop")

BG = "#111217"
CARD = "#1A1C23"
BORDER = "#282B35"
TEXT = "#ECEDF1"
DIM = "#9297A3"
ACCENT = "#7C5CFF"
GREEN = "#2ECC71"
RED = "#E74C3C"
YELLOW = "#F1C40F"
BTN = "#22242D"
BTN_ACC = "#3A3060"

for d in [DATA, CONF, TG_DIR, os.path.dirname(TG_CONF), os.path.dirname(AUTOSTART)]:
    os.makedirs(d, exist_ok=True)


def load_state():
    try:
        with open(STATE, "r", encoding="utf-8") as f:
            return json.load(f)
    except Exception:
        return {"base_dir": os.path.join(DATA, "base")}


def save_state(s):
    try:
        with open(STATE, "w", encoding="utf-8") as f:
            json.dump(s, f, ensure_ascii=False, indent=2)
    except Exception:
        pass


def run_cmd(args, cwd=None):
    try:
        p = subprocess.run(args, cwd=cwd, text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
        return p.returncode, p.stdout or ""
    except Exception as e:
        return 1, str(e)


def port_open(port, host="127.0.0.1", timeout=0.7):
    try:
        with socket.create_connection((host, int(port)), timeout=timeout):
            return True
    except Exception:
        return False


def tg_port():
    try:
        with open(TG_CONF, "r", encoding="utf-8") as f:
            return int(json.load(f).get("port", 1443))
    except Exception:
        return 1443


def ensure_tg_conf():
    if not os.path.exists(TG_CONF):
        os.makedirs(os.path.dirname(TG_CONF), exist_ok=True)
        with open(TG_CONF, "w", encoding="utf-8") as f:
            json.dump({"port": 1443, "host": "127.0.0.1"}, f, indent=2)


class Card(tk.Frame):
    def __init__(self, parent, x, y, w, h):
        super().__init__(parent, bg=CARD, highlightbackground=BORDER, highlightthickness=1, bd=0)
        self.place(x=x, y=y, width=w, height=h)


class Dot(tk.Canvas):
    def __init__(self, parent, x, y, color=DIM):
        super().__init__(parent, width=14, height=14, bg=CARD, highlightthickness=0, bd=0)
        self.place(x=x, y=y)
        self.set(color)

    def set(self, color):
        self.delete("all")
        self.create_oval(2, 2, 12, 12, fill=color, outline=color)


class Toggle(tk.Canvas):
    def __init__(self, parent, x, y, value=False, command=None):
        super().__init__(parent, width=56, height=28, bg=CARD, highlightthickness=0, bd=0, cursor="hand2")
        self.place(x=x, y=y)
        self.value = bool(value)
        self.command = command
        self.bind("<Button-1>", self._click)
        self.draw()

    def draw(self):
        self.delete("all")
        bg = ACCENT if self.value else "#3E424E"
        self.create_oval(0, 0, 28, 28, fill=bg, outline=bg)
        self.create_oval(28, 0, 56, 28, fill=bg, outline=bg)
        self.create_rectangle(14, 0, 42, 28, fill=bg, outline=bg)
        cx = 40 if self.value else 16
        self.create_oval(cx-10, 4, cx+10, 24, fill="white", outline="white")

    def set(self, v):
        self.value = bool(v)
        self.draw()

    def _click(self, e=None):
        self.value = not self.value
        self.draw()
        if self.command:
            self.command(self.value)


class App(tk.Tk):
    def __init__(self):
        super().__init__()
        self.title(APP)
        self.geometry("900x980")
        self.configure(bg=BG)
        self.resizable(False, False)
        self.state = load_state()
        self.base_dir = self.state.get("base_dir") or os.path.join(DATA, "base")
        self.q = queue.Queue()
        self.checks_expanded = True
        self._build()
        self.after(200, self._drain)
        self.after(500, self.refresh_all)
        self.after(2000, self._tick)

    def log(self, msg):
        ts = time.strftime("%H:%M:%S")
        self.q.put(f"{ts}   {msg}")

    def _drain(self):
        try:
            while True:
                m = self.q.get_nowait()
                self.logw.configure(state="normal")
                self.logw.insert("end", m + "\n")
                self.logw.see("end")
                self.logw.configure(state="disabled")
        except queue.Empty:
            pass
        self.after(200, self._drain)

    def lbl(self, parent, text, x, y, size=9, bold=False, color=TEXT):
        f = ("Segoe UI", size, "bold" if bold else "normal")
        w = tk.Label(parent, text=text, bg=CARD if isinstance(parent, Card) else BG, fg=color, font=f)
        w.place(x=x, y=y)
        return w

    def btn(self, parent, text, x, y, w, h, cmd, accent=False):
        bg = BTN_ACC if accent else BTN
        fg = "white" if accent else TEXT
        b = tk.Button(parent, text=text, command=cmd, bg=bg, fg=fg, activebackground=bg, activeforeground=fg,
                      relief="flat", bd=0, font=("Segoe UI", 9, "bold"), cursor="hand2")
        b.place(x=x, y=y, width=w, height=h)
        return b

    def _build(self):
        tk.Label(self, text="ZAPRET", bg=BG, fg=TEXT, font=("Segoe UI", 24, "bold")).place(x=26, y=8)
        self.pill = tk.Label(self, text="● неактивен", bg="#1E2028", fg=DIM, font=("Segoe UI", 10, "bold"), padx=16, pady=8)
        self.pill.place(x=692, y=14, width=182, height=44)

        self._service_card()
        self._strategy_card()
        self._checks_card()
        self._settings_card()
        self._telegram_card()
        self._log_area()

    def _service_card(self):
        c = Card(self, 26, 78, 848, 140)
        self.svc_card = c
        self.power = tk.Canvas(c, width=72, height=72, bg=CARD, highlightthickness=0, cursor="hand2")
        self.power.place(x=20, y=34)
        self.power.bind("<Button-1>", lambda e: self.toggle_service())
        self._draw_power(False)
        self.state_lbl = self.lbl(c, "Неактивен", 114, 22, 15, True)
        self.sub_lbl = self.lbl(c, "Служба zapret", 116, 54, 9, False, DIM)
        self.btn(c, "Перезапустить", 560, 22, 150, 32, self.restart_service)
        self.btn(c, "Папка", 720, 22, 108, 32, self.open_folder)
        self.lbl(c, "Автозапуск службы", 560, 78, 9, False, DIM)
        self.svc_auto = Toggle(c, 784, 74, False, self.on_svc_auto)

    def _draw_power(self, on):
        cv = self.power; cv.delete("all")
        col = GREEN if on else "#3E424E"
        cv.create_oval(4, 4, 68, 68, fill=col, outline=col)
        cv.create_arc(23, 23, 49, 49, start=-55, extent=290, style="arc", outline="white", width=4)
        cv.create_line(36, 17, 36, 34, fill="white", width=4, capstyle="round")

    def _strategy_card(self):
        c = Card(self, 26, 230, 848, 96)
        self.lbl(c, "СТРАТЕГИЯ", 20, 12, 8, True, DIM)
        self.strategy_var = tk.StringVar()
        self.strategy_cb = ttk.Combobox(c, textvariable=self.strategy_var, state="readonly", width=52)
        self.strategy_cb.place(x=18, y=36, width=540, height=30)
        self.btn(c, "Применить", 572, 34, 150, 34, self.apply_strategy, accent=True)

    def _checks_card(self):
        c = Card(self, 26, 338, 848, 226)
        self.checks_card = c
        self.lbl(c, "ПРОВЕРКА ДОСТУПА", 20, 12, 8, True, DIM)
        self.chk_toggle = self.btn(c, "▴", 788, 8, 40, 28, self.toggle_checks)
        self.check_rows = []
        names = [("YouTube", 40), ("Discord", 70), ("Roblox", 100), ("SoundCloud", 130), ("Telegram", 160)]
        for name, y in names:
            d = Dot(c, 24, y+4)
            self.lbl(c, name, 44, y-2, 10, True, TEXT)
            st = self.lbl(c, "Проверка…", 200, y-1, 9, False, DIM)
            self.check_rows.append((name, d, st, y))
        self.btn(c, "Проверить", 24, 186, 120, 30, self.run_checks)

    def _settings_card(self):
        c = Card(self, 26, 562, 848, 136)
        self.settings_card = c
        self.lbl(c, "НАСТРОЙКИ", 20, 12, 8, True, DIM)
        self.lbl(c, "Game Filter", 24, 42, 9, True)
        self.gf = ttk.Combobox(c, values=["disabled", "all", "tcp", "udp"], state="readonly", width=12)
        self.gf.place(x=120, y=38, width=130); self.gf.set("disabled")
        self.lbl(c, "IPSet Filter", 282, 42, 9, True)
        self.ipset = ttk.Combobox(c, values=["loaded", "none", "any"], state="readonly", width=12)
        self.ipset.place(x=374, y=38, width=130); self.ipset.set("loaded")
        self.lbl(c, "Auto-Update", 536, 42, 9, True)
        self.au = ttk.Combobox(c, values=["disabled", "enabled"], state="readonly", width=12)
        self.au.place(x=636, y=38, width=130); self.au.set("disabled")
        self.lbl(c, "Автозапуск приложения", 430, 92, 9, True)
        self.app_auto = Toggle(c, 596, 86, os.path.exists(AUTOSTART), self.on_app_auto)
        self.btn(c, "Сохранить", 24, 84, 150, 34, self.save_settings, accent=True)
        self.btn(c, "Replace fakes", 186, 84, 150, 34, self.replace_fakes)

    def _telegram_card(self):
        c = Card(self, 26, 710, 848, 114)
        self.tg_card = c
        self.lbl(c, "TELEGRAM / TgProxy", 20, 12, 8, True, DIM)
        self.tg_dot = Dot(c, 24, 42)
        self.tg_status = self.lbl(c, "TgProxy выключен", 44, 35, 10, True)
        self.lbl(c, "Порт", 24, 74, 9, False, DIM)
        self.tg_port_var = tk.StringVar(value=str(tg_port()))
        tk.Entry(c, textvariable=self.tg_port_var, bg="#22242D", fg=TEXT, insertbackground=TEXT, relief="flat", width=6).place(x=62, y=70, width=70, height=24)
        self.btn(c, "OK", 138, 68, 44, 28, self.save_tg_port)
        self.btn(c, "Настройки", 192, 68, 110, 28, self.tg_settings)
        self.btn(c, "Запустить", 490, 26, 120, 34, self.start_tg)
        self.btn(c, "Подключить Telegram", 620, 26, 150, 34, self.connect_tg, accent=True)
        self.btn(c, "Стоп", 780, 26, 48, 34, self.stop_tg)

    def _log_area(self):
        tk.Label(self, text="ЖУРНАЛ", bg=BG, fg=DIM, font=("Segoe UI", 8, "bold")).place(x=30, y=840)
        p = tk.Frame(self, bg="#0D0E12", highlightbackground="#21242C", highlightthickness=1)
        p.place(x=26, y=860, width=848, height=86)
        self.log_panel = p
        self.logw = tk.Text(p, bg="#0D0E12", fg="#A8AEBC", font=("Consolas", 9), relief="flat", bd=0, wrap="word", state="disabled")
        self.logw.place(x=10, y=6, width=826, height=72)

    # --- logic ---
    def base_path(self): return self.base_dir
    def service_sh(self): return os.path.join(self.base_path(), "service.sh")
    def conf_env(self): return os.path.join(self.base_path(), "conf.env")

    def refresh_all(self):
        self.load_strategies()
        self.refresh_tg()
        self.load_settings()

    def load_strategies(self):
        base = self.base_path(); items = []; seen=set()
        for root in [os.path.join(base, "zapret-latest"), os.path.join(base, "custom-strategies"), base]:
            if not os.path.isdir(root): continue
            try:
                for n in os.listdir(root):
                    if n.startswith("general") and n.endswith(".bat") and n not in seen:
                        seen.add(n); items.append(n)
            except Exception: pass
        items.sort()
        self.strategy_cb['values'] = items
        if items and not self.strategy_var.get():
            cur = self._read_conf_key('strategy')
            self.strategy_var.set(cur if cur in items else items[0])

    def _read_conf_key(self, key):
        try:
            with open(self.conf_env(), 'r', encoding='utf-8', errors='ignore') as f:
                for ln in f:
                    if ln.startswith(key+'='): return ln.split('=',1)[1].strip()
        except Exception: pass
        return ''

    def _write_conf_keys(self, updates):
        lines=[]; seen=set()
        try:
            if os.path.exists(self.conf_env()):
                with open(self.conf_env(),'r',encoding='utf-8',errors='ignore') as f: lines=f.read().splitlines()
        except Exception: pass
        out=[]
        for ln in lines:
            k = ln.split('=',1)[0] if '=' in ln else ''
            if k in updates: out.append(f"{k}={updates[k]}"); seen.add(k)
            else: out.append(ln)
        for k,v in updates.items():
            if k not in seen: out.append(f"{k}={v}")
        try:
            with open(self.conf_env(),'w',encoding='utf-8') as f: f.write('\n'.join(out)+'\n')
        except Exception as e: self.log(f"Ошибка записи conf.env: {e}")

    def apply_strategy(self):
        st = self.strategy_var.get().strip()
        if not st: return
        self._write_conf_keys({'strategy': st})
        self.log(f"Стратегия сохранена: {st}")
        self.run_service(['service','restart'])

    def run_service(self, args):
        sh = self.service_sh()
        if not os.path.exists(sh):
            self.log("База zapret не найдена. Укажите путь/установите базу."); return
        def work():
            self.log("$ bash service.sh " + " ".join(args))
            c,o = run_cmd(["bash", sh]+args, cwd=os.path.dirname(sh))
            self.log((o.strip() or f"Код {c}")[:4000])
        threading.Thread(target=work, daemon=True).start()

    def toggle_service(self): self.run_service(['service','start'])
    def restart_service(self): self.run_service(['service','restart'])
    def open_folder(self):
        d = self.base_path()
        if os.path.isdir(d): subprocess.Popen(['xdg-open', d])
        else: self.log('Папка базы не найдена.')

    def toggle_checks(self):
        self.checks_expanded = not self.checks_expanded
        h = 226 if self.checks_expanded else 52
        self.checks_card.place_configure(height=h)
        shift = 0 if self.checks_expanded else 174
        self.settings_card.place_configure(y=562-shift)
        self.tg_card.place_configure(y=710-shift)
        self.log_panel.place_configure(y=860-shift)
        self.chk_toggle.config(text='▴' if self.checks_expanded else '▾')
        for _,d,st,y in self.check_rows:
            if self.checks_expanded: d.place(x=24,y=y+4); st.place(x=200,y=y-1)
            else: d.place_forget(); st.place_forget()

    def run_checks(self):
        targets=[('YouTube','youtube.com',443),('Discord','discord.com',443),('Roblox','roblox.com',443),('SoundCloud','soundcloud.com',443),('Telegram','telegram.org',443)]
        def work():
            for name,host,port in targets:
                try:
                    socket.create_connection((host,port),timeout=4)
                    res=(GREEN,'Доступен')
                except Exception:
                    res=(RED,'Недоступен')
                self.after(0, lambda n=name,r=res: self._set_check(n,r))
        for _,d,st,_ in self.check_rows: self.after(0, lambda d=d,st=st: (d.set(YELLOW), st.config(text='Проверка…', fg=DIM)))
        threading.Thread(target=work, daemon=True).start()

    def _set_check(self,name,res):
        for n,d,st,_ in self.check_rows:
            if n==name: d.set(res[0]); st.config(text=res[1], fg=TEXT if res[0]==GREEN else DIM)

    def save_settings(self):
        self._write_conf_keys({'gamefilter': self.gf.get(), 'ipset': self.ipset.get(), 'autoupdate': self.au.get()})
        self.log('Настройки сохранены.')

    def load_settings(self):
        self.gf.set(self._read_conf_key('gamefilter') or 'disabled')
        self.ipset.set(self._read_conf_key('ipset') or 'loaded')
        self.au.set(self._read_conf_key('autoupdate') or 'disabled')

    def replace_fakes(self): self.log('Replace fakes: используйте инструменты базы zapret при необходимости.')

    def on_svc_auto(self, v): self.log('Автозапуск службы: настройте через "Сервис: install" в меню базы.')
    def on_app_auto(self, v):
        if v:
            exe=os.path.abspath(sys.argv[0])
            desk=f"[Desktop Entry]\nType=Application\nName=Zapret Control\nExec={sys.executable} {exe}\nX-GNOME-Autostart-enabled=true\n"
            with open(AUTOSTART,'w',encoding='utf-8') as f: f.write(desk)
            self.log('Автозапуск приложения включён.')
        else:
            try: os.remove(AUTOSTART)
            except Exception: pass
            self.log('Автозапуск приложения выключен.')

    # TgProxy
    def refresh_tg(self):
        p=tg_port()
        ok=port_open(p)
        self.tg_dot.set(GREEN if ok else DIM)
        self.tg_status.config(text=f"TgProxy работает · 127.0.0.1:{p}" if ok else "TgProxy выключен", fg=TEXT if ok else DIM)

    def install_tg(self):
        def work():
            ensure_tg_conf(); self.log('Скачивание TgProxy...')
            c,o=run_cmd(['curl','-fL','--retry','3','-o',TG_BIN+'.part',TG_RELEASE])
            if c!=0: self.log('Ошибка: '+o); return
            os.replace(TG_BIN+'.part',TG_BIN); os.chmod(TG_BIN,0o755); self.log('TgProxy установлен.'); self.after(0,self.refresh_tg)
        threading.Thread(target=work, daemon=True).start()

    def start_tg(self):
        ensure_tg_conf()
        if not os.path.exists(TG_BIN): self.log('Сначала установите TgProxy.'); return
        if port_open(tg_port()): self.log('TgProxy уже запущен.'); self.refresh_tg(); return
        try:
            with open(TG_LOG,'a',encoding='utf-8') as lf: subprocess.Popen([TG_BIN], cwd=TG_DIR, stdout=lf, stderr=lf, start_new_session=True)
            self.log('Запуск TgProxy...')
        except Exception as e: self.log('Ошибка: '+str(e))
        self.after(1000,self.refresh_tg)

    def stop_tg(self): run_cmd(['pkill','-f',TG_BIN]); self.log('TgProxy остановлен.'); self.after(300,self.refresh_tg)
    def save_tg_port(self):
        try: p=int(self.tg_port_var.get().strip())
        except: p=1443
        ensure_tg_conf()
        try:
            d={}
            with open(TG_CONF,'r',encoding='utf-8') as f: d=json.load(f)
        except: d={}
        d['port']=p
        with open(TG_CONF,'w',encoding='utf-8') as f: json.dump(d,f,indent=2)
        self.log(f'Порт TgProxy: {p}')

    def tg_settings(self): messagebox.showinfo(APP, f'Конфиг TgProxy:\n{TG_CONF}')
    def connect_tg(self):
        if not port_open(tg_port()): self.log('Сначала запустите TgProxy.'); return
        p=tg_port(); url=f'tg://proxy?server=127.0.0.1&port={p}&type=socks5'
        try: subprocess.Popen(['xdg-open', url]); self.log('Открываю подключение в Telegram...')
        except Exception as e: self.log(str(e))

    def _tick(self):
        self.refresh_tg()
        self.after(3000, self._tick)


if __name__ == '__main__':
    App().mainloop()
