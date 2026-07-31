using StardewValley;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StardewClaudeCompanion
{
    // 把玩家背包 + 地图上所有箱子/冰箱/建筑储物的完整物品清单打包成文字，
    // 附加到发给 Claude 的上下文里，让它能准确回答"我有没有某个物品"，
    // 而不用靠自己猜测或瞎编。物品名用 DisplayName，跟随游戏当前语言设置。
    public static class InventorySnapshotBuilder
    {
        public static string Build()
        {
            var counts = new Dictionary<string, int>();

            void AddItem(Item? item)
            {
                if (item == null) return;
                string name = item.DisplayName;
                counts[name] = counts.TryGetValue(name, out int existing) ? existing + item.Stack : item.Stack;
            }

            foreach (var item in Game1.player.Items)
                AddItem(item);

            Utility.iterateChestsAndStorage(AddItem);

            if (counts.Count == 0)
                return "【玩家当前物品清单】\n(背包和储物都是空的)";

            var sb = new StringBuilder();
            sb.Append("【玩家当前物品清单(背包+所有箱子/冰箱/储物)】\n");
            foreach (var entry in counts.OrderByDescending(e => e.Value))
                sb.Append(entry.Key).Append(" x").Append(entry.Value).Append('\n');

            return sb.ToString();
        }
    }
}
