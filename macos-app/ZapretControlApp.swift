import AppKit
import Foundation

@main
struct ZapretControlMain {
    static func main() {
        let app = NSApplication.shared
        let delegate = AppDelegate()
        app.delegate = delegate
        app.setActivationPolicy(.regular)
        app.run()
    }
}

final class AppDelegate: NSObject, NSApplicationDelegate {
    private var window: NSWindow!
    private var statusPill: NSTextField!
    private var stateLabel: NSTextField!
    private var subLabel: NSTextField!
    private var strategyPopup: NSPopUpButton!
    private var logView: NSTextView!
    private var tgStatusLabel: NSTextField!
    private var tgPortField: NSTextField!
    private var checkLabels: [String: (NSView, NSTextField)] = [:]
    private var appAutoBtn: NSButton!
    private var tgAutoBtn: NSButton!
    private var timer: Timer?
    private let fm = FileManager.default

    private var appSupport: URL { fm.urls(for: .applicationSupportDirectory, in: .userDomainMask)[0].appendingPathComponent("ZapretControl", isDirectory: true) }
    private var tgDir: URL { appSupport.appendingPathComponent("tgproxy", isDirectory: true) }
    private var tgBin: URL { tgDir.appendingPathComponent("tg-ws-proxy") }
    private var tgLog: URL { tgDir.appendingPathComponent("tg-ws-proxy.log") }
    private var tgConf: URL { fm.urls(for: .applicationSupportDirectory, in: .userDomainMask)[0].appendingPathComponent("TgWsProxy/config.json") }
    private var tgPlist: URL { fm.homeDirectoryForCurrentUser.appendingPathComponent("Library/LaunchAgents/com.flowseal.tg-ws-proxy.plist") }
    private var appPlist: URL { fm.homeDirectoryForCurrentUser.appendingPathComponent("Library/LaunchAgents/io.github.wnadiee.zapretcontrol.plist") }

    func applicationDidFinishLaunching(_ notification: Notification) {
        try? fm.createDirectory(at: tgDir, withIntermediateDirectories: true)
        try? fm.createDirectory(at: tgConf.deletingLastPathComponent(), withIntermediateDirectories: true)
        ensureTgConfig()
        buildWindow()
        refresh()
        timer = Timer.scheduledTimer(withTimeInterval: 2, repeats: true) { [weak self] _ in self?.refresh() }
    }

    func applicationShouldTerminateAfterLastWindowClosed(_ sender: NSApplication) -> Bool { true }

