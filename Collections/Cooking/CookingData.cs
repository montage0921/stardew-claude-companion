using System.Collections.Generic;

namespace StardewClaudeCompanion
{
    public static class CookingNameData
    {
        public static readonly Dictionary<string, string> Names = new()
        {
            { "FriedEgg", "煎蛋" },
            { "Omelet", "煎蛋卷" },
            { "Salad", "沙拉" },
            { "CheeseCauliflower", "奶酪花椰菜" },
            { "BakedFish", "烤鱼" },
            { "ParsnipSoup", "防风草汤" },
            { "VegetableMedley", "蔬菜杂烩" },
            { "CompleteBreakfast", "丰盛的早餐" },
            { "FriedCalamari", "炸鱿鱼圈" },
            { "StrangeBun", "奇怪的面包" },
            { "LuckyLunch", "幸运午餐" },
            { "FriedMushroom", "炸蘑菇" },
            { "Pizza", "披萨" },
            { "BeanHotpot", "豆焖锅" },
            { "GlazedYams", "蜜汁山药" },
            { "CarpSurprise", "惊喜鲤鱼" },
            { "HashBrowns", "土豆饼" },
            { "Pancakes", "煎饼" },
            { "SalmonDinner", "三文鱼晚餐" },
            { "FishTaco", "鱼肉卷饼" },
            { "CragyFishStew", "岩鱼炖菜" },
            { "TrapperSFishTaco", "猎人鱼肉卷饼" },
            { "IceCream", "冰淇淋" },
            { "Cookie", "曲奇饼" },
            { "Spaghetti", "意大利面" },
            { "FriedEel", "炸鳗鱼" },
            { "SpicyEel", "香辣鳗鱼" },
            { "Sashimi", "生鱼片" },
            { "MakiRoll", "寿司卷" },
            { "TomRiddlesHatSalad", "特殊沙拉" },
            { "TripleShotEspresso", "三倍浓缩咖啡" },
            { "MinersTreat", "矿工的美食" },
            { "RootsPlatter", "根茎拼盘" },
            { "PepperPoppers", "辣椒炮弹" },
            { "Bruschetta", "意式烤面包片" },
            { "Chowder", "杂烩汤" },
            { "FishStew", "鱼汤" },
            { "EscargotSauce", "蜗牛酱" },
            { "LobsterBisque", "龙虾浓汤" },
            { "MapleBar", "枫糖甜甜圈" },
            { "CrabCakes", "蟹肉饼" },
            { "ShrimpCocktail", "鲜虾鸡尾酒" },
            { "ChocolateCake", "巧克力蛋糕" },
            { "PinkCake", "粉色蛋糕" },
            { "RhubarbPie", "大黄派" },
            { "Cake", "蛋糕" },
            { "PumpkinPie", "南瓜派" },
            { "RadishSalad", "小萝卜沙拉" },
            { "FruitSalad", "水果沙拉" },
            { "BlueberryTart", "蓝莓挞" },
            { "AutumnsBounty", "秋之馈赠" },
            { "PumpkinSoup", "南瓜汤" },
            { "SuperMeal", "超级美食" },
            { "CranberrySauce", "蔓越莓酱" },
            { "StuffingRolls", "填料卷" },
            { "FarmersLunch", "农夫的午餐" },
            { "SurvivalBurger", "生存汉堡" },
            { "DishOTheSea", "海之料理" },
            { "MinerSSalad", "矿工沙拉" },
            { "Poi", "芋泥" },
            { "TropicalCurry", "热带咖喱" },
            { "EggplantParmesan", "茄子帕玛森" },
            { "RicePudding", "米布丁" },
            { "PoppyseedMuffin", "罂粟籽松饼" },
            { "ChefsBoat", "厨师之舟" },
        };

        // 料理分类没有已知需要排除的特殊项，先留空，后续发现再补充
        public static readonly HashSet<string> ExcludedFromCollection = new();

        // 反编译 CollectionsPage 确认：这几个物品虽然也是 Cooking 分类/类型，但不算菜谱，游戏本身在收藏页里跳过它们
        public static readonly HashSet<string> NonRecipeCookingItemIds = new()
        {
            "217", "772", "773", "279", "873",
        };

        // 反编译 CollectionsPage 确认：内部名和 cookingRecipes 记录用的名字对不上的几个特例，需要互相替换才能正确匹配"是否已学会"
        public static readonly Dictionary<string, string> RecipeNameRemap = new()
        {
            { "Cheese Cauli.", "Cheese Cauliflower" },
            { "Cheese Cauliflower", "Cheese Cauli." },
            { "Vegetable Medley", "Vegetable Stew" },
            { "Cookie", "Cookies" },
            { "Eggplant Parmesan", "Eggplant Parm." },
            { "Cranberry Sauce", "Cran. Sauce" },
            { "Dish O' The Sea", "Dish o' The Sea" },
        };
    }
}
