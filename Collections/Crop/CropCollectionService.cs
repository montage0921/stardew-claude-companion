using StardewModdingAPI;
using StardewValley;
using StardewValley.GameData.Objects;
using StardewValley.GameData.Crops;
using System.Collections.Generic;
using System.Linq;

namespace StardewClaudeCompanion
{
    public class CropCollectionService
    {
        private readonly IModHelper helper;

        public CropCollectionService(IModHelper helper)
        {
            this.helper = helper;
        }

        public List<string> GetMissingCropsReport()
        {
            var lines = new List<string>();

            var allObjects = this.helper.GameContent.Load<Dictionary<string, ObjectData>>("Data/Objects");
            var allCrops = this.helper.GameContent.Load<Dictionary<string, CropData>>("Data/Crops");

            // basicShipped 的 key 是收获物的纯数字/字符串 ID（不带 (O) 前缀）
            var shippedIds = Game1.player.basicShipped.Keys.ToHashSet();

            var missing = new List<CropRequirement>();

            foreach (var cropEntry in allCrops)
            {
                var cropData = cropEntry.Value;
                string harvestId = cropData.HarvestItemId;

                if (string.IsNullOrEmpty(harvestId)) continue;
                if (shippedIds.Contains(harvestId)) continue;

                string englishKey = GameDataHelper.ResolveEnglishKey(harvestId, allObjects);

                if (CropNameData.ExcludedFromCollection.Contains(englishKey)) continue;

                int totalDays = cropData.DaysInPhase?.Sum() ?? 0;

                missing.Add(new CropRequirement
                {
                    EnglishKey = englishKey,
                    NameZh = CropNameData.Names.ContainsKey(englishKey) ? CropNameData.Names[englishKey] : englishKey,
                    Seasons = cropData.Seasons?.Select(s => s.ToString()).ToList() ?? new List<string>(),
                    DaysToGrow = totalDays,
                    SeedSource = CropNameData.SpecialSeedSources.ContainsKey(englishKey)
                        ? CropNameData.SpecialSeedSources[englishKey]
                        : "Pierre's General Store 种子店"
                });
            }

            if (missing.Count == 0)
            {
                lines.Add("恭喜，已运输过所有作物！ (Congratulations, shipped all crops!)");
                return lines;
            }

            string currentSeason = Game1.currentSeason;
            string seasonEn = currentSeason switch { "spring" => "Spring", "summer" => "Summer", "fall" => "Fall", "winter" => "Winter", _ => currentSeason };
            lines.Add($"作物收集还差 {missing.Count} 种 (Missing {missing.Count} crops) | 当前季节: {currentSeason}({seasonEn})");

            foreach (var crop in missing)
            {
                bool seasonOk = crop.Seasons.Count == 0 || crop.Seasons.Any(s => s.ToLower() == currentSeason.ToLower());
                string status = seasonOk ? "✅ 现在能种 (Can plant now)" : "❌ 现在不是季节 (Wrong season)";
                string seasonsCn = crop.Seasons.Count > 0 ? string.Join(",", crop.Seasons) : "任意 (Any)";

                lines.Add($"  {crop.NameZh} ({crop.EnglishKey})");
                lines.Add($"    季节/Seasons: {seasonsCn} | 生长天数/Growth days: {crop.DaysToGrow}");
                lines.Add($"    {status}");
            }

            return lines;
        }
    }
}
