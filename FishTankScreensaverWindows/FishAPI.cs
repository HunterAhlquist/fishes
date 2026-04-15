using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace FishTankScreensaver
{
    public class FishData
    {
        public string Id { get; set; } = "";
        public string? ImageURL { get; set; }
        public string ArtistName { get; set; } = "Anonymous";
        public int Score { get; set; }
    }

    public static class FishAPI
    {
        private static readonly string BackendURL = "https://fishes-be-571679687712.northamerica-northeast1.run.app";
        private static readonly HttpClient Http = new();

        public static string GetOrderBy(string sortType)
        {
            return sortType switch
            {
                "popular" => "score",
                _ => "CreatedAt"
            };
        }

        public static async Task<List<FishData>> FetchFishAsync(string sortType, int limit = 50)
        {
            var results = new List<FishData>();
            try
            {
                var orderBy = GetOrderBy(sortType);
                var url = $"{BackendURL}/api/fish?limit={limit}&order=desc&isVisible=true&deleted=false&orderBy={orderBy}";
                if (sortType == "random")
                    url += "&random=true";

                FishLog.Log($"FetchFish: requesting {url}");

                var response = await Http.GetStringAsync(url);
                FishLog.Log($"FetchFish: received {response.Length} chars");

                using var doc = JsonDocument.Parse(response);

                if (!doc.RootElement.TryGetProperty("data", out var dataArray))
                {
                    FishLog.Log("FetchFish: no 'data' property in response");
                    return results;
                }

                foreach (var item in dataArray.EnumerateArray())
                {
                    var id = item.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;
                    if (id == null) continue;

                    string? imageUrl = null;
                    if (item.TryGetProperty("Image", out var img))
                        imageUrl = img.GetString();
                    else if (item.TryGetProperty("image", out var img2))
                        imageUrl = img2.GetString();

                    string artist = "Anonymous";
                    if (item.TryGetProperty("Artist", out var art))
                        artist = art.GetString() ?? "Anonymous";
                    else if (item.TryGetProperty("artist", out var art2))
                        artist = art2.GetString() ?? "Anonymous";

                    int score = 0;
                    if (item.TryGetProperty("score", out var sc) && sc.ValueKind == JsonValueKind.Number)
                        score = sc.GetInt32();
                    else
                    {
                        int up = 0, down = 0;
                        if (item.TryGetProperty("upvotes", out var upProp) && upProp.ValueKind == JsonValueKind.Number)
                            up = upProp.GetInt32();
                        if (item.TryGetProperty("downvotes", out var downProp) && downProp.ValueKind == JsonValueKind.Number)
                            down = downProp.GetInt32();
                        score = up - down;
                    }

                    results.Add(new FishData
                    {
                        Id = id,
                        ImageURL = imageUrl,
                        ArtistName = artist,
                        Score = score
                    });
                }

                FishLog.Log($"FetchFish: parsed {results.Count} fish");
            }
            catch (Exception ex)
            {
                FishLog.Log($"FetchFish ERROR: {ex}");
            }
            return results;
        }

        public static async Task<byte[]?> LoadImageDataAsync(string url)
        {
            try
            {
                return await Http.GetByteArrayAsync(url);
            }
            catch (Exception ex)
            {
                FishLog.Log($"LoadImage ERROR ({url}): {ex.Message}");
                return null;
            }
        }
    }
}
