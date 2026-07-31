using StardewModdingAPI;
using StardewValley;
using StardewValley.GameData.Objects;
using System.Collections.Generic;

namespace StardewClaudeCompanion
{
    public class CookingCollectionService
    {
        private readonly IModHelper helper;

        public CookingCollectionService(IModHelper helper)
        {
            this.helper = helper;
        }

        public List<string> GetMissingRecipesReport()
        {
            var lines = new List<string>();

            var allObjects = this.helper.GameContent.Load<Dictionary<string, ObjectData>>("Data/Objects");

            // 做菜能用背包 + 地图上所有箱子/冰箱/建筑储物，用游戏自带的遍历方法，覆盖面比自己查冰箱全面
            var storageItems = new List<Item>();
            Utility.iterateChestsAndStorage(item => storageItems.Add(item));

            var missing = new List<CookingRequirement>();

            foreach (var objEntry in allObjects)
            {
                var data = objEntry.Value;
                bool isCooking = data.Type == "Cooking" || data.Category == StardewValley.Object.CookingCategory;
                if (!isCooking) continue;

                if (CookingNameData.NonRecipeCookingItemIds.Contains(objEntry.Key)) continue;

                // Farmer.recipesCooked 才是"全收藏"的判定标准（做过没有），不是 cookingRecipes（那个只是学会了没有）
                if (Game1.player.recipesCooked.ContainsKey(objEntry.Key)) continue;

                string englishKey = GameDataHelper.GetEnglishKey(data.DisplayName);
                if (CookingNameData.ExcludedFromCollection.Contains(englishKey)) continue;

                string recipeNameKey = CookingNameData.RecipeNameRemap.ContainsKey(data.Name)
                    ? CookingNameData.RecipeNameRemap[data.Name]
                    : data.Name;
                bool isKnown = Game1.player.cookingRecipes.ContainsKey(recipeNameKey);

                var requirement = new CookingRequirement
                {
                    EnglishKey = englishKey,
                    NameZh = CookingNameData.Names.ContainsKey(englishKey) ? CookingNameData.Names[englishKey] : englishKey,
                    IsKnown = isKnown
                };

                var recipe = new CraftingRecipe(recipeNameKey, isCookingRecipe: true);
                foreach (var ingredient in recipe.recipeList)
                {
                    int owned = this.CountOwned(ingredient.Key, storageItems);
                    requirement.Ingredients.Add(new CookingIngredient
                    {
                        Name = GameDataHelper.ResolveIngredientName(ingredient.Key, allObjects),
                        RequiredCount = ingredient.Value,
                        OwnedCount = owned,
                        MissingCount = System.Math.Max(0, ingredient.Value - owned)
                    });
                }

                missing.Add(requirement);
            }

            if (missing.Count == 0)
            {
                lines.Add("恭喜，所有料理都做过了！ (Congratulations, cooked all recipes!)");
                return lines;
            }

            lines.Add($"料理收藏还差 {missing.Count} 道 (Missing {missing.Count} recipes):");

            foreach (var recipe in missing)
            {
                string status = recipe.IsKnown ? "✅ 已学会，还没做过 (Learned but not cooked)" : "❌ 还没学会配方 (Recipe unknown)";
                lines.Add($"  {recipe.NameZh} ({recipe.EnglishKey}) - {status}");

                if (recipe.Ingredients.Count == 0)
                {
                    lines.Add("    (无需材料 / No ingredients needed)");
                    continue;
                }

                foreach (var ingredient in recipe.Ingredients)
                {
                    string need = $"{ingredient.Name} x{ingredient.RequiredCount}";
                    string have = ingredient.MissingCount > 0
                        ? $"(现有 {ingredient.OwnedCount}，还缺 {ingredient.MissingCount} / Have {ingredient.OwnedCount}, need {ingredient.MissingCount} more)"
                        : "(已备齐 / Ready)";
                    lines.Add($"    需要 {need} {have}");
                }
            }

            return lines;
        }

        // 统计背包 + 所有箱子/储物里能匹配这个食材/分类号的数量，和游戏本身做菜时的判定口径一致
        private int CountOwned(string ingredientId, List<Item> storageItems)
        {
            int owned = 0;
            foreach (var item in Game1.player.Items)
            {
                if (item != null && CraftingRecipe.ItemMatchesForCrafting(item, ingredientId))
                    owned += item.Stack;
            }
            foreach (var item in storageItems)
            {
                if (item != null && CraftingRecipe.ItemMatchesForCrafting(item, ingredientId))
                    owned += item.Stack;
            }
            return owned;
        }
    }
}
