using StardewModdingAPI;
using StardewValley;
using StardewValley.GameData.Objects;
using StardewValley.Locations;
using System.Collections.Generic;
using System.Linq;

namespace StardewClaudeCompanion
{
    public class ArtifactCollectionService
    {
        private readonly IModHelper helper;
        private readonly IMonitor monitor;

        public ArtifactCollectionService(IModHelper helper, IMonitor monitor)
        {
            this.helper = helper;
            this.monitor = monitor;
        }

        public void PrintMissingArtifacts()
        {
            var allObjects = this.helper.GameContent.Load<Dictionary<string, ObjectData>>("Data/Objects");

            var missing = new List<ArtifactRequirement>();

            foreach (var objEntry in allObjects)
            {
                string qualifiedId = "(O)" + objEntry.Key;
                var tags = ItemContextTagManager.GetBaseContextTags(qualifiedId);
                if (!tags.Contains("item_type_arch")) continue;

                if (LibraryMuseum.HasDonatedArtifact(qualifiedId)) continue;

                string englishKey = GameDataHelper.GetEnglishKey(objEntry.Value.DisplayName);
                if (ArtifactNameData.ExcludedFromCollection.Contains(englishKey)) continue;

                missing.Add(new ArtifactRequirement
                {
                    EnglishKey = englishKey,
                    NameZh = ArtifactNameData.Names.ContainsKey(englishKey) ? ArtifactNameData.Names[englishKey] : englishKey,
                    DigSources = new Dictionary<string, float>(objEntry.Value.ArtifactSpotChances)
                });
            }

            if (missing.Count == 0)
            {
                this.monitor.Log("恭喜，博物馆古器物已全部捐赠！", LogLevel.Info);
                return;
            }

            this.monitor.Log($"博物馆古器物收藏还差 {missing.Count} 件:", LogLevel.Info);

            foreach (var artifact in missing)
            {
                this.monitor.Log($"  {artifact.NameZh} / {artifact.EnglishKey}", LogLevel.Info);

                if (artifact.DigSources.Count == 0)
                {
                    this.monitor.Log("    获得方式: 不是通过挖掘古器物地点获得(可能来自商店/怪物掉落/任务奖励，暂无数据)", LogLevel.Info);
                    continue;
                }

                foreach (var source in artifact.DigSources.OrderByDescending(s => s.Value))
                {
                    this.monitor.Log($"    可在 {source.Key} 挖掘古器物点获得，概率约 {source.Value * 100:0.#}%", LogLevel.Info);
                }
            }
        }
    }
}
