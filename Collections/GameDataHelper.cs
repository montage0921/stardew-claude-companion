using System.Collections.Generic;
using System.Text.RegularExpressions;
using StardewValley.GameData.Objects;

namespace StardewClaudeCompanion
{
    public static class GameDataHelper
    {
        public static string NormalizeItemId(string rawId) => rawId.Replace("(O)", "");

        public static string GetEnglishKey(string rawDisplayName)
        {
            var match = Regex.Match(rawDisplayName, @":(\w+)_Name");
            return match.Success ? match.Groups[1].Value : rawDisplayName;
        }

        public static string ResolveEnglishKey(string rawId, Dictionary<string, ObjectData> allObjects)
        {
            string id = NormalizeItemId(rawId);
            return allObjects.ContainsKey(id)
                ? GetEnglishKey(allObjects[id].DisplayName)
                : id;
        }

        // CraftingRecipe.recipeList 里的 key 除了具体物品 ID，还可能是负数的分类号（比如任意鱼类/任意蛋类）
        private static readonly Dictionary<string, string> CategoryNames = new()
        {
            { "-2", "任意宝石" },
            { "-4", "任意鱼类" },
            { "-5", "任意蛋类" },
            { "-6", "任意奶制品" },
            { "-7", "任意料理" },
            { "-12", "任意矿物" },
            { "-14", "任意肉类" },
            { "-26", "任意工匠制品" },
            { "-75", "任意蔬菜" },
            { "-79", "任意水果" },
            { "-80", "任意花卉" },
            { "-81", "任意野菜" },
            { "-777", "当季野生种子" },
        };

        // 解析配方食材名：可能是具体物品 ID，也可能是负数分类号
        public static string ResolveIngredientName(string idOrCategory, Dictionary<string, ObjectData> allObjects)
        {
            if (CategoryNames.ContainsKey(idOrCategory))
                return CategoryNames[idOrCategory];

            return allObjects.ContainsKey(idOrCategory)
                ? GetEnglishKey(allObjects[idOrCategory].DisplayName)
                : idOrCategory;
        }
    }
}
