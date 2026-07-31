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

        public List<string> GetMissingArtifactsReport()
        {
            var lines = new List<string>();

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
                    DigSources = objEntry.Value.ArtifactSpotChances != null ? new Dictionary<string, float>(objEntry.Value.ArtifactSpotChances) : new Dictionary<string, float>()
                });
            }

            if (missing.Count == 0)
            {
                lines.Add("恭喜，博物馆古器物已全部捐赠！ (Congratulations, donated all artifacts!)");
                return lines;
            }

            lines.Add($"博物馆古器物收藏还差 {missing.Count} 件 (Missing {missing.Count} artifacts):");

            foreach (var artifact in missing)
            {
                lines.Add($"  {artifact.NameZh} ({artifact.EnglishKey})");

                if (artifact.DigSources.Count == 0)
                {
                    lines.Add("    获得方式 (Source): 不是通过挖掘古器物地点获得 (Not from digging artifact spots)");
                    continue;
                }

                foreach (var source in artifact.DigSources.OrderByDescending(s => s.Value))
                {
                    lines.Add($"    可在 {source.Key} 挖掘古器物点获得 (Can dig at {source.Key}), 概率约 {source.Value * 100:0.#}% (Chance ~{source.Value * 100:0.#}%)");
                }
            }

            return lines;
        }
    }
}
