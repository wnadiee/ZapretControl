using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.ServiceProcess;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.Win32;
namespace ZapretControl
{
    public enum SvcState
    {
        NotInstalled,
        Running,
        Stopped,
        Pending,
        Unknown
    }
    public class StrategyInfo
    {
        public string Name;
        public string FilePath;
    }
    public class CheckResult
    {
        public bool Ok;
        public int Ms;
        public string Info;
    }
    public static class Core
    {
        public const string ServiceName = "zapret";
        public const string ServiceKey = @"SYSTEM\CurrentControlSet\Services\zapret";
        public const string StrategyValue = "zapret-discord-youtube";
        public const string TgProxyUrl = "https://github.com/Flowseal/tg-ws-proxy/releases/download/v1.11.4/TgWsProxy_windows.exe";
        public const string TgProxySha = "454205cbcf76f333934108d893b58a528373bce8096624e27f0b6dc16eea660f";
        public const string ZapretUrl = "https://github.com/Flowseal/zapret-discord-youtube/releases/download/1.10.4/zapret-discord-youtube-1.10.4.zip";
        public const string ZapretSha = "291af673bd0238506f9548e0e94ca0abf244000f4c9a06960544fb01c5f97b34";
        public const string DefaultStrategy = "general (ALT)";
        public static string LocalDir
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ZapretControl"); }
        }
        public static string BundledInstallDir
        {
            get { return Path.Combine(AppBaseDir, "zapret"); }
        }
        public static string DefaultInstallDir
        {
            get
            {
                if (IsInstallDir(BundledInstallDir)) return BundledInstallDir;
                return Path.Combine(LocalDir, "zapret");
            }
        }
        public static string ConfigFile
        {
            get { return Path.Combine(LocalDir, "config.ini"); }
        }
        public static string TgProxyExe
        {
            get { return Path.Combine(LocalDir, "tgproxy.svc"); }
        }
        public static void MigrateTgProxyExe()
        {
            try
            {
                string old = Path.Combine(LocalDir, "TgWsProxy.exe");
                string neu = TgProxyExe;
                if (File.Exists(old) && !File.Exists(neu))
                {
                    try { StopTgProxy(); } catch { }
                    File.Move(old, neu);
                }
            }
            catch { }
        }
        public static string AppBaseDir
        {
            get { return AppDomain.CurrentDomain.BaseDirectory; }
        }
        public static string EnsureArgDump()
        {
            string beside = Path.Combine(AppBaseDir, "ArgDump.exe");
            if (File.Exists(beside)) return beside;
            Directory.CreateDirectory(LocalDir);
            string local = Path.Combine(LocalDir, "ArgDump.exe");
            if (File.Exists(local) && new FileInfo(local).Length > 0) return local;
            using (var stream = typeof(Core).Assembly.GetManifestResourceStream("ZapretControl.ArgDump.exe"))
            {
                if (stream == null)
                    throw new FileNotFoundException("ArgDump.exe не найден. Переустановите приложение.");
                string temp = local + ".part";
                using (var file = File.Create(temp))
                    stream.CopyTo(file);
                if (File.Exists(local)) File.Delete(local);
                File.Move(temp, local);
            }
            return local;
        }
        public static bool ServiceInstalled()
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(ServiceKey))
                    return key != null;
            }
            catch { return false; }
        }
        public static SvcState GetServiceState()
        {
            if (!ServiceInstalled()) return SvcState.NotInstalled;
            try
            {
                using (var sc = new ServiceController(ServiceName))
                {
                    switch (sc.Status)
                    {
                        case ServiceControllerStatus.Running: return SvcState.Running;
                        case ServiceControllerStatus.Stopped: return SvcState.Stopped;
                        case ServiceControllerStatus.StartPending:
                        case ServiceControllerStatus.StopPending:
                        case ServiceControllerStatus.ContinuePending:
                        case ServiceControllerStatus.PausePending:
                            return SvcState.Pending;
                        default: return SvcState.Unknown;
                    }
                }
            }
            catch { return SvcState.Unknown; }
        }
        public static bool StartService(out string error)
        {
            error = null;
            try
            {
                using (var sc = new ServiceController(ServiceName))
                {
                    if (sc.Status != ServiceControllerStatus.Running)
                    {
                        if (sc.Status == ServiceControllerStatus.StartPending)
                        {
                            sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(25));
                        }
                        else
                        {
                            sc.Start();
                            sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(25));
                        }
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }
        public static bool StopService(out string error)
        {
            error = null;
            try
            {
                using (var sc = new ServiceController(ServiceName))
                {
                    if (sc.Status != ServiceControllerStatus.Stopped)
                    {
                        if (sc.Status == ServiceControllerStatus.StopPending)
                        {
                            sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(25));
                        }
                        else
                        {
                            sc.Stop();
                            sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(25));
                        }
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }
        public static bool GetServiceAutoStart()
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(ServiceKey))
                {
                    object value = key == null ? null : key.GetValue("Start");
                    return value is int && (int)value == 2;
                }
            }
            catch { return false; }
        }
        public static bool SetServiceAutoStart(bool enabled, out string error)
        {
            error = null;
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(ServiceKey, true))
                {
                    if (key == null) throw new Exception("Служба zapret не установлена.");
                    key.SetValue("Start", enabled ? 2 : 3, RegistryValueKind.DWord);
                }
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }
        private const string AppRunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string AppRunName = "ZapretControl";
        public static bool GetAppAutoStart()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(AppRunKey))
                {
                    if (key == null) return false;
                    return key.GetValue(AppRunName) != null;
                }
            }
            catch { return false; }
        }
        public static void SetAppAutoStart(bool enabled)
        {
            try
            {
                using (var key = Registry.CurrentUser.CreateSubKey(AppRunKey))
                {
                    if (key == null) return;
                    if (enabled)
                        key.SetValue(AppRunName, "\"" + System.Windows.Forms.Application.ExecutablePath + "\"", RegistryValueKind.String);
                    else
                        key.DeleteValue(AppRunName, false);
                }
            }
            catch { }
        }
        public static string GetServiceImagePath()
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(ServiceKey))
                {
                    if (key == null) return null;
                    return key.GetValue("ImagePath") as string;
                }
            }
            catch { return null; }
        }
        public static string GetCurrentStrategyName()
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(ServiceKey))
                {
                    if (key == null) return null;
                    return key.GetValue(StrategyValue) as string;
                }
            }
            catch { return null; }
        }
        public static bool IsInstallDir(string dir)
        {
            try
            {
                return !string.IsNullOrEmpty(dir) &&
                       File.Exists(Path.Combine(dir, "bin", "winws.exe"));
            }
            catch { return false; }
        }
        public static string ExtractExecutable(string commandLine)
        {
            if (string.IsNullOrEmpty(commandLine)) return null;
            commandLine = commandLine.Trim();
            if (commandLine.StartsWith("\""))
            {
                int close = commandLine.IndexOf('"', 1);
                if (close > 1) return commandLine.Substring(1, close - 1);
            }
            int space = commandLine.IndexOf(' ');
            return space > 0 ? commandLine.Substring(0, space) : commandLine;
        }
        public static string InstallDirFromService()
        {
            try
            {
                string image = GetServiceImagePath();
                if (string.IsNullOrEmpty(image)) return null;
                string exe = Environment.ExpandEnvironmentVariables(ExtractExecutable(image));
                if (string.IsNullOrEmpty(exe) || !File.Exists(exe)) return null;
                string bin = Path.GetDirectoryName(exe);
                if (string.IsNullOrEmpty(bin)) return null;
                string root = Path.GetDirectoryName(bin);
                return IsInstallDir(root) ? root : null;
            }
            catch { return null; }
        }
        public static string DetectInstallDir()
        {
            if (IsInstallDir(BundledInstallDir)) return BundledInstallDir;
            string fromService = InstallDirFromService();
            if (fromService != null) return fromService;
            string saved;
            if (LoadConfig().TryGetValue("install", out saved) && IsInstallDir(saved))
                return saved;
            if (IsInstallDir(Path.Combine(LocalDir, "zapret"))) return Path.Combine(LocalDir, "zapret");
            string user = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string[] roots = new string[]
            {
                Path.Combine(user, "Downloads"),
                Path.Combine(user, "Desktop"),
                Path.Combine(user, "Documents"),
                user,
                @"C:\"
            };
            foreach (var root in roots)
            {
                string found = SearchInstall(root, root == @"C:\" ? 1 : 3);
                if (found != null) return found;
            }
            return null;
        }
        private static string SearchInstall(string root, int depth)
        {
            try
            {
                if (!Directory.Exists(root)) return null;
                if (IsInstallDir(root)) return root;
                if (depth <= 0) return null;
                foreach (var dir in Directory.GetDirectories(root))
                {
                    if (IsInstallDir(dir)) return dir;
                    string found = SearchInstall(dir, depth - 1);
                    if (found != null) return found;
                }
            }
            catch { }
            return null;
        }
        public static string DownloadAndInstallZapret(Action<string> log)
        {
            Directory.CreateDirectory(LocalDir);
            string stamp = DateTime.Now.Ticks.ToString();
            string zip = Path.Combine(Path.GetTempPath(), "zc_zapret_" + stamp + ".zip");
            string tempDir = Path.Combine(Path.GetTempPath(), "zc_zapret_" + stamp);
            try
            {
                if (log != null) log("Скачивание zapret (около 1,5 МБ)...");
                using (var client = new WebClient())
                {
                    client.Headers.Add("User-Agent", "ZapretControl/1.0");
                    client.DownloadFile(ZapretUrl, zip);
                }
                string hash = HashFile(zip);
                if (!string.Equals(hash, ZapretSha, StringComparison.OrdinalIgnoreCase))
                    throw new Exception("Контрольная сумма zapret не совпала. Установка отменена.");
                if (log != null) log("Распаковка архива...");
                Directory.CreateDirectory(tempDir);
                ZipFile.ExtractToDirectory(zip, tempDir);
                string root = FindInstallRoot(tempDir);
                if (root == null)
                    throw new Exception("В архиве не найдена папка bin\\winws.exe.");
                string target = DefaultInstallDir;
                if (Directory.Exists(target)) Directory.Delete(target, true);
                MoveDirectory(root, target);
                ApplyCustomizations(target, log);
                if (log != null) log("Установлено: " + target);
                string error;
                if (!EnsureServiceInstalled(target, log, out error))
                    throw new Exception("Служба не установлена: " + error);
                return target;
            }
            finally
            {
                TryDelete(zip);
                TryDeleteDir(tempDir);
            }
        }
        public static void ApplyCustomizations(string installDir, Action<string> log)
        {
            try
            {
                if (string.IsNullOrEmpty(installDir)) return;
                string lists = Path.Combine(installDir, "lists");
                Directory.CreateDirectory(lists);
                string userList = Path.Combine(lists, "list-general-user.txt");
                string[] domains = new string[] {
                    "# user list",
                    "domain.example.abc",
                    "roblox.com",
                    "www.roblox.com",
                    "api.roblox.com",
                    "apis.roblox.com",
                    "auth.roblox.com",
                    "avatar.roblox.com",
                    "assetdelivery.roblox.com",
                    "catalog.roblox.com",
                    "chat.roblox.com",
                    "clientsettings.roblox.com",
                    "clientsettingscdn.roblox.com",
                    "data.roblox.com",
                    "develop.roblox.com",
                    "economy.roblox.com",
                    "friends.roblox.com",
                    "games.roblox.com",
                    "groups.roblox.com",
                    "inventory.roblox.com",
                    "locale.roblox.com",
                    "notifications.roblox.com",
                    "presence.roblox.com",
                    "privatemessages.roblox.com",
                    "publish.roblox.com",
                    "realtime.roblox.com",
                    "thumbnails.roblox.com",
                    "trades.roblox.com",
                    "translations.roblox.com",
                    "users.roblox.com",
                    "web.roblox.com",
                    "titanium.roblox.com",
                    "setup.roblox.com",
                    "versioncompatibility.api.roblox.com",
                    "metrics.roblox.com",
                    "ephemeralcounters.api.roblox.com",
                    "rbxcdn.com",
                    "rbxcdn.net",
                    "rbxcdn.xyz",
                    "rbx.com",
                    "robloxlabs.com",
                    "setup.rbxcdn.com",
                    "arkoselabs.com",
                    "soundcloud.com",
                    "sndcdn.com"
                };
                File.WriteAllLines(userList, domains, new UTF8Encoding(false));
                SetAutoUpdateEnabled(installDir, false);
                if (log != null) log("Применены пользовательские списки (Roblox/SoundCloud).");
            }
            catch (Exception ex)
            {
                if (log != null) log("Не удалось применить изменения списков: " + ex.Message);
            }
        }
        private static string FindInstallRoot(string dir)
        {
            try
            {
                if (!Directory.Exists(dir)) return null;
                if (IsInstallDir(dir)) return dir;
                foreach (var sub in Directory.GetDirectories(dir))
                {
                    if (IsInstallDir(sub)) return sub;
                    foreach (var sub2 in Directory.GetDirectories(sub))
                        if (IsInstallDir(sub2)) return sub2;
                }
            }
            catch { }
            return null;
        }
        private static void MoveDirectory(string source, string target)
        {
            Directory.CreateDirectory(target);
            foreach (var file in Directory.GetFiles(source))
                File.Move(file, Path.Combine(target, Path.GetFileName(file)));
            foreach (var dir in Directory.GetDirectories(source))
                MoveDirectory(dir, Path.Combine(target, Path.GetFileName(dir)));
            try { Directory.Delete(source, true); }
            catch { }
        }
        public static bool EnsureServiceInstalled(string installDir, Action<string> log, out string error)
        {
            error = null;
            try
            {
                if (GetServiceState() == SvcState.Running && SameDir(InstallDirFromService(), installDir))
                    return true;
                var strategies = GetStrategies(installDir);
                if (strategies.Count == 0)
                    throw new Exception("В папке нет стратегий general*.bat.");
                StrategyInfo chosen = strategies.FirstOrDefault(s => s.Name == DefaultStrategy) ?? strategies[0];
                if (log != null) log("Стратегия: " + chosen.Name);
                string args = ExtractArguments(chosen.FilePath, installDir, log);
                string winws = Path.Combine(installDir, "bin", "winws.exe");
                string bat = Path.Combine(Path.GetTempPath(), "zc_install_" + DateTime.Now.Ticks + ".bat");
                var sb = new StringBuilder();
                sb.AppendLine("@echo off");
                sb.AppendLine("net stop zapret >nul 2>&1");
                sb.AppendLine("sc delete zapret >nul 2>&1");
                sb.AppendLine("sc create zapret binPath= \"\\\"" + winws + "\\\" " + args + "\" DisplayName= \"zapret\" start= auto");
                sb.AppendLine("sc description zapret \"Zapret DPI bypass software\"");
                sb.AppendLine("reg add \"HKLM\\System\\CurrentControlSet\\Services\\zapret\" /v zapret-discord-youtube /t REG_SZ /d \"" + chosen.Name + "\" /f >nul");
                sb.AppendLine("netsh interface tcp set global timestamps=enabled >nul 2>&1");
                try
                {
                    File.WriteAllText(bat, sb.ToString(), new UTF8Encoding(false));
                    var psi = new ProcessStartInfo("cmd.exe", "/c \"" + bat + "\"");
                    psi.UseShellExecute = false;
                    psi.CreateNoWindow = true;
                    psi.WindowStyle = ProcessWindowStyle.Hidden;
                    using (var p = Process.Start(psi))
                        p.WaitForExit(30000);
                }
                finally
                {
                    TryDelete(bat);
                }
                if (!ServiceInstalled())
                    throw new Exception("sc create не создал службу.");
                if (!StartService(out error))
                    throw new Exception("Служба не запустилась: " + error);
                if (log != null) log("Служба zapret установлена и запущена.");
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }
        private static bool SameDir(string a, string b)
        {
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return false;
            return string.Equals(a.TrimEnd('\\'), b.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase);
        }
        private static void TryDeleteDir(string path)
        {
            try { if (Directory.Exists(path)) Directory.Delete(path, true); }
            catch { }
        }
        public static List<StrategyInfo> GetStrategies(string installDir)
        {
            var list = new List<StrategyInfo>();
            if (string.IsNullOrEmpty(installDir) || !Directory.Exists(installDir)) return list;
            foreach (var file in Directory.GetFiles(installDir, "general*.bat"))
            {
                var info = new StrategyInfo();
                info.Name = Path.GetFileNameWithoutExtension(file);
                info.FilePath = file;
                list.Add(info);
            }
            list.Sort(delegate(StrategyInfo a, StrategyInfo b)
            {
                return string.CompareOrdinal(NaturalKey(a.Name), NaturalKey(b.Name));
            });
            return list;
        }
        private static string NaturalKey(string name)
        {
            return Regex.Replace(name.ToLowerInvariant(), @"\d+",
                delegate(Match m) { return m.Value.PadLeft(8, '0'); });
        }
        public static string ExtractArguments(string batPath, string installDir, Action<string> log)
        {
            string binDir = Path.Combine(installDir, "bin");
            string winws = Path.Combine(binDir, "winws.exe");
            if (!File.Exists(winws))
                throw new FileNotFoundException("winws.exe не найден в папке bin.");
            string argDumpSource = EnsureArgDump();
            string stamp = Process.GetCurrentProcess().Id + "_" + DateTime.Now.Ticks;
            string dumpName = "_zc_argdump_" + stamp + ".exe";
            string dumpPath = Path.Combine(binDir, dumpName);
            string tempBat = Path.Combine(installDir, "_zc_extract_" + stamp + ".bat");
            string outFile = Path.Combine(Path.GetTempPath(), "zc_args_" + stamp + ".txt");
            try
            {
                File.Copy(argDumpSource, dumpPath, true);
                string text = File.ReadAllText(batPath, Encoding.UTF8);
                text = Regex.Replace(text, "winws\\.exe", dumpName, RegexOptions.IgnoreCase);
                text = Regex.Replace(text, @"(?im)^\s*call\s+service\.bat\s+status_zapret\s*$", "");
                text = Regex.Replace(text, @"(?im)^\s*pause\s*$", "");
                File.WriteAllText(tempBat, text, new UTF8Encoding(false));
                var psi = new ProcessStartInfo("cmd.exe", "/c \"" + tempBat + "\"");
                psi.WorkingDirectory = installDir;
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                psi.WindowStyle = ProcessWindowStyle.Hidden;
                psi.RedirectStandardInput = true;
                psi.EnvironmentVariables["ZC_ARGDUMP_OUT"] = outFile;
                psi.EnvironmentVariables["NO_UPDATE_CHECK"] = "1";
                using (var p = Process.Start(psi))
                {
                    try { p.StandardInput.Close(); }
                    catch { }
                    if (!p.WaitForExit(12000))
                    {
                        try { p.Kill(); } catch { }
                        try { p.WaitForExit(3000); } catch { }
                    }
                }
                string dumped = null;
                for (int i = 0; i < 100; i++)
                {
                    try
                    {
                        if (File.Exists(outFile))
                        {
                            dumped = File.ReadAllText(outFile, Encoding.UTF8);
                            if (!string.IsNullOrEmpty(dumped.Trim())) break;
                        }
                    }
                    catch { }
                    Thread.Sleep(100);
                }
                if (dumped == null || dumped.Trim().Length == 0)
                    throw new Exception("Скрипт стратегии не вернул параметры запуска.");
                dumped = dumped.Trim();
                string args;
                if (dumped.StartsWith("\""))
                {
                    int close = dumped.IndexOf('"', 1);
                    args = close > 0 ? dumped.Substring(close + 1).Trim() : "";
                }
                else
                {
                    int space = dumped.IndexOf(' ');
                    args = space > 0 ? dumped.Substring(space + 1).Trim() : "";
                }
                if (args.Length == 0)
                    throw new Exception("Не удалось разобрать параметры стратегии.");
                if (args.IndexOf('%') >= 0)
                    throw new Exception("В параметрах остались неразрешённые переменные: " + args);
                if (args.IndexOf("--wf-tcp", StringComparison.OrdinalIgnoreCase) < 0)
                    throw new Exception("В стратегии не найдены параметры winws.");
                if (log != null) log("Параметры стратегии получены.");
                return args;
            }
            finally
            {
                TryDelete(tempBat);
                TryDelete(dumpPath);
                TryDelete(outFile);
            }
        }
        public static bool ApplyStrategy(string installDir, StrategyInfo strategy, Action<string> log, out string error)
        {
            error = null;
            try
            {
                string args = ExtractArguments(strategy.FilePath, installDir, log);
                string winws = Path.Combine(installDir, "bin", "winws.exe");
                string imagePath = "\"" + winws + "\" " + args;
                using (var key = Registry.LocalMachine.OpenSubKey(ServiceKey, true))
                {
                    if (key == null) throw new Exception("Служба zapret не установлена.");
                    key.SetValue("ImagePath", imagePath, RegistryValueKind.ExpandString);
                    key.SetValue(StrategyValue, strategy.Name, RegistryValueKind.String);
                }
                if (log != null) log("Стратегия записана в службу: " + strategy.Name);
                string ignored;
                if (GetServiceState() == SvcState.Running)
                {
                    if (log != null) log("Перезапуск службы...");
                    StopService(out ignored);
                }
                if (!StartService(out error))
                    throw new Exception("Служба не запустилась: " + error);
                if (log != null) log("Служба запущена с новой стратегией.");
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }
        public static string NormalizeArgs(string args)
        {
            if (args == null) return "";
            string result = args.ToLowerInvariant().Replace("\"", " ");
            result = Regex.Replace(result, @"(^|\s)--([a-z0-9-]+)=", "$1--$2 ");
            result = Regex.Replace(result, @"\s+", " ");
            return result.Trim();
        }
        public static string GetServiceArgs()
        {
            string image = GetServiceImagePath();
            if (string.IsNullOrEmpty(image)) return null;
            string exe = ExtractExecutable(image.Trim());
            if (string.IsNullOrEmpty(exe)) return null;
            int index = image.IndexOf(exe, StringComparison.OrdinalIgnoreCase);
            if (index < 0) return null;
            return image.Substring(index + exe.Length).Trim();
        }
        public static CheckResult CheckUrl(string url, int timeoutMs)
        {
            var watch = Stopwatch.StartNew();
            try
            {
                var request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";
                request.Timeout = timeoutMs;
                request.ReadWriteTimeout = timeoutMs;
                request.Proxy = null;
                request.AllowAutoRedirect = true;
                request.KeepAlive = false;
                request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) ZapretControl/1.0";
                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    watch.Stop();
                    int code = (int)response.StatusCode;
                    var result = new CheckResult();
                    result.Ok = code < 500;
                    result.Ms = (int)watch.ElapsedMilliseconds;
                    result.Info = code.ToString();
                    return result;
                }
            }
            catch (WebException ex)
            {
                watch.Stop();
                var response = ex.Response as HttpWebResponse;
                if (response != null)
                {
                    int code = (int)response.StatusCode;
                    response.Close();
                    var result = new CheckResult();
                    result.Ok = code < 500;
                    result.Ms = (int)watch.ElapsedMilliseconds;
                    result.Info = code.ToString();
                    return result;
                }
                var failed = new CheckResult();
                failed.Ok = false;
                failed.Ms = (int)watch.ElapsedMilliseconds;
                failed.Info = ex.Status.ToString();
                return failed;
            }
            catch (Exception ex)
            {
                watch.Stop();
                var failed = new CheckResult();
                failed.Ok = false;
                failed.Ms = (int)watch.ElapsedMilliseconds;
                failed.Info = ex.Message;
                return failed;
            }
        }
        public static bool TcpAlive(string host, int port, int timeoutMs)
        {
            try
            {
                using (var client = new TcpClient())
                {
                    var async = client.BeginConnect(host, port, null, null);
                    if (!async.AsyncWaitHandle.WaitOne(timeoutMs)) return false;
                    client.EndConnect(async);
                    return true;
                }
            }
            catch { return false; }
        }
        public static bool TelegramDirectAlive()
        {
            return TcpAlive("149.154.167.99", 443, 4000);
        }
        public static bool TgProxyRunning()
        {
            try
            {
                if (Process.GetProcessesByName("tgproxy").Length > 0) return true;
                if (Process.GetProcessesByName("TgWsProxy").Length > 0) return true;
            }
            catch { }
            try { if (TgProxyPortOpen()) return true; } catch { }
            return false;
        }
        public static int GetTgProxyPort()
        {
            try
            {
                if (!File.Exists(TgConfigPath)) return 1443;
                string text = File.ReadAllText(TgConfigPath);
                var m = Regex.Match(text, "\"port\"\\s*:\\s*(\\d+)");
                if (m.Success) { int p; if (int.TryParse(m.Groups[1].Value, out p) && p > 0 && p < 65536) return p; }
            }
            catch { }
            return 1443;
        }
        public static void SetTgProxyPort(int port)
        {
            SetTgConfigInt("port", port);
        }
        public static string ReadTgConfigText()
        {
            try { if (File.Exists(TgConfigPath)) return File.ReadAllText(TgConfigPath, Encoding.UTF8); } catch { }
            return "{}";
        }
        public static void WriteTgConfigText(string text)
        {
            try
            {
                string dir = Path.GetDirectoryName(TgConfigPath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(TgConfigPath, text, new UTF8Encoding(false));
            }
            catch { }
        }
        public static int GetTgConfigInt(string key, int def)
        {
            try
            {
                string t = ReadTgConfigText();
                var m = Regex.Match(t, "\"" + Regex.Escape(key) + "\"\\s*:\\s*(-?\\d+)");
                if (m.Success) { int v; if (int.TryParse(m.Groups[1].Value, out v)) return v; }
            }
            catch { }
            return def;
        }
        public static bool GetTgConfigBool(string key, bool def)
        {
            try
            {
                string t = ReadTgConfigText();
                var m = Regex.Match(t, "\"" + Regex.Escape(key) + "\"\\s*:\\s*(true|false)", RegexOptions.IgnoreCase);
                if (m.Success) return m.Groups[1].Value.Equals("true", StringComparison.OrdinalIgnoreCase);
            }
            catch { }
            return def;
        }
        public static string GetTgConfigString(string key, string def)
        {
            try
            {
                string t = ReadTgConfigText();
                var m = Regex.Match(t, "\"" + Regex.Escape(key) + "\"\\s*:\\s*\"([^\"]*)\"");
                if (m.Success) return m.Groups[1].Value;
            }
            catch { }
            return def;
        }
        public static void SetTgConfigInt(string key, int value)
        {
            string t = ReadTgConfigText();
            string pat = "\"" + Regex.Escape(key) + "\"\\s*:\\s*-?\\d+";
            string rep = "\"" + key + "\": " + value;
            if (Regex.IsMatch(t, pat)) t = Regex.Replace(t, pat, rep);
            else t = InsertTgConfigField(t, rep);
            WriteTgConfigText(t);
        }
        public static void SetTgConfigBool(string key, bool value)
        {
            string t = ReadTgConfigText();
            string pat = "\"" + Regex.Escape(key) + "\"\\s*:\\s*(true|false)";
            string rep = "\"" + key + "\": " + (value ? "true" : "false");
            if (Regex.IsMatch(t, pat, RegexOptions.IgnoreCase)) t = Regex.Replace(t, pat, rep, RegexOptions.IgnoreCase);
            else t = InsertTgConfigField(t, rep);
            WriteTgConfigText(t);
        }
        public static void SetTgConfigString(string key, string value)
        {
            string t = ReadTgConfigText();
            string val = (value ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"");
            string pat = "\"" + Regex.Escape(key) + "\"\\s*:\\s*\"[^\"]*\"";
            string rep = "\"" + key + "\": \"" + val + "\"";
            if (Regex.IsMatch(t, pat)) t = Regex.Replace(t, pat, rep);
            else t = InsertTgConfigField(t, rep);
            WriteTgConfigText(t);
        }
        private static string InsertTgConfigField(string text, string field)
        {
            if (string.IsNullOrWhiteSpace(text) || text.Trim() == "{}") return "{\r\n  " + field + "\r\n}\r\n";
            int idx = text.LastIndexOf('}');
            if (idx <= 0) return text;
            string before = text.Substring(0, idx).TrimEnd();
            if (!before.EndsWith(",")) before += ",";
            return before + "\r\n  " + field + "\r\n}\r\n";
        }
        public static bool TgProxyPortOpen()
        {
            return TcpAlive("127.0.0.1", GetTgProxyPort(), 150);
        }
        public static string TgConfigPath
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TgWsProxy", "config.json"); }
        }
        public static string GetTelegramLink(out int port)
        {
            port = GetTgProxyPort();
            try
            {
                if (!File.Exists(TgConfigPath)) return null;
                string text = File.ReadAllText(TgConfigPath);
                var secretMatch = Regex.Match(text, "\"secret\"\\s*:\\s*\"([^\"]+)\"");
                if (!secretMatch.Success || secretMatch.Groups[1].Value.Length == 0) return null;
                string secret = secretMatch.Groups[1].Value;
                if (secret.Length == 32 && !secret.StartsWith("dd", StringComparison.OrdinalIgnoreCase))
                    secret = "dd" + secret;
                return "tg://proxy?server=127.0.0.1&port=" + port + "&secret=" + secret;
            }
            catch { return null; }
        }
        public static string FindTelegramDesktop()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string[] candidates = new string[]
            {
                Path.Combine(appData, "Telegram Desktop", "Telegram.exe"),
                Path.Combine(local, "Programs", "Telegram Desktop", "Telegram.exe"),
                Path.Combine(programFiles, "Telegram Desktop", "Telegram.exe")
            };
            foreach (var path in candidates)
                if (File.Exists(path)) return path;
            return null;
        }
        public static void EnsureTgProtocol(string telegramExe)
        {
            try
            {
                if (string.IsNullOrEmpty(telegramExe) || !File.Exists(telegramExe)) return;
                using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Classes\tg"))
                {
                    key.SetValue("", "URL:Telegram Link");
                    key.SetValue("URL Protocol", "");
                    using (var command = key.CreateSubKey(@"shell\open\command"))
                        command.SetValue("", "\"" + telegramExe + "\" \"%1\"");
                }
            }
            catch { }
        }
        public static void EnsureTgProxy(Action<string> log)
        {
            MigrateTgProxyExe();
            if (File.Exists(TgProxyExe)) return;
            Directory.CreateDirectory(LocalDir);
            string temp = TgProxyExe + ".part";
            if (File.Exists(temp)) File.Delete(temp);
            if (log != null) log("Скачивание TgProxy (около 22 МБ)...");
            using (var client = new WebClient())
            {
                client.Headers.Add("User-Agent", "ZapretControl/1.0");
                client.DownloadFile(TgProxyUrl, temp);
            }
            string hash = HashFile(temp);
            if (!string.Equals(hash, TgProxySha, StringComparison.OrdinalIgnoreCase))
            {
                File.Delete(temp);
                throw new Exception("Контрольная сумма TgProxy не совпала. Загрузка отменена.");
            }
            if (File.Exists(TgProxyExe)) File.Delete(TgProxyExe);
            File.Move(temp, TgProxyExe);
            if (log != null) log("TgProxy загружен и проверен.");
        }
        public static void StartTgProxy()
        {
            MigrateTgProxyExe();
            if (!File.Exists(TgProxyExe))
                throw new FileNotFoundException("TgProxy не найден.");
            if (TgProxyRunning() || TgProxyPortOpen()) return;
            var psi = new ProcessStartInfo(TgProxyExe);
            psi.WorkingDirectory = LocalDir;
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            psi.WindowStyle = ProcessWindowStyle.Hidden;
            try { Process.Start(psi); }
            catch
            {
                psi.UseShellExecute = true;
                psi.CreateNoWindow = false;
                psi.WindowStyle = ProcessWindowStyle.Hidden;
                Process.Start(psi);
            }
        }
        public static void StopTgProxy()
        {
            foreach (var name in new string[] { "tgproxy", "TgWsProxy" })
            {
                try
                {
                    foreach (var process in Process.GetProcessesByName(name))
                    {
                        try { process.Kill(); } catch { }
                        finally { process.Dispose(); }
                    }
                }
                catch { }
            }
        }
        public static string HashFile(string path)
        {
            using (var sha = SHA256.Create())
            using (var stream = File.OpenRead(path))
            {
                byte[] hash = sha.ComputeHash(stream);
                var sb = new StringBuilder();
                foreach (byte b in hash) sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }
        public static Dictionary<string, string> LoadConfig()
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                if (!File.Exists(ConfigFile)) return result;
                foreach (var line in File.ReadAllLines(ConfigFile))
                {
                    int index = line.IndexOf('=');
                    if (index <= 0) continue;
                    string key = line.Substring(0, index).Trim();
                    string value = line.Substring(index + 1).Trim();
                    if (key.Length > 0) result[key] = value;
                }
            }
            catch { }
            return result;
        }
        public static void SaveConfig(Dictionary<string, string> values)
        {
            Directory.CreateDirectory(LocalDir);
            var sb = new StringBuilder();
            foreach (var pair in values)
                sb.Append(pair.Key).Append('=').Append(pair.Value).Append("\r\n");
            File.WriteAllText(ConfigFile, sb.ToString(), Encoding.UTF8);
        }
        public static string GetLang()
        {
            try
            {
                string v;
                if (LoadConfig().TryGetValue("lang", out v) && (v == "en" || v == "ru")) return v;
            }
            catch { }
            return "ru";
        }
        public static void SetLang(string lang)
        {
            try
            {
                var cfg = LoadConfig();
                cfg["lang"] = (lang == "en") ? "en" : "ru";
                SaveConfig(cfg);
            }
            catch { }
        }
        private static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch { }
        }
        public static string GetGameFilterStatus(string installDir)
        {
            try
            {
                if (string.IsNullOrEmpty(installDir)) return "disabled";
                string path = Path.Combine(installDir, "utils", "game_filter.enabled");
                if (!File.Exists(path)) return "disabled";
                string mode = "all";
                foreach (var line in File.ReadAllLines(path))
                {
                    var t = line.Trim();
                    if (t.StartsWith("mode=", StringComparison.OrdinalIgnoreCase)) mode = t.Substring(5).Trim().ToLowerInvariant();
                }
                if (mode == "tcp") return "tcp";
                if (mode == "udp") return "udp";
                if (mode == "disabled") return "disabled";
                return "all";
            }
            catch { return "disabled"; }
        }
        public static void SetGameFilter(string installDir, string mode)
        {
            if (string.IsNullOrEmpty(installDir)) return;
            string utils = Path.Combine(installDir, "utils");
            Directory.CreateDirectory(utils);
            string path = Path.Combine(utils, "game_filter.enabled");
            if (mode == "disabled")
            {
                TryDelete(path);
                return;
            }
            var sb = new StringBuilder();
            sb.AppendLine("mode=" + mode);
            if (mode == "all" || mode == "tcp") sb.AppendLine("tcp=1024-65535");
            if (mode == "all" || mode == "udp") sb.AppendLine("udp=1024-65535");
            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
        }
        public static string GetIPSetStatus(string installDir)
        {
            try
            {
                if (string.IsNullOrEmpty(installDir)) return "unknown";
                string listFile = Path.Combine(installDir, "lists", "ipset-all.txt");
                if (!File.Exists(listFile)) return "any";
                var lines = File.ReadAllLines(listFile).Where(l => l.Trim().Length > 0).ToList();
                if (lines.Count == 0) return "any";
                if (lines.Count == 1 && lines[0].Trim() == "203.0.113.113/32") return "none";
                return "loaded";
            }
            catch { return "unknown"; }
        }
        public static void CycleIPSet(string installDir)
        {
            if (string.IsNullOrEmpty(installDir)) return;
            string listFile = Path.Combine(installDir, "lists", "ipset-all.txt");
            string backup = listFile + ".backup";
            Directory.CreateDirectory(Path.GetDirectoryName(listFile));
            string status = GetIPSetStatus(installDir);
            if (status == "loaded")
            {
                try { if (File.Exists(backup)) File.Delete(backup); } catch { }
                if (File.Exists(listFile)) File.Move(listFile, backup);
                File.WriteAllText(listFile, "203.0.113.113/32\r\n", new UTF8Encoding(false));
            }
            else if (status == "none")
            {
                File.WriteAllText(listFile, "", new UTF8Encoding(false));
            }
            else
            {
                if (File.Exists(backup))
                {
                    try { if (File.Exists(listFile)) File.Delete(listFile); } catch { }
                    File.Move(backup, listFile);
                }
                else
                {
                    File.WriteAllText(listFile, "203.0.113.113/32\r\n", new UTF8Encoding(false));
                }
            }
        }
        public static bool GetAutoUpdateEnabled(string installDir)
        {
            try
            {
                if (string.IsNullOrEmpty(installDir)) return false;
                return File.Exists(Path.Combine(installDir, "utils", "check_updates.enabled"));
            }
            catch { return false; }
        }
        public static void SetAutoUpdateEnabled(string installDir, bool enabled)
        {
            if (string.IsNullOrEmpty(installDir)) return;
            string utils = Path.Combine(installDir, "utils");
            Directory.CreateDirectory(utils);
            string path = Path.Combine(utils, "check_updates.enabled");
            if (enabled) File.WriteAllText(path, "ENABLED\r\n", new UTF8Encoding(false));
            else TryDelete(path);
        }
        public static List<string> GetFakeBinFiles(string installDir)
        {
            var list = new List<string>();
            try
            {
                if (string.IsNullOrEmpty(installDir)) return list;
                string bin = Path.Combine(installDir, "bin");
                if (!Directory.Exists(bin)) return list;
                foreach (var f in Directory.GetFiles(bin, "*.bin"))
                {
                    string name = Path.GetFileNameWithoutExtension(f);
                    if (!name.StartsWith("ACTIVE_", StringComparison.OrdinalIgnoreCase))
                        list.Add(Path.GetFileName(f));
                }
                list.Sort(StringComparer.OrdinalIgnoreCase);
            }
            catch { }
            return list;
        }
        public static void SetActiveFake(string installDir, string fileName, bool forDiscord)
        {
            if (string.IsNullOrEmpty(installDir) || string.IsNullOrEmpty(fileName)) return;
            string bin = Path.Combine(installDir, "bin");
            string src = Path.Combine(bin, fileName);
            if (!File.Exists(src)) return;
            string dst = Path.Combine(bin, forDiscord ? "ACTIVE_DISCORD_UDP.bin" : "ACTIVE_GAME_UDP.bin");
            File.Copy(src, dst, true);
        }
        public static int SelfTest()
        {
            var report = new StringBuilder();
            int failed = 0;
            Action<string, bool> check = delegate(string name, bool ok)
            {
                report.AppendLine((ok ? "PASS  " : "FAIL  ") + name);
                if (!ok) failed++;
            };
            string dir = DetectInstallDir();
            check("папка zapret найдена", dir != null);
            if (dir != null)
            {
                report.AppendLine("  папка: " + dir);
                var strategies = GetStrategies(dir);
                check("стратегии найдены (" + strategies.Count + ")", strategies.Count > 5);
                string currentName = GetCurrentStrategyName();
                check("текущая стратегия: " + (currentName ?? "?"), !string.IsNullOrEmpty(currentName));
                StrategyInfo current = strategies.FirstOrDefault(s => s.Name == currentName);
                check("файл текущей стратегии найден", current != null);
                if (current != null)
                {
                    string args = null;
                    try { args = ExtractArguments(current.FilePath, dir, null); }
                    catch (Exception ex) { report.AppendLine("  ошибка: " + ex.Message); }
                    check("аргументы извлечены", args != null && args.IndexOf("--wf-tcp", StringComparison.OrdinalIgnoreCase) >= 0);
                    if (args != null)
                    {
                        string serviceArgs = GetServiceArgs();
                        check("совпадает с аргументами службы",
                            NormalizeArgs(args) == NormalizeArgs(serviceArgs));
                        if (NormalizeArgs(args) != NormalizeArgs(serviceArgs))
                        {
                            report.AppendLine("  extracted: " + NormalizeArgs(args));
                            report.AppendLine("  service  : " + NormalizeArgs(serviceArgs));
                        }
                    }
                }
                StrategyInfo alt = strategies.FirstOrDefault(s => s.Name == "general (FAKE TLS AUTO)");
                if (alt != null)
                {
                    string altArgs = null;
                    try { altArgs = ExtractArguments(alt.FilePath, dir, null); }
                    catch (Exception ex) { report.AppendLine("  ошибка ALT: " + ex.Message); }
                    check("другая стратегия извлекается", altArgs != null && altArgs.IndexOf("--wf-tcp", StringComparison.OrdinalIgnoreCase) >= 0);
                }
            }
            check("TgProxy скачан", File.Exists(TgProxyExe));
            if (File.Exists(TgConfigPath))
            {
                int port;
                string link = GetTelegramLink(out port);
                check("ссылка tg:// сформирована", link != null && link.StartsWith("tg://proxy?"));
                if (link != null) report.AppendLine("  " + link);
            }
            check("служба установлена", ServiceInstalled());
            report.AppendLine();
            report.AppendLine(failed == 0 ? "ИТОГ: OK" : "ИТОГ: ОШИБОК " + failed);
            try
            {
                File.WriteAllText(Path.Combine(AppBaseDir, "selftest.txt"), report.ToString(), Encoding.UTF8);
                Console.WriteLine(report.ToString());
            }
            catch { }
            return failed == 0 ? 0 : 1;
        }
    }
}
