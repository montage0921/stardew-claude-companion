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
        private readonly IMonitor monitor;

        public MineralCollectionService(IModHelper helper, IMonitor monitor)
        {
            this.helper = helper;
            this.monitor = monitor;
        }

        public void PrintMissingMinerals()
        {
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
                this.monitor.Log("恭喜，矿物收藏已全部集齐！", LogLevel.Info);
                return;
            }

            this.monitor.Log($"矿物收藏还差 {missing.Count} 种:", LogLevel.Info);

            foreach (var mineral in missing)
            {
                string category = mineral.IsGem ? "宝石" : "矿物";
                this.monitor.Log($"  {mineral.NameZh} / {mineral.EnglishKey} [{category}]", LogLevel.Info);
            }
        }
    }
}
