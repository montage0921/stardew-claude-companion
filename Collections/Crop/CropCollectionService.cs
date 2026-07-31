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

        public List<ReportLine> GetMissingCropsReport()
        {
            var lines = new List<ReportLine>();

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
                    ItemId = "(O)" + harvestId,
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
                lines.Add(ReportLine.Of("恭喜，已运输过所有作物！"));
                return lines;
            }

            string currentSeason = Game1.currentSeason;
            lines.Add(ReportLine.Of($"作物收集还差 {missing.Count} 种，当前季节: {currentSeason}"));

            foreach (var crop in missing)
            {
                bool seasonOk = crop.Seasons.Count == 0 || crop.Seasons.Any(s => s.ToLower() == currentSeason.ToLower());
                string status = seasonOk ? "【能种】现在能种" : "【不能】现在不是季节";
                string seasonsCn = crop.Seasons.Count > 0 ? string.Join(",", crop.Seasons) : "任意";

                lines.Add(ReportLine.EntryTitle($"{crop.NameZh} ({crop.EnglishKey})", crop.ItemId));
                lines.Add(ReportLine.Of($"    季节: {seasonsCn} | 生长天数: {crop.DaysToGrow}"));
                lines.Add(ReportLine.Of($"    {status}"));
            }

            return lines;
        }
    }
}
