using StardewModdingAPI;
using StardewValley;
using System.Collections.Generic;
using System.Linq;

namespace StardewClaudeCompanion
{
    public class AchievementCollectionService
    {
        private readonly IModHelper helper;

        public AchievementCollectionService(IModHelper helper)
        {
            this.helper = helper;
        }

        public List<ReportLine> GetMissingAchievementsReport()
        {
            var lines = new List<ReportLine>();

            // Data/Achievements 的 value 格式是 "名称^描述^..."，用 ^ 分隔，第一段是名称
            var allAchievements = this.helper.GameContent.Load<Dictionary<int, string>>("Data/Achievements");

            var missing = new List<(int Id, string Name)>();
            foreach (var entry in allAchievements)
            {
                if (Game1.player.achievements.Contains(entry.Key)) continue;

                string name = entry.Value.Split('^')[0];
                missing.Add((entry.Key, name));
            }

            if (missing.Count == 0)
            {
                lines.Add(ReportLine.Of("恭喜，所有成就都已解锁！"));
                return lines;
            }

            lines.Add(ReportLine.Of($"成就还差 {missing.Count} 个:"));

            foreach (var (id, name) in missing.OrderBy(m => m.Id))
            {
                string nameZh = AchievementNameData.Names.TryGetValue(id, out var zh) ? zh : name;
                lines.Add(ReportLine.Of($"  {nameZh} ({name})"));

                if (AchievementNameData.Conditions.TryGetValue(id, out var condition))
                    lines.Add(ReportLine.Of($"    解锁条件: {condition}"));
            }

            return lines;
        }
    }
}
