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
        private readonly IMonitor monitor;

        public FishCollectionService(IModHelper helper, IMonitor monitor)
        {
            this.helper = helper;
            this.monitor = monitor;
        }

        public void PrintCaughtFish()
        {
            var allObjects = this.helper.GameContent.Load<Dictionary<string, ObjectData>>("Data/Objects");
            var fishCaught = Game1.player.fishCaught;
            this.monitor.Log($"你一共钓到过 {fishCaught.Count()} 种鱼:", LogLevel.Info);

            foreach (var entry in fishCaught.Pairs)
            {
                string englishKey = GameDataHelper.ResolveEnglishKey(entry.Key, allObjects);
                string nameZh = FishData.Names.ContainsKey(englishKey) ? FishData.Names[englishKey] : "?";
                this.monitor.Log($"  {nameZh} / {englishKey} x{entry.Value[0]}", LogLevel.Info);
            }
        }

        public void PrintMissingFishWithDetails()
        {
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
                this.monitor.Log("恭喜，已完成全鱼类收集！", LogLevel.Info);
                return;
            }

            string currentSeason = Game1.currentSeason;
            bool isRaining = Game1.isRaining;
            int currentTime = Game1.timeOfDay;
            int playerFishingLevel = Game1.player.FishingLevel;

            this.monitor.Log($"图鉴还差 {missing.Count} 种，当前: {currentSeason}季 / {(isRaining ? "下雨" : "晴天")} / {currentTime}点 / 钓鱼等级{playerFishingLevel}", LogLevel.Info);

            foreach (var fish in missing)
            {
                bool seasonOk = fish.Seasons.Count == 0 || fish.Seasons.Any(s => s.ToLower() == currentSeason.ToLower());
                bool weatherOk = fish.Weather == "both" || string.IsNullOrEmpty(fish.Weather) ||
                                 (fish.Weather == "rainy" && isRaining) ||
                                 (fish.Weather == "sunny" && !isRaining);
                bool levelOk = playerFishingLevel >= fish.MinFishingLevel;

                bool canCatchNow = seasonOk && weatherOk && levelOk;
                string status = canCatchNow ? "✅ 现在能钓" : "❌ 现在不行";

                this.monitor.Log($"  {fish.NameZh} / {fish.EnglishKey}", LogLevel.Info);
                string locationStr = fish.Locations.Count > 0 ? string.Join(",", fish.Locations) : "特殊地点(矿洞/事件专属，待完善)";
                this.monitor.Log($"    季节: {(fish.Seasons.Count > 0 ? string.Join(",", fish.Seasons) : "任意")} | 天气: {fish.Weather} | 需等级: {fish.MinFishingLevel} | 地点: {locationStr}", LogLevel.Info);
                this.monitor.Log($"    {status}", LogLevel.Info);
            }
        }
    }
}
