import Cocoa
import ScreenSaver

class ConfigureSheetController: NSObject, NSWindowDelegate {

    let window: NSWindow
    private weak var screenSaverView: FishTankView?

    private let colorWell: NSColorWell
    private let fishCountSlider: NSSlider
    private let fishCountLabel: NSTextField
    private let sortPopup: NSPopUpButton

    private var originalColor: NSColor = .white
    private var originalFishCount: Int = 50
    private var originalSortType: FishAPI.SortType = .recent

    init(screenSaverView: FishTankView) {
        self.screenSaverView = screenSaverView

        let contentRect = NSRect(x: 0, y: 0, width: 420, height: 240)
        window = NSWindow(contentRect: contentRect, styleMask: [.titled], backing: .buffered, defer: true)
        window.title = "Fish Tank Screensaver Options"

        let contentView = NSView(frame: contentRect)

        // Title
        let titleLabel = NSTextField(labelWithString: "Fish Tank Screensaver")
        titleLabel.font = NSFont.boldSystemFont(ofSize: 16)
        titleLabel.frame = NSRect(x: 20, y: 195, width: 380, height: 30)
        contentView.addSubview(titleLabel)

        // Background Color
        let colorLabel = NSTextField(labelWithString: "Background Color:")
        colorLabel.frame = NSRect(x: 20, y: 155, width: 150, height: 20)
        contentView.addSubview(colorLabel)

        colorWell = NSColorWell(frame: NSRect(x: 180, y: 151, width: 60, height: 28))
        colorWell.color = screenSaverView.currentBackgroundColor
        contentView.addSubview(colorWell)

        // Fish Count
        let countLabel = NSTextField(labelWithString: "Number of Fish:")
        countLabel.frame = NSRect(x: 20, y: 115, width: 150, height: 20)
        contentView.addSubview(countLabel)

        fishCountSlider = NSSlider(value: Double(screenSaverView.currentFishCount), minValue: 1, maxValue: 100, target: nil, action: nil)
        fishCountSlider.frame = NSRect(x: 180, y: 115, width: 170, height: 20)
        fishCountSlider.isContinuous = true
        contentView.addSubview(fishCountSlider)

        fishCountLabel = NSTextField(labelWithString: "\(screenSaverView.currentFishCount)")
        fishCountLabel.frame = NSRect(x: 358, y: 115, width: 40, height: 20)
        fishCountLabel.alignment = .right
        contentView.addSubview(fishCountLabel)

        // Sort Type
        let sortLabel = NSTextField(labelWithString: "Fish Sorting:")
        sortLabel.frame = NSRect(x: 20, y: 77, width: 150, height: 20)
        contentView.addSubview(sortLabel)

        sortPopup = NSPopUpButton(frame: NSRect(x: 180, y: 73, width: 200, height: 28))
        sortPopup.addItems(withTitles: ["Most Recent", "Most Popular", "Random"])
        switch screenSaverView.currentSortType {
        case .recent: sortPopup.selectItem(at: 0)
        case .popular: sortPopup.selectItem(at: 1)
        case .random: sortPopup.selectItem(at: 2)
        }
        contentView.addSubview(sortPopup)

        // Buttons
        let cancelButton = NSButton(title: "Cancel", target: nil, action: nil)
        cancelButton.frame = NSRect(x: 220, y: 20, width: 85, height: 32)
        cancelButton.bezelStyle = .rounded
        contentView.addSubview(cancelButton)

        let okButton = NSButton(title: "OK", target: nil, action: nil)
        okButton.frame = NSRect(x: 315, y: 20, width: 85, height: 32)
        okButton.bezelStyle = .rounded
        okButton.keyEquivalent = "\r"
        contentView.addSubview(okButton)

        window.contentView = contentView

        super.init()

        fishCountSlider.target = self
        fishCountSlider.action = #selector(fishCountSliderChanged(_:))
        okButton.target = self
        okButton.action = #selector(okPressed(_:))
        cancelButton.target = self
        cancelButton.action = #selector(cancelPressed(_:))
        window.delegate = self

        originalColor = screenSaverView.currentBackgroundColor
        originalFishCount = screenSaverView.currentFishCount
        originalSortType = screenSaverView.currentSortType
    }

    @objc private func fishCountSliderChanged(_ sender: NSSlider) {
        fishCountLabel.stringValue = "\(Int(sender.doubleValue))"
    }

    @objc private func okPressed(_ sender: Any) {
        guard let view = screenSaverView else { return }
        view.currentBackgroundColor = colorWell.color
        view.currentFishCount = Int(fishCountSlider.doubleValue)
        switch sortPopup.indexOfSelectedItem {
        case 0: view.currentSortType = .recent
        case 1: view.currentSortType = .popular
        case 2: view.currentSortType = .random
        default: break
        }
        view.savePreferences()
        closeSheet()
    }

    @objc private func cancelPressed(_ sender: Any) {
        guard let view = screenSaverView else { return }
        view.currentBackgroundColor = originalColor
        view.currentFishCount = originalFishCount
        view.currentSortType = originalSortType
        closeSheet()
    }

    private func closeSheet() {
        if let parent = window.sheetParent {
            parent.endSheet(window)
        } else {
            window.close()
        }
    }
}
