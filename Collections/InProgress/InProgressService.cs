using StardewValley;
using StardewValley.TerrainFeatures;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StardewClaudeCompanion
{
    // 汇总所有"需要等待时间"的东西：地里正在生长的作物、机器里正在加工的物品。
    // 遍历所有地点（不只是玩家当前所在的那个），这样不用站在农场也能查询。
    public class InProgressService
    {
        public List<ReportLine> GetInProgressReport()
        {
            var lines = new List<ReportLine>();

            var crops = this.CollectGrowingCrops();
            var machines = this.CollectProcessingMachines();

            if (crops.Count == 0 && machines.Count == 0)
            {
                lines.Add(ReportLine.Of("目前没有正在生长的作物，也没有正在加工的机器。"));
                return lines;
            }

            if (crops.Count > 0)
            {
                // 同一块地点里、同一种作物、可摘状态、剩余天数相同的合并成一行，不然种一整片地全部单独列出会很长
                var groups = crops
                    .GroupBy(c => (c.ItemId, c.LocationName, c.HasHarvestableFruit, c.DaysRemaining))
                    .OrderBy(g => g.Key.HasHarvestableFruit ? 0 : 1)
                    .ThenBy(g => g.Key.DaysRemaining);

                lines.Add(ReportLine.Of($"正在生长的作物 (共 {crops.Count} 株):"));
                foreach (var group in groups)
                {
                    var first = group.First();
                    int count = group.Count();
                    lines.Add(ReportLine.EntryTitle($"{first.NameZh} ({first.EnglishKey}) x{count} - {first.LocationName}", first.ItemId));

                    string status = first.HasHarvestableFruit
                        ? "    已成熟，可以收获"
                        : $"    还需 {Math.Max(1, first.DaysRemaining)} 天成熟/再长出";
                    lines.Add(ReportLine.Of(status));
                }
            }

            if (machines.Count > 0)
            {
                // 同一块地点里、同一种机器、同一种产出、剩余时间相同的合并成一行
                var groups = machines
                    .GroupBy(m => (m.MachineItemId, m.OutputEnglishKey, m.LocationName, m.IsReady, m.MinutesRemaining))
                    .OrderBy(g => g.Key.IsReady ? 0 : 1)
                    .ThenBy(g => g.Key.MinutesRemaining);

                lines.Add(ReportLine.Of($"正在加工的机器 (共 {machines.Count} 台):"));
                foreach (var group in groups)
                {
                    var first = group.First();
                    int count = group.Count();
                    lines.Add(ReportLine.EntryTitle($"{first.MachineNameZh} ({first.MachineEnglishKey}) x{count} - {first.LocationName}", first.MachineItemId));
                    lines.Add(ReportLine.Of($"    正在制作: {first.OutputNameZh} ({first.OutputEnglishKey})"));
                    lines.Add(ReportLine.Of(first.IsReady
                        ? "    已完成，可以收取"
                        : $"    还需约 {first.MinutesRemaining} 分钟 (约 {first.MinutesRemaining / 60} 小时 {first.MinutesRemaining % 60} 分钟)"));
                }
            }

            return lines;
        }

        private List<GrowingCropInfo> CollectGrowingCrops()
        {
            var result = new List<GrowingCropInfo>();
            var allObjects = Game1.content.Load<Dictionary<string, StardewValley.GameData.Objects.ObjectData>>("Data/Objects");

            Utility.ForEachLocation(location =>
            {
                foreach (var pair in location.terrainFeatures.Pairs)
                {
                    if (pair.Value is not HoeDirt dirt || dirt.crop == null)
                        continue;

                    var crop = dirt.crop;
                    string harvestId = crop.indexOfHarvest.Value;
                    if (string.IsNullOrEmpty(harvestId))
                        continue;

                    bool fullyGrown = crop.fullyGrown.Value || crop.currentPhase.Value >= crop.phaseDays.Count - 1;

                    // fullyGrown 只代表植株已经发育完全，不代表现在有果实能摘：
                    // 像玉米这类可持续收获的作物，采摘后 dayOfCurrentPhase 会被设成 RegrowDays 天数并逐日倒数，
                    // 倒数到 0 之前是"已成熟但果实还没长出来"，不是"可以收获"。
                    bool hasHarvestableFruit = fullyGrown && crop.dayOfCurrentPhase.Value <= 0;
                    int daysRemaining;
                    if (!fullyGrown)
                    {
                        daysRemaining = 0;
                        int phase = crop.currentPhase.Value;
                        daysRemaining += crop.phaseDays[phase] - crop.dayOfCurrentPhase.Value;
                        for (int p = phase + 1; p < crop.phaseDays.Count - 1; p++)
                            daysRemaining += crop.phaseDays[p];
                    }
                    else
                    {
                        // 已成熟：dayOfCurrentPhase 此时就是距离果实再长出还差几天（0 表示现在就能摘）
                        daysRemaining = crop.dayOfCurrentPhase.Value;
                    }

                    string englishKey = GameDataHelper.ResolveEnglishKey(harvestId, allObjects);
                    result.Add(new GrowingCropInfo
                    {
                        ItemId = "(O)" + GameDataHelper.NormalizeItemId(harvestId),
                        EnglishKey = englishKey,
                        NameZh = CropNameData.Names.TryGetValue(englishKey, out var zh) ? zh : englishKey,
                        LocationName = location.Name,
                        HasHarvestableFruit = hasHarvestableFruit,
                        DaysRemaining = daysRemaining
                    });
                }
                return true;
            }, includeInteriors: true);

            return result;
        }

        private List<ProcessingMachineInfo> CollectProcessingMachines()
        {
            var result = new List<ProcessingMachineInfo>();
            var allObjects = Game1.content.Load<Dictionary<string, StardewValley.GameData.Objects.ObjectData>>("Data/Objects");

            Utility.ForEachLocation(location =>
            {
                foreach (var obj in location.objects.Values)
                {
                    if (!obj.bigCraftable.Value || obj.heldObject.Value == null)
                        continue;

                    string machineEnglishKey = GameDataHelper.GetEnglishKey(obj.DisplayName);
                    string outputEnglishKey = GameDataHelper.ResolveEnglishKey(obj.heldObject.Value.ItemId, allObjects);

                    result.Add(new ProcessingMachineInfo
                    {
                        MachineItemId = obj.QualifiedItemId,
                        MachineEnglishKey = machineEnglishKey,
                        MachineNameZh = machineEnglishKey,
                        OutputEnglishKey = outputEnglishKey,
                        OutputNameZh = outputEnglishKey,
                        LocationName = location.Name,
                        IsReady = obj.readyForHarvest.Value || obj.MinutesUntilReady <= 0,
                        MinutesRemaining = obj.MinutesUntilReady
                    });
                }
                return true;
            }, includeInteriors: true);

            return result;
        }

        private class GrowingCropInfo
        {
            public string ItemId = "";
            public string EnglishKey = "";
            public string NameZh = "";
            public string LocationName = "";
            public bool HasHarvestableFruit;
            public int DaysRemaining;
        }

        private class ProcessingMachineInfo
        {
            public string MachineItemId = "";
            public string MachineEnglishKey = "";
            public string MachineNameZh = "";
            public string OutputEnglishKey = "";
            public string OutputNameZh = "";
            public string LocationName = "";
            public bool IsReady;
            public int MinutesRemaining;
        }
    }
}
