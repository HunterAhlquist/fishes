import ScreenSaver
import Cocoa

class FishTankView: ScreenSaverView {

    // MARK: - Properties

    private var fishes: [Fish] = []
    private var isLoading = false
    private var hasStartedLoading = false

    // Preferences
    private var backgroundColor: NSColor = NSColor(red: 0.878, green: 0.969, blue: 0.98, alpha: 1.0) // #e0f7fa
    private var fishCount: Int = 50
    private var sortType: FishAPI.SortType = .recent

    // Preference keys
    private static let bgColorRedKey = "BGColorRed"
    private static let bgColorGreenKey = "BGColorGreen"
    private static let bgColorBlueKey = "BGColorBlue"
    private static let fishCountKey = "FishCount"
    private static let sortTypeKey = "SortType"

    private lazy var defaults: ScreenSaverDefaults? = {
        ScreenSaverDefaults(forModuleWithName: Bundle(for: FishTankView.self).bundleIdentifier!)
    }()

    private var configureSheetController: ConfigureSheetController?

    // MARK: - Initialization

    override init?(frame: NSRect, isPreview: Bool) {
        super.init(frame: frame, isPreview: isPreview)
        commonInit()
    }

    required init?(coder: NSCoder) {
        super.init(coder: coder)
        commonInit()
    }

    private func commonInit() {
        animationTimeInterval = 1.0 / 60.0
        loadPreferences()
    }

    // MARK: - Preferences

    private func loadPreferences() {
        guard let defaults = defaults else { return }
        defaults.register(defaults: [
            FishTankView.bgColorRedKey: 0.878,
            FishTankView.bgColorGreenKey: 0.969,
            FishTankView.bgColorBlueKey: 0.98,
            FishTankView.fishCountKey: 50,
            FishTankView.sortTypeKey: "recent"
        ])

        let r = CGFloat(defaults.double(forKey: FishTankView.bgColorRedKey))
        let g = CGFloat(defaults.double(forKey: FishTankView.bgColorGreenKey))
        let b = CGFloat(defaults.double(forKey: FishTankView.bgColorBlueKey))
        backgroundColor = NSColor(red: r, green: g, blue: b, alpha: 1.0)
        fishCount = max(1, min(100, defaults.integer(forKey: FishTankView.fishCountKey)))

        let sortStr = defaults.string(forKey: FishTankView.sortTypeKey) ?? "recent"
        sortType = FishAPI.SortType(rawValue: sortStr) ?? .recent
    }

    func savePreferences() {
        guard let defaults = defaults else { return }
        var r: CGFloat = 0, g: CGFloat = 0, b: CGFloat = 0, a: CGFloat = 0
        backgroundColor.usingColorSpace(.sRGB)?.getRed(&r, green: &g, blue: &b, alpha: &a)
        defaults.set(Double(r), forKey: FishTankView.bgColorRedKey)
        defaults.set(Double(g), forKey: FishTankView.bgColorGreenKey)
        defaults.set(Double(b), forKey: FishTankView.bgColorBlueKey)
        defaults.set(fishCount, forKey: FishTankView.fishCountKey)
        defaults.set(sortType.rawValue, forKey: FishTankView.sortTypeKey)
        defaults.synchronize()
    }

    // MARK: - Configure Sheet

    override var hasConfigureSheet: Bool { true }

    override var configureSheet: NSWindow? {
        if configureSheetController == nil {
            configureSheetController = ConfigureSheetController(screenSaverView: self)
        }
        return configureSheetController?.window
    }

    var currentBackgroundColor: NSColor {
        get { backgroundColor }
        set { backgroundColor = newValue; savePreferences() }
    }

    var currentFishCount: Int {
        get { fishCount }
        set {
            let old = fishCount
            fishCount = max(1, min(100, newValue))
            savePreferences()
            if fishCount != old { adjustFishCount() }
        }
    }

    var currentSortType: FishAPI.SortType {
        get { sortType }
        set { sortType = newValue; savePreferences(); reloadFish() }
    }

