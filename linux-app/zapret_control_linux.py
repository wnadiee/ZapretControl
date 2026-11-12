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
BG = "#0F1117"
BG2 = "#141824"
CARD = "#191C26"
CARD2 = "#1E2230"
BORDER = "#2C3140"
TEXT = "#ECEEF5"
DIM = "#9096A8"
ACCENT = "#7C5CFF"
ACCENT2 = "#9A7BFF"
GREEN = "#2ECC71"
RED = "#E05A5A"
YELLOW = "#D7A63A"
BTN = "#252938"
BTN_H = "#2E3346"
BTN_ACC = "#4A3A84"
BTN_ACC_H = "#5A48A0"
def ensure_dirs():
    for d in [DATA, CONF, TG_DIR, os.path.dirname(AUTOSTART)]:
        os.makedirs(d, exist_ok=True)
def load_state():
    try:
        with open(STATE, "r", encoding="utf-8") as f:
            return json.load(f)
    except Exception:
        return {}
def save_state(s):
    ensure_dirs()
    try:
        with open(STATE, "w", encoding="utf-8") as f:
            json.dump(s, f, ensure_ascii=False, indent=2)
    except Exception:
        pass
def run_cmd(args, cwd=None):
    try:
        p = subprocess.run(args, cwd=cwd, text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, timeout=60)
        return p.returncode, p.stdout or ""
    except Exception as e:
        return 1, str(e)
def port_open(port, host="127.0.0.1", timeout=0.5):
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
            json.dump({"port": 1443, "host": "127.0.0.1", "pool": 4, "buf_kb": 256, "log_max_mb": 5}, f, indent=2)
class Lang:
    en = False
    _listeners = []
    @staticmethod
    def s(ru, en):
        return en if Lang.en else ru
    @staticmethod
    def toggle():
        Lang.en = not Lang.en
        for cb in list(Lang._listeners):
            try:
                cb()
            except Exception:
                pass
L = Lang.s
class Card(tk.Frame):
    def __init__(self, parent):
        super().__init__(parent, bg=CARD, highlightbackground=BORDER, highlightthickness=1, bd=0)
class Dot(tk.Canvas):
    def __init__(self, parent, color=DIM):
        super().__init__(parent, width=14, height=14, bg=CARD, highlightthickness=0, bd=0)
        self._id = self.create_oval(2, 2, 12, 12, fill=color, outline="")
    def set(self, color):
        self.itemconfig(self._id, fill=color)
class Toggle(tk.Canvas):
    def __init__(self, parent, value=False, command=None):
        super().__init__(parent, width=56, height=28, bg=CARD, highlightthickness=0, bd=0, cursor="hand2")
        self.value = bool(value)
        self.command = command
        self.bind("<Button-1>", self._click)
        self.draw()
    def draw(self):
        self.delete("all")
        bg = ACCENT if self.value else "#3E424E"
        self.create_oval(2, 2, 26, 26, fill=bg, outline="")
        self.create_oval(30, 2, 54, 26, fill=bg, outline="")
        self.create_rectangle(14, 2, 42, 26, fill=bg, outline="")
        x = 32 if self.value else 6
        self.create_oval(x, 4, x+20, 24, fill="white", outline="")
    def _click(self, _):
        self.value = not self.value
        self.draw()
        if self.command:
            self.command(self.value)
    def set(self, v):
        self.value = bool(v)
        self.draw()
