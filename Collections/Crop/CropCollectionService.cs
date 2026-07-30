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
        private readonly IMonitor monitor;

        public CropCollectionService(IModHelper helper, IMonitor monitor)
        {
            this.helper = helper;
            this.monitor = monitor;
        }

        public void PrintMissingCrops()
        {
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
                this.monitor.Log("恭喜，已运输过所有作物！", LogLevel.Info);
                return;
            }

            string currentSeason = Game1.currentSeason;
            this.monitor.Log($"作物收集还差 {missing.Count} 种，当前季节: {currentSeason}", LogLevel.Info);

            foreach (var crop in missing)
            {
                bool seasonOk = crop.Seasons.Count == 0 || crop.Seasons.Any(s => s.ToLower() == currentSeason.ToLower());
                string status = seasonOk ? "✅ 现在能种" : "❌ 现在不是季节";

                this.monitor.Log($"  {crop.NameZh} / {crop.EnglishKey}", LogLevel.Info);
                this.monitor.Log($"    季节: {(crop.Seasons.Count > 0 ? string.Join(",", crop.Seasons) : "任意")} | 生长天数: {crop.DaysToGrow}", LogLevel.Info);
                this.monitor.Log($"    {status}", LogLevel.Info);
            }
        }
    }
}