    // MARK: - Fish Loading

    private func loadFishFromAPI() {
        guard !isLoading else { return }
        isLoading = true

        FishAPI.fetchFish(sort: sortType, limit: fishCount) { [weak self] fishDataList in
            guard let self = self else { return }

            let validFish = fishDataList.filter { fd in
                guard let url = fd.imageURL, url.hasPrefix("http") else { return false }
                return true
            }

            if validFish.isEmpty {
                self.isLoading = false
                return
            }

            var imageDataMap: [(FishData, Data)] = []
            let lock = NSLock()
            let group = DispatchGroup()

            for fishData in validFish {
                group.enter()
                FishAPI.loadImageData(from: fishData.imageURL!) { data in
                    if let data = data {
                        lock.lock()
                        imageDataMap.append((fishData, data))
                        lock.unlock()
                    }
                    group.leave()
                }
            }

            group.notify(queue: .main) { [weak self] in
                guard let self = self else { return }

                let fishSize = self.calculateFishSize()
                let cw = self.bounds.width
                let ch = self.bounds.height

                var newFishes: [Fish] = []
                for (fishData, data) in imageDataMap {
                    guard let nsImage = NSImage(data: data) else { continue }
                    let fish = Fish(
                        image: nsImage,
                        canvasWidth: cw, canvasHeight: ch,
                        fishWidth: fishSize.width, fishHeight: fishSize.height,
                        artist: fishData.artistName, docId: fishData.id, score: fishData.score
                    )
                    newFishes.append(fish)
                }

                self.fishes = newFishes
                self.isLoading = false
            }
        }
    }

    private func reloadFish() {
        fishes.removeAll()
        isLoading = false
        hasStartedLoading = false
    }

    private func adjustFishCount() {
        if fishCount < fishes.count {
            fishes.removeLast(fishes.count - fishCount)
        } else if fishCount > fishes.count {
            isLoading = false
            loadFishFromAPI()
        }
    }

    private func calculateFishSize() -> NSSize {
        let baseDimension = min(bounds.width, bounds.height)
        let fishWidth = floor(baseDimension * 0.1)
        let fishHeight = floor(fishWidth * 0.6)
        return NSSize(width: max(30, min(150, fishWidth)), height: max(18, min(90, fishHeight)))
    }

    // MARK: - Animation

    override func startAnimation() { super.startAnimation() }
    override func stopAnimation() { super.stopAnimation() }

    override func animateOneFrame() {
        if !hasStartedLoading && bounds.width > 0 && bounds.height > 0 {
            hasStartedLoading = true
            loadFishFromAPI()
        }

        for fish in fishes {
            fish.updatePhysics(canvasWidth: bounds.width, canvasHeight: bounds.height)
            fish.updateEntrance()
            fish.updateDeath()
        }
        fishes.removeAll { $0.isDeathComplete }
        setNeedsDisplay(bounds)
    }

    override func draw(_ rect: NSRect) {
        guard let ctx = NSGraphicsContext.current?.cgContext else { return }
        let time = CACurrentMediaTime() / 0.5

        ctx.setFillColor(backgroundColor.cgColor)
        ctx.fill(bounds)

        if fishes.isEmpty {
            let text = (isLoading ? "Loading fish..." : "No fish available.") as NSString
            let attrs: [NSAttributedString.Key: Any] = [
                .font: NSFont.systemFont(ofSize: 24, weight: .light),
                .foregroundColor: NSColor.gray
            ]
            let size = text.size(withAttributes: attrs)
            let point = NSPoint(x: (bounds.width - size.width) / 2, y: (bounds.height - size.height) / 2)
            NSGraphicsContext.saveGraphicsState()
            NSGraphicsContext.current = NSGraphicsContext(cgContext: ctx, flipped: false)
            text.draw(at: point, withAttributes: attrs)
            NSGraphicsContext.restoreGraphicsState()
            return
        }

        for fish in fishes {
            fish.draw(in: ctx, time: time)
        }
    }
}
