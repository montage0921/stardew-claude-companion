using StardewModdingAPI;
using StardewValley;
using StardewValley.GameData.Objects;
using StardewValley.GameData.Locations;
using System.Collections.Generic;
using System.Linq;

namespace StardewClaudeCompanion
{
    public class FishCollectionService
    {
        private static readonly HashSet<string> ExcludedFromCollection = new()
        {
            "SonofCrimsonfish", "MsAngler", "LegendII", "GlacierfishJr", "RadioactiveCarp",
        };

        private readonly IModHelper helper;

        public FishCollectionService(IModHelper helper)
        {
            this.helper = helper;
        }

        public List<string> GetMissingFishReport()
        {
            var lines = new List<string>();

            var allObjects = this.helper.GameContent.Load<Dictionary<string, ObjectData>>("Data/Objects");
            var allFishData = this.helper.GameContent.Load<Dictionary<string, string>>("Data/Fish");
            var allLocations = this.helper.GameContent.Load<Dictionary<string, LocationData>>("Data/Locations");

            var caughtIds = Game1.player.fishCaught.Keys
                .Select(GameDataHelper.NormalizeItemId)
                .ToHashSet();

            var fishLocationMap = new Dictionary<string, (List<string> Locations, HashSet<string> Seasons)>();

            foreach (var locEntry in allLocations)
            {
                string locationName = locEntry.Key;
                var fishList = locEntry.Value.Fish;
                if (fishList == null) continue;

                foreach (var fishSpawn in fishList)
                {
                    string fishId = fishSpawn.ItemId != null ? GameDataHelper.NormalizeItemId(fishSpawn.ItemId) : "";
                    if (string.IsNullOrEmpty(fishId)) continue;

                    if (!fishLocationMap.ContainsKey(fishId))
                        fishLocationMap[fishId] = (new List<string>(), new HashSet<string>());

                    if (!fishLocationMap[fishId].Locations.Contains(locationName))
                        fishLocationMap[fishId].Locations.Add(locationName);

                    if (fishSpawn.Season.HasValue)
                        fishLocationMap[fishId].Seasons.Add(fishSpawn.Season.Value.ToString());
                }
            }

            var missing = new List<FishRequirement>();

            foreach (var fishEntry in allFishData)
            {
                string fishId = fishEntry.Key;
                if (caughtIds.Contains(fishId)) continue;

                string englishKey = GameDataHelper.ResolveEnglishKey(fishId, allObjects);

                if (ExcludedFromCollection.Contains(englishKey)) continue;

                var fields = fishEntry.Value.Split('/');
                var req = new FishRequirement
                {
                    EnglishKey = englishKey,
                    NameZh = FishData.Names.ContainsKey(englishKey) ? FishData.Names[englishKey] : englishKey
                };

                bool isTrap = fields.Length > 1 && fields[1] == "trap";

                if (!isTrap && fields.Length > 13)
                {
                    req.TimeRange = fields[5];
                    req.Weather = fields[7];
                    int.TryParse(fields[12], out req.MinFishingLevel);
                }

                if (fishLocationMap.ContainsKey(fishId))
                {
                    req.Locations = fishLocationMap[fishId].Locations;
                    req.Seasons = fishLocationMap[fishId].Seasons.ToList();
                }

                missing.Add(req);
            }

            if (missing.Count == 0)
            {
                lines.Add("恭喜，已完成全鱼类收集！ (Congratulations, completed all fish collection!)");
                return lines;
            }

            string currentSeason = Game1.currentSeason;
            bool isRaining = Game1.isRaining;
            int currentTime = Game1.timeOfDay;
            int playerFishingLevel = Game1.player.FishingLevel;

            string seasonEn = currentSeason switch { "spring" => "Spring", "summer" => "Summer", "fall" => "Fall", "winter" => "Winter", _ => currentSeason };
            string weatherCn = isRaining ? "下雨" : "晴天";
            string weatherEn = isRaining ? "Rainy" : "Sunny";
            lines.Add($"图鉴还差 {missing.Count} 种 (Missing {missing.Count} fish) | 当前: {currentSeason}({seasonEn})季 {weatherCn}({weatherEn}) {currentTime}点 钓鱼等级{playerFishingLevel}(Level {playerFishingLevel})");

            foreach (var fish in missing)
            {
                bool seasonOk = fish.Seasons.Count == 0 || fish.Seasons.Any(s => s.ToLower() == currentSeason.ToLower());
                bool weatherOk = fish.Weather == "both" || string.IsNullOrEmpty(fish.Weather) ||
                                 (fish.Weather == "rainy" && isRaining) ||
                                 (fish.Weather == "sunny" && !isRaining);
                bool levelOk = playerFishingLevel >= fish.MinFishingLevel;

                bool canCatchNow = seasonOk && weatherOk && levelOk;
                string status = canCatchNow ? "✅ 现在能钓 (Can catch now)" : "❌ 现在不行 (Cannot catch now)";

                lines.Add($"  {fish.NameZh} ({fish.EnglishKey})");
                string locationStr = fish.Locations.Count > 0 ? string.Join(",", fish.Locations) : "特殊地点 (Special location)";
                string seasonsStr = fish.Seasons.Count > 0 ? string.Join(",", fish.Seasons) : "任意 (Any)";
                string weatherStr = fish.Weather == "both" || string.IsNullOrEmpty(fish.Weather) ? "任意 (Both)" :
                                    fish.Weather == "rainy" ? "下雨 (Rainy)" : "晴天 (Sunny)";
                lines.Add($"    季节/Seasons: {seasonsStr} | 天气/Weather: {weatherStr} | 需等级/Level: {fish.MinFishingLevel} | 地点/Location: {locationStr}");
                lines.Add($"    {status}");
            }

            return lines;
        }
    }
}
