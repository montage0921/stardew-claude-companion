using StardewModdingAPI;
using StardewValley;
using StardewValley.GameData.Objects;
using StardewValley.TerrainFeatures;
using System.Collections.Generic;
using System.Linq;

namespace StardewClaudeCompanion
{
    public static class GameSnapshotBuilder
    {
        public static string Build(IModHelper helper)
        {
            var allObjects = helper.GameContent.Load<Dictionary<string, ObjectData>>("Data/Objects");
            var fishCaught = Game1.player.fishCaught;

            var caughtFishList = new List<object>();
            foreach (var entry in fishCaught.Pairs)
            {
                string englishKey = GameDataHelper.ResolveEnglishKey(entry.Key, allObjects);
                caughtFishList.Add(new { name = englishKey, count = entry.Value[0] });
            }

            // 作物数据：遍历当前地图的所有地块
            var cropList = new List<object>();
            if (Game1.currentLocation != null)
            {
                foreach (var feature in Game1.currentLocation.terrainFeatures.Pairs)
                {
                    if (feature.Value is HoeDirt dirt && dirt.crop != null)
                    {
                        cropList.Add(new
                        {
                            cropId = dirt.crop.indexOfHarvest.Value,
                            daysGrown = dirt.crop.dayOfCurrentPhase.Value,
                            currentPhase = dirt.crop.currentPhase.Value,
                            isFullyGrown = dirt.crop.fullyGrown.Value,
                            needsWater = dirt.needsWatering() && !dirt.state.Value.Equals(1),
                            isWatered = dirt.state.Value == 1
                        });
                    }
                }
            }

            // 村民好感度数据
            var friendshipList = new List<object>();
            foreach (var entry in Game1.player.friendshipData.Pairs)
            {
                friendshipList.Add(new
                {
                    npcName = entry.Key,
                    heartLevel = entry.Value.Points / 250, // 每250点=1颗心
                    points = entry.Value.Points,
                    giftsThisWeek = entry.Value.GiftsThisWeek,
                    talkedToday = entry.Value.TalkedToToday
                });
            }

            var snapshot = new
            {
                playerName = Game1.player.Name,
                currentSeason = Game1.currentSeason,
                currentDay = Game1.dayOfMonth,
                currentYear = Game1.year,
                isRaining = Game1.isRaining,
                timeOfDay = Game1.timeOfDay,
                playerGold = Game1.player.Money,
                fishingLevel = Game1.player.FishingLevel,
                farmingLevel = Game1.player.FarmingLevel,
                miningLevel = Game1.player.MiningLevel,
                totalFishCaughtSpecies = fishCaught.Count(),
                caughtFish = caughtFishList,
                currentLocationName = Game1.currentLocation?.Name ?? "unknown",
                crops = cropList,
                villagerFriendships = friendshipList
            };

            return System.Text.Json.JsonSerializer.Serialize(snapshot);
        }
    }
}
