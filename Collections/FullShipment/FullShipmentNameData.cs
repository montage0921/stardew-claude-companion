using System.Collections.Generic;

namespace StardewClaudeCompanion
{
    // "全部出货"清单涵盖作物+野外采集物+工匠制品等很广的范围，这里先复用其他分类已有的翻译，
    // 没有对应中文名的会直接显示英文名，不阻塞功能可用性。
    public static class FullShipmentNameData
    {
        public static readonly Dictionary<string, string> Names = new()
        {
            // 常见野外采集物(Forage)，作物/矿物/鱼类分类各自的翻译表里没有覆盖这些
            { "WildHorseradish", "野生辣根" },
            { "Daffodil", "黄水仙" },
            { "Leek", "韭葱" },
            { "Dandelion", "蒲公英" },
            { "SpringOnion", "小葱" },
            { "Salmonberry", "鲑鱼莓" },
            { "SweetPea", "香豌豆" },
            { "SpiceBerry", "香料莓" },
            { "GrapeStarter", "葡萄" },
            { "Fiddlehead", "紫萁" },
            { "CommonMushroom", "普通蘑菇" },
            { "WildPlum", "野生李子" },
            { "Hazelnut", "榛子" },
            { "Blackberry", "黑莓" },
            { "Chanterelle", "鸡油菌" },
            { "HolidayCactus", "圣诞仙人掌" },
            { "WinterRoot", "冬根" },
            { "CrystalFruit", "水晶果" },
            { "SnowYam", "雪山药" },
            { "CrocusFairy", "番红花" },
            { "Crocus", "番红花" },
            { "CoconutFruit", "椰子" },
            { "Coconut", "椰子" },
            { "Cactus Fruit", "仙人掌果" },
            { "CactusFruit", "仙人掌果" },
            { "Cave Carrot", "洞穴胡萝卜" },
            { "CaveCarrot", "洞穴胡萝卜" },
            { "Morel", "羊肚菌" },
            { "PurpleMushroom", "紫蘑菇" },
            { "MagicRockCandy", "魔法摇滚糖" },
            { "SecretNote", "神秘笔记" },
            { "SpringSeeds", "春季种子" },
            { "SummerSeeds", "夏季种子" },
            { "FallSeeds", "秋季种子" },
            { "WinterSeeds", "冬季种子" },
            { "TeaLeaves", "茶叶" },
        };
    }
}
