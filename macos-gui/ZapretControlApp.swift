import AppKit
import Foundation

@main
struct ZapretControlMain {
    static func main() {
        let app = NSApplication.shared
        let delegate = AppDelegate()
        app.delegate = delegate
        app.setActivationPolicy(.accessory)
        app.run()
    }
}

final class AppDelegate: NSObject, NSApplicationDelegate, NSMenuDelegate {
    private let fm = FileManager.default
    private var statusItem: NSStatusItem!
    private let menu = NSMenu()
    private var tgStatusItem = NSMenuItem()
    private var tgStartItem = NSMenuItem()
    private var tgStopItem = NSMenuItem()
    private var tgAutostartItem = NSMenuItem()
    private var appAutostartItem = NSMenuItem()
    private var timer: Timer?

    private var appSupport: URL { fm.urls(for: .applicationSupportDirectory, in: .userDomainMask)[0].appendingPathComponent("ZapretControl", isDirectory: true) }
    private var tgDir: URL { appSupport.appendingPathComponent("tgproxy", isDirectory: true) }
    private var tgBin: URL { tgDir.appendingPathComponent("tg-ws-proxy") }
    private var tgLog: URL { tgDir.appendingPathComponent("tg-ws-proxy.log") }
    private var tgConfDir: URL { fm.urls(for: .applicationSupportDirectory, in: .userDomainMask)[0].appendingPathComponent("TgWsProxy", isDirectory: true) }
    private var tgConf: URL { tgConfDir.appendingPathComponent("config.json") }
    private var tgPlist: URL { fm.homeDirectoryForCurrentUser.appendingPathComponent("Library/LaunchAgents/com.flowseal.tg-ws-proxy.plist") }
    private var appPlist: URL { fm.homeDirectoryForCurrentUser.appendingPathComponent("Library/LaunchAgents/io.github.wnadiee.zapretcontrol.plist") }

    func applicationDidFinishLaunching(_ notification: Notification) {
        try? fm.createDirectory(at: tgDir, withIntermediateDirectories: true)
        try? fm.createDirectory(at: tgConfDir, withIntermediateDirectories: true)
        ensureTgConfig()
        buildMenu()
        refresh()
        timer = Timer.scheduledTimer(withTimeInterval: 2, repeats: true) { [weak self] _ in self?.refresh() }
    }

    func menuNeedsUpdate(_ menu: NSMenu) { refresh() }

