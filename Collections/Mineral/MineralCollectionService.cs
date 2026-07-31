using StardewModdingAPI;
using StardewValley;
using StardewValley.GameData.Objects;
using StardewValley.Locations;
using System.Collections.Generic;

namespace StardewClaudeCompanion
{
    public class MineralCollectionService
    {
        private readonly IModHelper helper;

        public MineralCollectionService(IModHelper helper)
        {
            this.helper = helper;
        }

        public List<string> GetMissingMineralsReport()
        {
            var lines = new List<string>();

            var allObjects = this.helper.GameContent.Load<Dictionary<string, ObjectData>>("Data/Objects");

            var missing = new List<MineralRequirement>();

            foreach (var objEntry in allObjects)
            {
                var data = objEntry.Value;
                bool isGem = data.Category == StardewValley.Object.GemCategory;
                bool isMineral = data.Category == StardewValley.Object.mineralsCategory;
                if (!isGem && !isMineral) continue;

                // 矿物收藏页的判定标准和古器物一样，是博物馆捐赠状态，不是 Farmer.mineralsFound
                if (LibraryMuseum.HasDonatedArtifact("(O)" + objEntry.Key)) continue;

                string englishKey = GameDataHelper.GetEnglishKey(data.DisplayName);
                if (MineralNameData.ExcludedFromCollection.Contains(englishKey)) continue;

                missing.Add(new MineralRequirement
                {
                    EnglishKey = englishKey,
                    NameZh = MineralNameData.Names.ContainsKey(englishKey) ? MineralNameData.Names[englishKey] : englishKey,
                    IsGem = isGem
                });
            }

            if (missing.Count == 0)
            {
                lines.Add("恭喜，矿物收藏已全部集齐！ (Congratulations, completed all minerals!)");
                return lines;
            }

            lines.Add($"矿物收藏还差 {missing.Count} 种 (Missing {missing.Count} minerals):");

            foreach (var mineral in missing)
            {
                string category = mineral.IsGem ? "宝石 (Gem)" : "矿物 (Mineral)";
                lines.Add($"  {mineral.NameZh} ({mineral.EnglishKey}) [{category}]");
            }

            return lines;
        }
    }
}
