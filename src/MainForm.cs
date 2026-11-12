using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Windows.Forms;
namespace ZapretControl
{
    public class MainForm : Form
    {
        private static readonly Color Bg = Color.FromArgb(17, 18, 23);
        private static readonly Color CardBg = Color.FromArgb(26, 28, 35);
        private static readonly Color CardBorder = Color.FromArgb(40, 43, 53);
        private static readonly Color TextMain = Color.FromArgb(236, 237, 241);
        private static readonly Color TextDim = Color.FromArgb(146, 151, 163);
        private static readonly Color Accent = Color.FromArgb(124, 92, 255);
        private static readonly Color Green = Color.FromArgb(46, 204, 113);
        private static readonly Color Red = Color.FromArgb(231, 76, 60);
        private static readonly Color Yellow = Color.FromArgb(241, 196, 15);
        private readonly bool _dev;
        private string _installDir;
        private List<StrategyInfo> _strategies;
        private bool _silent;
        private bool _busy;
        private bool _checking;
        private bool _exiting;
        private bool _trayHintShown;
        private StatusPill _pill;
        private PowerButton _power;
        private Label _stateLabel;
        private Label _subLabel;
        private ToggleSwitch _autoToggle;
        private StrategySelector _strategySelector;
        private Button _applyButton;
        private Button _folderButton;
        private Dot _ytDot;
        private Dot _dcDot;
        private Dot _rbDot;
        private Dot _scDot;
        private Dot _tgDot;
        private Label _ytState;
        private Label _dcState;
        private Label _rbState;
        private Label _scState;
        private Label _tgState;
        private Panel _checksCard;
        private Panel _tgCard;
        private Label _logTitle;
        private Panel _logPanel;
        private Button _checksToggle;
        private LangToggleButton _langToggle;
        private Label _lblStrategy;
        private Label _lblChecks;
        private Label _lblSettings;
        private Label _lblLang;
        private Label _lblAppAutostart;
        private Label _lblSvcAutostart;
        private Label _lblPort;
        private Button _btnRestart;
        private Button _btnApply;
        private Button _btnCheck;
        private Button _btnSave;
        private Button _btnTgSettings;
        private Button _btnTgConnect;
        private Icon _iconOn;
        private Icon _iconOff;
        private bool _lastIconOn;
        private bool _checksExpanded = true;
        private Dot _tgServiceDot;
        private Label _tgServiceLabel;
        private Button _tgStart;
        private Button _tgStop;
        private TextBox _tgPortBox;
        private Panel _settingsCard;
        private ComboBox _gameFilterCombo;
        private ComboBox _ipsetCombo;
        private ComboBox _autoUpdateCombo;
        private ComboBox _langCombo;
        private ToggleSwitch _appAutoToggle;
        private RichTextBox _log;
        private NotifyIcon _tray;
        private Timer _refreshTimer;
        private Timer _checksTimer;
        public MainForm(bool dev)
        {
            _dev = dev;
            _strategies = new List<StrategyInfo>();
            Loc.Init();
            SuspendLayout();
            BuildUi();
            LoadInstall();
            RefreshState();
            ResumeLayout(false);
            InitTimers();
            InitTray();
            Log(Loc.S("Zapret Control запущен.", "Zapret Control started."));
            if (_dev) Log(Loc.S("Тестовый режим: управление службой отключено.", "Test mode: service control is disabled."));
        }
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            if (!TryAutoStart()) RunChecks();
        }
        private void BuildUi()
        {
            Text = "Zapret Control";
            ClientSize = new Size(900, 958);
            BackColor = Bg;
            ForeColor = TextMain;
            Font = new Font("Segoe UI", 9f);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleMode = AutoScaleMode.None;
            DoubleBuffered = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
            UpdateStyles();
            Icon = AppIcon();
            var title = MakeLabel(this, "ZAPRET", 26, 12, 24f, true, TextMain);
            title.Font = new Font("Segoe UI Semibold", 24f);
            _langToggle = new LangToggleButton();
            _langToggle.SetBounds(586, 19, 92, 34);
            _langToggle.Toggled += delegate { ToggleLanguage(); };
            Controls.Add(_langToggle);
            _pill = new StatusPill();
            _pill.SetBounds(692, 14, 182, 44);
            Controls.Add(_pill);
            BuildServiceCard();
            BuildStrategyCard();
            BuildChecksCard();
            BuildSettingsCard();
            BuildTelegramCard();
            BuildLog();
        }
        private void BuildServiceCard()
        {
            var card = CreateCard(26, 78, 848, 140);
            _power = new PowerButton();
            _power.SetBounds(20, 34, 72, 72);
            _power.Click += delegate { ToggleService(); };
            card.Controls.Add(_power);
            _stateLabel = MakeLabel(card, Loc.S("Активен", "Active"), 114, 22, 15f, true, TextMain);
            _subLabel = MakeLabel(card, Loc.S("Служба zapret", "zapret service"), 116, 54, 9f, false, TextDim);
            _btnRestart = MakeButton(card, Loc.S("Перезапустить", "Restart"), 560, 22, 150, 32, false);
            _btnRestart.Click += delegate { RestartService(); };
            var folder = MakeButton(card, Loc.S("Папка", "Folder"), 720, 22, 108, 32, false);
            folder.Click += delegate { OpenInstallFolder(); };
            _folderButton = folder;
            _lblSvcAutostart = MakeLabel(card, Loc.S("Автозапуск службы", "Service autostart"), 560, 78, 9f, false, TextDim);
            _autoToggle = new ToggleSwitch();
            _autoToggle.SetBounds(784, 74, 56, 28);
            _autoToggle.CheckedChanged += OnAutoChanged;
            card.Controls.Add(_autoToggle);
            Controls.Add(card);
        }
        private void BuildStrategyCard()
        {
            var card = CreateCard(26, 230, 848, 96);
            _lblStrategy = MakeLabel(card, Loc.S("СТРАТЕГИЯ", "STRATEGY"), 20, 12, 8f, true, TextDim);
            _strategySelector = new StrategySelector();
            _strategySelector.SetBounds(18, 34, 540, 34);
            _strategySelector.SelectionChanged += delegate { };
            card.Controls.Add(_strategySelector);
            _btnApply = MakeButton(card, Loc.S("Применить", "Apply"), 572, 34, 150, 34, true);
            _applyButton = _btnApply;
            _btnApply.Click += delegate { ApplySelectedStrategy(); };
            Controls.Add(card);
        }
        private void BuildChecksCard()
        {
            var card = CreateCard(26, 338, 848, 226);
            _checksCard = card;
            _lblChecks = MakeLabel(card, Loc.S("ПРОВЕРКА ДОСТУПА", "ACCESS CHECK"), 20, 12, 8f, true, TextDim);
            _checksToggle = MakeButton(card, "▲", 788, 8, 40, 28, false);
            _checksToggle.Font = new Font("Segoe UI", 7.5f);
            _checksToggle.Click += delegate { SetChecksExpanded(!_checksExpanded); };
            _ytDot = AddCheckRow(card, 24, 40, "YouTube", out _ytState);
            _dcDot = AddCheckRow(card, 24, 70, "Discord", out _dcState);
            _rbDot = AddCheckRow(card, 24, 100, "Roblox", out _rbState);
            _scDot = AddCheckRow(card, 24, 130, "SoundCloud", out _scState);
            _tgDot = AddCheckRow(card, 24, 160, "Telegram", out _tgState);
            _btnCheck = MakeButton(card, Loc.S("Проверить", "Check"), 24, 186, 120, 30, false);
            _btnCheck.Click += delegate { RunChecks(); };
            Controls.Add(card);
        }
        private Dot AddCheckRow(Control parent, int x, int y, string name, out Label state)
        {
            var dot = new Dot();
            dot.SetBounds(x, y + 4, 12, 12);
            parent.Controls.Add(dot);
            MakeLabel(parent, name, x + 20, y - 3, 10.5f, true, TextMain);
            state = MakeLabel(parent, Loc.S("проверка…", "checking…"), x + 176, y - 2, 9f, false, TextDim);
            return dot;
        }
        internal void SetChecksExpanded(bool expanded)
        {
            _checksExpanded = expanded;
            int shift = expanded ? 0 : 160;
            _checksCard.Height = expanded ? 226 : 52;
            foreach (Control control in _checksCard.Controls)
            {
                if (control.Top >= 30) control.Visible = expanded;
            }
            _settingsCard.Top = 562 - shift;
            _tgCard.Top = 710 - shift;
            _logTitle.Top = 840 - shift;
            _logPanel.Top = 860 - shift;
            ClientSize = new Size(900, 958 - shift);
            _checksToggle.Text = expanded ? "▲" : "▼";
        }
        private Dot AddCheckRow(Control parent, int y, string name, out Label state)
        {
            var dot = new Dot();
            dot.SetBounds(24, y + 4, 12, 12);
            parent.Controls.Add(dot);
            MakeLabel(parent, name, 44, y - 3, 10.5f, true, TextMain);
            state = MakeLabel(parent, Loc.S("проверка…", "checking…"), 200, y - 2, 9f, false, TextDim);
            return dot;
        }
        private void BuildSettingsCard()
        {
            var card = CreateCard(26, 562, 848, 136);
            _settingsCard = card;
            _lblSettings = MakeLabel(card, Loc.S("НАСТРОЙКИ", "SETTINGS"), 20, 12, 8f, true, TextDim);
            MakeLabel(card, "Game Filter", 24, 42, 9.5f, true, TextMain);
            _gameFilterCombo = new ComboBox();
            _gameFilterCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            _gameFilterCombo.FlatStyle = FlatStyle.Flat;
            _gameFilterCombo.BackColor = Color.FromArgb(34, 36, 45);
            _gameFilterCombo.ForeColor = TextMain;
            _gameFilterCombo.Items.AddRange(new object[] { "disabled", "all", "tcp", "udp" });
            _gameFilterCombo.SetBounds(120, 38, 130, 26);
            card.Controls.Add(_gameFilterCombo);
            MakeLabel(card, "IPSet Filter", 282, 42, 9.5f, true, TextMain);
            _ipsetCombo = new ComboBox();
            _ipsetCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            _ipsetCombo.FlatStyle = FlatStyle.Flat;
            _ipsetCombo.BackColor = Color.FromArgb(34, 36, 45);
            _ipsetCombo.ForeColor = TextMain;
            _ipsetCombo.Items.AddRange(new object[] { "loaded", "none", "any" });
            _ipsetCombo.SetBounds(374, 38, 130, 26);
            card.Controls.Add(_ipsetCombo);
            _autoUpdateCombo = new ComboBox();
            _autoUpdateCombo.Items.AddRange(new object[] { "disabled", "enabled" });
            _autoUpdateCombo.SelectedItem = "disabled";
            _autoUpdateCombo.Visible = false;
            _lblAppAutostart = MakeLabel(card, Loc.S("Автозапуск приложения", "App autostart"), 430, 92, 9.5f, true, TextMain);
            _appAutoToggle = new ToggleSwitch();
            _appAutoToggle.SetBounds(596, 86, 56, 28);
            card.Controls.Add(_appAutoToggle);
            _btnSave = MakeButton(card, Loc.S("Сохранить", "Save"), 24, 84, 150, 34, true);
            _btnSave.Click += delegate { SaveSettings(); };
            var fakeBtn = MakeButton(card, "Replace fakes", 186, 84, 150, 34, false);
            fakeBtn.Click += delegate { ReplaceActiveFakes(); };
            Controls.Add(card);
        }
        private void BuildTelegramCard()
        {
            var card = CreateCard(26, 710, 848, 114);
            _tgCard = card;
            MakeLabel(card, "TELEGRAM / TgProxy", 20, 12, 8f, true, TextDim);
            _tgServiceDot = new Dot();
            _tgServiceDot.SetBounds(24, 42, 12, 12);
            card.Controls.Add(_tgServiceDot);
            _tgServiceLabel = MakeLabel(card, Loc.S("TgProxy выключен", "TgProxy is off"), 44, 35, 10f, true, TextMain);
            _lblPort = MakeLabel(card, Loc.S("Порт", "Port"), 24, 74, 9f, false, TextDim);
            _tgPortBox = new TextBox();
            _tgPortBox.BorderStyle = BorderStyle.FixedSingle;
            _tgPortBox.BackColor = Color.FromArgb(34, 36, 45);
            _tgPortBox.ForeColor = TextMain;
            _tgPortBox.Font = new Font("Segoe UI", 9f);
            _tgPortBox.SetBounds(62, 70, 70, 24);
            _tgPortBox.Text = Core.GetTgProxyPort().ToString();
            card.Controls.Add(_tgPortBox);
            var portSave = MakeButton(card, "OK", 138, 68, 44, 28, false);
            portSave.Click += delegate { SaveTgPort(); };
            _btnTgSettings = MakeButton(card, Loc.S("Настройки", "Settings"), 192, 68, 110, 28, false);
            _btnTgSettings.Click += delegate { ShowTgProxySettings(); };
            _tgStart = MakeButton(card, Loc.S("Запустить", "Start"), 490, 26, 120, 34, false);
            _tgStart.Click += delegate { StartTelegramProxy(true); };
            _btnTgConnect = MakeButton(card, Loc.S("Подключить Telegram", "Connect Telegram"), 620, 26, 150, 34, true);
            _btnTgConnect.Click += delegate { ConnectTelegram(); };
            _tgStop = MakeButton(card, Loc.S("Стоп", "Stop"), 780, 26, 48, 34, false);
            _tgStop.Click += delegate { StopTelegramProxy(); };
            Controls.Add(card);
        }
        private void BuildLog()
        {
            _logTitle = MakeLabel(this, Loc.S("ЖУРНАЛ", "LOG"), 30, 840, 8f, true, TextDim);
            var panel = new Panel();
            panel.SetBounds(26, 860, 848, 86);
            _logPanel = panel;
            panel.BackColor = Color.FromArgb(13, 14, 18);
            panel.Paint += delegate(object sender, PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var path = Ui.RoundRect(new Rectangle(0, 0, panel.Width - 1, panel.Height - 1), 12))
                using (var pen = new Pen(Color.FromArgb(33, 36, 44), 1f))
                    e.Graphics.DrawPath(pen, path);
            };
            panel.Resize += delegate { Ui.RoundRegion(panel, 12); };
            Controls.Add(panel);
            _log = new RichTextBox();
            _log.BorderStyle = BorderStyle.None;
            _log.BackColor = Color.FromArgb(13, 14, 18);
            _log.ForeColor = Color.FromArgb(168, 174, 188);
            _log.Font = new Font("Consolas", 8.5f);
            _log.ReadOnly = true;
            _log.TabStop = false;
            _log.DetectUrls = false;
            _log.ScrollBars = RichTextBoxScrollBars.Vertical;
            _log.SetBounds(14, 9, 818, 68);
            panel.Controls.Add(_log);
        }
        private Panel CreateCard(int x, int y, int width, int height)
        {
            var card = new Panel();
            card.SetBounds(x, y, width, height);
            card.BackColor = CardBg;
            card.Paint += delegate(object sender, PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                var rect = new Rectangle(0, 0, card.Width - 1, card.Height - 1);
                using (var path = Ui.RoundRect(rect, 16))
                using (var brush = new LinearGradientBrush(rect, Color.FromArgb(30, 32, 40), Color.FromArgb(24, 26, 33), LinearGradientMode.Vertical))
                using (var pen = new Pen(Color.FromArgb(52, 56, 68), 1f))
                {
                    e.Graphics.FillPath(brush, path);
                    e.Graphics.DrawPath(pen, path);
                }
            };
            card.Resize += delegate { Ui.RoundRegion(card, 16); };
            return card;
        }
        private Label MakeLabel(Control parent, string text, int x, int y, float size, bool bold, Color color)
        {
            var label = new Label();
            label.Text = text;
            label.Font = new Font(bold ? "Segoe UI Semibold" : "Segoe UI", size);
            label.ForeColor = color;
            label.BackColor = Color.Transparent;
            label.AutoSize = true;
            label.Location = new Point(x, y);
            parent.Controls.Add(label);
            return label;
        }
        private Button MakeButton(Control parent, string text, int x, int y, int width, int height, bool accent)
        {
            var button = new NiceButton();
            button.Text = text;
            button.Accent = accent;
            button.SetBounds(x, y, width, height);
            parent.Controls.Add(button);
            return button;
        }
        private void LoadInstall()
        {
            _installDir = Core.DetectInstallDir();
            if (_installDir != null)
            {
                Log(Loc.S("Найдена установка zapret: " + _installDir, "Found zapret installation: " + _installDir));
                try { Core.ApplyCustomizations(_installDir, null); } catch { }
                SaveInstallPath();
                PopulateStrategies();
            }
            else
            {
                Log(Loc.S("Установка zapret не найдена. Нажмите «Папка», чтобы указать её.", "zapret installation not found. Click \"Folder\" to select it."));
                _applyButton.Enabled = false;
            }
        }
        private void SaveInstallPath()
        {
            try
            {
                var config = Core.LoadConfig();
                config["install"] = _installDir ?? "";
                Core.SaveConfig(config);
            }
            catch { }
        }
        private void PopulateStrategies()
        {
            _strategies = Core.GetStrategies(_installDir);
            var names = new List<string>();
            foreach (var strategy in _strategies) names.Add(strategy.Name);
            string current = Core.GetCurrentStrategyName();
            int index = _strategies.FindIndex(delegate(StrategyInfo s) { return s.Name == current; });
            if (index < 0 && _strategies.Count > 0) index = 0;
            _silent = true;
            _strategySelector.SetItems(names, index);
            _silent = false;
            _applyButton.Enabled = _strategies.Count > 0 && !_dev;
        }
        private void OpenInstallFolder()
        {
            if (_installDir == null)
            {
                InstallZapret();
                return;
            }
            try { Process.Start("explorer.exe", "\"" + _installDir + "\""); }
            catch (Exception ex) { Log("Не удалось открыть папку: " + ex.Message); }
        }
        private void BrowseInstallFolder()
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Выберите папку zapret (в ней должны быть bin и general.bat)";
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                if (!Core.IsInstallDir(dialog.SelectedPath))
                {
                    MessageBox.Show(this, "В выбранной папке нет bin\\winws.exe.", "Zapret Control",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                _installDir = dialog.SelectedPath;
                SaveInstallPath();
                PopulateStrategies();
                RefreshState();
                Log("Папка установки: " + _installDir);
                if (Core.GetServiceState() == SvcState.NotInstalled)
                    InstallServiceOnly();
                else
                    RunChecks();
            }
        }
        private void InstallZapret()
        {
            if (_busy) return;
            var answer = MessageBox.Show(this,
                "Установка zapret не найдена.\n\n" +
                "Скачать оригинальный zapret-discord-youtube (≈1,5 МБ) и установить службу автоматически?\n\n" +
                "«Нет» — указать уже существующую папку.",
                "Zapret Control", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            if (answer == DialogResult.Cancel) return;
            if (answer == DialogResult.No)
            {
                BrowseInstallFolder();
                return;
            }
            SetBusy(true);
            Log("Загрузка zapret...");
            System.Threading.Tasks.Task.Factory.StartNew(delegate
            {
                string error = null;
                string dir = null;
                try { dir = Core.DownloadAndInstallZapret(Log); }
                catch (Exception ex) { error = ex.Message; }
                BeginInvoke(new Action(delegate
                {
                    SetBusy(false);
                    if (error != null)
                    {
                        Log("Ошибка установки: " + error);
                        MessageBox.Show(this, "Не удалось установить zapret.\n\n" + error,
                            "Zapret Control", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    _installDir = dir;
                    SaveInstallPath();
                    PopulateStrategies();
                    RefreshState();
                    Log("zapret установлен: " + dir);
                    RunChecks();
                }));
            });
        }
        private void InstallServiceOnly()
        {
            if (_dev || _busy || _installDir == null) return;
            SetBusy(true);
            Log("Установка службы zapret...");
            System.Threading.Tasks.Task.Factory.StartNew(delegate
            {
                string error;
                bool ok = Core.EnsureServiceInstalled(_installDir, Log, out error);
                BeginInvoke(new Action(delegate
                {
                    SetBusy(false);
                    RefreshState();
                    if (ok)
                    {
                        Log("Служба zapret установлена и запущена.");
                        RunChecks();
                    }
                    else
                    {
                        Log("Ошибка установки службы: " + error);
                        MessageBox.Show(this, "Не удалось установить службу.\n\n" + error,
                            "Zapret Control", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }));
            });
        }
        private bool TryAutoStart()
        {
            if (_dev || _installDir == null) return false;
            SvcState state = Core.GetServiceState();
            if (state == SvcState.Running || state == SvcState.Pending) return false;
            SetBusy(true);
            Log(state == SvcState.NotInstalled
                ? "Служба не установлена — устанавливаю..."
                : "Автозапуск службы zapret...");
            System.Threading.Tasks.Task.Factory.StartNew(delegate
            {
                string error = null;
                bool ok;
                if (Core.GetServiceState() == SvcState.NotInstalled)
                    ok = Core.EnsureServiceInstalled(_installDir, Log, out error);
                else
                    ok = Core.StartService(out error);
                BeginInvoke(new Action(delegate
                {
                    SetBusy(false);
                    RefreshState();
                    if (ok) Log("Служба zapret запущена.");
                    else Log("Автозапуск не удался: " + error);
                    RunChecks();
                }));
            });
            return true;
        }
        private void RefreshState()
        {
            SvcState state = Core.GetServiceState();
            bool running = state == SvcState.Running;
            _power.On = running;
            _pill.Active = running;
            UpdateStateIcons(running);
            switch (state)
            {
                case SvcState.NotInstalled:
                    _stateLabel.Text = Loc.S("Служба не найдена", "Service not found");
                    _stateLabel.ForeColor = Red;
                    break;
                case SvcState.Running:
                    _stateLabel.Text = Loc.S("Активен", "Active");
                    _stateLabel.ForeColor = Green;
                    break;
                case SvcState.Pending:
                    _stateLabel.Text = Loc.S("Перезапуск…", "Restarting…");
                    _stateLabel.ForeColor = Yellow;
                    break;
                default:
                    _stateLabel.Text = Loc.S("Выключен", "Stopped");
                    _stateLabel.ForeColor = TextMain;
                    break;
            }
            string strategyName = Core.GetCurrentStrategyName();
            if (_installDir != null)
                _subLabel.Text = Loc.S("Служба zapret", "zapret service") + (string.IsNullOrEmpty(strategyName) ? "" : " · " + strategyName);
            else
                _subLabel.Text = Loc.S("Установка не найдена — нажмите «Установить»", "Installation not found — click \"Install\"");
            if (_folderButton != null)
                _folderButton.Text = _installDir == null ? Loc.S("Установить", "Install") : Loc.S("Папка", "Folder");
            _silent = true;
            _autoToggle.Checked = Core.GetServiceAutoStart();
            _silent = false;
            bool tgProc = false;
            try { tgProc = System.Diagnostics.Process.GetProcessesByName("tgproxy").Length > 0 || System.Diagnostics.Process.GetProcessesByName("TgWsProxy").Length > 0; } catch { }
            bool tgPort = Core.TgProxyPortOpen();
            bool tgActive = tgProc || tgPort;
            int tgP = Core.GetTgProxyPort();
            if (tgPort)
            {
                _tgServiceDot.Color = Green;
                _tgServiceLabel.Text = Loc.S("TgProxy работает · 127.0.0.1:" + tgP, "TgProxy running · 127.0.0.1:" + tgP);
                _tgServiceLabel.ForeColor = TextMain;
            }
            else if (tgActive)
            {
                _tgServiceDot.Color = Yellow;
                _tgServiceLabel.Text = Loc.S("TgProxy запускается…", "TgProxy starting…");
                _tgServiceLabel.ForeColor = TextMain;
            }
            else
            {
                _tgServiceDot.Color = Color.FromArgb(120, 124, 136);
                _tgServiceLabel.Text = Loc.S("TgProxy выключен", "TgProxy is off");
                _tgServiceLabel.ForeColor = TextDim;
            }
            if (_tgStart != null)
            {
                _tgStart.Text = File.Exists(Core.TgProxyExe) ? Loc.S("Запустить", "Start") : Loc.S("Установить", "Install");
                _tgStart.Enabled = !tgActive && !_busy;
            }
            if (_tgStop != null) _tgStop.Enabled = tgActive && !_busy;
            if (_installDir != null && string.IsNullOrEmpty(strategyName))
                _subLabel.Text = Loc.S("Служба zapret · стратегия не определена", "zapret service · strategy not set");
            RefreshSettings();
        }
        private void ToggleService()
        {
            if (_dev) { Log("Тестовый режим: управление службой отключено."); return; }
            if (_busy) return;
            bool running = Core.GetServiceState() == SvcState.Running;
            bool starting = !running;
            SetBusy(true);
            Log(starting ? "Запуск службы zapret…" : "Остановка службы zapret…");
            System.Threading.Tasks.Task.Factory.StartNew(delegate
            {
                string error;
                bool ok = starting ? Core.StartService(out error) : Core.StopService(out error);
                BeginInvoke(new Action(delegate
                {
                    SetBusy(false);
                    RefreshState();
                    if (ok)
                    {
                        Log(starting ? "Запущено." : "Выключено.");
                        RunChecks();
                    }
                    else
                    {
                        Log("Ошибка службы: " + error);
                        MessageBox.Show(this, "Не удалось изменить состояние службы.\n\n" + error,
                            "Zapret Control", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }));
            });
        }
        private void RestartService()
        {
            if (_dev) { Log("Тестовый режим: перезапуск отключён."); return; }
            if (_busy) return;
            SetBusy(true);
            Log("Перезапуск службы zapret…");
            System.Threading.Tasks.Task.Factory.StartNew(delegate
            {
                string stopError;
                Core.StopService(out stopError);
                System.Threading.Thread.Sleep(600);
                string startError;
                bool ok = Core.StartService(out startError);
                BeginInvoke(new Action(delegate
                {
                    SetBusy(false);
                    RefreshState();
                    if (ok)
                    {
                        Log("Служба перезапущена.");
                        RunChecks();
                    }
                    else
                    {
                        Log("Ошибка запуска: " + startError);
                        MessageBox.Show(this, "Служба не запустилась.\n\n" + startError,
                            "Zapret Control", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }));
            });
        }
        private void OnAutoChanged(object sender, EventArgs e)
        {
            if (_silent) return;
            if (_dev)
            {
                _silent = true;
                _autoToggle.Checked = Core.GetServiceAutoStart();
                _silent = false;
                Log("Тестовый режим: настройка не изменена.");
                return;
            }
            bool enabled = _autoToggle.Checked;
            string error;
            bool ok = Core.SetServiceAutoStart(enabled, out error);
            if (ok)
            {
                Log(enabled ? "Автозапуск службы включён." : "Автозапуск службы выключен.");
            }
            else
            {
                Log("Ошибка автозапуска: " + error);
                _silent = true;
                _autoToggle.Checked = Core.GetServiceAutoStart();
                _silent = false;
                MessageBox.Show(this, "Не удалось изменить автозапуск.\n\n" + error,
                    "Zapret Control", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        private void ApplySelectedStrategy()
        {
            if (_dev) { Log("Тестовый режим: смена стратегии отключена."); return; }
            if (_busy || _installDir == null) return;
            int index = _strategySelector.SelectedIndex;
            if (index < 0 || index >= _strategies.Count) return;
            StrategyInfo strategy = _strategies[index];
            var answer = MessageBox.Show(this,
                "Применить стратегию «" + strategy.Name + "»?\n\nСлужба zapret будет перезапущена.",
                "Zapret Control", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (answer != DialogResult.Yes) return;
            SetBusy(true);
            Log("Применение стратегии: " + strategy.Name);
            System.Threading.Tasks.Task.Factory.StartNew(delegate
            {
                string error;
                bool ok = Core.ApplyStrategy(_installDir, strategy, Log, out error);
                BeginInvoke(new Action(delegate
                {
                    SetBusy(false);
                    RefreshState();
                    if (ok)
                    {
                        Log("Готово. Стратегия: " + strategy.Name);
                        RunChecks();
                    }
                    else
                    {
                        Log("Ошибка стратегии: " + error);
                        MessageBox.Show(this, "Не удалось применить стратегию.\n\n" + error,
                            "Zapret Control", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }));
            });
        }
        private void RunChecks()
        {
            if (_checking) return;
            _checking = true;
            string checking = Loc.S("проверка…", "checking…");
            SetCheck(_ytDot, _ytState, checking, CheckState.Pending);
            SetCheck(_dcDot, _dcState, checking, CheckState.Pending);
            SetCheck(_rbDot, _rbState, checking, CheckState.Pending);
            SetCheck(_scDot, _scState, checking, CheckState.Pending);
            SetCheck(_tgDot, _tgState, checking, CheckState.Pending);
            System.Threading.Tasks.Task.Factory.StartNew(delegate
            {
                CheckResult youtube = Core.CheckUrl("https://www.youtube.com/generate_204", 10000);
                CheckResult discord = Core.CheckUrl("https://discord.com/api/v10/gateway", 10000);
                CheckResult roblox = Core.CheckUrl("https://www.roblox.com/", 10000);
                CheckResult soundcloud = Core.CheckUrl("https://soundcloud.com/", 10000);
                bool direct = Core.TelegramDirectAlive();
                bool proxyPort = Core.TgProxyPortOpen();
                int tgP = Core.GetTgProxyPort();
                BeginInvoke(new Action(delegate
                {
                    if (youtube.Ok)
                        SetCheck(_ytDot, _ytState, Loc.S("доступен · " + youtube.Ms + " мс", "reachable · " + youtube.Ms + " ms"), CheckState.Ok);
                    else
                    {
                        SetCheck(_ytDot, _ytState, Loc.S("недоступен · " + youtube.Info, "unreachable · " + youtube.Info), CheckState.Fail);
                        Log(Loc.S("YouTube недоступен: " + youtube.Info, "YouTube unreachable: " + youtube.Info));
                    }
                    if (discord.Ok)
                        SetCheck(_dcDot, _dcState, Loc.S("доступен · " + discord.Ms + " мс", "reachable · " + discord.Ms + " ms"), CheckState.Ok);
                    else
                    {
                        SetCheck(_dcDot, _dcState, Loc.S("недоступен · " + discord.Info, "unreachable · " + discord.Info), CheckState.Fail);
                        Log(Loc.S("Discord недоступен: " + discord.Info, "Discord unreachable: " + discord.Info));
                    }
                    if (roblox.Ok)
                        SetCheck(_rbDot, _rbState, Loc.S("доступен · " + roblox.Ms + " мс", "reachable · " + roblox.Ms + " ms"), CheckState.Ok);
                    else
                    {
                        SetCheck(_rbDot, _rbState, Loc.S("недоступен · " + roblox.Info, "unreachable · " + roblox.Info), CheckState.Fail);
                        Log(Loc.S("Roblox недоступен: " + roblox.Info, "Roblox unreachable: " + roblox.Info));
                    }
                    if (soundcloud.Ok)
                        SetCheck(_scDot, _scState, Loc.S("доступен · " + soundcloud.Ms + " мс", "reachable · " + soundcloud.Ms + " ms"), CheckState.Ok);
                    else
                    {
                        SetCheck(_scDot, _scState, Loc.S("недоступен · " + soundcloud.Info, "unreachable · " + soundcloud.Info), CheckState.Fail);
                        Log(Loc.S("SoundCloud недоступен: " + soundcloud.Info, "SoundCloud unreachable: " + soundcloud.Info));
                    }
                    if (proxyPort)
                        SetCheck(_tgDot, _tgState, Loc.S("через прокси · 127.0.0.1:" + tgP, "via proxy · 127.0.0.1:" + tgP), CheckState.Ok);
                    else if (direct)
                        SetCheck(_tgDot, _tgState, Loc.S("напрямую доступен", "reachable directly"), CheckState.Ok);
                    else
                        SetCheck(_tgDot, _tgState, Loc.S("недоступен", "unreachable"), CheckState.Fail);
                    _checking = false;
                }));
            });
        }
        private enum CheckState { Pending, Ok, Fail }
        private void SetCheck(Dot dot, Label label, string text, CheckState state)
        {
            label.Text = text;
            if (state == CheckState.Ok)
            {
                dot.Color = Green;
                label.ForeColor = Color.FromArgb(160, 210, 180);
            }
            else if (state == CheckState.Fail)
            {
                dot.Color = Red;
                label.ForeColor = Color.FromArgb(210, 160, 160);
            }
            else
            {
                dot.Color = Yellow;
                label.ForeColor = TextDim;
            }
        }
        private void StartTelegramProxy(bool askDownload)
        {
            if (_busy) return;
            if (!File.Exists(Core.TgProxyExe))
            {
                if (askDownload)
                {
                    var answer = MessageBox.Show(this,
                        "Скачать TgProxy (≈22 МБ)?",
                        "Zapret Control", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (answer != DialogResult.Yes) return;
                }
                SetBusy(true);
                Log("Загрузка TgProxy…");
                System.Threading.Tasks.Task.Factory.StartNew(delegate
                {
                    try
                    {
                        Core.EnsureTgProxy(Log);
                        Core.StartTgProxy();
                        for (int i = 0; i < 30 && !Core.TgProxyPortOpen(); i++)
                            System.Threading.Thread.Sleep(500);
                        Log("TgProxy запущен.");
                    }
                    catch (Exception ex)
                    {
                        Log("Ошибка TgProxy: " + ex.Message);
                    }
                    BeginInvoke(new Action(delegate
                    {
                        SetBusy(false);
                        RefreshState();
                    }));
                });
                return;
            }
            if (Core.TgProxyPortOpen() || Core.TgProxyRunning())
            {
                Log("TgProxy уже запущен.");
                RefreshState();
                return;
            }
            try
            {
                Core.StartTgProxy();
                Log("TgProxy запущен.");
            }
            catch (Exception ex)
            {
                Log("Ошибка TgProxy: " + ex.Message);
                MessageBox.Show(this, "Не удалось запустить TgProxy.\n\n" + ex.Message,
                    "Zapret Control", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            System.Threading.Tasks.Task.Factory.StartNew(delegate
            {
                for (int i = 0; i < 30 && !Core.TgProxyPortOpen(); i++)
                    System.Threading.Thread.Sleep(500);
                BeginInvoke(new Action(delegate { RefreshState(); }));
            });
        }
        private void ConnectTelegram()
        {
            if (_busy) return;
            if (!File.Exists(Core.TgProxyExe))
            {
                var answer = MessageBox.Show(this,
                    "Для Telegram нужен TgProxy (≈22 МБ).\nСкачать и запустить?",
                    "Zapret Control", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (answer != DialogResult.Yes) return;
            }
            SetBusy(true);
            Log("Подготовка подключения Telegram…");
            System.Threading.Tasks.Task.Factory.StartNew(delegate
            {
                string error = null;
                try
                {
                    Core.EnsureTgProxy(Log);
                    Core.StartTgProxy();
                    for (int i = 0; i < 40 && !Core.TgProxyPortOpen(); i++)
                        System.Threading.Thread.Sleep(500);
                    if (!Core.TgProxyPortOpen())
                        throw new Exception("Локальный прокси не поднялся на порту 1443.");
                }
                catch (Exception ex)
                {
                    error = ex.Message;
                }
                BeginInvoke(new Action(delegate
                {
                    SetBusy(false);
                    RefreshState();
                    if (error != null)
                    {
                        Log("Ошибка Telegram: " + error);
                        MessageBox.Show(this, "Не удалось подготовить прокси.\n\n" + error,
                            "Zapret Control", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    int port;
                    string link = Core.GetTelegramLink(out port);
                    if (string.IsNullOrEmpty(link))
                    {
                        Log("Ссылка прокси ещё не готова.");
                        MessageBox.Show(this,
                            "Прокси запущен, но ссылка ещё не готова.\nОткройте значок TgProxy в трее → «Открыть в Telegram».",
                            "Zapret Control", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                    if (Core.FindTelegramDesktop() == null)
                    {
                        var download = MessageBox.Show(this,
                            "Telegram Desktop не установлен.\nОткрыть страницу загрузки?",
                            "Zapret Control", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                        if (download == DialogResult.Yes)
                            Process.Start("https://desktop.telegram.org/");
                        return;
                    }
                    try
                    {
                        string telegramExe = Core.FindTelegramDesktop();
                        Core.EnsureTgProtocol(telegramExe);
                        try
                        {
                            Process.Start(new ProcessStartInfo("explorer.exe", "\"" + link + "\"")
                            {
                                UseShellExecute = true
                            });
                        }
                        catch
                        {
                            Process.Start(new ProcessStartInfo(link) { UseShellExecute = true });
                        }
                        Log("Открываю Telegram со ссылкой на прокси…");
                    }
                    catch (Exception ex)
                    {
                        Log("Не удалось открыть Telegram: " + ex.Message);
                    }
                }));
            });
        }
        private void StopTelegramProxy()
        {
            try
            {
                Core.StopTgProxy();
                Log("TgProxy остановлен.");
            }
            catch (Exception ex)
            {
                Log("Ошибка остановки TgProxy: " + ex.Message);
            }
            RefreshState();
        }
        private void Log(string message)
        {
            if (_log == null) return;
            if (_log.InvokeRequired)
            {
                try { _log.BeginInvoke(new Action<string>(Log), message); }
                catch { }
                return;
            }
            _log.AppendText(DateTime.Now.ToString("HH:mm:ss") + "   " + message + "\r\n");
            _log.SelectionStart = _log.TextLength;
            _log.ScrollToCaret();
        }
        private void SetBusy(bool busy)
        {
            _busy = busy;
            Cursor = busy ? Cursors.WaitCursor : Cursors.Default;
            if (_power != null) _power.Enabled = !busy;
            if (_strategySelector != null) _strategySelector.Enabled = !busy;
            if (_applyButton != null) _applyButton.Enabled = !busy && !_dev && _strategies.Count > 0;
            if (_folderButton != null) _folderButton.Enabled = !busy;
            if (_tgStart != null) _tgStart.Enabled = !busy && !Core.TgProxyRunning();
            if (_tgStop != null) _tgStop.Enabled = !busy && Core.TgProxyRunning();
        }
        private void InitTimers()
        {
            _refreshTimer = new Timer();
            _refreshTimer.Interval = 5000;
            _refreshTimer.Tick += delegate { RefreshState(); };
            _refreshTimer.Start();
            _checksTimer = new Timer();
            _checksTimer.Interval = 60000;
            _checksTimer.Tick += delegate { RunChecks(); };
            _checksTimer.Start();
        }
        private void InitTray()
        {
            var menu = new ContextMenuStrip();
            menu.Items.Add(Loc.S("Открыть", "Open"), null, delegate { ShowFromTray(); });
            menu.Items.Add(Loc.S("Включить", "Enable"), null, delegate { if (Core.GetServiceState() != SvcState.Running) ToggleService(); });
            menu.Items.Add(Loc.S("Выключить", "Disable"), null, delegate { if (Core.GetServiceState() == SvcState.Running) ToggleService(); });
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(Loc.S("Выход", "Exit"), null, delegate { ExitApplication(); });
            _tray = new NotifyIcon();
            _tray.Icon = AppIcon();
            _tray.Text = "Zapret Control";
            _tray.Visible = true;
            _tray.ContextMenuStrip = menu;
            _tray.DoubleClick += delegate { ShowFromTray(); };
        }
        private void ShowFromTray()
        {
            Show();
            WindowState = FormWindowState.Normal;
            Activate();
            BringToFront();
        }
        private void ToggleLanguage()
        {
            try
            {
                string newLang = Loc.En ? "ru" : "en";
                Core.SetLang(newLang);
                Loc.Lang = newLang;
                ApplyLanguage();
            }
            catch { }
        }
        private void ApplyLanguage()
        {
            if (InvokeRequired) { try { BeginInvoke(new Action(ApplyLanguage)); } catch { } return; }
            if (_langToggle != null) _langToggle.Sync();
            if (_lblStrategy != null) _lblStrategy.Text = Loc.S("СТРАТЕГИЯ", "STRATEGY");
            if (_lblChecks != null) _lblChecks.Text = Loc.S("ПРОВЕРКА ДОСТУПА", "ACCESS CHECK");
            if (_lblSettings != null) _lblSettings.Text = Loc.S("НАСТРОЙКИ", "SETTINGS");
            if (_lblAppAutostart != null) _lblAppAutostart.Text = Loc.S("Автозапуск приложения", "App autostart");
            if (_lblSvcAutostart != null) _lblSvcAutostart.Text = Loc.S("Автозапуск службы", "Service autostart");
            if (_lblPort != null) _lblPort.Text = Loc.S("Порт", "Port");
            if (_logTitle != null) _logTitle.Text = Loc.S("ЖУРНАЛ", "LOG");
            if (_btnRestart != null) _btnRestart.Text = Loc.S("Перезапустить", "Restart");
            if (_btnApply != null) _btnApply.Text = Loc.S("Применить", "Apply");
            if (_btnCheck != null) _btnCheck.Text = Loc.S("Проверить", "Check");
            if (_btnSave != null) _btnSave.Text = Loc.S("Сохранить", "Save");
            if (_btnTgSettings != null) _btnTgSettings.Text = Loc.S("Настройки", "Settings");
            if (_btnTgConnect != null) _btnTgConnect.Text = Loc.S("Подключить Telegram", "Connect Telegram");
            if (_tgStop != null) _tgStop.Text = Loc.S("Стоп", "Stop");
            if (_tray != null && _tray.ContextMenuStrip != null)
            {
                var items = _tray.ContextMenuStrip.Items;
                if (items.Count >= 5)
                {
                    items[0].Text = Loc.S("Открыть", "Open");
                    items[1].Text = Loc.S("Включить", "Enable");
                    items[2].Text = Loc.S("Выключить", "Disable");
                    items[4].Text = Loc.S("Выход", "Exit");
                }
            }
            if (_strategySelector != null) _strategySelector.Invalidate();
            if (_pill != null) _pill.Invalidate();
            RefreshState();
        }
        private void RestartApp()
        {
            try
            {
                string exe = Application.ExecutablePath;
                var psi = new ProcessStartInfo("cmd.exe", "/c ping -n 2 127.0.0.1 >nul & start \"\" \"" + exe + "\"");
                psi.CreateNoWindow = true;
                psi.UseShellExecute = false;
                psi.WindowStyle = ProcessWindowStyle.Hidden;
                Process.Start(psi);
            }
            catch { }
            _exiting = true;
            if (_tray != null) { try { _tray.Visible = false; } catch { } }
            try { Application.Exit(); } catch { Environment.Exit(0); }
        }
        private void ExitApplication()
        {
            _exiting = true;
            Close();
        }
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            ApplyDarkTitleBar();
        }
        internal static Icon AppIcon()
        {
            try
            {
                var icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                if (icon != null) return icon;
            }
            catch { }
            return SystemIcons.Shield;
        }
        private static Icon CreateStateIcon(bool on)
        {
            try
            {
                using (var bmp = new Bitmap(32, 32))
                using (var g = Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.Clear(Color.Transparent);
                    Color c = on ? Color.FromArgb(46, 204, 113) : Color.FromArgb(120, 124, 136);
                    using (var b = new SolidBrush(c)) g.FillEllipse(b, 2, 2, 28, 28);
                    using (var pen = new Pen(Color.White, 2.8f))
                    {
                        pen.StartCap = LineCap.Round; pen.EndCap = LineCap.Round;
                        g.DrawArc(pen, 10, 10, 12, 12, -55, 290);
                        g.DrawLine(pen, 16, 7, 16, 14);
                    }
                    return Icon.FromHandle(bmp.GetHicon());
                }
            }
            catch { return AppIcon(); }
        }
        private void UpdateStateIcons(bool on)
        {
            try
            {
                if (_iconOn == null) _iconOn = CreateStateIcon(true);
                if (_iconOff == null) _iconOff = CreateStateIcon(false);
                if (_lastIconOn == on && Icon != null) return;
                _lastIconOn = on;
                var ic = on ? _iconOn : _iconOff;
                Icon = ic;
                if (_tray != null) _tray.Icon = ic;
            }
            catch { }
        }
        private void ApplyDarkTitleBar()
        {
            try
            {
                int value = 1;
                if (DwmSetWindowAttribute(Handle, 20, ref value, sizeof(int)) != 0)
                    DwmSetWindowAttribute(Handle, 19, ref value, sizeof(int));
            }
            catch { }
        }
        [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            try
            {
                using (var brush = new LinearGradientBrush(ClientRectangle,
                    Color.FromArgb(18, 19, 26), Color.FromArgb(13, 14, 20), LinearGradientMode.Vertical))
                {
                    e.Graphics.FillRectangle(brush, ClientRectangle);
                }
            }
            catch { base.OnPaintBackground(e); }
        }
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!_exiting && !_dev)
            {
                e.Cancel = true;
                Hide();
                if (!_trayHintShown)
                {
                    _trayHintShown = true;
                    _tray.ShowBalloonTip(2500, "Zapret Control",
                        Loc.S("Свернуто в трей.", "Minimized to tray."), ToolTipIcon.Info);
                }
                return;
            }
            if (_refreshTimer != null) { _refreshTimer.Stop(); _refreshTimer.Dispose(); }
            if (_checksTimer != null) { _checksTimer.Stop(); _checksTimer.Dispose(); }
            if (_tray != null) { _tray.Visible = false; _tray.Dispose(); }
            if (_iconOn != null) { try { _iconOn.Dispose(); } catch { } }
            if (_iconOff != null) { try { _iconOff.Dispose(); } catch { } }
            base.OnFormClosing(e);
        }
        private void RefreshSettings()
        {
            try
            {
                    if (_appAutoToggle != null) _appAutoToggle.Checked = Core.GetAppAutoStart();
                if (_autoUpdateCombo != null) { _autoUpdateCombo.SelectedItem = "disabled"; _autoUpdateCombo.Enabled = false; }
                if (_installDir == null || _gameFilterCombo == null) return;
                string gf = Core.GetGameFilterStatus(_installDir);
                _gameFilterCombo.SelectedItem = gf;
                if (_gameFilterCombo.SelectedIndex < 0) _gameFilterCombo.SelectedIndex = 0;
                string ip = Core.GetIPSetStatus(_installDir);
                _ipsetCombo.SelectedItem = ip;
                if (_ipsetCombo.SelectedIndex < 0) _ipsetCombo.SelectedIndex = 0;
            }
            catch { }
        }
        private void SaveSettings()
        {
            try
            {
                bool appAuto = _appAutoToggle != null && _appAutoToggle.Checked;
                Core.SetAppAutoStart(appAuto);
                string newLang = (_langCombo != null && _langCombo.SelectedIndex == 1) ? "en" : "ru";
                bool langChanged = newLang != Loc.Lang;
                if (_installDir != null)
                {
                    string gf = _gameFilterCombo.SelectedItem != null ? _gameFilterCombo.SelectedItem.ToString() : "disabled";
                    Core.SetGameFilter(_installDir, gf);
                    string ip = _ipsetCombo.SelectedItem != null ? _ipsetCombo.SelectedItem.ToString() : "loaded";
                    string curIp = Core.GetIPSetStatus(_installDir);
                    int guard = 0;
                    while (curIp != ip && guard++ < 4) { Core.CycleIPSet(_installDir); curIp = Core.GetIPSetStatus(_installDir); }
                    Core.SetAutoUpdateEnabled(_installDir, false);
                    try { Core.ApplyCustomizations(_installDir, null); } catch { }
                }
                Core.SetLang(newLang);
                Loc.Lang = newLang;
                Log(Loc.S("Настройки сохранены.", "Settings saved."));
                if (langChanged) ApplyLanguage();
                RefreshSettings();
                if (_installDir != null) ApplyCurrentStrategySilent();
            }
            catch (Exception ex) { Log(Loc.S("Ошибка сохранения: " + ex.Message, "Save error: " + ex.Message)); }
        }
        private void ApplyCurrentStrategySilent()
        {
            if (_dev || _busy || _installDir == null) return;
            int index = _strategySelector != null ? _strategySelector.SelectedIndex : -1;
            if (index < 0 || index >= _strategies.Count) return;
            StrategyInfo strategy = _strategies[index];
            SetBusy(true);
            Log("Применение стратегии: " + strategy.Name);
            System.Threading.Tasks.Task.Factory.StartNew(delegate
            {
                string error;
                bool ok = Core.ApplyStrategy(_installDir, strategy, Log, out error);
                BeginInvoke(new Action(delegate
                {
                    SetBusy(false);
                    RefreshState();
                    if (ok) { Log("Готово. Стратегия: " + strategy.Name); RunChecks(); }
                    else { Log("Ошибка стратегии: " + error); }
                }));
            });
        }
        private void ShowTgProxySettings()
        {
            using (var dlg = new Form())
            {
                dlg.Text = Loc.S("Настройки TgProxy", "TgProxy settings");
                dlg.ClientSize = new Size(500, 430);
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.MinimizeBox = false;
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.BackColor = Bg;
                dlg.ForeColor = TextMain;
                dlg.Font = new Font("Segoe UI", 9f);
                int y = 14;
                Action<string> section = (title) => {
                    var l = new Label(); l.Text = title; l.Font = new Font("Segoe UI Semibold", 8.5f); l.ForeColor = TextDim; l.Location = new Point(16, y); l.AutoSize = true; dlg.Controls.Add(l);
                    y += 22;
                };
                Func<string,int,TextBox> field = (label, w) => {
                    var l = new Label(); l.Text = label; l.Location = new Point(28, y + 3); l.AutoSize = true; l.ForeColor = TextMain; dlg.Controls.Add(l);
                    var tb = new TextBox(); tb.BorderStyle = BorderStyle.FixedSingle; tb.BackColor = Color.FromArgb(34,36,45); tb.ForeColor = TextMain; tb.SetBounds(170, y, w, 24); dlg.Controls.Add(tb);
                    return tb;
                };
                Func<string,int,CheckBox> check = (label, x) => {
                    var cb = new CheckBox(); cb.Text = label; cb.Location = new Point(x, y); cb.AutoSize = true; cb.ForeColor = TextMain; dlg.Controls.Add(cb);
                    return cb;
                };
                section(Loc.S("Подключение", "Connection"));
                var portTb = field(Loc.S("Порт", "Port"), 90); portTb.Text = Core.GetTgConfigInt("port", 1443).ToString();
                var hostLbl = new Label(); hostLbl.Text = "Host"; hostLbl.Location = new Point(290, y + 3); hostLbl.AutoSize = true; hostLbl.ForeColor = TextMain; dlg.Controls.Add(hostLbl);
                var hostTb = new TextBox(); hostTb.BorderStyle = BorderStyle.FixedSingle; hostTb.BackColor = Color.FromArgb(34,36,45); hostTb.ForeColor = TextMain; hostTb.SetBounds(330, y, 140, 24); hostTb.Text = Core.GetTgConfigString("host", "127.0.0.1"); dlg.Controls.Add(hostTb);
                y += 34;
                section(Loc.S("Производительность", "Performance"));
                var bufTb = field(Loc.S("Буфер (КБ)", "Buffer (KB)"), 90); bufTb.Text = Core.GetTgConfigInt("buf_kb", 256).ToString();
                var poolLbl = new Label(); poolLbl.Text = Loc.S("Пул", "Pool"); poolLbl.Location = new Point(290, y + 3); poolLbl.AutoSize = true; poolLbl.ForeColor = TextMain; dlg.Controls.Add(poolLbl);
                var poolTb = new TextBox(); poolTb.BorderStyle = BorderStyle.FixedSingle; poolTb.BackColor = Color.FromArgb(34,36,45); poolTb.ForeColor = TextMain; poolTb.SetBounds(330, y, 70, 24); poolTb.Text = Core.GetTgConfigInt("pool_size", 4).ToString(); dlg.Controls.Add(poolTb);
                y += 30;
                var logTb = field(Loc.S("Лог (МБ)", "Log (MB)"), 90); logTb.Text = Core.GetTgConfigInt("log_max_mb", 5).ToString();
                y += 34;
                section(Loc.S("Режимы", "Modes"));
                var cf = check("cfproxy", 28); cf.Checked = Core.GetTgConfigBool("cfproxy", true);
                var h2 = check("h2", 170); h2.Checked = Core.GetTgConfigBool("h2", true);
                var nosec = check("no_secure", 290); nosec.Checked = Core.GetTgConfigBool("no_secure", false);
                y += 28;
                var ftdc = check("force_test_dc", 28); ftdc.Checked = Core.GetTgConfigBool("force_test_dc", false);
                y += 34;
                section(Loc.S("Дополнительно", "Advanced"));
                var verbose = check("verbose", 28); verbose.Checked = Core.GetTgConfigBool("verbose", false);
                var cfu = check("cfproxy_user_domain", 170); cfu.Checked = Core.GetTgConfigBool("cfproxy_user_domain_enabled", false);
                var cfw = check("cfproxy_worker", 350); cfw.Checked = Core.GetTgConfigBool("cfproxy_worker_enabled", false);
                var save = MakeButton(dlg, Loc.S("Сохранить", "Save"), 250, 382, 120, 34, true);
                var cancel = MakeButton(dlg, Loc.S("Отмена", "Cancel"), 380, 382, 100, 34, false);
                save.Click += delegate {
                    int port, lmb, bk, ps;
                    if (!int.TryParse(portTb.Text.Trim(), out port) || port < 1 || port > 65535) port = Core.GetTgConfigInt("port", 1443);
                    if (!int.TryParse(logTb.Text.Trim(), out lmb) || lmb < 1) lmb = 5;
                    if (!int.TryParse(bufTb.Text.Trim(), out bk) || bk < 1) bk = 256;
                    if (!int.TryParse(poolTb.Text.Trim(), out ps) || ps < 1) ps = 4;
                    bool running = Core.TgProxyRunning();
                    if (running) { try { Core.StopTgProxy(); } catch { } }
                    Core.SetTgConfigInt("port", port);
                    Core.SetTgConfigString("host", (hostTb.Text ?? "127.0.0.1").Trim());
                    Core.SetTgConfigInt("log_max_mb", lmb);
                    Core.SetTgConfigInt("buf_kb", bk);
                    Core.SetTgConfigInt("pool_size", ps);
                    Core.SetTgConfigBool("verbose", verbose.Checked);
                    Core.SetTgConfigBool("cfproxy", cf.Checked);
                    Core.SetTgConfigBool("h2", h2.Checked);
                    Core.SetTgConfigBool("no_secure", nosec.Checked);
                    Core.SetTgConfigBool("force_test_dc", ftdc.Checked);
                    Core.SetTgConfigBool("cfproxy_user_domain_enabled", cfu.Checked);
                    Core.SetTgConfigBool("cfproxy_worker_enabled", cfw.Checked);
                    if (_tgPortBox != null) _tgPortBox.Text = port.ToString();
                    Log(Loc.S("Настройки TgProxy сохранены.", "TgProxy settings saved."));
                    if (running)
                    {
                        try { Core.StartTgProxy(); Log(Loc.S("TgProxy перезапущен.", "TgProxy restarted.")); } catch (Exception ex) { Log(Loc.S("Ошибка запуска TgProxy: " + ex.Message, "TgProxy start error: " + ex.Message)); }
                    }
                    dlg.DialogResult = DialogResult.OK;
                    dlg.Close();
                };
                cancel.Click += delegate { dlg.DialogResult = DialogResult.Cancel; dlg.Close(); };
                dlg.AcceptButton = save; dlg.CancelButton = cancel;
                dlg.ShowDialog(this);
                RefreshState();
            }
        }
        private void SaveTgPort()
        {
            int port;
            if (!int.TryParse((_tgPortBox.Text ?? "").Trim(), out port) || port < 1 || port > 65535)
            {
                Log(Loc.S("Неверный порт TgProxy.", "Invalid TgProxy port."));
                _tgPortBox.Text = Core.GetTgProxyPort().ToString();
                return;
            }
            bool running = Core.TgProxyRunning();
            if (running) { try { Core.StopTgProxy(); } catch { } }
            Core.SetTgProxyPort(port);
            Log(Loc.S("Порт TgProxy: " + port, "TgProxy port: " + port));
            if (running)
            {
                try { Core.StartTgProxy(); Log(Loc.S("TgProxy перезапущен.", "TgProxy restarted.")); } catch (Exception ex) { Log(Loc.S("Ошибка запуска TgProxy: " + ex.Message, "TgProxy start error: " + ex.Message)); }
            }
            RefreshState();
        }
        private void ReplaceActiveFakes()
        {
            if (_installDir == null) return;
            var files = Core.GetFakeBinFiles(_installDir);
            if (files.Count == 0) { Log("Нет .bin файлов для замены."); return; }
            using (var dlg = new Form())
            {
                dlg.Text = "Replace active fakes";
                dlg.ClientSize = new Size(420, 160);
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.MinimizeBox = false;
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.BackColor = Bg; dlg.ForeColor = TextMain;
                var l1 = new Label(); l1.Text = "Discord fake:"; l1.Location = new Point(12, 18); l1.AutoSize = true; l1.ForeColor = TextMain; dlg.Controls.Add(l1);
                var cb1 = new ComboBox(); cb1.DropDownStyle = ComboBoxStyle.DropDownList; cb1.SetBounds(110, 14, 290, 24); foreach (var f in files) cb1.Items.Add(f); if (cb1.Items.Count > 0) cb1.SelectedIndex = 0; dlg.Controls.Add(cb1);
                var l2 = new Label(); l2.Text = "Game fake:"; l2.Location = new Point(12, 58); l2.AutoSize = true; l2.ForeColor = TextMain; dlg.Controls.Add(l2);
                var cb2 = new ComboBox(); cb2.DropDownStyle = ComboBoxStyle.DropDownList; cb2.SetBounds(110, 54, 290, 24); foreach (var f in files) cb2.Items.Add(f); if (cb2.Items.Count > 0) cb2.SelectedIndex = 0; dlg.Controls.Add(cb2);
                var ok = new Button(); ok.Text = "OK"; ok.SetBounds(220, 110, 80, 30); ok.DialogResult = DialogResult.OK; dlg.Controls.Add(ok);
                var cancel = new Button(); cancel.Text = "Отмена"; cancel.SetBounds(310, 110, 80, 30); cancel.DialogResult = DialogResult.Cancel; dlg.Controls.Add(cancel);
                dlg.AcceptButton = ok; dlg.CancelButton = cancel;
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        if (cb1.SelectedItem != null) Core.SetActiveFake(_installDir, cb1.SelectedItem.ToString(), true);
                        if (cb2.SelectedItem != null) Core.SetActiveFake(_installDir, cb2.SelectedItem.ToString(), false);
                        Log("Active fakes обновлены. Нажмите «Применить».");
                    }
                    catch (Exception ex) { Log("Replace fakes: " + ex.Message); }
                }
            }
        }
    }
}
