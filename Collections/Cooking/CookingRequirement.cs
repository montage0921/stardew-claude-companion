namespace StardewClaudeCompanion
{
    public class CookingRequirement
    {
        public string ItemId = "";
        public string EnglishKey = "";
        public string NameZh = "";
        public bool IsKnown;
        public List<CookingIngredient> Ingredients = new();
    }

    public class CookingIngredient
    {
        public string Name = "";
        public int RequiredCount;
        public int OwnedCount;
        public int MissingCount;
    }
}
