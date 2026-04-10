import Cocoa

class Fish {
    /// Source fish pixel data (RGBA, row-major, bottom-to-top for CG)
    var pixelData: [UInt8] = []
    var pixelWidth: Int = 0
    var pixelHeight: Int = 0

    /// Scratch buffer for the wiggled fish (wider to accommodate tail movement)
    var wiggleBuffer: [UInt8] = []
    var wiggleWidth: Int = 0
    var wiggleHeight: Int = 0

    var x: CGFloat
    var y: CGFloat
    var direction: CGFloat
    var vx: CGFloat
    var vy: CGFloat
    var phase: CGFloat
    var amplitude: CGFloat
    var speed: CGFloat
    var width: CGFloat
    var height: CGFloat
    var artist: String
    var docId: String?
    var peduncle: CGFloat
    var score: Int

    var isDying: Bool = false
    var isEntering: Bool = false
    var opacity: CGFloat = 1.0
    var scale: CGFloat = 1.0
    var deathStartTime: TimeInterval = 0
    var enterStartTime: TimeInterval = 0
    var originalY: CGFloat = 0

    // Reusable CGContext for wiggle rendering
    private var wiggleContext: CGContext?
    private static let colorSpace = CGColorSpace(name: CGColorSpace.sRGB)!

    init(image: NSImage, canvasWidth: CGFloat, canvasHeight: CGFloat, fishWidth: CGFloat, fishHeight: CGFloat, artist: String = "Anonymous", docId: String? = nil, score: Int = 0) {
        self.width = fishWidth
        self.height = fishHeight
        self.artist = artist
        self.docId = docId
        self.score = score
        self.peduncle = 0.4
        self.phase = CGFloat.random(in: 0...(2 * .pi))
        self.amplitude = CGFloat.random(in: 20...32)
        self.speed = CGFloat.random(in: 1.5...2.5)
        self.direction = Bool.random() ? 1 : -1
        self.x = CGFloat.random(in: 0...(max(1, canvasWidth - fishWidth)))
        self.y = CGFloat.random(in: 0...(max(1, canvasHeight - fishHeight)))
        self.vx = speed * direction * 0.1
        self.vy = CGFloat.random(in: -0.25...0.25)

        self.pixelWidth = Int(fishWidth)
        self.pixelHeight = Int(fishHeight)

        // Extra width on each side for tail wiggle displacement
        let wiggleMargin = 14 // max wiggle is ~12px
        self.wiggleWidth = pixelWidth + wiggleMargin * 2
        self.wiggleHeight = pixelHeight

        extractPixelData(from: image)
        setupWiggleBuffer()
    }

    private func extractPixelData(from image: NSImage) {
        let w = pixelWidth, h = pixelHeight
        guard w > 0, h > 0 else { return }
        guard let srcCG = image.cgImage(forProposedRect: nil, context: nil, hints: nil) else { return }

        pixelData = [UInt8](repeating: 0, count: w * h * 4)
        guard let ctx = CGContext(data: &pixelData, width: w, height: h,
                                  bitsPerComponent: 8, bytesPerRow: w * 4,
                                  space: Fish.colorSpace,
                                  bitmapInfo: CGImageAlphaInfo.premultipliedLast.rawValue) else { return }

        ctx.interpolationQuality = .high
        let srcW = CGFloat(srcCG.width), srcH = CGFloat(srcCG.height)
        let scale = min(CGFloat(w) / max(srcW, 1), CGFloat(h) / max(srcH, 1))
        let drawW = srcW * scale, drawH = srcH * scale
        let dx = (CGFloat(w) - drawW) / 2, dy = (CGFloat(h) - drawH) / 2
        ctx.draw(srcCG, in: CGRect(x: dx, y: dy, width: drawW, height: drawH))
    }

    private func setupWiggleBuffer() {
        wiggleBuffer = [UInt8](repeating: 0, count: wiggleWidth * wiggleHeight * 4)

        // Create a reusable CGContext backed by our wiggle buffer
        wiggleContext = CGContext(data: &wiggleBuffer,
                                 width: wiggleWidth, height: wiggleHeight,
                                 bitsPerComponent: 8, bytesPerRow: wiggleWidth * 4,
                                 space: Fish.colorSpace,
                                 bitmapInfo: CGImageAlphaInfo.premultipliedLast.rawValue)
    }