    private func buildWindow() {
        let rect = NSRect(x: 0, y: 0, width: 900, height: 980)
        window = NSWindow(contentRect: rect, styleMask: [.titled, .closable, .miniaturizable], backing: .buffered, defer: false)
        window.title = "Zapret Control"
        window.center()
        let cv = NSView(frame: rect)
        cv.wantsLayer = true
        cv.layer?.backgroundColor = NSColor(calibratedRed: 0.067, green: 0.071, blue: 0.09, alpha: 1).cgColor
        window.contentView = cv

        let title = label("ZAPRET", 24, true, .white)
        title.frame = NSRect(x: 26, y: 930, width: 300, height: 36); cv.addSubview(title)
        statusPill = label("● неактивен", 11, true, NSColor(calibratedWhite: 0.6, alpha: 1))
        statusPill.alignment = .center
        statusPill.frame = NSRect(x: 692, y: 928, width: 182, height: 34)
        statusPill.wantsLayer = true; statusPill.layer?.backgroundColor = NSColor(calibratedRed: 0.12, green: 0.125, blue: 0.157, alpha: 1).cgColor; statusPill.layer?.cornerRadius = 17
        cv.addSubview(statusPill)

        // Service card
        let svc = card(x: 26, y: 778, w: 848, h: 140, in: cv)
        let power = NSButton(title: "⏻", target: self, action: #selector(openZapretMac))
        power.bezelStyle = .circular; power.frame = NSRect(x: 20, y: 34, width: 72, height: 72); svc.addSubview(power)
        stateLabel = label("Неактивен", 15, true, .white); stateLabel.frame = NSRect(x: 114, y: 82, width: 300, height: 22); svc.addSubview(stateLabel)
        subLabel = label("Служба zapret", 9, false, dim()); subLabel.frame = NSRect(x: 116, y: 60, width: 300, height: 16); svc.addSubview(subLabel)
        addBtn(svc, "Перезапустить", 560, 86, 150, 32, #selector(openZapretMac), false)
        addBtn(svc, "Папка", 720, 86, 108, 32, #selector(openLists), false)
        let autoLbl = label("Автозапуск службы", 9, false, dim()); autoLbl.frame = NSRect(x: 560, y: 40, width: 160, height: 16); svc.addSubview(autoLbl)
        let svcAuto = label("(в ZapretMac)", 9, false, dim()); svcAuto.frame = NSRect(x: 700, y: 40, width: 120, height: 16); svc.addSubview(svcAuto)

        // Strategy card
        let strat = card(x: 26, y: 670, w: 848, h: 96, in: cv)
        let stTitle = label("СТРАТЕГИЯ", 8, true, dim()); stTitle.frame = NSRect(x: 20, y: 72, width: 200, height: 14); strat.addSubview(stTitle)
        strategyPopup = NSPopUpButton(frame: NSRect(x: 18, y: 30, width: 540, height: 30))
        strategyPopup.addItems(withTitles: ["(настраивается в ZapretMac)"])
        strat.addSubview(strategyPopup)
        addBtn(strat, "Применить", 572, 28, 150, 34, #selector(openZapretMac), true)

        // Checks card
        let checks = card(x: 26, y: 432, w: 848, h: 226, in: cv)
        let chTitle = label("ПРОВЕРКА ДОСТУПА", 8, true, dim()); chTitle.frame = NSRect(x: 20, y: 202, width: 200, height: 14); checks.addSubview(chTitle)
        let rows = [("YouTube", 160), ("Discord", 130), ("Roblox", 100), ("SoundCloud", 70), ("Telegram", 40)]
        for (name, y) in rows {
            let dot = NSView(frame: NSRect(x: 24, y: y+4, width: 12, height: 12))
            dot.wantsLayer = true; dot.layer?.cornerRadius = 6; dot.layer?.backgroundColor = NSColor.gray.cgColor
            checks.addSubview(dot)
            let nl = label(name, 10, true, .white); nl.frame = NSRect(x: 44, y: y-2, width: 120, height: 16); checks.addSubview(nl)
            let sl = label("Проверка…", 9, false, dim()); sl.frame = NSRect(x: 200, y: y-1, width: 200, height: 16); checks.addSubview(sl)
            checkLabels[name] = (dot, sl)
        }
        addBtn(checks, "Проверить", 24, 6, 120, 30, #selector(runChecks), false)

        // Settings card
        let settings = card(x: 26, y: 284, w: 848, h: 136, in: cv)
        let setTitle = label("НАСТРОЙКИ", 8, true, dim()); setTitle.frame = NSRect(x: 20, y: 112, width: 200, height: 14); settings.addSubview(setTitle)
        let gf = label("Game Filter", 9, true, .white); gf.frame = NSRect(x: 24, y: 74, width: 90, height: 16); settings.addSubview(gf)
        settings.addSubview(popup(["disabled","all","tcp","udp"], 120, 70, 130))
        let ip = label("IPSet Filter", 9, true, .white); ip.frame = NSRect(x: 282, y: 74, width: 90, height: 16); settings.addSubview(ip)
        settings.addSubview(popup(["loaded","none","any"], 374, 70, 130))
        let au = label("Auto-Update", 9, true, .white); au.frame = NSRect(x: 536, y: 74, width: 90, height: 16); settings.addSubview(au)
        settings.addSubview(popup(["disabled","enabled"], 636, 70, 130))
        let appAutoLbl = label("Автозапуск приложения", 9, true, .white); appAutoLbl.frame = NSRect(x: 430, y: 28, width: 170, height: 16); settings.addSubview(appAutoLbl)
        appAutoBtn = NSButton(checkboxWithTitle: "", target: self, action: #selector(toggleAppAutostart))
        appAutoBtn.frame = NSRect(x: 600, y: 26, width: 24, height: 20); settings.addSubview(appAutoBtn)
        addBtn(settings, "Сохранить", 24, 20, 150, 34, #selector(saveSettings), true)
        addBtn(settings, "Replace fakes", 186, 20, 150, 34, #selector(noop), false)

        // Telegram card
        let tg = card(x: 26, y: 158, w: 848, h: 114, in: cv)
        let tgTitle = label("TELEGRAM / TgProxy", 8, true, dim()); tgTitle.frame = NSRect(x: 20, y: 90, width: 200, height: 14); tg.addSubview(tgTitle)
        tgStatusLabel = label("TgProxy выключен", 10, true, .white); tgStatusLabel.frame = NSRect(x: 44, y: 64, width: 420, height: 18); tg.addSubview(tgStatusLabel)
        let portLbl = label("Порт", 9, false, dim()); portLbl.frame = NSRect(x: 24, y: 30, width: 40, height: 16); tg.addSubview(portLbl)
        tgPortField = NSTextField(string: String(tgPort()))
        tgPortField.frame = NSRect(x: 62, y: 26, width: 70, height: 24); tg.addSubview(tgPortField)
        addBtn(tg, "OK", 138, 24, 44, 28, #selector(saveTgPort), false)
        addBtn(tg, "Настройки", 192, 24, 110, 28, #selector(openTgConfig), false)
        addBtn(tg, "Запустить", 490, 54, 120, 34, #selector(startTg), false)
        addBtn(tg, "Подключить Telegram", 620, 54, 150, 34, #selector(connectTelegram), true)
        addBtn(tg, "Стоп", 780, 54, 48, 34, #selector(stopTg), false)
        tgAutoBtn = NSButton(checkboxWithTitle: "Автозапуск TgProxy", target: self, action: #selector(toggleTgAutostart))
        tgAutoBtn.frame = NSRect(x: 320, y: 28, width: 160, height: 18); tg.addSubview(tgAutoBtn)

        // Log
        let logTitle = label("ЖУРНАЛ", 8, true, dim()); logTitle.frame = NSRect(x: 30, y: 130, width: 100, height: 14); cv.addSubview(logTitle)
        let scroll = NSScrollView(frame: NSRect(x: 26, y: 20, width: 848, height: 106))
        scroll.hasVerticalScroller = true; scroll.borderType = .lineBorder
        logView = NSTextView(frame: scroll.bounds)
        logView.isEditable = false; logView.backgroundColor = NSColor(calibratedRed: 0.05, green: 0.055, blue: 0.07, alpha: 1)
        logView.textColor = NSColor(calibratedWhite: 0.7, alpha: 1); logView.font = NSFont.monospacedSystemFont(ofSize: 11, weight: .regular)
        scroll.documentView = logView; cv.addSubview(scroll)

        window.makeKeyAndOrderFront(nil)
        NSApp.activate(ignoringOtherApps: true)
        log("Zapret Control запущен.")
    }

    private func dim() -> NSColor { NSColor(calibratedRed: 0.573, green: 0.592, blue: 0.64, alpha: 1) }
    private func card(x: CGFloat, y: CGFloat, w: CGFloat, h: CGFloat, in parent: NSView) -> NSView {
        let v = NSView(frame: NSRect(x: x, y: y, width: w, height: h))
        v.wantsLayer = true
        v.layer?.backgroundColor = NSColor(calibratedRed: 0.102, green: 0.11, blue: 0.137, alpha: 1).cgColor
        v.layer?.cornerRadius = 14
        v.layer?.borderWidth = 1
        v.layer?.borderColor = NSColor(calibratedRed: 0.157, green: 0.169, blue: 0.208, alpha: 1).cgColor
        parent.addSubview(v); return v
    }
    private func label(_ t: String, _ s: CGFloat, _ bold: Bool, _ c: NSColor) -> NSTextField {
        let f = NSTextField(labelWithString: t)
        f.font = bold ? NSFont.boldSystemFont(ofSize: s) : NSFont.systemFont(ofSize: s)
        f.textColor = c; f.backgroundColor = .clear; return f
    }
    private func addBtn(_ p: NSView, _ t: String, _ x: CGFloat, _ y: CGFloat, _ w: CGFloat, _ h: CGFloat, _ a: Selector, _ accent: Bool) {
        let b = NSButton(title: t, target: self, action: a)
        b.frame = NSRect(x: x, y: y, width: w, height: h); b.bezelStyle = .rounded; p.addSubview(b)
    }
    private func popup(_ items: [String], _ x: CGFloat, _ y: CGFloat, _ w: CGFloat) -> NSPopUpButton {
        let p = NSPopUpButton(frame: NSRect(x: x, y: y, width: w, height: 26)); p.addItems(withTitles: items); return p
    }

    private func ensureTgConfig() {
        if !fm.fileExists(atPath: tgConf.path) { try? "{ \"port\": 1443, \"host\": \"127.0.0.1\" }\n".write(to: tgConf, atomically: true, encoding: .utf8) }
    }
    private func tgPort() -> Int {
        guard let d = try? Data(contentsOf: tgConf), let o = try? JSONSerialization.jsonObject(with: d) as? [String:Any], let p = o["port"] as? Int else { return 1443 }
        return p
    }
    private func portOpen(_ port: Int) -> Bool {
        var addr = sockaddr_in(); addr.sin_len = UInt8(MemoryLayout<sockaddr_in>.size); addr.sin_family = sa_family_t(AF_INET)
        addr.sin_port = in_port_t(UInt16(port).bigEndian); addr.sin_addr = in_addr(s_addr: inet_addr("127.0.0.1"))
        let s = socket(AF_INET, SOCK_STREAM, 0); if s < 0 { return false }; defer { close(s) }
        var tv = timeval(tv_sec: 0, tv_usec: 300000); setsockopt(s, SOL_SOCKET, SO_SNDTIMEO, &tv, socklen_t(MemoryLayout<timeval>.size))
        let r = withUnsafePointer(to: &addr){ $0.withMemoryRebound(to: sockaddr.self, capacity: 1){ connect(s,$0,socklen_t(MemoryLayout<sockaddr_in>.size)) } }
        return r == 0
    }
    @discardableResult private func run(_ path: String, _ args: [String]) -> Int32 {
        let p = Process(); p.executableURL = URL(fileURLWithPath: path); p.arguments = args
        let pipe = Pipe(); p.standardOutput = pipe; p.standardError = pipe
        try? p.run(); p.waitUntilExit(); return p.terminationStatus
    }

    private func log(_ s: String) {
        let ts = DateFormatter.localizedString(from: Date(), dateStyle: .none, timeStyle: .medium)
        DispatchQueue.main.async { self.logView.string += "\(ts)  \(s)\n"; self.logView.scrollToEndOfDocument(nil) }
    }

    private func refresh() {
        DispatchQueue.main.async {
            let ok = self.portOpen(self.tgPort())
            self.tgStatusLabel.stringValue = ok ? "TgProxy работает · 127.0.0.1:\(self.tgPort())" : "TgProxy выключен"
            self.tgStatusLabel.textColor = ok ? .white : self.dim()
            self.statusPill.stringValue = ok ? "● TgProxy" : "● неактивен"
            self.appAutoBtn.state = self.fm.fileExists(atPath: self.appPlist.path) ? .on : .off
            self.tgAutoBtn.state = self.fm.fileExists(atPath: self.tgPlist.path) ? .on : .off
        }
    }

    @objc private func runChecks() {
        let map = [("YouTube","youtube.com"),("Discord","discord.com"),("Roblox","roblox.com"),("SoundCloud","soundcloud.com"),("Telegram","telegram.org")]
        DispatchQueue.global().async {
            for (n,h) in map {
                let ok = self.run("/sbin/ping", ["-c","1","-W","2000",h]) == 0
                DispatchQueue.main.async {
                    if let (dot,sl) = self.checkLabels[n] { dot.layer?.backgroundColor = (ok ? NSColor.systemGreen : NSColor.systemRed).cgColor; sl.stringValue = ok ? "Доступен" : "Недоступен"; sl.textColor = ok ? .white : self.dim() }
                }
            }
        }
        log("Проверка выполнена.")
    }

    @objc private func startTg() {
        ensureTgConfig(); guard fm.fileExists(atPath: tgBin.path) else { log("Сначала установите TgProxy."); return }
        if portOpen(tgPort()) { log("TgProxy уже запущен."); refresh(); return }
        let p = Process(); p.executableURL = tgBin; p.currentDirectoryURL = tgDir
        if !fm.fileExists(atPath: tgLog.path) { fm.createFile(atPath: tgLog.path, contents: nil) }
        if let h = try? FileHandle(forWritingTo: tgLog) { h.seekToEndOfFile(); p.standardOutput = h; p.standardError = h }
        try? p.run(); log("Запуск TgProxy..."); DispatchQueue.main.asyncAfter(deadline: .now()+1){ self.refresh() }
    }
    @objc private func stopTg() { run("/usr/bin/pkill", ["-f", tgBin.path]); log("TgProxy остановлен."); refresh() }
    @objc private func installTg() {
        log("Скачивание TgProxy...")
        DispatchQueue.global().async {
            let url = "https://github.com/Flowseal/tg-ws-proxy/releases/latest/download/TgWsProxy_macos_universal.dmg"
            let tmp = self.tgDir.appendingPathComponent("tg.dmg")
            let ok = self.run("/usr/bin/curl", ["-fL","--retry","3","-o",tmp.path,url]) == 0 && self.extractDmg(tmp)
            try? self.fm.removeItem(at: tmp)
            DispatchQueue.main.async { self.log(ok ? "TgProxy установлен." : "Ошибка установки TgProxy."); self.refresh() }
        }
    }
    private func extractDmg(_ dmg: URL) -> Bool {
        let mnt = URL(fileURLWithPath: NSTemporaryDirectory()).appendingPathComponent("tgws-\(UUID().uuidString)")
        try? fm.createDirectory(at: mnt, withIntermediateDirectories: true)
        guard run("/usr/bin/hdiutil", ["attach", dmg.path, "-nobrowse","-quiet","-mountpoint",mnt.path]) == 0 else { return false }
        defer { _ = run("/usr/bin/hdiutil", ["detach", mnt.path, "-quiet"]); try? fm.removeItem(at: mnt) }
        var found: URL?
        if let e = fm.enumerator(at: mnt, includingPropertiesForKeys: nil) { for case let u as URL in e { let n=u.lastPathComponent.lowercased(); if n.contains("tg") && (n.contains("proxy") || n=="tg-ws-proxy") { found=u; break } } }
        guard let src=found else { return false }
        try? fm.removeItem(at: tgBin); try? fm.copyItem(at: src, to: tgBin); try? fm.setAttributes([.posixPermissions:0o755], ofItemAtPath: tgBin.path)
        return fm.fileExists(atPath: tgBin.path)
    }
    @objc private func saveTgPort() {
        let p = Int(tgPortField.stringValue) ?? 1443
        var d: [String:Any] = [:]
        if let data = try? Data(contentsOf: tgConf), let o = try? JSONSerialization.jsonObject(with: data) as? [String:Any] { d=o }
        d["port"]=p; if let data = try? JSONSerialization.data(withJSONObject: d, options: [.prettyPrinted]) { try? data.write(to: tgConf) }
        log("Порт TgProxy: \(p)")
    }
    @objc private func toggleTgAutostart() {
        if fm.fileExists(atPath: tgPlist.path) { run("/bin/launchctl", ["unload", tgPlist.path]); try? fm.removeItem(at: tgPlist) }
        else {
            let plist = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<!DOCTYPE plist PUBLIC \"-//Apple//DTD PLIST 1.0//EN\" \"http://www.apple.com/DTDs/PropertyList-1.0.dtd\">\n<plist version=\"1.0\"><dict><key>Label</key><string>com.flowseal.tg-ws-proxy</string><key>ProgramArguments</key><array><string>\(tgBin.path)</string></array><key>RunAtLoad</key><true/><key>KeepAlive</key><true/></dict></plist>"
            try? plist.write(to: tgPlist, atomically: true, encoding: .utf8); run("/bin/launchctl", ["load", tgPlist.path])
        }; refresh()
    }
    @objc private func toggleAppAutostart() {
        if fm.fileExists(atPath: appPlist.path) { run("/bin/launchctl", ["unload", appPlist.path]); try? fm.removeItem(at: appPlist) }
        else {
            let exe = Bundle.main.executablePath ?? ""
            let plist = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<!DOCTYPE plist PUBLIC \"-//Apple//DTD PLIST 1.0//EN\" \"http://www.apple.com/DTDs/PropertyList-1.0.dtd\">\n<plist version=\"1.0\"><dict><key>Label</key><string>io.github.wnadiee.zapretcontrol</string><key>ProgramArguments</key><array><string>\(exe)</string></array><key>RunAtLoad</key><true/></dict></plist>"
            try? plist.write(to: appPlist, atomically: true, encoding: .utf8); run("/bin/launchctl", ["load", appPlist.path])
        }; refresh()
    }
    @objc private func openZapretMac() {
        for c in ["/Applications/ZapretMac.app", fm.homeDirectoryForCurrentUser.appendingPathComponent("Applications/ZapretMac.app").path] where fm.fileExists(atPath: c) { NSWorkspace.shared.open(URL(fileURLWithPath: c)); return }
        if let u = URL(string:"https://github.com/Flowseal/zapret-mac-discord-youtube/releases/latest") { NSWorkspace.shared.open(u) }
    }
    @objc private func openLists() {
        let d = fm.homeDirectoryForCurrentUser.appendingPathComponent("Library/Application Support/ZapretMac/lists")
        if fm.fileExists(atPath: d.path) { NSWorkspace.shared.open(d) } else { log("Списки ZapretMac не найдены.") }
    }
    @objc private func openTgConfig() { NSWorkspace.shared.open(tgConf.deletingLastPathComponent()) }
    @objc private func connectTelegram() {
        guard portOpen(tgPort()) else { log("Сначала запустите TgProxy."); return }
        if let u = URL(string: "tg://proxy?server=127.0.0.1&port=\(tgPort())&type=socks5") { NSWorkspace.shared.open(u) }
    }
    @objc private func saveSettings() { log("Настройки сохранены. (Стратегии/IPSet — в ZapretMac)") }
    @objc private func noop() { log("Операция недоступна в этой версии.") }
}