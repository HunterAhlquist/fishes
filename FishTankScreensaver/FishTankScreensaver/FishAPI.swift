import Foundation
import Cocoa

struct FishData {
    let id: String
    let imageURL: String?
    let artistName: String
    let score: Int
}

class FishAPI {
    static let backendURL = "https://fishes-be-571679687712.northamerica-northeast1.run.app"

    enum SortType: String {
        case recent = "recent"
        case popular = "popular"
        case random = "random"

        var orderBy: String {
            switch self {
            case .recent: return "CreatedAt"
            case .popular: return "score"
            case .random: return "CreatedAt"
            }
        }
    }

    /// Parse fish array manually to handle mixed types in JSON
    private static func parseFishArray(from jsonData: Data) -> [FishData] {
        guard let json = try? JSONSerialization.jsonObject(with: jsonData) as? [String: Any],
              let dataArray = json["data"] as? [[String: Any]] else {
            return []
        }

        var results: [FishData] = []
        for item in dataArray {
            guard let id = item["id"] as? String else { continue }
            let imageURL = (item["Image"] as? String) ?? (item["image"] as? String)
            let artist = (item["Artist"] as? String) ?? (item["artist"] as? String) ?? "Anonymous"
            let upvotes = (item["upvotes"] as? Int) ?? 0
            let downvotes = (item["downvotes"] as? Int) ?? 0
            let score = (item["score"] as? Int) ?? (upvotes - downvotes)

            results.append(FishData(id: id, imageURL: imageURL, artistName: artist, score: score))
        }
        return results
    }

    static func fetchFish(sort: SortType, limit: Int = 50, completion: @escaping ([FishData]) -> Void) {
        var components = URLComponents(string: "\(backendURL)/api/fish")!
        var queryItems = [
            URLQueryItem(name: "limit", value: "\(limit)"),
            URLQueryItem(name: "order", value: "desc"),
            URLQueryItem(name: "isVisible", value: "true"),
            URLQueryItem(name: "deleted", value: "false"),
            URLQueryItem(name: "orderBy", value: sort.orderBy)
        ]
        if sort == .random {
            queryItems.append(URLQueryItem(name: "random", value: "true"))
        }
        components.queryItems = queryItems

        guard let url = components.url else {
            FishLog.log("fetchFish: invalid URL")
            DispatchQueue.main.async { completion([]) }
            return
        }

        FishLog.log("fetchFish: requesting \(url.absoluteString)")

        let task = URLSession.shared.dataTask(with: url) { data, response, error in
            if let error = error {
                FishLog.log("fetchFish: network error: \(error.localizedDescription)")
                DispatchQueue.main.async { completion([]) }
                return
            }
            guard let data = data else {
                FishLog.log("fetchFish: no data returned")
                DispatchQueue.main.async { completion([]) }
                return
            }
            if let httpResponse = response as? HTTPURLResponse {
                FishLog.log("fetchFish: HTTP \(httpResponse.statusCode), \(data.count) bytes")
            }

            let results = parseFishArray(from: data)
            FishLog.log("fetchFish: parsed \(results.count) fish")
            DispatchQueue.main.async { completion(results) }
        }
        task.resume()
    }

    /// Downloads image data on a background thread
    static func loadImageData(from urlString: String, completion: @escaping (Data?) -> Void) {
        guard let url = URL(string: urlString) else {
            completion(nil)
            return
        }
        let task = URLSession.shared.dataTask(with: url) { data, _, error in
            if let error = error {
                FishLog.log("loadImage: error: \(error.localizedDescription)")
            }
            completion(data)
        }
        task.resume()
    }
}

// Simple file-based logger for debugging screensaver issues
class FishLog {
    static let logPath = "/tmp/fishtank_screensaver.log"

    static func log(_ message: String) {
        let timestamp = ISO8601DateFormatter().string(from: Date())
        let line = "[\(timestamp)] \(message)\n"
        if let handle = FileHandle(forWritingAtPath: logPath) {
            handle.seekToEndOfFile()
            handle.write(line.data(using: .utf8)!)
            handle.closeFile()
        } else {
            FileManager.default.createFile(atPath: logPath, contents: line.data(using: .utf8))
        }
    }
}
