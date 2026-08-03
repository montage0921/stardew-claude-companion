using System.Collections.Generic;

namespace StardewClaudeCompanion
{
    // 探险家公会"消灭怪物"目标的中文名。
    // Data/MonsterSlayerQuests 没有 .zh-CN 本地化文件，它的 DisplayName 是指向 Strings\Locations 的 token，
    // 所以游戏界面是英文时 TokenParser 只会返回英文。这里按解析出来的英文名兜底翻译，
    // 保证不管游戏语言是什么，我们的面板都显示中文。
    public static class MonsterGoalNameData
    {
        // key 用 TokenParser 解析出的英文名（已 Trim），value 是中文名
        public static readonly Dictionary<string, string> Names = new()
        {
            { "Slimes", "史莱姆" },
            { "Void Spirits", "虚空之灵" },
            { "Bats", "蝙蝠" },
            { "Skeletons", "骷髅" },
            { "Cave Insects", "洞穴昆虫" },
            { "Duggies", "土拨鼠" },
            { "Dust Sprites", "尘埃精灵" },
            { "Rock Crabs", "石蟹" },
            { "Mummies", "木乃伊" },
            { "Pepper Rex", "胡椒暴龙" },
            { "Serpents", "海蛇" },
            { "Magma Sprites", "岩浆精灵" },
        };
    }
}
