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
    private var lang: String = "ru"
    private var headerDot: NSView!
    private var statusPill: NSTextField!
    private var langRu: NSTextField!
    private var langEn: NSTextField!
    private var langKnob: NSView!
    private var langTrack: NSView!
    private var stateLabel: NSTextField!
    private var subLabel: NSTextField!
    private var strategyPopup: NSPopUpButton!
    private var logView: NSTextView!
    private var tgDot: NSView!
    private var tgStatusLabel: NSTextField!
    private var tgPortField: NSTextField!
    private var checkLabels: [String: (NSView, NSTextField)] = [:]
    private var appAutoBtn: NSButton!
    private var tgAutoBtn: NSButton!
    private var tgStartButton: NSButton!
    private var timer: Timer?
    private let fm = FileManager.default
    private var lblStrategy: NSTextField!
    private var lblChecks: NSTextField!
    private var lblSettings: NSTextField!
    private var lblLog: NSTextField!
    private var lblAppAuto: NSTextField!
    private var lblTgAuto: NSTextField!
    private var lblPort: NSTextField!
    private var btnRestart: NSButton!
    private var btnFolder: NSButton!
    private var btnApply: NSButton!
    private var btnCheck: NSButton!
    private var btnSave: NSButton!
    private var btnReplace: NSButton!
    private var btnTgSettings: NSButton!
    private var btnTgConnect: NSButton!
    private var btnTgStop: NSButton!
    private var btnOpenZapret: NSButton!
    private var appSupport: URL { fm.urls(for: .applicationSupportDirectory, in: .userDomainMask)[0].appendingPathComponent("ZapretControl", isDirectory: true) }
    private var stateURL: URL { appSupport.appendingPathComponent("state.json") }
    private var tgDir: URL { appSupport.appendingPathComponent("tgproxy", isDirectory: true) }
    private var tgBin: URL { tgDir.appendingPathComponent("tg-ws-proxy") }
    private var tgLog: URL { tgDir.appendingPathComponent("tg-ws-proxy.log") }
    private var tgConf: URL { fm.urls(for: .applicationSupportDirectory, in: .userDomainMask)[0].appendingPathComponent("TgWsProxy/config.json") }
    private var tgPlist: URL { fm.homeDirectoryForCurrentUser.appendingPathComponent("Library/LaunchAgents/com.flowseal.tg-ws-proxy.plist") }
    private var appPlist: URL { fm.homeDirectoryForCurrentUser.appendingPathComponent("Library/LaunchAgents/io.github.wnadiee.zapretcontrol.plist") }
    func applicationDidFinishLaunching(_ notification: Notification) {
        try? fm.createDirectory(at: appSupport, withIntermediateDirectories: true)
        try? fm.createDirectory(at: tgDir, withIntermediateDirectories: true)
        try? fm.createDirectory(at: tgConf.deletingLastPathComponent(), withIntermediateDirectories: true)
        ensureTgConfig()
        lang = loadState()["lang"] as? String ?? "ru"
        buildUI()
        applyLanguage()
        timer = Timer.scheduledTimer(withTimeInterval: 2.0, repeats: true) { [weak self] _ in self?.refresh() }
        refresh()
        runChecks()
    }
    func applicationShouldTerminateAfterLastWindowClosed(_ sender: NSApplication) -> Bool { true }
    private func s(_ ru: String, _ en: String) -> String { lang == "en" ? en : ru }
    private func loadState() -> [String: Any] {
        guard let d = try? Data(contentsOf: stateURL), let o = try? JSONSerialization.jsonObject(with: d) as? [String: Any] else { return [:] }
        return o
    }
    private func saveState(_ st: [String: Any]) {
        var cur = loadState(); for (k,v) in st { cur[k]=v }
        if let d = try? JSONSerialization.data(withJSONObject: cur, options: [.prettyPrinted]) { try? d.write(to: stateURL) }
    }
    private func buildUI() {
        let rect = NSRect(x: 0, y: 0, width: 900, height: 940)
        window = NSWindow(contentRect: rect, styleMask: [.titled, .closable, .miniaturizable], backing: .buffered, defer: false)
        window.title = "Zapret Control"
        window.center()
        let cv = NSView(frame: rect)
        cv.wantsLayer = true
        cv.layer?.backgroundColor = NSColor(calibratedRed: 0.059, green: 0.067, blue: 0.09, alpha: 1).cgColor
        window.contentView = cv
        let title = label("ZAPRET", 22, true, .white)
        title.frame = NSRect(x: 28, y: 892, width: 200, height: 30); cv.addSubview(title)
        headerDot = NSView(frame: NSRect(x: 690, y: 900, width: 10, height: 10))
        headerDot.wantsLayer = true; headerDot.layer?.cornerRadius = 5
        headerDot.layer?.backgroundColor = NSColor(calibratedRed: 0.47, green: 0.49, blue: 0.53, alpha: 1).cgColor
        cv.addSubview(headerDot)
        statusPill = label(s("ВЫКЛЮЧЕН","STOPPED"), 10, true, NSColor(calibratedRed: 0.588, green: 0.604, blue: 0.647, alpha: 1))
        statusPill.frame = NSRect(x: 706, y: 892, width: 90, height: 24); cv.addSubview(statusPill)
        langTrack = NSView(frame: NSRect(x: 804, y: 892, width: 74, height: 26))
        langTrack.wantsLayer = true
        langTrack.layer?.cornerRadius = 13
        langTrack.layer?.backgroundColor = NSColor(calibratedRed: 0.165, green: 0.18, blue: 0.23, alpha: 1).cgColor
        langTrack.layer?.borderWidth = 1
        langTrack.layer?.borderColor = NSColor(calibratedRed: 0.23, green: 0.25, blue: 0.32, alpha: 1).cgColor
        cv.addSubview(langTrack)
        langRu = label("RU", 9, true, .white); langRu.frame = NSRect(x: 8, y: 4, width: 24, height: 18); langTrack.addSubview(langRu)
        langEn = label("EN", 9, true, dim()); langEn.frame = NSRect(x: 42, y: 4, width: 24, height: 18); langTrack.addSubview(langEn)
        langKnob = NSView(frame: NSRect(x: 3, y: 3, width: 34, height: 20))
        langKnob.wantsLayer = true; langKnob.layer?.cornerRadius = 10
        langKnob.layer?.backgroundColor = NSColor(calibratedRed: 0.486, green: 0.36, blue: 1.0, alpha: 1).cgColor
        langTrack.addSubview(langKnob)
        let langClick = NSClickGestureRecognizer(target: self, action: #selector(toggleLanguage))
        langTrack.addGestureRecognizer(langClick)
        let svc = card(x: 26, y: 736, w: 848, h: 140, in: cv)
        btnOpenZapret = addBtn(svc, s("Открыть ZapretMac","Open ZapretMac"), 20, 22, 180, 36, #selector(openZapretMac), false)
        stateLabel = label(s("Активен","Active"), 16, true, .white); stateLabel.frame = NSRect(x: 20, y: 78, width: 420, height: 24); svc.addSubview(stateLabel)
        subLabel = label(s("Интеграция с ZapretMac / TgProxy","ZapretMac / TgProxy integration"), 10, false, dim()); subLabel.frame = NSRect(x: 20, y: 56, width: 520, height: 18); svc.addSubview(subLabel)
        btnRestart = addBtn(svc, s("Перезапустить","Restart"), 560, 86, 150, 32, #selector(openZapretMac), false)
        btnFolder = addBtn(svc, s("Папка","Folder"), 720, 86, 108, 32, #selector(openLists), false)
        let strat = card(x: 26, y: 628, w: 848, h: 96, in: cv)
        lblStrategy = label(s("СТРАТЕГИЯ","STRATEGY"), 8, true, dim()); lblStrategy.frame = NSRect(x: 20, y: 72, width: 240, height: 14); strat.addSubview(lblStrategy)
        strategyPopup = NSPopUpButton(frame: NSRect(x: 18, y: 30, width: 540, height: 30))
        strategyPopup.addItems(withTitles: ["(ZapretMac)"])
        strat.addSubview(strategyPopup)
        btnApply = addBtn(strat, s("Применить","Apply"), 572, 28, 150, 34, #selector(openZapretMac), true)
        let checks = card(x: 26, y: 390, w: 848, h: 226, in: cv)
        lblChecks = label(s("ПРОВЕРКА ДОСТУПА","ACCESS CHECK"), 8, true, dim()); lblChecks.frame = NSRect(x: 20, y: 202, width: 240, height: 14); checks.addSubview(lblChecks)
        let names = ["YouTube","Discord","Roblox","SoundCloud","Telegram"]
        for (i, name) in names.enumerated() {
            let y = CGFloat(160 - i*30)
            let dot = NSView(frame: NSRect(x: 24, y: y, width: 12, height: 12))
            dot.wantsLayer = true; dot.layer?.cornerRadius = 6; dot.layer?.backgroundColor = NSColor.gray.cgColor
            checks.addSubview(dot)
            let nl = label(name, 10, true, .white); nl.frame = NSRect(x: 44, y: y-2, width: 120, height: 16); checks.addSubview(nl)
            let sl = label(s("проверка...","checking..."), 9, false, dim()); sl.frame = NSRect(x: 220, y: y-2, width: 320, height: 16); checks.addSubview(sl)
            checkLabels[name] = (dot, sl)
        }
        btnCheck = addBtn(checks, s("Проверить","Check"), 24, 8, 120, 30, #selector(runChecks), false)
        let settings = card(x: 26, y: 242, w: 848, h: 136, in: cv)
        lblSettings = label(s("НАСТРОЙКИ","SETTINGS"), 8, true, dim()); lblSettings.frame = NSRect(x: 20, y: 112, width: 240, height: 14); settings.addSubview(lblSettings)
        let gf = label("Game Filter", 9, true, .white); gf.frame = NSRect(x: 24, y: 74, width: 90, height: 16); settings.addSubview(gf)
        settings.addSubview(popup(["disabled","all","tcp","udp"], 120, 70, 130))
        let ip = label("IPSet Filter", 9, true, .white); ip.frame = NSRect(x: 282, y: 74, width: 90, height: 16); settings.addSubview(ip)
        settings.addSubview(popup(["loaded","none","any"], 374, 70, 130))
        lblAppAuto = label(s("Автозапуск приложения","App autostart"), 9, true, .white); lblAppAuto.frame = NSRect(x: 430, y: 30, width: 180, height: 16); settings.addSubview(lblAppAuto)
        appAutoBtn = NSButton(checkboxWithTitle: "", target: self, action: #selector(toggleAppAutostart))
        appAutoBtn.frame = NSRect(x: 620, y: 26, width: 24, height: 20); settings.addSubview(appAutoBtn)
        btnSave = addBtn(settings, s("Сохранить","Save"), 24, 20, 150, 34, #selector(saveSettings), true)
        btnReplace = addBtn(settings, "Replace fakes", 186, 20, 150, 34, #selector(noop), false)
        let tg = card(x: 26, y: 116, w: 848, h: 114, in: cv)
        let tgTitle = label("TELEGRAM / TgProxy", 8, true, dim()); tgTitle.frame = NSRect(x: 20, y: 90, width: 240, height: 14); tg.addSubview(tgTitle)
        tgDot = NSView(frame: NSRect(x: 24, y: 62, width: 12, height: 12)); tgDot.wantsLayer = true; tgDot.layer?.cornerRadius = 6; tgDot.layer?.backgroundColor = NSColor.gray.cgColor; tg.addSubview(tgDot)
        tgStatusLabel = label(s("TgProxy выключен","TgProxy is off"), 10, true, .white); tgStatusLabel.frame = NSRect(x: 44, y: 56, width: 400, height: 20); tg.addSubview(tgStatusLabel)
        lblPort = label(s("Порт","Port"), 9, false, dim()); lblPort.frame = NSRect(x: 24, y: 26, width: 40, height: 18); tg.addSubview(lblPort)
        tgPortField = NSTextField(string: "\(tgPort())"); tgPortField.frame = NSRect(x: 62, y: 24, width: 70, height: 22); tg.addSubview(tgPortField)
        addBtn(tg, "OK", 138, 22, 44, 28, #selector(saveTgPort), false)
        btnTgSettings = addBtn(tg, s("Настройки","Settings"), 192, 22, 110, 28, #selector(openTgSettings), false)
        tgStartButton = addBtn(tg, s("Запустить","Start"), 480, 54, 130, 34, #selector(startTg), false)
        btnTgConnect = addBtn(tg, s("Подключить Telegram","Connect Telegram"), 620, 54, 160, 34, #selector(connectTelegram), true)
        btnTgStop = addBtn(tg, s("Стоп","Stop"), 790, 54, 48, 34, #selector(stopTg), false)
        lblTgAuto = label(s("Автозапуск TgProxy","TgProxy autostart"), 9, true, .white); lblTgAuto.frame = NSRect(x: 480, y: 24, width: 160, height: 18); tg.addSubview(lblTgAuto)
        tgAutoBtn = NSButton(checkboxWithTitle: "", target: self, action: #selector(toggleTgAutostart))
        tgAutoBtn.frame = NSRect(x: 640, y: 22, width: 24, height: 20); tg.addSubview(tgAutoBtn)
        lblLog = label(s("ЖУРНАЛ","LOG"), 8, true, dim()); lblLog.frame = NSRect(x: 30, y: 94, width: 120, height: 14); cv.addSubview(lblLog)
        let scroll = NSScrollView(frame: NSRect(x: 26, y: 12, width: 848, height: 78))
        scroll.hasVerticalScroller = true; scroll.borderType = .lineBorder
        scroll.wantsLayer = true; scroll.layer?.cornerRadius = 8
        logView = NSTextView(frame: scroll.bounds); logView.isEditable = false
        logView.backgroundColor = NSColor(calibratedRed: 0.05, green: 0.055, blue: 0.07, alpha: 1)
        logView.textColor = NSColor(calibratedRed: 0.66, green: 0.68, blue: 0.74, alpha: 1)
        logView.font = NSFont.monospacedSystemFont(ofSize: 11, weight: .regular)
        scroll.documentView = logView; cv.addSubview(scroll)
        window.makeKeyAndOrderFront(nil)
        NSApp.activate(ignoringOtherApps: true)
        log(s("Готово.", "Ready."))
    }
    private func dim() -> NSColor { NSColor(calibratedRed: 0.565, green: 0.588, blue: 0.66, alpha: 1) }
    private func card(x: CGFloat, y: CGFloat, w: CGFloat, h: CGFloat, in parent: NSView) -> NSView {
        let v = NSView(frame: NSRect(x: x, y: y, width: w, height: h))
        v.wantsLayer = true
        v.layer?.backgroundColor = NSColor(calibratedRed: 0.098, green: 0.11, blue: 0.15, alpha: 1).cgColor
        v.layer?.cornerRadius = 14
        v.layer?.borderWidth = 1
        v.layer?.borderColor = NSColor(calibratedRed: 0.173, green: 0.192, blue: 0.25, alpha: 1).cgColor
        parent.addSubview(v); return v
    }
    private func label(_ text: String, _ size: CGFloat, _ bold: Bool, _ color: NSColor) -> NSTextField {
        let l = NSTextField(labelWithString: text)
        l.font = bold ? NSFont.boldSystemFont(ofSize: size) : NSFont.systemFont(ofSize: size)
        l.textColor = color; return l
    }
    @discardableResult private func addBtn(_ p: NSView, _ t: String, _ x: CGFloat, _ y: CGFloat, _ w: CGFloat, _ h: CGFloat, _ sel: Selector, _ accent: Bool) -> NSButton {
        let b = NSButton(title: t, target: self, action: sel)
        b.frame = NSRect(x: x, y: y, width: w, height: h); b.bezelStyle = .rounded
        b.contentTintColor = accent ? NSColor(calibratedRed: 0.486, green: 0.36, blue: 1.0, alpha: 1) : nil
        p.addSubview(b); return b
    }
    private func popup(_ items: [String], _ x: CGFloat, _ y: CGFloat, _ w: CGFloat) -> NSPopUpButton {
        let p = NSPopUpButton(frame: NSRect(x: x, y: y, width: w, height: 26)); p.addItems(withTitles: items); return p
    }
    private func ensureTgConfig() {
        if !fm.fileExists(atPath: tgConf.path) { try? "{ \"port\": 1443, \"host\": \"127.0.0.1\", \"pool\": 4, \"buf_kb\": 256, \"log_max_mb\": 5 }\n".write(to: tgConf, atomically: true, encoding: .utf8) }
    }
    private func tgPort() -> Int {
        guard let d = try? Data(contentsOf: tgConf), let o = try? JSONSerialization.jsonObject(with: d) as? [String: Any], let p = o["port"] as? Int else { return 1443 }
        return p
    }
    private func portOpen(_ port: Int) -> Bool {
        var addr = sockaddr_in(); addr.sin_len = UInt8(MemoryLayout<sockaddr_in>.size); addr.sin_family = sa_family_t(AF_INET)
        addr.sin_port = in_port_t(UInt16(port).bigEndian); addr.sin_addr = in_addr(s_addr: inet_addr("127.0.0.1"))
        let sfd = socket(AF_INET, SOCK_STREAM, 0); if sfd < 0 { return false }
        var tv = timeval(tv_sec: 0, tv_usec: 300000); setsockopt(sfd, SOL_SOCKET, SO_SNDTIMEO, &tv, socklen_t(MemoryLayout<timeval>.size))
        let r = withUnsafePointer(to: &addr){ $0.withMemoryRebound(to: sockaddr.self, capacity: 1){ connect(sfd, $0, socklen_t(MemoryLayout<sockaddr_in>.size)) } }
        close(sfd); return r == 0
    }
    @discardableResult private func run(_ path: String, _ args: [String]) -> Int32 {
        let p = Process(); p.executableURL = URL(fileURLWithPath: path); p.arguments = args
        let pipe = Pipe(); p.standardOutput = pipe; p.standardError = pipe
        try? p.run(); p.waitUntilExit(); return p.terminationStatus
    }
    private func log(_ msg: String) {
        let ts = DateFormatter.localizedString(from: Date(), dateStyle: .none, timeStyle: .medium)
        DispatchQueue.main.async {
            self.logView.string += "\(ts)  \(msg)\n"
            self.logView.scrollToEndOfDocument(nil)
        }
    }
    private func refresh() {
        DispatchQueue.main.async {
            let ok = self.portOpen(self.tgPort())
            let onColor = NSColor(calibratedRed: 0.18, green: 0.8, blue: 0.44, alpha: 1)
            let offColor = NSColor(calibratedRed: 0.47, green: 0.49, blue: 0.53, alpha: 1)
            self.headerDot.layer?.backgroundColor = (ok ? onColor : offColor).cgColor
            self.tgDot.layer?.backgroundColor = (ok ? onColor : offColor).cgColor
            self.statusPill.stringValue = ok ? self.s("РАБОТАЕТ","RUNNING") : self.s("ВЫКЛЮЧЕН","STOPPED")
            self.statusPill.textColor = ok ? NSColor(calibratedRed: 0.494, green: 0.91, blue: 0.651, alpha: 1) : offColor
            self.stateLabel.stringValue = ok ? self.s("Активен","Active") : self.s("Выключен","Stopped")
            self.stateLabel.textColor = ok ? onColor : .white
            self.subLabel.stringValue = ok ? "TgProxy 127.0.0.1:\(self.tgPort())" : self.s("Интеграция с ZapretMac / TgProxy","ZapretMac / TgProxy integration")
            self.tgStatusLabel.stringValue = ok ? "TgProxy \(self.s("работает на","running on")) 127.0.0.1:\(self.tgPort())" : self.s("TgProxy выключен","TgProxy is off")
            self.tgStatusLabel.textColor = ok ? .white : self.dim()
            self.tgStartButton.title = self.fm.fileExists(atPath: self.tgBin.path) ? self.s("Запустить","Start") : self.s("Установить","Install")
            self.appAutoBtn.state = self.fm.fileExists(atPath: self.appPlist.path) ? .on : .off
            self.tgAutoBtn.state = self.fm.fileExists(atPath: self.tgPlist.path) ? .on : .off
        }
    }
    @objc private func toggleLanguage() {
        lang = (lang == "en") ? "ru" : "en"
        saveState(["lang": lang])
        animateLangToggle()
        applyLanguage()
    }
    private func animateLangToggle() {
        let en = (lang == "en")
        NSAnimationContext.runAnimationGroup { ctx in
            ctx.duration = 0.22
            ctx.timingFunction = CAMediaTimingFunction(name: .easeInEaseOut)
            self.langKnob.animator().frame.origin.x = en ? 37 : 3
        }
        langRu.textColor = en ? dim() : .white
        langEn.textColor = en ? .white : dim()
    }
    private func applyLanguage() {
        let en = (lang == "en")
        langKnob.frame.origin.x = en ? 37 : 3
        langRu.textColor = en ? dim() : .white
        langEn.textColor = en ? .white : dim()
        lblStrategy.stringValue = s("СТРАТЕГИЯ","STRATEGY")
        lblChecks.stringValue = s("ПРОВЕРКА ДОСТУПА","ACCESS CHECK")
        lblSettings.stringValue = s("НАСТРОЙКИ","SETTINGS")
        lblLog.stringValue = s("ЖУРНАЛ","LOG")
        lblAppAuto.stringValue = s("Автозапуск приложения","App autostart")
        lblTgAuto.stringValue = s("Автозапуск TgProxy","TgProxy autostart")
        lblPort.stringValue = s("Порт","Port")
        btnRestart.title = s("Перезапустить","Restart")
        btnFolder.title = s("Папка","Folder")
        btnApply.title = s("Применить","Apply")
        btnCheck.title = s("Проверить","Check")
        btnSave.title = s("Сохранить","Save")
        btnTgSettings.title = s("Настройки","Settings")
        btnTgConnect.title = s("Подключить Telegram","Connect Telegram")
        btnTgStop.title = s("Стоп","Stop")
        btnOpenZapret.title = s("Открыть ZapretMac","Open ZapretMac")
        for (name, pair) in checkLabels {
            let txt = pair.1.stringValue
            if txt == "проверка..." || txt == "checking..." { pair.1.stringValue = s("проверка...","checking...") }
            _ = name
        }
        refresh()
    }
    @objc private func runChecks() {
        let map = [("YouTube","youtube.com"),("Discord","discord.com"),("Roblox","roblox.com"),("SoundCloud","soundcloud.com"),("Telegram","telegram.org")]
        for (n, pair) in checkLabels { pair.0.layer?.backgroundColor = NSColor.systemYellow.cgColor; pair.1.stringValue = s("проверка...","checking..."); pair.1.textColor = dim() }
        DispatchQueue.global().async {
            for (n,h) in map {
                let t = Date()
                let ok = self.run("/sbin/ping", ["-c","1","-W","2000",h]) == 0
                let ms = Int(Date().timeIntervalSince(t)*1000)
                DispatchQueue.main.async {
                    if let (dot,sl) = self.checkLabels[n] {
                        dot.layer?.backgroundColor = (ok ? NSColor.systemGreen : NSColor.systemRed).cgColor
                        sl.stringValue = ok ? "\(self.s("доступно за","reachable in")) \(ms) ms" : self.s("недоступно","unreachable")
                        sl.textColor = ok ? NSColor(calibratedRed: 0.627, green: 0.824, blue: 0.706, alpha: 1) : NSColor(calibratedRed: 0.824, green: 0.627, blue: 0.627, alpha: 1)
                    }
                }
            }
        }
        log(s("Проверка выполнена.", "Checks completed."))
    }
    @objc private func startTg() {
        ensureTgConfig()
        if !fm.fileExists(atPath: tgBin.path) { installTg(); return }
        if portOpen(tgPort()) { log(s("TgProxy уже запущен.","TgProxy already running.")); refresh(); return }
        let p = Process(); p.executableURL = tgBin; p.currentDirectoryURL = tgDir
        if !fm.fileExists(atPath: tgLog.path) { fm.createFile(atPath: tgLog.path, contents: nil) }
        if let h = try? FileHandle(forWritingTo: tgLog) { h.seekToEndOfFile(); p.standardOutput = h; p.standardError = h }
        try? p.run(); log(s("Запуск TgProxy...","Starting TgProxy...")); DispatchQueue.main.asyncAfter(deadline: .now()+1){ self.refresh() }
    }
    @objc private func stopTg() { run("/usr/bin/pkill", ["-f", tgBin.path]); log(s("TgProxy остановлен.","TgProxy stopped.")); refresh() }
    @objc private func installTg() {
        log(s("Загрузка TgProxy...","Downloading TgProxy..."))
        DispatchQueue.global().async {
            let url = "https://github.com/Flowseal/tg-ws-proxy/releases/latest/download/TgWsProxy_macos_universal.dmg"
            let tmp = self.tgDir.appendingPathComponent("tg.dmg")
            let ok = self.run("/usr/bin/curl", ["-fL","--retry","3","-o",tmp.path,url]) == 0 && self.extractDmg(tmp)
            try? self.fm.removeItem(at: tmp)
            DispatchQueue.main.async { self.log(ok ? self.s("TgProxy установлен.","TgProxy installed.") : self.s("Ошибка установки TgProxy.","TgProxy install error.")); self.refresh() }
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
        log("\(s("Порт TgProxy","TgProxy port")): \(p)")
    }
    @objc private func openTgSettings() {
        ensureTgConfig()
        var cfg: [String: Any] = [:]
        if let data = try? Data(contentsOf: tgConf), let o = try? JSONSerialization.jsonObject(with: data) as? [String: Any] { cfg = o }
        let alert = NSAlert()
        alert.messageText = s("Настройки TgProxy", "TgProxy settings")
        alert.informativeText = s("Подключение и производительность", "Connection and performance")
        let acc = NSView(frame: NSRect(x: 0, y: 0, width: 320, height: 150))
        func row(_ y: CGFloat, _ title: String, _ key: String, _ def: Any) -> NSTextField {
            let l = NSTextField(labelWithString: title); l.frame = NSRect(x: 0, y: y, width: 140, height: 20); acc.addSubview(l)
            let v = "\(cfg[key] ?? def)"
            let f = NSTextField(string: v); f.frame = NSRect(x: 150, y: y-2, width: 140, height: 22); acc.addSubview(f); return f
        }
        let fPort = row(120, s("Порт","Port"), "port", 1443)
        let fHost = row(90, "Host", "host", "127.0.0.1")
        let fPool = row(60, s("Пул","Pool"), "pool", 4)
        let fBuf = row(30, s("Буфер (КБ)","Buffer (KB)"), "buf_kb", 256)
        let fLog = row(0, s("Лог (МБ)","Log (MB)"), "log_max_mb", 5)
        alert.accessoryView = acc
        alert.addButton(withTitle: s("Сохранить","Save"))
        alert.addButton(withTitle: s("Отмена","Cancel"))
        let resp = alert.runModal()
        if resp == .alertFirstButtonReturn {
            cfg["port"] = Int(fPort.stringValue) ?? 1443
            cfg["host"] = fHost.stringValue.isEmpty ? "127.0.0.1" : fHost.stringValue
            cfg["pool"] = Int(fPool.stringValue) ?? 4
            cfg["buf_kb"] = Int(fBuf.stringValue) ?? 256
            cfg["log_max_mb"] = Int(fLog.stringValue) ?? 5
            if let data = try? JSONSerialization.data(withJSONObject: cfg, options: [.prettyPrinted]) { try? data.write(to: tgConf) }
            log(s("Настройки TgProxy сохранены.","TgProxy settings saved."))
            if portOpen(tgPort()) { stopTg(); DispatchQueue.main.asyncAfter(deadline: .now()+0.6){ self.startTg() } }
        }
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
        if fm.fileExists(atPath: d.path) { NSWorkspace.shared.open(d) } else { log(s("Папка ZapretMac не найдена.","ZapretMac folder not found.")) }
    }
    @objc private func connectTelegram() {
        guard portOpen(tgPort()) else { log(s("Сначала запустите TgProxy.","Start TgProxy first.")); return }
        if let u = URL(string: "tg://proxy?server=127.0.0.1&port=\(tgPort())&type=socks5") { NSWorkspace.shared.open(u) }
    }
    @objc private func saveSettings() { log(s("Настройки сохранены. (Стратегия/IPSet — в ZapretMac)", "Settings saved. (Strategy/IPSet — in ZapretMac)")) }
    @objc private func noop() { log(s("Функция недоступна в этой версии.","Not available in this version.")) }
}
