using StardewModdingAPI;
using StardewValley;
using StardewValley.GameData;
using StardewValley.TokenizableStrings;
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
            }
            else
            {
                lines.Add(ReportLine.Of($"成就还差 {missing.Count} 个:"));

                foreach (var (id, name) in missing.OrderBy(m => m.Id))
                {
                    string nameZh = AchievementNameData.Names.TryGetValue(id, out var zh) ? zh : name;
                    lines.Add(ReportLine.Of($"  {nameZh} ({name})"));

                    if (AchievementNameData.Conditions.TryGetValue(id, out var condition))
                        lines.Add(ReportLine.Of($"    解锁条件: {condition}"));
                }
            }

            this.AddMonsterEradicationReport(lines);

            return lines;
        }

        // "杀怪英雄"(Protector of the Valley)不在 Data/Achievements 里，也不会写进 Game1.player.achievements——
        // 反编译 Stats.checkForMonsterSlayerAchievement() 可见它只走 getSteamAchievement("Achievement_KeeperOfTheMysticRings"),
        // 属于平台成就。所以这里单独按 AdventureGuild.areAllMonsterSlayerQuestsComplete() 的逻辑重算进度。
        private void AddMonsterEradicationReport(List<ReportLine> lines)
        {
            var quests = this.helper.GameContent.Load<Dictionary<string, MonsterSlayerQuestData>>("Data/MonsterSlayerQuests");

            var incomplete = new List<(string Name, int Killed, int Need)>();
            foreach (var entry in quests)
            {
                var goal = entry.Value;
                if (goal.Targets == null) continue;

                int killed = goal.Targets.Sum(t => Game1.stats.getMonstersKilled(t));
                if (killed >= goal.Count) continue;

                // DisplayName 是指向 Strings\Locations 的 token，游戏界面是英文时只会解析出英文，
                // 所以再过一层自己的对照表转中文；表里没有的（比如模组新增目标）就保留原样。
                string raw = (goal.DisplayName != null ? TokenParser.ParseText(goal.DisplayName) : entry.Key)?.Trim() ?? entry.Key;

                string name = MonsterGoalNameData.Names.TryGetValue(raw, out var zh)
                    ? $"{zh} ({raw})"
                    : raw;
                incomplete.Add((name, killed, goal.Count));
            }

            lines.Add(ReportLine.Of(""));

            if (incomplete.Count == 0)
            {
                lines.Add(ReportLine.Of("杀怪英雄: 已完成所有消灭怪物的目标！"));
                return;
            }

            lines.Add(ReportLine.Of($"杀怪英雄 (Protector of the Valley) 还差 {incomplete.Count} 个目标:"));
            lines.Add(ReportLine.Of("  解锁条件: 完成探险家公会中的消灭怪物的目标"));

            foreach (var (name, killed, need) in incomplete.OrderBy(g => g.Name))
                lines.Add(ReportLine.Of($"    {name}: {killed}/{need}"));
        }
    }
}
