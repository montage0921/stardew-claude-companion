using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.Objects;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace StardewClaudeCompanion
{
    public class ModEntry : Mod
    {
        // 这些鱼虽然存在于游戏数据里，但 Collections 图鉴不给它们留格子，
        // 不需要钓到就能达成"全收集"
        private static readonly HashSet<string> ExcludedFromCollection = new()
        {
            "SonofCrimsonfish",
            "MsAngler",
            "LegendII",
            "GlacierfishJr",
            "RadioactiveCarp",
        };

        public override void Entry(IModHelper helper)
        {
            this.Monitor.Log("Stardew Claude Companion loaded successfully!", LogLevel.Info);
            helper.Events.Input.ButtonPressed += this.OnButtonPressed;
        }

        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;

            if (e.Button == SButton.F5)
            {
                this.PrintCaughtFish();
            }
            else if (e.Button == SButton.F6)
            {
                this.PrintMissingFish();
            }
        }

        private string GetEnglishKey(string rawDisplayName)
        {
            var match = Regex.Match(rawDisplayName, @":(\w+)_Name");
            return match.Success ? match.Groups[1].Value : rawDisplayName;
        }

        private void PrintCaughtFish()
        {
            var allObjects = this.Helper.GameContent.Load<Dictionary<string, ObjectData>>("Data/Objects");
            var fishCaught = Game1.player.fishCaught;
            this.Monitor.Log($"你一共钓到过 {fishCaught.Count()} 种鱼:", LogLevel.Info);

            foreach (var entry in fishCaught.Pairs)
            {
                string rawId = entry.Key.Replace("(O)", "");
                string englishKey = allObjects.ContainsKey(rawId)
                    ? this.GetEnglishKey(allObjects[rawId].DisplayName)
                    : rawId;
                string nameZh = FishData.Names.ContainsKey(englishKey) ? FishData.Names[englishKey] : "?";
                this.Monitor.Log($"  {nameZh} / {englishKey} x{entry.Value[0]}", LogLevel.Info);
            }
        }

        private void PrintMissingFish()
        {
            var allFishData = this.Helper.GameContent.Load<Dictionary<string, string>>("Data/Fish");
            var allObjects = this.Helper.GameContent.Load<Dictionary<string, ObjectData>>("Data/Objects");

            var caughtIds = Game1.player.fishCaught.Keys
                .Select(k => k.Replace("(O)", ""))
                .ToHashSet();

            var missing = new List<(string EnglishKey, string NameZh)>();

            foreach (var fishId in allFishData.Keys)
            {
                if (caughtIds.Contains(fishId))
                    continue;

                string englishKey = allObjects.ContainsKey(fishId)
                    ? this.GetEnglishKey(allObjects[fishId].DisplayName)
                    : fishId;

                // 跳过不计入图鉴的特殊变体
                if (ExcludedFromCollection.Contains(englishKey))
                    continue;

                string nameZh = FishData.Names.ContainsKey(englishKey) ? FishData.Names[englishKey] : englishKey;
                missing.Add((englishKey, nameZh));
            }

            if (missing.Count == 0)
            {
                this.Monitor.Log("恭喜，已完成全鱼类收集！", LogLevel.Info);
            }
            else
            {
                this.Monitor.Log($"图鉴还差 {missing.Count} 种:", LogLevel.Info);
                foreach (var (englishKey, nameZh) in missing)
                {
                    this.Monitor.Log($"  {nameZh} / {englishKey}", LogLevel.Info);
                }
            }
        }
    }
}