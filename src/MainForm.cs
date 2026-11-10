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
        private bool _checksExpanded = true;
        private Dot _tgServiceDot;
        private Label _tgServiceLabel;
        private Button _tgStart;
        private Button _tgStop;
        private Panel _settingsCard;
        private Label _gameFilterValue;
        private Label _ipsetValue;
        private Label _autoUpdateValue;
        private RichTextBox _log;
        private NotifyIcon _tray;
        private Timer _refreshTimer;
        private Timer _checksTimer;

        public MainForm(bool dev)
        {
            _dev = dev;
            _strategies = new List<StrategyInfo>();

            SuspendLayout();
            BuildUi();
            LoadInstall();
            RefreshState();
            ResumeLayout(false);

            InitTimers();
            InitTray();

            Log("Zapret Control запущен.");
            if (_dev) Log("Тестовый режим: управление службой отключено.");
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            if (!TryAutoStart()) RunChecks();
        }

        private void BuildUi()
        {
            Text = "Zapret Control";
            ClientSize = new Size(900, 920);
            BackColor = Bg;
            ForeColor = TextMain;
            Font = new Font("Segoe UI", 9f);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleMode = AutoScaleMode.None;
            DoubleBuffered = true;
            Icon = AppIcon();

            var title = MakeLabel(this, "ZAPRET", 26, 12, 24f, true, TextMain);
            title.Font = new Font("Segoe UI Semibold", 24f);

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

            _stateLabel = MakeLabel(card, "Активен", 114, 22, 15f, true, TextMain);

            _subLabel = MakeLabel(card, "Служба zapret", 116, 54, 9f, false, TextDim);

            var restart = MakeButton(card, "Перезапустить", 560, 22, 150, 32, false);
            restart.Click += delegate { RestartService(); };

            var folder = MakeButton(card, "Папка", 720, 22, 108, 32, false);
            folder.Click += delegate { OpenInstallFolder(); };
            _folderButton = folder;

            var autoLabel = MakeLabel(card, "Автозапуск службы", 560, 78, 9f, false, TextDim);
            _autoToggle = new ToggleSwitch();
            _autoToggle.SetBounds(784, 74, 56, 28);
            _autoToggle.CheckedChanged += OnAutoChanged;
            card.Controls.Add(_autoToggle);

            Controls.Add(card);
        }

        private void BuildStrategyCard()
        {
            var card = CreateCard(26, 230, 848, 96);

            MakeLabel(card, "СТРАТЕГИЯ", 20, 12, 8f, true, TextDim);

            _strategySelector = new StrategySelector();
            _strategySelector.SetBounds(18, 34, 540, 34);
            _strategySelector.SelectionChanged += delegate { };
            card.Controls.Add(_strategySelector);

            _applyButton = MakeButton(card, "Применить", 572, 34, 150, 34, true);
            _applyButton.Click += delegate { ApplySelectedStrategy(); };

            Controls.Add(card);
        }

        private void BuildChecksCard()
        {
            var card = CreateCard(26, 338, 848, 212);
            _checksCard = card;

            MakeLabel(card, "ПРОВЕРКА ДОСТУПА", 20, 12, 8f, true, TextDim);

            var checkButton = MakeButton(card, "Проверить", 664, 8, 104, 28, false);
            checkButton.Click += delegate { RunChecks(); };

            _checksToggle = MakeButton(card, "▲", 788, 8, 40, 28, false);
            _checksToggle.Font = new Font("Segoe UI", 7.5f);
            _checksToggle.Click += delegate { SetChecksExpanded(!_checksExpanded); };

            _ytDot = AddCheckRow(card, 24, 40, "YouTube", out _ytState);
            _dcDot = AddCheckRow(card, 24, 74, "Discord", out _dcState);
            _rbDot = AddCheckRow(card, 24, 108, "Roblox", out _rbState);
            _scDot = AddCheckRow(card, 24, 142, "SoundCloud", out _scState);
            _tgDot = AddCheckRow(card, 24, 176, "Telegram", out _tgState);

            Controls.Add(card);
        }

        private Dot AddCheckRow(Control parent, int x, int y, string name, out Label state)
        {
            var dot = new Dot();
            dot.SetBounds(x, y + 4, 12, 12);
            parent.Controls.Add(dot);

            MakeLabel(parent, name, x + 20, y - 3, 10.5f, true, TextMain);

            state = MakeLabel(parent, "проверка…", x + 176, y - 2, 9f, false, TextDim);
            return dot;
        }

        internal void SetChecksExpanded(bool expanded)
        {
            _checksExpanded = expanded;
            int shift = expanded ? 0 : 160;

            _checksCard.Height = expanded ? 212 : 52;

            foreach (Control control in _checksCard.Controls)
            {
                if (control.Top >= 30) control.Visible = expanded;
            }

            _settingsCard.Top = 562 - shift;
            _tgCard.Top = 682 - shift;
            _logTitle.Top = 794 - shift;
            _logPanel.Top = 814 - shift;
            ClientSize = new Size(900, 920 - shift);

            _checksToggle.Text = expanded ? "▲" : "▼";
        }

        private Dot AddCheckRow(Control parent, int y, string name, out Label state)
        {
            var dot = new Dot();
            dot.SetBounds(24, y + 4, 12, 12);
            parent.Controls.Add(dot);

            MakeLabel(parent, name, 44, y - 3, 10.5f, true, TextMain);

            state = MakeLabel(parent, "проверка…", 200, y - 2, 9f, false, TextDim);
            return dot;
        }

        private void BuildSettingsCard()
        {
            var card = CreateCard(26, 562, 848, 108);
            _settingsCard = card;

            MakeLabel(card, "НАСТРОЙКИ", 20, 12, 8f, true, TextDim);

            MakeLabel(card, "Game Filter", 24, 40, 9.5f, true, TextMain);
            _gameFilterValue = MakeLabel(card, "-", 130, 40, 9f, false, TextDim);
            var gfBtn = MakeButton(card, "Переключить", 240, 34, 120, 28, false);
            gfBtn.Click += delegate { CycleGameFilter(); };

            MakeLabel(card, "IPSet Filter", 390, 40, 9.5f, true, TextMain);
            _ipsetValue = MakeLabel(card, "-", 480, 40, 9f, false, TextDim);
            var ipBtn = MakeButton(card, "Переключить", 560, 34, 120, 28, false);
            ipBtn.Click += delegate { CycleIPSetFilter(); };

            MakeLabel(card, "Auto-Update", 24, 74, 9.5f, true, TextMain);
            _autoUpdateValue = MakeLabel(card, "-", 130, 74, 9f, false, TextDim);
            var auBtn = MakeButton(card, "Переключить", 240, 68, 120, 28, false);
            auBtn.Click += delegate { ToggleAutoUpdate(); };

            var fakeBtn = MakeButton(card, "Replace fakes", 390, 68, 120, 28, false);
            fakeBtn.Click += delegate { ReplaceActiveFakes(); };

            MakeLabel(card, "после смены: Перезапустить", 560, 74, 8.5f, false, TextDim);

            Controls.Add(card);
        }
        private void BuildTelegramCard()
        {
            var card = CreateCard(26, 682, 848, 96);
            _tgCard = card;

            MakeLabel(card, "TELEGRAM", 20, 12, 8f, true, TextDim);

            _tgServiceDot = new Dot();
            _tgServiceDot.SetBounds(24, 46, 12, 12);
            card.Controls.Add(_tgServiceDot);

            _tgServiceLabel = MakeLabel(card, "TgProxy выключен", 44, 39, 10f, true, TextMain);

            _tgStart = MakeButton(card, "Запустить", 490, 30, 120, 32, false);
            _tgStart.Click += delegate { StartTelegramProxy(true); };

            var connect = MakeButton(card, "Подключить Telegram", 620, 30, 150, 32, true);
            connect.Click += delegate { ConnectTelegram(); };

            _tgStop = MakeButton(card, "Стоп", 780, 30, 48, 32, false);
            _tgStop.Click += delegate { StopTelegramProxy(); };

            Controls.Add(card);
        }

        private void BuildLog()
        {
            _logTitle = MakeLabel(this, "ЖУРНАЛ", 30, 794, 8f, true, TextDim);

            var panel = new Panel();
            panel.SetBounds(26, 814, 848, 86);
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
                using (var path = Ui.RoundRect(new Rectangle(0, 0, card.Width - 1, card.Height - 1), 14))
                using (var pen = new Pen(CardBorder, 1f))
                    e.Graphics.DrawPath(pen, path);
            };
            card.Resize += delegate { Ui.RoundRegion(card, 14); };
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
            var button = new Button();
            button.Text = text;
            button.Font = new Font("Segoe UI Semibold", 9f);
            button.ForeColor = accent ? Color.White : TextMain;
            button.BackColor = accent ? Color.FromArgb(58, 48, 96) : Color.FromArgb(34, 36, 45);
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = accent ? Color.FromArgb(110, 90, 190) : Color.FromArgb(52, 55, 66);
            button.FlatAppearance.MouseOverBackColor = accent ? Color.FromArgb(72, 58, 118) : Color.FromArgb(44, 47, 58);
            button.FlatAppearance.MouseDownBackColor = accent ? Color.FromArgb(82, 66, 132) : Color.FromArgb(52, 55, 66);
            button.Cursor = Cursors.Hand;
            button.SetBounds(x, y, width, height);
            button.Resize += delegate { Ui.RoundRegion(button, 10); };
            parent.Controls.Add(button);
            return button;
        }

        private void LoadInstall()
        {
            _installDir = Core.DetectInstallDir();
            if (_installDir != null)
            {
                Log("Найдена установка zapret: " + _installDir);
                SaveInstallPath();
                PopulateStrategies();
            }
            else
            {
                Log("Установка zapret не найдена. Нажмите «Папка», чтобы указать её.");
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

            switch (state)
            {
                case SvcState.NotInstalled:
                    _stateLabel.Text = "Служба не найдена";
                    _stateLabel.ForeColor = Red;
                    break;
                case SvcState.Running:
                    _stateLabel.Text = "Активен";
                    _stateLabel.ForeColor = Green;
                    break;
                case SvcState.Pending:
                    _stateLabel.Text = "Перезапуск…";
                    _stateLabel.ForeColor = Yellow;
                    break;
                default:
                    _stateLabel.Text = "Выключен";
                    _stateLabel.ForeColor = TextMain;
                    break;
            }

            string strategyName = Core.GetCurrentStrategyName();
            if (_installDir != null)
                _subLabel.Text = "Служба zapret" + (string.IsNullOrEmpty(strategyName) ? "" : " · " + strategyName);
            else
                _subLabel.Text = "Установка не найдена — нажмите «Установить»";

            if (_folderButton != null)
                _folderButton.Text = _installDir == null ? "Установить" : "Папка";

            _silent = true;
            _autoToggle.Checked = Core.GetServiceAutoStart();
            _silent = false;

            bool tgRunning = Core.TgProxyRunning();
            bool tgPort = Core.TgProxyPortOpen();

            if (tgRunning && tgPort)
            {
                _tgServiceDot.Color = Green;
                _tgServiceLabel.Text = "TgProxy · 127.0.0.1:1443";
                _tgServiceLabel.ForeColor = TextMain;
            }
            else if (tgRunning)
            {
                _tgServiceDot.Color = Yellow;
                _tgServiceLabel.Text = "TgProxy запускается…";
                _tgServiceLabel.ForeColor = TextMain;
            }
            else
            {
                _tgServiceDot.Color = Color.FromArgb(120, 124, 136);
                _tgServiceLabel.Text = "TgProxy выключен";
                _tgServiceLabel.ForeColor = TextDim;
            }

            if (_tgStart != null)
            {
                _tgStart.Text = File.Exists(Core.TgProxyExe) ? "Запустить" : "Установить";
                _tgStart.Enabled = !tgRunning && !_busy;
            }
            if (_tgStop != null) _tgStop.Enabled = tgRunning && !_busy;

            if (string.IsNullOrEmpty(strategyName))
                _subLabel.Text = "Служба zapret · стратегия не определена";

            RefreshSettings();
        }

        private void ToggleService()
        {
            if (_dev) { Log("Тестовый режим: управление службой отключено."); return; }
            if (_busy) return;

            SvcState state = Core.GetServiceState();
            if (state == SvcState.NotInstalled)
            {
                if (_installDir == null) InstallZapret();
                else InstallServiceOnly();
                return;
            }

            bool starting = state != SvcState.Running;
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
                        Log(starting ? "Включено." : "Выключено.");
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

            SetCheck(_ytDot, _ytState, "проверка…", CheckState.Pending);
            SetCheck(_dcDot, _dcState, "проверка…", CheckState.Pending);
            SetCheck(_rbDot, _rbState, "проверка…", CheckState.Pending);
            SetCheck(_scDot, _scState, "проверка…", CheckState.Pending);
            SetCheck(_tgDot, _tgState, "проверка…", CheckState.Pending);

            System.Threading.Tasks.Task.Factory.StartNew(delegate
            {
                CheckResult youtube = Core.CheckUrl("https://www.youtube.com/generate_204", 10000);
                CheckResult discord = Core.CheckUrl("https://discord.com/api/v10/gateway", 10000);
                CheckResult roblox = Core.CheckUrl("https://www.roblox.com/", 10000);
                CheckResult soundcloud = Core.CheckUrl("https://soundcloud.com/", 10000);
                bool direct = Core.TelegramDirectAlive();
                bool proxyPort = Core.TgProxyPortOpen();

                BeginInvoke(new Action(delegate
                {
                    if (youtube.Ok)
                        SetCheck(_ytDot, _ytState, "доступен · " + youtube.Ms + " мс", CheckState.Ok);
                    else
                    {
                        SetCheck(_ytDot, _ytState, "недоступен · " + youtube.Info, CheckState.Fail);
                        Log("YouTube недоступен: " + youtube.Info);
                    }

                    if (discord.Ok)
                        SetCheck(_dcDot, _dcState, "доступен · " + discord.Ms + " мс", CheckState.Ok);
                    else
                    {
                        SetCheck(_dcDot, _dcState, "недоступен · " + discord.Info, CheckState.Fail);
                        Log("Discord недоступен: " + discord.Info);
                    }

                    if (roblox.Ok)
                        SetCheck(_rbDot, _rbState, "доступен · " + roblox.Ms + " мс", CheckState.Ok);
                    else
                    {
                        SetCheck(_rbDot, _rbState, "недоступен · " + roblox.Info, CheckState.Fail);
                        Log("Roblox недоступен: " + roblox.Info);
                    }

                    if (soundcloud.Ok)
                        SetCheck(_scDot, _scState, "доступен · " + soundcloud.Ms + " мс", CheckState.Ok);
                    else
                    {
                        SetCheck(_scDot, _scState, "недоступен · " + soundcloud.Info, CheckState.Fail);
                        Log("SoundCloud недоступен: " + soundcloud.Info);
                    }

                    if (proxyPort)
                        SetCheck(_tgDot, _tgState, "через прокси · 127.0.0.1:1443", CheckState.Ok);
                    else if (direct)
                        SetCheck(_tgDot, _tgState, "напрямую доступен", CheckState.Ok);
                    else
                        SetCheck(_tgDot, _tgState, "недоступен", CheckState.Fail);

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
            _refreshTimer.Interval = 2500;
            _refreshTimer.Tick += delegate { RefreshState(); };
            _refreshTimer.Start();

            _checksTimer = new Timer();
            _checksTimer.Interval = 45000;
            _checksTimer.Tick += delegate { RunChecks(); };
            _checksTimer.Start();
        }

        private void InitTray()
        {
            var menu = new ContextMenuStrip();
            menu.Items.Add("Открыть", null, delegate { ShowFromTray(); });
            menu.Items.Add("Включить", null, delegate { if (Core.GetServiceState() != SvcState.Running) ToggleService(); });
            menu.Items.Add("Выключить", null, delegate { if (Core.GetServiceState() == SvcState.Running) ToggleService(); });
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Выход", null, delegate { ExitApplication(); });

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
                        "Свернуто в трей.", ToolTipIcon.Info);
                }
                return;
            }

            if (_refreshTimer != null) { _refreshTimer.Stop(); _refreshTimer.Dispose(); }
            if (_checksTimer != null) { _checksTimer.Stop(); _checksTimer.Dispose(); }
            if (_tray != null) { _tray.Visible = false; _tray.Dispose(); }

            base.OnFormClosing(e);
        }

        private void RefreshSettings()
        {
            if (_installDir == null)
            {
                if (_gameFilterValue != null) _gameFilterValue.Text = "-";
                if (_ipsetValue != null) _ipsetValue.Text = "-";
                if (_autoUpdateValue != null) _autoUpdateValue.Text = "-";
                return;
            }
            try
            {
                string gf = Core.GetGameFilterStatus(_installDir);
                if (gf == "all") gf = "enabled";
                _gameFilterValue.Text = gf;
                _ipsetValue.Text = Core.GetIPSetStatus(_installDir);
                _autoUpdateValue.Text = Core.GetAutoUpdateEnabled(_installDir) ? "enabled" : "disabled";
            }
            catch { }
        }

        private void ReapplyHint()
        {
            Log("Нажмите «Применить» для применения настроек к службе.");
        }

        private void CycleGameFilter()
        {
            if (_installDir == null) return;
            string cur = Core.GetGameFilterStatus(_installDir);
            string next = cur == "disabled" ? "all" : (cur == "all" ? "tcp" : (cur == "tcp" ? "udp" : "disabled"));
            try { Core.SetGameFilter(_installDir, next); Log("Game Filter: " + next); } catch (Exception ex) { Log("Game Filter: " + ex.Message); }
            RefreshSettings();
            ReapplyHint();
        }

        private void CycleIPSetFilter()
        {
            if (_installDir == null) return;
            try { Core.CycleIPSet(_installDir); Log("IPSet Filter: " + Core.GetIPSetStatus(_installDir)); } catch (Exception ex) { Log("IPSet: " + ex.Message); }
            RefreshSettings();
            ReapplyHint();
        }

        private void ToggleAutoUpdate()
        {
            if (_installDir == null) return;
            bool cur = Core.GetAutoUpdateEnabled(_installDir);
            try { Core.SetAutoUpdateEnabled(_installDir, !cur); Log("Auto-Update: " + (!cur ? "enabled" : "disabled")); } catch (Exception ex) { Log("Auto-Update: " + ex.Message); }
            RefreshSettings();
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
                        Log("Active fakes обновлены.");
                        ReapplyHint();
                    }
                    catch (Exception ex) { Log("Replace fakes: " + ex.Message); }
                }
            }
        }

    }
}
