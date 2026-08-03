using StardewModdingAPI;
using StardewValley;
using StardewValley.GameData.Objects;
using System.Collections.Generic;
using System.Linq;

namespace StardewClaudeCompanion
{
    // 对应成就"Full Shipment"(运输过每一样物品)的判定范围，复刻自 Utility.getFarmerItemsShippedPercent()
    // 和 Object.isPotentialBasicShipped() 的反编译逻辑：不止是作物，也包括野外采集物(Forage)等
    // 所有"可运输商品"类物品，但排除古器物/鱼类/矿物/料理(这些已经在各自的分类Tab里单独追踪)。
    public class FullShipmentCollectionService
    {
        // 复刻 Object.isPotentialBasicShipped() 里被排除的 Category 列表
        private static readonly HashSet<int> ExcludedCategories = new()
        {
            -999, // litterCategory
            -103, // skillBooksCategory
            -102, // booksCategory
            -96,  // ringCategory
            -74,  // SeedsCategory
            -29,  // equipmentCategory
            -24,  // furnitureCategory
            -22,  // tackleCategory
            -21,  // baitCategory
            -20,  // junkCategory
            -19,  // fertilizerCategory
            -14,  // meatCategory
            -12,  // mineralsCategory
            -8,   // CraftingCategory
            -7,   // CookingCategory
            -2,   // GemCategory
            0,
        };

        private readonly IModHelper helper;

        public FullShipmentCollectionService(IModHelper helper)
        {
            this.helper = helper;
        }

        public List<ReportLine> GetMissingShipmentReport()
        {
            var lines = new List<ReportLine>();

            var allObjects = this.helper.GameContent.Load<Dictionary<string, ObjectData>>("Data/Objects");
            var shipped = Game1.player.basicShipped;

            var missing = new List<(string Id, string EnglishKey, string NameZh)>();
            foreach (var entry in allObjects)
            {
                string itemId = entry.Key;
                var data = entry.Value;

                if (!IsPotentialBasicShipped(itemId, data)) continue;
                if (shipped.ContainsKey(itemId)) continue;

                string englishKey = GameDataHelper.GetEnglishKey(data.DisplayName);
                missing.Add((itemId, englishKey, FullShipmentNameData.Names.TryGetValue(englishKey, out var zh) ? zh : englishKey));
            }

            if (missing.Count == 0)
            {
                lines.Add(ReportLine.Of("恭喜，已运输过所有物品！"));
                return lines;
            }

            lines.Add(ReportLine.Of($"全部出货还差 {missing.Count} 种 (不含鱼类/矿物/古器物/料理，那些在各自的Tab里):"));

            foreach (var (id, englishKey, nameZh) in missing.OrderBy(m => m.NameZh))
                lines.Add(ReportLine.EntryTitle($"{nameZh} ({englishKey})", "(O)" + id));

            return lines;
        }

        // 复刻 StardewValley.Object.isPotentialBasicShipped(itemId, category, objectType) 的判定逻辑
        private static bool IsPotentialBasicShipped(string itemId, ObjectData data)
        {
            if (itemId == "433") return true; // 游戏原逻辑里硬编码的特例(金蛋)

            switch (data.Type)
            {
                case "Arch":
                case "Fish":
                case "Minerals":
                case "Cooking":
                    return false;
            }

            if (ExcludedCategories.Contains(data.Category)) return false;
            if (data.ExcludeFromShippingCollection) return false;

            return true;
        }
    }
}
