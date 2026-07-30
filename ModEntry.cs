using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.Objects;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace StardewClaudeCompanion
{
    public class ModEntry : Mod
    {
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
                var allObjects = this.Helper.GameContent.Load<Dictionary<string, ObjectData>>("Data/Objects");

                var fishCaught = Game1.player.fishCaught;
                this.Monitor.Log($"你一共钓到过 {fishCaught.Count()} 种鱼:", LogLevel.Info);

                foreach (var entry in fishCaught.Pairs)
                {
                    string rawId = entry.Key.Replace("(O)", "");
                    string englishKey = rawId; // 保底

                    if (allObjects.ContainsKey(rawId))
                    {
                        string rawDisplayName = allObjects[rawId].DisplayName;
                        // 格式: [LocalizedText Strings\Objects:Sunfish_Name]
                        // 用正则提取冒号后、_Name 前的部分
                        var match = Regex.Match(rawDisplayName, @":(\w+)_Name");
                        if (match.Success)
                        {
                            englishKey = match.Groups[1].Value; // 得到 "Sunfish"
                        }
                    }

                    string nameZh = FishData.Names.ContainsKey(englishKey)
                        ? FishData.Names[englishKey]
                        : "?";

                    this.Monitor.Log($"  {nameZh} / {englishKey} x{entry.Value[0]}", LogLevel.Info);
                }
            }
        }
    }
}