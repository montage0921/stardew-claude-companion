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

        public ArtifactCollectionService(IModHelper helper)
        {
            this.helper = helper;
        }

        public List<ReportLine> GetMissingArtifactsReport()
        {
            var lines = new List<ReportLine>();

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
                    ItemId = qualifiedId,
                    EnglishKey = englishKey,
                    NameZh = ArtifactNameData.Names.ContainsKey(englishKey) ? ArtifactNameData.Names[englishKey] : englishKey,
                    DigSources = objEntry.Value.ArtifactSpotChances != null ? new Dictionary<string, float>(objEntry.Value.ArtifactSpotChances) : new Dictionary<string, float>()
                });
            }

            if (missing.Count == 0)
            {
                lines.Add(ReportLine.Of("恭喜，博物馆古器物已全部捐赠！"));
                return lines;
            }

            lines.Add(ReportLine.Of($"博物馆古器物收藏还差 {missing.Count} 件:"));

            foreach (var artifact in missing)
            {
                lines.Add(ReportLine.EntryTitle($"{artifact.NameZh} ({artifact.EnglishKey})", artifact.ItemId));

                if (artifact.DigSources.Count == 0)
                {
                    lines.Add(ReportLine.Of("    获得方式: 不是通过挖掘古器物地点获得"));
                    continue;
                }

                foreach (var source in artifact.DigSources.OrderByDescending(s => s.Value))
                {
                    lines.Add(ReportLine.Of($"    可在 {source.Key} 挖掘古器物点获得，概率约 {source.Value * 100:0.#}%"));
                }
            }

            return lines;
        }
    }
}