    /// Build the wiggled fish frame by copying columns with offset directly in the pixel buffer
    private func buildWiggleFrame(time: Double) -> CGImage? {
        let w = pixelWidth, h = pixelHeight
        guard w > 0, h > 0, pixelData.count == w * h * 4 else { return nil }

        let tailEnd = Int(CGFloat(w) * peduncle)
        let margin = (wiggleWidth - w) / 2
        let timeFactor = CGFloat(time) * 3 + phase

        // Clear wiggle buffer
        wiggleBuffer.withUnsafeMutableBufferPointer { buf in
            buf.baseAddress?.initialize(repeating: 0, count: buf.count)
        }

        // Copy each column with wiggle offset
        for i in 0..<w {
            let t: CGFloat
            let wiggle: CGFloat
            let srcCol: Int
            let dstBaseX: Int

            if direction >= 0 {
                let isTail = i < tailEnd
                t = isTail ? CGFloat(tailEnd - i - 1) / max(CGFloat(tailEnd - 1), 1) : 0
                wiggle = isTail ? sin(timeFactor + t * 2) * t * 12 : 0
                srcCol = i
                dstBaseX = margin + i + Int(wiggle.rounded())
            } else {
                let isTail = i >= w - tailEnd
                t = isTail ? CGFloat(i - (w - tailEnd)) / max(CGFloat(tailEnd - 1), 1) : 0
                wiggle = isTail ? sin(timeFactor + t * 2) * t * 12 : 0
                srcCol = w - i - 1
                dstBaseX = margin + i - Int(wiggle.rounded())
            }

            guard dstBaseX >= 0, dstBaseX < wiggleWidth else { continue }

            // Copy the column pixel by pixel (4 bytes per pixel)
            for row in 0..<h {
                let srcIdx = (row * w + srcCol) * 4
                let dstIdx = (row * wiggleWidth + dstBaseX) * 4
                wiggleBuffer[dstIdx]     = pixelData[srcIdx]
                wiggleBuffer[dstIdx + 1] = pixelData[srcIdx + 1]
                wiggleBuffer[dstIdx + 2] = pixelData[srcIdx + 2]
                wiggleBuffer[dstIdx + 3] = pixelData[srcIdx + 3]
            }
        }

        return wiggleContext?.makeImage()
    }

    func updatePhysics(canvasWidth: CGFloat, canvasHeight: CGFloat) {
        guard !isDying, !isEntering else { return }

        vx += speed * direction * 0.1
        x += vx
        y += vy

        var hitEdge = false
        if x <= 0 {
            x = 0; direction = 1; vx = abs(vx); hitEdge = true
        } else if x >= canvasWidth - width {
            x = canvasWidth - width; direction = -1; vx = -abs(vx); hitEdge = true
        }
        if y <= 0 {
            y = 0; vy = abs(vy) * 0.5; hitEdge = true
        } else if y >= canvasHeight - height {
            y = canvasHeight - height; vy = -abs(vy) * 0.5; hitEdge = true
        }

        vx *= 0.85; vy *= 0.85

        let maxVel = speed * 2
        let velMag = sqrt(vx * vx + vy * vy)
        if velMag > maxVel {
            vx = (vx / velMag) * maxVel
            vy = (vy / velMag) * maxVel
        }
        if abs(vx) < 0.1 { vx = speed * direction * 0.1 }
        if hitEdge {
            vx += speed * direction * 0.2
            vy += CGFloat.random(in: -0.15...0.15)
        }
    }

    func updateEntrance() {
        guard isEntering else { return }
        let elapsed = CACurrentMediaTime() - enterStartTime
        let progress = min(elapsed / 1.0, 1.0)
        opacity = CGFloat(progress)
        scale = 0.3 + CGFloat(progress) * 0.7
        if progress >= 1.0 {
            isEntering = false; opacity = 1.0; scale = 1.0
        }
    }

    func updateDeath() {
        guard isDying else { return }
        let elapsed = CACurrentMediaTime() - deathStartTime
        let progress = min(elapsed / 2.0, 1.0)
        opacity = 1.0 - CGFloat(progress)
        y = originalY + CGFloat(progress * progress) * 200
    }

    var isDeathComplete: Bool {
        guard isDying else { return false }
        return CACurrentMediaTime() - deathStartTime >= 2.0
    }

    func draw(in ctx: CGContext, time: Double) {
        guard pixelWidth > 0, pixelHeight > 0 else { return }
        guard let frame = buildWiggleFrame(time: time) else { return }

        let swimY: CGFloat = isDying ? y : y + sin(CGFloat(time) + phase) * amplitude
        let margin = CGFloat((wiggleWidth - pixelWidth) / 2)

        ctx.saveGState()

        if opacity < 1.0 { ctx.setAlpha(opacity) }

        if isDying {
            ctx.translateBy(x: x - margin, y: swimY + CGFloat(pixelHeight))
            ctx.scaleBy(x: 1, y: -1)
        } else if isEntering && scale != 1 {
            let cx = x + width / 2
            let cy = swimY + height / 2
            ctx.translateBy(x: cx, y: cy)
            ctx.scaleBy(x: scale, y: scale)
            ctx.translateBy(x: -(width / 2 + margin), y: -height / 2)
        } else {
            ctx.translateBy(x: x - margin, y: swimY)
        }

        ctx.draw(frame, in: CGRect(x: 0, y: 0, width: wiggleWidth, height: wiggleHeight))
        ctx.restoreGState()
    }
}
