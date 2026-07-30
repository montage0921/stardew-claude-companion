using System.Collections.Generic;

namespace StardewClaudeCompanion
{
    public static class ArtifactNameData
    {
        public static readonly Dictionary<string, string> Names = new()
        {
            { "DwarfScrollI", "矮人卷轴一" },
            { "DwarfScrollII", "矮人卷轴二" },
            { "DwarfScrollIII", "矮人卷轴三" },
            { "DwarfScrollIV", "矮人卷轴四" },
            { "ChippedAmphora", "破损的双耳瓶" },
            { "AncientDoll", "远古玩偶" },
            { "GoldenMask", "黄金面具" },
            { "GoldenRelic", "黄金遗物" },
            { "RareDisc", "稀有唱片" },
            { "Anchor", "锚" },
            { "AncientSword", "远古之剑" },
            { "RustySpoon", "生锈的勺子" },
            { "RustySpur", "生锈的马刺" },
            { "RustyCog", "生锈的齿轮" },
            { "ChickenStatue", "鸡雕像" },
            { "AncientSeed", "远古种子" },
            { "PrehistoricTool", "史前工具" },
            { "DriedStarfish", "干海星" },
            { "GlassShards", "玻璃碎片" },
            { "BoneFlute", "骨笛" },
            { "PrehistoricHandaxe", "史前手斧" },
            { "DwarvishHelm", "矮人头盔" },
            { "DwarfGadget", "矮人小工具" },
            { "AncientDrum", "远古鼓" },
            { "PrehistoricScapula", "史前肩胛骨" },
            { "PrehistoricTibia", "史前胫骨" },
            { "PrehistoricSkull", "史前头骨" },
            { "SkeletalHand", "骨骼手" },
            { "PrehistoricRib", "史前肋骨" },
            { "PrehistoricVertebra", "史前脊椎骨" },
            { "SkeletalTail", "骨骼尾巴" },
            { "NautilusFossil", "鹦鹉螺化石" },
            { "AmphibianFossil", "两栖动物化石" },
            { "PalmFossil", "棕榈化石" },
            { "Trilobite", "三叶虫" },
            { "ChewingStick", "咀嚼棒" },
            { "OrnamentalFan", "装饰扇" },
            { "StrangeDollGreen", "奇怪玩偶(绿)" },
            { "StrangeDollYellow", "奇怪玩偶(黄)" },
            { "Arrowhead", "箭头" },
        };

        // 古器物分类没有已知需要排除的特殊项，先留空，后续发现再补充
        public static readonly HashSet<string> ExcludedFromCollection = new();
    }
}
