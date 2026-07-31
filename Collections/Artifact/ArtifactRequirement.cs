using System.Collections.Generic;

namespace StardewClaudeCompanion
{
    public class ArtifactRequirement
    {
        public string ItemId = "";
        public string EnglishKey = "";
        public string NameZh = "";

        // 地点名 -> 挖到概率，来自游戏自带的 ObjectData.ArtifactSpotChances
        public Dictionary<string, float> DigSources = new();
    }
}
