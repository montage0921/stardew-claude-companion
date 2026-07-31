using System.Collections.Generic;

namespace StardewClaudeCompanion
{
    public class CropRequirement
    {
        public string ItemId = "";
        public string EnglishKey = "";
        public string NameZh = "";
        public List<string> Seasons = new();
        public int DaysToGrow;
        public string SeedSource = "Pierre's General Store 种子店"; // 默认值
    }
}