using System.Collections.Generic;

namespace StardewClaudeCompanion
{
    public class FishRequirement
    {
        public string ItemId = "";
        public string EnglishKey = "";
        public string NameZh = "";
        public string TimeRange = "";
        public string Weather = "";
        public int MinFishingLevel;
        public List<string> Seasons = new();
        public List<string> Locations = new();
    }
}