using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.Objects;
using StardewValley.GameData.Locations;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace StardewClaudeCompanion
{
    public class ModEntry : Mod
    {
        private static readonly HashSet<string> ExcludedFromCollection = new()
        {
            "SonofCrimsonfish", "MsAngler", "LegendII", "GlacierfishJr", "RadioactiveCarp",
        };

        private Config Config = null!;
        private ClaudeApiClient? claudeClient;

        public override void Entry(IModHelper helper)
        {
            this.Config = this.Helper.ReadConfig<Config>();
            this.claudeClient = new ClaudeApiClient(this.Config.AnthropicApiKey);
            this.Monitor.Log("Stardew Claude Companion loaded successfully!", LogLevel.Info);
            helper.Events.Input.ButtonPressed += this.OnButtonPressed;
        }

        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;

            if (e.Button == SButton.F5)
                this.PrintCaughtFish();
            else if (e.Button == SButton.F6)
                this.PrintMissingFishWithDetails();
            else if (e.Button == SButton.F8)
            {
                this.Monitor.Log("正在问 Claude...", LogLevel.Info);
                Task.Run(async () =>
                {
                    string testContext = "{\"caughtFishCount\": 69, \"currentSeason\": \"summer\", \"playerGold\": 50000}";
                    string answer = await this.claudeClient!.AskAsync("我现在钓鱼进度怎么样？", testContext);
                    this.Monitor.Log($"Claude 回答: {answer}", LogLevel.Info);
                });
            }
        }

        private string GetEnglishKey(string rawDisplayName)
        {
            var match = Regex.Match(rawDisplayName, @":(\w+)_Name");
            return match.Success ? match.Groups[1].Value : rawDisplayName;
        }

        private void PrintCaughtFish()
        {
            var allObjects = this.Helper.GameContent.Load<Dictionary<string, ObjectData>>("Data/Objects");
            var fishCaught = Game1.player.fishCaught;
            this.Monitor.Log($"你一共钓到过 {fishCaught.Count()} 种鱼:", LogLevel.Info);

            foreach (var entry in fishCaught.Pairs)
            {
                string rawId = entry.Key.Replace("(O)", "");
                string englishKey = allObjects.ContainsKey(rawId)
                    ? this.GetEnglishKey(allObjects[rawId].DisplayName)
                    : rawId;
                string nameZh = FishData.Names.ContainsKey(englishKey) ? FishData.Names[englishKey] : "?";
                this.Monitor.Log($"  {nameZh} / {englishKey} x{entry.Value[0]}", LogLevel.Info);
            }
        }

        private void PrintMissingFishWithDetails()
        {
            var allObjects = this.Helper.GameContent.Load<Dictionary<string, ObjectData>>("Data/Objects");
            var allFishData = this.Helper.GameContent.Load<Dictionary<string, string>>("Data/Fish");
            var allLocations = this.Helper.GameContent.Load<Dictionary<string, LocationData>>("Data/Locations");

            var caughtIds = Game1.player.fishCaught.Keys
                .Select(k => k.Replace("(O)", ""))
                .ToHashSet();

            var fishLocationMap = new Dictionary<string, (List<string> Locations, HashSet<string> Seasons)>();

            foreach (var locEntry in allLocations)
            {
                string locationName = locEntry.Key;
                var fishList = locEntry.Value.Fish;
                if (fishList == null) continue;

                foreach (var fishSpawn in fishList)
                {
                    string fishId = fishSpawn.ItemId?.Replace("(O)", "") ?? "";
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

                string englishKey = allObjects.ContainsKey(fishId)
                    ? this.GetEnglishKey(allObjects[fishId].DisplayName)
                    : fishId;

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
                this.Monitor.Log("恭喜，已完成全鱼类收集！", LogLevel.Info);
                return;
            }

            string currentSeason = Game1.currentSeason;
            bool isRaining = Game1.isRaining;
            int currentTime = Game1.timeOfDay;
            int playerFishingLevel = Game1.player.FishingLevel;

            this.Monitor.Log($"图鉴还差 {missing.Count} 种，当前: {currentSeason}季 / {(isRaining ? "下雨" : "晴天")} / {currentTime}点 / 钓鱼等级{playerFishingLevel}", LogLevel.Info);

            foreach (var fish in missing)
            {
                bool seasonOk = fish.Seasons.Count == 0 || fish.Seasons.Any(s => s.ToLower() == currentSeason.ToLower());
                bool weatherOk = fish.Weather == "both" || string.IsNullOrEmpty(fish.Weather) ||
                                 (fish.Weather == "rainy" && isRaining) ||
                                 (fish.Weather == "sunny" && !isRaining);
                bool levelOk = playerFishingLevel >= fish.MinFishingLevel;

                bool canCatchNow = seasonOk && weatherOk && levelOk;
                string status = canCatchNow ? "✅ 现在能钓" : "❌ 现在不行";

                this.Monitor.Log($"  {fish.NameZh} / {fish.EnglishKey}", LogLevel.Info);
                string locationStr = fish.Locations.Count > 0 ? string.Join(",", fish.Locations) : "特殊地点(矿洞/事件专属，待完善)";
                this.Monitor.Log($"    季节: {(fish.Seasons.Count > 0 ? string.Join(",", fish.Seasons) : "任意")} | 天气: {fish.Weather} | 需等级: {fish.MinFishingLevel} | 地点: {locationStr}", LogLevel.Info);
                this.Monitor.Log($"    {status}", LogLevel.Info);
            }
        }

        private string BuildGameSnapshot()
        {
            var allObjects = this.Helper.GameContent.Load<Dictionary<string, ObjectData>>("Data/Objects");
            var fishCaught = Game1.player.fishCaught;

            var caughtFishList = new List<object>();
            foreach (var entry in fishCaught.Pairs)
            {
                string rawId = entry.Key.Replace("(O)", "");
                string englishKey = allObjects.ContainsKey(rawId)
                    ? this.GetEnglishKey(allObjects[rawId].DisplayName)
                    : rawId;
                caughtFishList.Add(new
                {
                    name = englishKey,
                    count = entry.Value[0]
                });
            }

            var snapshot = new
            {
                playerName = Game1.player.Name,
                currentSeason = Game1.currentSeason,
                currentDay = Game1.dayOfMonth,
                currentYear = Game1.year,
                isRaining = Game1.isRaining,
                timeOfDay = Game1.timeOfDay,
                playerGold = Game1.player.Money,
                fishingLevel = Game1.player.FishingLevel,
                farmingLevel = Game1.player.FarmingLevel,
                miningLevel = Game1.player.MiningLevel,
                totalFishCaughtSpecies = fishCaught.Count(),
                caughtFish = caughtFishList
            };

            return System.Text.Json.JsonSerializer.Serialize(snapshot);
        }
    }
}