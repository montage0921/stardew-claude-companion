using System.Collections.Generic;

namespace StardewClaudeCompanion
{
    public static class MineralNameData
    {
        public static readonly Dictionary<string, string> Names = new()
        {
            // 宝石 (Category: Gem)
            { "Emerald", "绿宝石" },
            { "Aquamarine", "海蓝宝石" },
            { "Ruby", "红宝石" },
            { "Amethyst", "紫水晶" },
            { "Topaz", "黄玉" },
            { "Sapphire", "蓝宝石" },
            { "Diamond", "钻石" },
            { "PrismaticShard", "棱彩碎片" },

            // 基础矿物 (Category: Minerals)
            { "Quartz", "石英" },
            { "FireQuartz", "火水晶" },
            { "FrozenTear", "冻之泪" },
            { "EarthCrystal", "大地水晶" },
            { "Marble", "大理石" },

            // 地质晶洞类矿物
            { "Alamite", "云母石" },
            { "Bixite", "黑曜矿" },
            { "Baryte", "重晶石" },
            { "Aerinite", "蓝铁矿" },
            { "Calcite", "方解石" },
            { "Dolomite", "白云石" },
            { "Esperite", "银星石" },
            { "Fluorapatite", "氟磷灰石" },
            { "Geminite", "双子石" },
            { "Helvite", "海尔石" },
            { "Jamborite", "詹博石" },
            { "Jagoite", "杰戈石" },
            { "Kyanite", "蓝晶石" },
            { "Lunarite", "月光石" },
            { "Malachite", "孔雀石" },
            { "Neptunite", "海王石" },
            { "Nekoite", "猫眼石" },
            { "Orpiment", "雌黄" },
            { "PetrifiedSlime", "石化史莱姆" },
            { "Pyrite", "黄铁矿" },
            { "OceanStone", "海洋石" },
            { "GhostCrystal", "幽灵水晶" },
            { "Tigerseye", "虎眼石" },
            { "Jasper", "碧玉" },
            { "Opal", "蛋白石" },
            { "FireOpal", "火蛋白石" },
            { "Celestine", "天青石" },
            { "Sandstone", "砂岩" },
            { "Granite", "花岗岩" },
            { "Basalt", "玄武岩" },
            { "Limestone", "石灰岩" },
            { "Soapstone", "皂石" },
            { "Hematite", "赤铁矿" },
            { "Mudstone", "泥岩" },
            { "Obsidian", "黑曜石" },
            { "Slate", "板岩" },
            { "Fluorite", "萤石" },
        };

        // 矿物分类没有已知需要排除的特殊项，先留空，后续发现再补充
        public static readonly HashSet<string> ExcludedFromCollection = new();
    }
}