class LangToggle(tk.Canvas):
    def __init__(self, parent):
        super().__init__(parent, width=74, height=28, bg=BG, highlightthickness=0, bd=0, cursor="hand2")
        self._pos = 1.0 if Lang.en else 0.0
        self._target = self._pos
        self._anim = None
        self.bind("<Button-1>", lambda e: self._toggle())
        self.draw()
    def _toggle(self):
        Lang.toggle()
    def sync(self):
        self._target = 1.0 if Lang.en else 0.0
        self._animate()
    def _animate(self):
        if self._anim:
            try: self.after_cancel(self._anim)
            except Exception: pass
            self._anim = None
        step = 0.18 if self._target > self._pos else -0.18
        self._pos += step
        if (step > 0 and self._pos >= self._target) or (step < 0 and self._pos <= self._target):
            self._pos = self._target
            self.draw()
            return
        self.draw()
        self._anim = self.after(16, self._animate)
    def draw(self):
        self.delete("all")
        w, h = 74, 28
        r = 14
        self.create_oval(0,0,28,28, fill="#2A2E3B", outline="#3A4052")
        self.create_oval(46,0,74,28, fill="#2A2E3B", outline="#3A4052")
        self.create_rectangle(14,0,60,28, fill="#2A2E3B", outline="")
        self.create_line(14,0,60,0, fill="#3A4052")
        self.create_line(14,28,60,28, fill="#3A4052")
        ru_c = TEXT if not Lang.en else DIM
        en_c = TEXT if Lang.en else DIM
        self.create_text(18,14, text="RU", fill=ru_c, font=("Segoe UI", 8, "bold"))
        self.create_text(56,14, text="EN", fill=en_c, font=("Segoe UI", 8, "bold"))
        kx = 4 + int(self._pos * 38)
        self.create_oval(kx,3,kx+22,25, fill=ACCENT, outline=ACCENT2)