    private func buildMenu() {
        statusItem = NSStatusBar.system.statusItem(withLength: NSStatusItem.variableLength)
        statusItem.menu = menu
        menu.delegate = self

        let title = NSMenuItem(title: "Zapret Control", action: nil, keyEquivalent: "")
        title.isEnabled = false
        menu.addItem(title)
        menu.addItem(.separator())

        tgStatusItem = NSMenuItem(title: "TgProxy: —", action: nil, keyEquivalent: "")
        tgStatusItem.isEnabled = false
        menu.addItem(tgStatusItem)

        tgStartItem = NSMenuItem(title: "Запустить TgProxy", action: #selector(startTg), keyEquivalent: "")
        tgStartItem.target = self; menu.addItem(tgStartItem)
        tgStopItem = NSMenuItem(title: "Остановить TgProxy", action: #selector(stopTg), keyEquivalent: "")
        tgStopItem.target = self; menu.addItem(tgStopItem)

        let installTg = NSMenuItem(title: "Установить/обновить TgProxy", action: #selector(installTg), keyEquivalent: "")
        installTg.target = self; menu.addItem(installTg)

        tgAutostartItem = NSMenuItem(title: "Автозапуск TgProxy", action: #selector(toggleTgAutostart), keyEquivalent: "")
        tgAutostartItem.target = self; menu.addItem(tgAutostartItem)

        menu.addItem(.separator())
        let openZapret = NSMenuItem(title: "Открыть ZapretMac", action: #selector(openZapretMac), keyEquivalent: "")
        openZapret.target = self; menu.addItem(openZapret)
        let openLists = NSMenuItem(title: "Открыть списки ZapretMac", action: #selector(openLists), keyEquivalent: "")
        openLists.target = self; menu.addItem(openLists)

        menu.addItem(.separator())
        let checks = NSMenuItem(title: "Проверить доступность", action: #selector(runChecks), keyEquivalent: "")
        checks.target = self; menu.addItem(checks)

        appAutostartItem = NSMenuItem(title: "Автозапуск приложения", action: #selector(toggleAppAutostart), keyEquivalent: "")
        appAutostartItem.target = self; menu.addItem(appAutostartItem)

        menu.addItem(.separator())
        let quit = NSMenuItem(title: "Выход", action: #selector(quit), keyEquivalent: "q")
        quit.target = self; menu.addItem(quit)
    }

    private func refresh() {
        let port = tgPort()
        let running = isPortOpen(port)
        statusItem.button?.title = running ? "ZC: TgProxy" : "ZC"
        tgStatusItem.title = running ? "TgProxy: работает (127.0.0.1:\(port))" : "TgProxy: не запущен"
        tgStartItem.isEnabled = !running && fm.fileExists(atPath: tgBin.path)
        tgStopItem.isEnabled = running
        tgAutostartItem.state = fm.fileExists(atPath: tgPlist.path) ? .on : .off
        appAutostartItem.state = fm.fileExists(atPath: appPlist.path) ? .on : .off
    }

    private func ensureTgConfig() {
        if !fm.fileExists(atPath: tgConf.path) {
            try? "{ \"port\": 1443, \"host\": \"127.0.0.1\" }\n".write(to: tgConf, atomically: true, encoding: .utf8)
        }
    }

    private func tgPort() -> Int {
        guard let data = try? Data(contentsOf: tgConf),
              let obj = try? JSONSerialization.jsonObject(with: data) as? [String: Any],
              let p = obj["port"] as? Int else { return 1443 }
        return p
    }

    private func isPortOpen(_ port: Int) -> Bool {
        var addr = sockaddr_in()
        addr.sin_len = UInt8(MemoryLayout<sockaddr_in>.size)
        addr.sin_family = sa_family_t(AF_INET)
        addr.sin_port = in_port_t(UInt16(port).bigEndian)
        addr.sin_addr = in_addr(s_addr: inet_addr("127.0.0.1"))
        let s = socket(AF_INET, SOCK_STREAM, 0)
        if s < 0 { return false }
        defer { close(s) }
        var tv = timeval(tv_sec: 0, tv_usec: 300000)
        setsockopt(s, SOL_SOCKET, SO_SNDTIMEO, &tv, socklen_t(MemoryLayout<timeval>.size))
        let res = withUnsafePointer(to: &addr) { $0.withMemoryRebound(to: sockaddr.self, capacity: 1) { connect(s, $0, socklen_t(MemoryLayout<sockaddr_in>.size)) } }
        return res == 0
    }

    @objc private func startTg() {
        ensureTgConfig()
        guard fm.fileExists(atPath: tgBin.path) else { info("Сначала установите TgProxy."); return }
        if isPortOpen(tgPort()) { refresh(); return }
        let task = Process()
        task.executableURL = tgBin
        task.currentDirectoryURL = tgDir
        let log = try? FileHandle(forWritingTo: tgLog) ?? FileHandle.nullDevice
        if fm.fileExists(atPath: tgLog.path) { log?.seekToEndOfFile() } else { fm.createFile(atPath: tgLog.path, contents: nil); }
        task.standardOutput = log
        task.standardError = log
        try? task.run()
        DispatchQueue.main.asyncAfter(deadline: .now() + 0.8) { [weak self] in self?.refresh() }
    }

    @objc private func stopTg() {
        run("/usr/bin/pkill", ["-f", tgBin.path])
        refresh()
    }

    @objc private func installTg() {
        let url = "https://github.com/Flowseal/tg-ws-proxy/releases/latest/download/TgWsProxy_macos_universal.dmg"
        DispatchQueue.global().async {
            let tmp = self.tgDir.appendingPathComponent("tg.dmg")
            let ok = self.runCapture("/usr/bin/curl", ["-fL", "--retry", "3", "-o", tmp.path, url]).status == 0
            if ok { _ = self.extractDmg(tmp) }
            try? self.fm.removeItem(at: tmp)
            DispatchQueue.main.async {
                self.refresh()
                self.info(ok && self.fm.fileExists(atPath: self.tgBin.path) ? "TgProxy установлен." : "Не удалось установить TgProxy.")
            }
        }
    }

    private func extractDmg(_ dmg: URL) -> Bool {
        let mnt = URL(fileURLWithPath: NSTemporaryDirectory()).appendingPathComponent("tgws-\(UUID().uuidString)")
        try? fm.createDirectory(at: mnt, withIntermediateDirectories: true)
        guard runCapture("/usr/bin/hdiutil", ["attach", dmg.path, "-nobrowse", "-quiet", "-mountpoint", mnt.path]).status == 0 else { return false }
        defer { _ = runCapture("/usr/bin/hdiutil", ["detach", mnt.path, "-quiet"]); try? fm.removeItem(at: mnt) }
        let candidates = (try? fm.contentsOfDirectory(at: mnt, includingPropertiesForKeys: nil, options: [.skipsHiddenFiles])) ?? []
        var found: URL?
        for c in candidates {
            if c.lastPathComponent == "tg-ws-proxy" || c.lastPathComponent.hasPrefix("TgWsProxy") { found = c; break }
        }
        if found == nil {
            if let e = fm.enumerator(at: mnt, includingPropertiesForKeys: [.isExecutableKey], options: [.skipsHiddenFiles]) {
                for case let u as URL in e {
                    let name = u.lastPathComponent.lowercased()
                    if name.contains("tg") && name.contains("proxy") { found = u; break }
                }
            }
        }
        guard let src = found else { return false }
        try? fm.removeItem(at: tgBin)
        try? fm.copyItem(at: src, to: tgBin)
        try? fm.setAttributes([.posixPermissions: 0o755], ofItemAtPath: tgBin.path)
        return fm.fileExists(atPath: tgBin.path)
    }

    @objc private func toggleTgAutostart() {
        if fm.fileExists(atPath: tgPlist.path) {
            run("/bin/launchctl", ["unload", tgPlist.path]); try? fm.removeItem(at: tgPlist)
        } else {
            let plist = """
            <?xml version=\"1.0\" encoding=\"UTF-8\"?>
            <!DOCTYPE plist PUBLIC \"-//Apple//DTD PLIST 1.0//EN\" \"http://www.apple.com/DTDs/PropertyList-1.0.dtd\">
            <plist version=\"1.0\"><dict>
            <key>Label</key><string>com.flowseal.tg-ws-proxy</string>
            <key>ProgramArguments</key><array><string>\(tgBin.path)</string></array>
            <key>RunAtLoad</key><true/><key>KeepAlive</key><true/>
            </dict></plist>
            """
            try? plist.write(to: tgPlist, atomically: true, encoding: .utf8)
            run("/bin/launchctl", ["load", tgPlist.path])
        }
        refresh()
    }

    @objc private func toggleAppAutostart() {
        if fm.fileExists(atPath: appPlist.path) {
            run("/bin/launchctl", ["unload", appPlist.path]); try? fm.removeItem(at: appPlist)
        } else {
            let exe = Bundle.main.executablePath ?? ""
            let plist = """
            <?xml version=\"1.0\" encoding=\"UTF-8\"?>
            <!DOCTYPE plist PUBLIC \"-//Apple//DTD PLIST 1.0//EN\" \"http://www.apple.com/DTDs/PropertyList-1.0.dtd\">
            <plist version=\"1.0\"><dict>
            <key>Label</key><string>io.github.wnadiee.zapretcontrol</string>
            <key>ProgramArguments</key><array><string>\(exe)</string></array>
            <key>RunAtLoad</key><true/>
            </dict></plist>
            """
            try? plist.write(to: appPlist, atomically: true, encoding: .utf8)
            run("/bin/launchctl", ["load", appPlist.path])
        }
        refresh()
    }

    @objc private func openZapretMac() {
        let candidates = [
            "/Applications/ZapretMac.app",
            fm.homeDirectoryForCurrentUser.appendingPathComponent("Applications/ZapretMac.app").path
        ]
        for c in candidates where fm.fileExists(atPath: c) {
            NSWorkspace.shared.open(URL(fileURLWithPath: c)); return
        }
        if let url = URL(string: "https://github.com/Flowseal/zapret-mac-discord-youtube/releases/latest") {
            NSWorkspace.shared.open(url)
        }
    }

    @objc private func openLists() {
        let dir = fm.homeDirectoryForCurrentUser.appendingPathComponent("Library/Application Support/ZapretMac/lists", isDirectory: true)
        if fm.fileExists(atPath: dir.path) { NSWorkspace.shared.open(dir) }
        else { info("Списки ZapretMac не найдены. Сначала запустите ZapretMac.") }
    }

    @objc private func runChecks() {
        let hosts = [("YouTube","youtube.com"),("Discord","discord.com"),("Telegram","telegram.org")]
        var lines: [String] = []
        for (name, host) in hosts {
            let ok = runCapture("/sbin/ping", ["-c", "1", "-W", "2000", host]).status == 0
            lines.append("\(name): \(ok ? "доступен" : "недоступен")")
        }
        info(lines.joined(separator: "\n"))
    }

    @objc private func quit() { NSApp.terminate(nil) }

    @discardableResult private func run(_ path: String, _ args: [String]) -> Int32 { runCapture(path, args).status }
    private func runCapture(_ path: String, _ args: [String]) -> (status: Int32, out: String) {
        let p = Process(); p.executableURL = URL(fileURLWithPath: path); p.arguments = args
        let pipe = Pipe(); p.standardOutput = pipe; p.standardError = pipe
        do { try p.run(); p.waitUntilExit() } catch { return (1, "") }
        let data = pipe.fileHandleForReading.readDataToEndOfFile()
        return (p.terminationStatus, String(data: data, encoding: .utf8) ?? "")
    }

    private func info(_ text: String) {
        DispatchQueue.main.async {
            let a = NSAlert(); a.messageText = "Zapret Control"; a.informativeText = text; a.runModal()
        }
    }
}
