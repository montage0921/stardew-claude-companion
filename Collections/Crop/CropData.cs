using System.Collections.Generic;

namespace StardewClaudeCompanion
{
    public static class CropNameData
    {
        public static readonly Dictionary<string, string> Names = new()
        {
            // 春季
            { "BlueJazz", "蓝色喇叭花" },
            { "Carrot", "胡萝卜" },
            { "Cauliflower", "花椰菜" },
            { "CoffeeBean", "咖啡豆" },
            { "Garlic", "大蒜" },
            { "GreenBean", "四季豆" },
            { "Kale", "羽衣甘蓝" },
            { "Parsnip", "防风草" },
            { "Potato", "土豆" },
            { "Rhubarb", "大黄" },
            { "Strawberry", "草莓" },
            { "Tulip", "郁金香" },
            { "UnmilledRice", "稻谷" },

            // 夏季
            { "Blueberry", "蓝莓" },
            { "Corn", "玉米" },
            { "Hops", "啤酒花" },
            { "HotPepper", "红辣椒" },
            { "Melon", "甜瓜" },
            { "Poppy", "罂粟" },
            { "Radish", "小萝卜" },
            { "RedCabbage", "紫甘蓝" },
            { "Starfruit", "杨桃" },
            { "SummerSpangle", "夏日星章花" },
            { "SummerSquash", "西葫芦" },
            { "Sunflower", "向日葵" },
            { "Tomato", "番茄" },
            { "Wheat", "小麦" },

            // 秋季
            { "Amaranth", "苋菜" },
            { "Artichoke", "洋蓟" },
            { "Beet", "甜菜" },
            { "BokChoy", "小白菜" },
            { "Broccoli", "西兰花" },
            { "Cranberries", "蔓越莓" },
            { "Eggplant", "茄子" },
            { "FairyRose", "仙女玫瑰" },
            { "Grape", "葡萄" },
            { "Pumpkin", "南瓜" },
            { "Yam", "山药" },

            // 冬季
            { "Powdermelon", "雪蜜瓜" },

            // 特殊/多季
            { "AncientFruit", "神秘果" },
            { "CactusFruit", "仙人掌果" },
            { "Fiber", "纤维" },
            { "MixedFlowerSeeds", "混合花种" },
            { "MixedSeeds", "混合种子" },
            { "Pineapple", "菠萝" },
            { "TaroRoot", "芋头" },
            { "SweetGemBerry", "甜宝石莓" },
            { "TeaLeaves", "茶叶" },
            { "WildSeeds", "野生种子" },
            { "QiFruit", "Qi的水果" },
        };

        // 特殊作物的真实获取途径，找不到就用默认的"种子店"
        public static readonly Dictionary<string, string> SpecialSeedSources = new()
        {
            { "SweetGemBerry", "旅行商人(周五/周日限定,1000金) 或 种子机" },
            { "AncientFruit", "旅行商人(低概率) / 矿洞打怪掉落神秘种子 / 博物馆捐赠解锁配方 / 种子机" },
            { "CoffeeBean", "旅行商人(2500金) 或 灰尘精灵掉落(1%概率)" },
            { "Rhubarb", "沙漠绿洲商店" },
            { "Strawberry", "复活节兔子节限定购买" },
            { "Carrot", "秋末/冬末采集绿色神器点(挖到种子)" },
            { "SummerSquash", "春末采集绿色神器点" },
            { "Broccoli", "夏末采集绿色神器点" },
            { "Powdermelon", "秋末采集绿色神器点" },
            { "TaroRoot", "姜岛专属，姜岛商店购买" },
            { "Pineapple", "姜岛专属，姜岛商店购买" },
            { "CactusFruit", "沙漠绿洲商店(仙人掌种子)" },
        };

        // 限时任务/特殊产物，不计入常规"全收集"追踪
        public static readonly HashSet<string> ExcludedFromCollection = new()
        {
            "QiFruit", // Mr. Qi 限时挑战任务专属，任务结束后消失，不是常规收集目标
            "MixedFlowerSeeds", // 这是种子包，不是收获物本身
            "MixedSeeds", // 同上
            "WildSeeds", // 同上
        };
    }
}