class NiceBtn(tk.Canvas):
    def __init__(self, parent, text, cmd, accent=False, width=120, height=32):
        super().__init__(parent, width=width, height=height, bg=parent["bg"], highlightthickness=0, bd=0, cursor="hand2")
        self.text = text
        self.cmd = cmd
        self.accent = accent
        self.hover = False
        self.w = width; self.h = height
        self.bind("<Enter>", lambda e: self._set_hover(True))
        self.bind("<Leave>", lambda e: self._set_hover(False))
        self.bind("<Button-1>", lambda e: self.cmd() if self.cmd else None)
        self.draw()
    def set_text(self, t):
        self.text = t
        self.draw()
    def _set_hover(self, v):
        self.hover = v
        self.draw()
    def draw(self):
        self.delete("all")
        r = 10
        if self.accent:
            fill = BTN_ACC_H if self.hover else BTN_ACC
            border = ACCENT2 if self.hover else ACCENT
            fg = "white"
        else:
            fill = BTN_H if self.hover else BTN
            border = "#40475A" if self.hover else BORDER
            fg = TEXT
        x1,y1,x2,y2 = 1,1,self.w-1,self.h-1
        self.create_arc(x1,y1,x1+2*r,y1+2*r, start=90, extent=90, fill=fill, outline=fill)
        self.create_arc(x2-2*r,y1,x2,y1+2*r, start=0, extent=90, fill=fill, outline=fill)
        self.create_arc(x2-2*r,y2-2*r,x2,y2, start=270, extent=90, fill=fill, outline=fill)
        self.create_arc(x1,y2-2*r,x1+2*r,y2, start=180, extent=90, fill=fill, outline=fill)
        self.create_rectangle(x1+r,y1,x2-r,y2, fill=fill, outline=fill)
        self.create_rectangle(x1,y1+r,x2,y2-r, fill=fill, outline=fill)
        self.create_arc(x1,y1,x1+2*r,y1+2*r, start=90, extent=90, style="arc", outline=border)
        self.create_arc(x2-2*r,y1,x2,y1+2*r, start=0, extent=90, style="arc", outline=border)
        self.create_arc(x2-2*r,y2-2*r,x2,y2, start=270, extent=90, style="arc", outline=border)
        self.create_arc(x1,y2-2*r,x1+2*r,y2, start=180, extent=90, style="arc", outline=border)
        self.create_line(x1+r,y1,x2-r,y1, fill=border)
        self.create_line(x1+r,y2,x2-r,y2, fill=border)
        self.create_line(x1,y1+r,x1,y2-r, fill=border)
        self.create_line(x2,y1+r,x2,y2-r, fill=border)
        self.create_text(self.w//2, self.h//2, text=self.text, fill=fg, font=("Segoe UI Semibold", 9))
class App(tk.Tk):
    def __init__(self):
        super().__init__()
        self.title(APP)
        self.geometry("900x980")
        self.configure(bg=BG)
        self.resizable(False, False)
        ensure_dirs()
        ensure_tg_conf()
        self.state = load_state()
        self.base_dir = self.state.get("base_dir") or self._detect_base()
        Lang.en = self.state.get("lang", "ru") == "en"
        self._lang_widgets = {}
        self._build()
        Lang._listeners.append(self.apply_language)
        self.log(L.s("Готово.", "Ready."))
        self.refresh_all()
        self.after(2500, self._tick)
    def _detect_base(self):
        for c in [os.path.join(DATA, "base"), os.path.expanduser("~/zapret-discord-youtube-linux"), "/opt/zapret"]:
            if os.path.exists(os.path.join(c, "service.sh")):
                return c
        return os.path.join(DATA, "base")
    def lbl(self, parent, text, x, y, size=9, bold=False, color=TEXT):
        font = ("Segoe UI", size, "bold" if bold else "normal")
        bg = CARD if isinstance(parent, (Card, tk.Frame)) and parent.cget('bg') == CARD else (CARD if isinstance(parent, Card) else BG)
        w = tk.Label(parent, text=text, bg=parent["bg"], fg=color, font=font)
        w.place(x=x, y=y)
        return w
    def _build(self):
        hdr = tk.Frame(self, bg=BG)
        hdr.place(x=26, y=14, width=848, height=44)
        tk.Label(hdr, text="ZAPRET", bg=BG, fg=TEXT, font=("Segoe UI", 20, "bold")).place(x=0, y=0)
        self.status_dot = tk.Canvas(hdr, width=12, height=12, bg=BG, highlightthickness=0, bd=0)
        self.status_dot.place(x=748, y=16)
        self.status_dot_id = self.status_dot.create_oval(1,1,11,11, fill="#3E424E", outline="")
        self.pill = tk.Label(hdr, text="", bg=BG, fg=DIM, font=("Segoe UI", 9, "bold"))
        self.pill.place(x=764, y=10)
        self.lang_toggle = LangToggle(hdr)
        self.lang_toggle.place(x=660, y=8)
        self._service_card()
        self._strategy_card()
        self._checks_card()
        self._settings_card()
        self._telegram_card()
        self._log_area()
    def _service_card(self):
        c = Card(self); c.place(x=26, y=64, width=848, height=140)
        self.power_canvas = tk.Canvas(c, width=72, height=72, bg=CARD, highlightthickness=0, bd=0, cursor="hand2")
        self.power_canvas.place(x=20, y=34)
        self.power_canvas.bind("<Button-1>", lambda e: self.toggle_service())
        self.state_lbl = self.lbl(c, "", 114, 22, 15, True)
        self.sub_lbl = self.lbl(c, "", 116, 54, 9, False, DIM)
        self.btn_restart = NiceBtn(c, L("Перезапустить","Restart"), self.restart_service, False, 150, 32); self.btn_restart.place(x=560, y=22)
        self.btn_folder = NiceBtn(c, L("Папка","Folder"), self.open_folder, False, 108, 32); self.btn_folder.place(x=720, y=22)
        self.lbl_svc_auto = self.lbl(c, L("Автозапуск службы","Service autostart"), 560, 78, 9, False, DIM)
        self.svc_auto = Toggle(c, False, self.on_svc_auto); self.svc_auto.place(x=784, y=74)
    def _strategy_card(self):
        c = Card(self); c.place(x=26, y=216, width=848, height=96)
        self.lbl_strategy = self.lbl(c, L("СТРАТЕГИЯ","STRATEGY"), 20, 12, 8, True, DIM)
        self.strategy_var = tk.StringVar()
        self.strategy_cb = ttk.Combobox(c, textvariable=self.strategy_var, state="readonly")
        self.strategy_cb.place(x=18, y=36, width=540, height=30)
        self.btn_apply = NiceBtn(c, L("Применить","Apply"), self.apply_strategy, True, 150, 34); self.btn_apply.place(x=572, y=34)
    def _checks_card(self):
        self.checks_card = Card(self); self.checks_card.place(x=26, y=324, width=848, height=226)
        c = self.checks_card
        self.lbl_checks = self.lbl(c, L("ПРОВЕРКА ДОСТУПА","ACCESS CHECK"), 20, 12, 8, True, DIM)
        self.chk_toggle = tk.Button(c, text="▲", command=self.toggle_checks, bg=BTN, fg=TEXT, relief="flat", bd=0, cursor="hand2")
        self.chk_toggle.place(x=788, y=8, width=40, height=28)
        self.check_rows = []
        targets = ["YouTube","Discord","Roblox","SoundCloud","Telegram"]
        for i, name in enumerate(targets):
            y = 40 + i*30
            d = Dot(c, DIM); d.place(x=24, y=y+4)
            self.lbl(c, name, 44, y-3, 10, True, TEXT)
            st = self.lbl(c, L("проверка...","checking..."), 220, y-2, 9, False, DIM)
            self.check_rows.append((name,d,st,y))
        self.btn_check = NiceBtn(c, L("Проверить","Check"), self.run_checks, False, 120, 30); self.btn_check.place(x=24, y=186)
        self.checks_expanded = True
    def _settings_card(self):
        self.settings_card = Card(self); self.settings_card.place(x=26, y=562, width=848, height=136)
        c = self.settings_card
        self.lbl_settings = self.lbl(c, L("НАСТРОЙКИ","SETTINGS"), 20, 12, 8, True, DIM)
        self.lbl(c, "Game Filter", 24, 42, 9, True)
        self.gf = ttk.Combobox(c, values=["disabled","all","tcp","udp"], state="readonly", width=12); self.gf.place(x=120, y=38)
        self.lbl(c, "IPSet Filter", 282, 42, 9, True)
        self.ipset = ttk.Combobox(c, values=["loaded","none","any"], state="readonly", width=12); self.ipset.place(x=374, y=38)
        self.lbl(c, "Auto-Update", 536, 42, 9, True)
        self.au = ttk.Combobox(c, values=["disabled","enabled"], state="readonly", width=12); self.au.place(x=636, y=38)
        self.lbl_app_auto = self.lbl(c, L("Автозапуск приложения","App autostart"), 430, 92, 9, True)
        self.app_auto = Toggle(c, os.path.exists(AUTOSTART), self.on_app_auto); self.app_auto.place(x=620, y=86)
        self.btn_save = NiceBtn(c, L("Сохранить","Save"), self.save_settings, True, 150, 34); self.btn_save.place(x=24, y=84)
        self.btn_choose = NiceBtn(c, L("Выбрать базу","Choose base"), self.choose_base, False, 150, 34); self.btn_choose.place(x=186, y=84)
    def _telegram_card(self):
        self.tg_card = Card(self); self.tg_card.place(x=26, y=710, width=848, height=114)
        c = self.tg_card
        self.lbl(c, "TELEGRAM / TgProxy", 20, 12, 8, True, DIM)
        self.tg_dot = Dot(c, DIM); self.tg_dot.place(x=24, y=42)
        self.tg_status = self.lbl(c, "", 44, 35, 10, True)
        self.lbl_port = self.lbl(c, L("Порт","Port"), 24, 74, 9, False, DIM)
        self.tg_port_var = tk.StringVar(value=str(tg_port()))
        tk.Entry(c, textvariable=self.tg_port_var, bg="#22242D", fg=TEXT, insertbackground=TEXT, relief="flat", highlightthickness=1, highlightbackground=BORDER).place(x=62, y=70, width=70, height=24)
        NiceBtn(c, "OK", self.save_tg_port, False, 44, 28).place(x=138, y=68)
        self.btn_tg_settings = NiceBtn(c, L("Настройки","Settings"), self.tg_settings, False, 110, 28); self.btn_tg_settings.place(x=192, y=68)
        self.btn_tg_start = NiceBtn(c, L("Запустить","Start"), self.start_tg, False, 130, 34); self.btn_tg_start.place(x=480, y=26)
        self.btn_tg_connect = NiceBtn(c, L("Подключить Telegram","Connect Telegram"), self.connect_tg, True, 160, 34); self.btn_tg_connect.place(x=620, y=26)
        self.btn_tg_stop = NiceBtn(c, L("Стоп","Stop"), self.stop_tg, False, 48, 34); self.btn_tg_stop.place(x=790, y=26)
    def _log_area(self):
        self.lbl_log = tk.Label(self, text=L("ЖУРНАЛ","LOG"), bg=BG, fg=DIM, font=("Segoe UI", 8, "bold"))
        self.lbl_log.place(x=30, y=840)
        p = tk.Frame(self, bg="#0D0E12", highlightbackground="#21242C", highlightthickness=1)
        p.place(x=26, y=860, width=848, height=100)
        self.log_panel = p
        self.logw = tk.Text(p, bg="#0D0E12", fg="#A8AEBC", font=("Consolas", 9), relief="flat", bd=0, wrap="word", state="disabled")
        self.logw.place(x=10, y=6, width=826, height=86)
        self._log_q = queue.Queue()
        self.after(120, self._drain_log)
    def log(self, msg):
        self._log_q.put(msg)
    def _drain_log(self):
        try:
            while True:
                m = self._log_q.get_nowait()
                ts = time.strftime("%H:%M:%S")
                self.logw.configure(state="normal")
                self.logw.insert("end", f"{ts}   {m}\n")
                self.logw.see("end")
                self.logw.configure(state="disabled")
        except queue.Empty:
            pass
        self.after(120, self._drain_log)
    def base_path(self): return self.base_dir
    def service_sh(self): return os.path.join(self.base_path(), "service.sh")
    def conf_env(self): return os.path.join(self.base_path(), "conf.env")
    def _draw_power(self, running):
        c = self.power_canvas
        c.delete("all")
        col = GREEN if running else "#3E424E"
        c.create_oval(4,4,68,68, fill=col, outline="")
        c.create_arc(23,23,49,49, start=-55, extent=290, style="arc", outline="white", width=4)
        c.create_line(36,18,36,30, fill="white", width=4, capstyle="round")
    def _set_window_icon(self, running):
        try:
            col = GREEN if running else "#787C88"
            img = tk.PhotoImage(width=32, height=32)
            for y in range(32):
                for x in range(32):
                    dx, dy = x-16, y-16
                    if dx*dx + dy*dy <= 196:
                        img.put(col, (x,y))
            self._icon_img = img
            self.iconphoto(False, img)
        except Exception:
            pass
    def refresh_all(self):
        self.load_strategies()
        self.load_settings()
        self.refresh_state()
    def is_running(self):
        sh = self.service_sh()
        if not os.path.exists(sh): return False
        c,_ = run_cmd(["bash", sh, "service", "status"], cwd=os.path.dirname(sh))
        if c == 0: return True
        c,_ = run_cmd(["pgrep", "-f", "nfqws|tpws|zapret"], cwd=None)
        return c == 0
    def refresh_state(self):
        running = self.is_running()
        has_base = os.path.exists(self.service_sh())
        self._draw_power(running)
        self.status_dot.itemconfig(self.status_dot_id, fill=(GREEN if running else "#787C88"))
        self.pill.configure(text=L("РАБОТАЕТ","RUNNING") if running else L("ВЫКЛЮЧЕН","STOPPED"), fg=("#7EE8A6" if running else "#969AA5"))
        self.state_lbl.configure(text=L("Активен","Active") if running else (L("Служба не найдена","Service not found") if not has_base else L("Выключен","Stopped")), fg=(GREEN if running else (RED if not has_base else TEXT)))
        st = self._read_conf_key('strategy')
        if not has_base:
            self.sub_lbl.configure(text=L("Установите базу zapret в папку ниже","Install zapret base using the button"))
            self.btn_folder.set_text(L("Установить","Install"))
        else:
            self.sub_lbl.configure(text=(L("Служба zapret","zapret service") + (f" • {st}" if st else "")))
            self.btn_folder.set_text(L("Папка","Folder"))
        self._set_window_icon(running)
        self.refresh_tg()
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
        except Exception as e: self.log(f"{L('Ошибка записи','Write error')} conf.env: {e}")
    def apply_strategy(self):
        st = self.strategy_var.get().strip()
        if not st: return
        self._write_conf_keys({'strategy': st})
        self.log(f"{L('Выбрана стратегия','Selected strategy')}: {st}")
        self.run_service(['service','restart'], after=self.run_checks)
    def run_service(self, args, after=None):
        sh = self.service_sh()
        if not os.path.exists(sh):
            self.log(L("База zapret не найдена. Установите/выберите базу.", "zapret base not found. Install/select base.")); return
        def work():
            cmd = ["bash", sh] + args
            self.log("$ bash service.sh " + " ".join(args))
            c,o = run_cmd(cmd, cwd=os.path.dirname(sh))
            if c != 0 and hasattr(os,'geteuid') and os.geteuid() != 0 and shutil.which("pkexec"):
                self.log(L("Нужны права, пробую pkexec...", "Need privileges, trying pkexec..."))
                c,o = run_cmd(["pkexec", "bash", sh] + args, cwd=os.path.dirname(sh))
            self.log((o.strip() or f"code {c}")[:4000])
            self.after(0, self.refresh_state)
            if after: self.after(0, after)
        threading.Thread(target=work, daemon=True).start()
    def toggle_service(self):
        if self.is_running(): self.run_service(['service','stop'])
        else: self.run_service(['service','start'])
    def restart_service(self): self.run_service(['service','restart'])
    def open_folder(self):
        d = self.base_path()
        if not os.path.isdir(d):
            if messagebox.askyesno(APP, L("База zapret не найдена.\nУстановить автоматически?", "zapret base not found.\nInstall automatically?")):
                self.install_base()
            else:
                picked = filedialog.askdirectory(title=L("Выберите папку zapret","Select zapret folder"), initialdir=DATA)
                if picked:
                    self.base_dir = picked; self.state['base_dir']=picked; save_state(self.state); self.refresh_all()
            return
        try: subprocess.Popen(['xdg-open', d])
        except Exception:
            try: subprocess.Popen(['gio','open',d])
            except Exception as e: self.log(str(e))
    def install_base(self):
        dest = os.path.join(DATA, 'base')
        if os.path.exists(dest):
            self.base_dir = dest; self.state['base_dir']=dest; save_state(self.state); self.refresh_all(); return
        def work():
            self.log(L('Загрузка базы zapret...', 'Downloading zapret base...'))
            c,o = run_cmd(['git','clone','--depth','1', BASE_REPO, dest])
            if c!=0: self.log(L('Ошибка установки: ','Install error: ')+o); return
            self.base_dir = dest; self.state['base_dir']=dest; save_state(self.state)
            self.log(L('База установлена: ','Base installed: ')+dest)
            self.after(0, self.refresh_all)
        threading.Thread(target=work, daemon=True).start()
    def toggle_checks(self):
        self.checks_expanded = not self.checks_expanded
        h = 226 if self.checks_expanded else 52
        self.checks_card.place_configure(height=h)
        shift = 0 if self.checks_expanded else 174
        self.settings_card.place_configure(y=562-shift)
        self.tg_card.place_configure(y=710-shift)
        self.log_panel.place_configure(y=860-shift)
        self.lbl_log.place_configure(y=840-shift)
        self.chk_toggle.config(text='▲' if self.checks_expanded else '▼')
        for _,d,st,y in self.check_rows:
            if self.checks_expanded: d.place(x=24,y=y+4); st.place(x=220,y=y-2)
            else: d.place_forget(); st.place_forget()
    def run_checks(self):
        targets=[('YouTube','youtube.com',443),('Discord','discord.com',443),('Roblox','roblox.com',443),('SoundCloud','soundcloud.com',443),('Telegram','telegram.org',443)]
        def work():
            for name,host,port in targets:
                t=time.time()
                try:
                    socket.create_connection((host,port),timeout=4)
                    ms=int((time.time()-t)*1000)
                    res=(GREEN,f"{L('доступно за','reachable in')} {ms} ms")
                except Exception as e:
                    res=(RED,L('недоступно','unreachable')+' • '+(str(e).split(':')[-1].strip()[:30] or 'error'))
                self.after(0, lambda n=name,r=res: self._set_check(n,r))
        for _,d,st,_ in self.check_rows:
            d.set(YELLOW); st.config(text=L('проверка...','checking...'), fg=DIM)
        threading.Thread(target=work, daemon=True).start()
    def _set_check(self,name,res):
        for n,d,st,_ in self.check_rows:
            if n==name:
                d.set(res[0]); st.config(text=res[1], fg=("#A0D2B4" if res[0]==GREEN else ("#D2A0A0" if res[0]==RED else DIM)))
    def save_settings(self):
        self._write_conf_keys({'gamefilter': self.gf.get(), 'ipset': self.ipset.get(), 'autoupdate': self.au.get()})
        self.state['lang'] = 'en' if Lang.en else 'ru'
        save_state(self.state)
        self.log(L('Настройки сохранены.','Settings saved.'))
        self.refresh_state()
    def load_settings(self):
        self.gf.set(self._read_conf_key('gamefilter') or 'disabled')
        self.ipset.set(self._read_conf_key('ipset') or 'loaded')
        self.au.set(self._read_conf_key('autoupdate') or 'disabled')
    def choose_base(self):
        picked = filedialog.askdirectory(title=L("Выберите папку zapret","Select zapret folder"), initialdir=os.path.dirname(self.base_path()) or DATA)
        if picked:
            self.base_dir = picked; self.state['base_dir']=picked; save_state(self.state)
            self.log(L("Папка базы: ","Base folder: ") + picked); self.refresh_all()
    def on_svc_auto(self, v):
        self.log(L('Автозапуск службы: управляется в настройках базы zapret.', 'Service autostart: managed in zapret base settings.'))
    def on_app_auto(self, v):
        if v:
            exe=os.path.abspath(sys.argv[0])
            desk=f"[Desktop Entry]\nType=Application\nName=Zapret Control\nExec={sys.executable} {exe}\nX-GNOME-Autostart-enabled=true\n"
            try:
                with open(AUTOSTART,'w',encoding='utf-8') as f: f.write(desk)
                self.log(L('Автозапуск приложения включён.','App autostart enabled.'))
            except Exception as e: self.log(str(e))
        else:
            try: os.remove(AUTOSTART)
            except Exception: pass
            self.log(L('Автозапуск приложения выключен.','App autostart disabled.'))
    def refresh_tg(self):
        p=tg_port(); ok=port_open(p)
        self.tg_dot.set(GREEN if ok else "#787C88")
        self.tg_status.config(text=f"TgProxy {L('работает на','running on')} 127.0.0.1:{p}" if ok else L("TgProxy выключен","TgProxy is off"), fg=TEXT if ok else DIM)
        self.btn_tg_start.set_text(L("Запустить","Start") if os.path.exists(TG_BIN) else L("Установить","Install"))
    def install_tg(self):
        def work():
            ensure_tg_conf(); self.log(L('Загрузка TgProxy...','Downloading TgProxy...'))
            c,o=run_cmd(['curl','-fL','--retry','3','-o',TG_BIN+'.part',TG_RELEASE])
            if c!=0: self.log(L('Ошибка: ','Error: ')+o); return
            os.replace(TG_BIN+'.part',TG_BIN); os.chmod(TG_BIN,0o755); self.log(L('TgProxy установлен.','TgProxy installed.')); self.after(0,self.refresh_tg)
        threading.Thread(target=work, daemon=True).start()
    def start_tg(self):
        ensure_tg_conf()
        if not os.path.exists(TG_BIN): self.install_tg(); return
        if port_open(tg_port()): self.log(L('TgProxy уже запущен.','TgProxy already running.')); self.refresh_tg(); return
        try:
            with open(TG_LOG,'a',encoding='utf-8') as lf:
                subprocess.Popen([TG_BIN], cwd=TG_DIR, stdout=lf, stderr=lf, start_new_session=True)
            self.log(L('Запуск TgProxy...','Starting TgProxy...'))
        except Exception as e: self.log(L('Ошибка: ','Error: ')+str(e))
        self.after(1000,self.refresh_tg)
    def stop_tg(self):
        run_cmd(['pkill','-f','tg-ws-proxy']); self.log(L('TgProxy остановлен.','TgProxy stopped.')); self.after(300,self.refresh_tg)
    def save_tg_port(self):
        try: p=int(self.tg_port_var.get().strip())
        except Exception: p=1443
        ensure_tg_conf()
        try:
            with open(TG_CONF,'r',encoding='utf-8') as f: d=json.load(f)
        except Exception: d={}
        d['port']=p
        with open(TG_CONF,'w',encoding='utf-8') as f: json.dump(d,f,indent=2)
        self.log(f"{L('Порт TgProxy','TgProxy port')}: {p}")
    def tg_settings(self):
        ensure_tg_conf()
        dlg = tk.Toplevel(self)
        dlg.title(L("Настройки TgProxy", "TgProxy settings"))
        dlg.configure(bg=BG)
        dlg.geometry("420x360")
        dlg.transient(self)
        try:
            with open(TG_CONF,'r',encoding='utf-8') as f: cfg=json.load(f)
        except Exception: cfg={}
        vars_map = {}
        def add_row(row, label, key, default):
            tk.Label(dlg, text=label, bg=BG, fg=TEXT, font=("Segoe UI", 9)).grid(row=row, column=0, sticky="w", padx=16, pady=6)
            v = tk.StringVar(value=str(cfg.get(key, default)))
            tk.Entry(dlg, textvariable=v, bg="#22242D", fg=TEXT, insertbackground=TEXT, relief="flat", width=16).grid(row=row, column=1, sticky="w", padx=8, pady=6)
            vars_map[key]=v
        tk.Label(dlg, text=L("Подключение","Connection"), bg=BG, fg=DIM, font=("Segoe UI Semibold", 9)).grid(row=0,column=0,columnspan=2,sticky="w",padx=16,pady=(14,4))
        add_row(1, L("Порт","Port"), "port", 1443)
        add_row(2, "Host", "host", "127.0.0.1")
        tk.Label(dlg, text=L("Производительность","Performance"), bg=BG, fg=DIM, font=("Segoe UI Semibold", 9)).grid(row=3,column=0,columnspan=2,sticky="w",padx=16,pady=(10,4))
        add_row(4, L("Пул","Pool"), "pool", 4)
        add_row(5, L("Буфер (КБ)","Buffer (KB)"), "buf_kb", 256)
        add_row(6, L("Лог (МБ)","Log (MB)"), "log_max_mb", 5)
        def save():
            for k,v in vars_map.items():
                val = v.get().strip()
                if k in ("port","pool","buf_kb","log_max_mb"):
                    try: cfg[k]=int(val)
                    except Exception: pass
                else: cfg[k]=val
            try:
                os.makedirs(os.path.dirname(TG_CONF), exist_ok=True)
                with open(TG_CONF,'w',encoding='utf-8') as f: json.dump(cfg,f,indent=2)
                self.log(L("Настройки TgProxy сохранены.","TgProxy settings saved."))
                if port_open(tg_port()):
                    self.stop_tg(); self.after(600, self.start_tg)
            except Exception as e: self.log(str(e))
            dlg.destroy()
        btnf = tk.Frame(dlg, bg=BG); btnf.grid(row=7,column=0,columnspan=2,sticky="e",padx=16,pady=18)
        NiceBtn(btnf, L("Сохранить","Save"), save, True, 120, 32).pack(side="right")
        NiceBtn(btnf, L("Отмена","Cancel"), dlg.destroy, False, 100, 32).pack(side="right", padx=8)
    def connect_tg(self):
        if not port_open(tg_port()): self.log(L('Сначала запустите TgProxy.','Start TgProxy first.')); return
        p=tg_port(); url=f'tg://proxy?server=127.0.0.1&port={p}&type=socks5'
        try: subprocess.Popen(['xdg-open', url]); self.log(L('Открываю подключение в Telegram...','Opening Telegram connection...'))
        except Exception as e: self.log(str(e))
    def apply_language(self):
        self.lang_toggle.sync()
        self.state['lang'] = 'en' if Lang.en else 'ru'
        save_state(self.state)
        self.lbl_strategy.config(text=L("СТРАТЕГИЯ","STRATEGY"))
        self.lbl_checks.config(text=L("ПРОВЕРКА ДОСТУПА","ACCESS CHECK"))
        self.lbl_settings.config(text=L("НАСТРОЙКИ","SETTINGS"))
        self.lbl_svc_auto.config(text=L("Автозапуск службы","Service autostart"))
        self.lbl_app_auto.config(text=L("Автозапуск приложения","App autostart"))
        self.lbl_port.config(text=L("Порт","Port"))
        self.lbl_log.config(text=L("ЖУРНАЛ","LOG"))
        self.btn_restart.set_text(L("Перезапустить","Restart"))
        self.btn_apply.set_text(L("Применить","Apply"))
        self.btn_check.set_text(L("Проверить","Check"))
        self.btn_save.set_text(L("Сохранить","Save"))
        self.btn_choose.set_text(L("Выбрать базу","Choose base"))
        self.btn_tg_settings.set_text(L("Настройки","Settings"))
        self.btn_tg_connect.set_text(L("Подключить Telegram","Connect Telegram"))
        self.btn_tg_stop.set_text(L("Стоп","Stop"))
        for name,d,st,_ in self.check_rows:
            cur = st.cget('text')
            if cur in ("проверка...","checking..."):
                st.config(text=L("проверка...","checking..."))
        self.refresh_state()
    def _tick(self):
        try: self.refresh_state()
        except Exception: pass
        self.after(3000, self._tick)
if __name__ == '__main__':
    App().mainloop()
