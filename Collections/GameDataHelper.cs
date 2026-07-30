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
    }
}
