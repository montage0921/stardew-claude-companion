namespace StardewClaudeCompanion
{
    // 收集报告里的一行；ItemId 非空时该行是某个物品条目的标题行(用于画图标)，
    // IsEntryStart 标记新物品条目的开始(用于画分割线)。
    public class ReportLine
    {
        public string Text = "";
        public string? ItemId;
        public bool IsEntryStart;

        public static ReportLine Of(string text) => new() { Text = text };

        public static ReportLine EntryTitle(string text, string itemId) => new() { Text = text, ItemId = itemId, IsEntryStart = true };
    }
}
