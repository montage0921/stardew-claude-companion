using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace StardewClaudeCompanion
{
    public class CompanionMenu : IClickableMenu
    {
        private const int WindowWidth = 1100;
        private const int WindowHeight = 680;
        private const int LineHeight = 26;
        private const int IconSize = 32;
        private const int IconGutter = 40; // 图标列宽度，文字统一从这里开始画
        private const int GrowingCropsTabIndex = 5;
        private const int ProcessingMachinesTabIndex = 6;
        private const int ChatTabIndex = 7;

        // 复用游戏收藏页(CollectionsPage)/游戏菜单(GameMenu)里真实的图标坐标，视觉上和游戏自带菜单保持一致。
        // ItemId 非空时改用真实物品(浇水壶/橡木桶)画图标，避免瞎猜 mouseCursors 坐标猜错图案。
        private static readonly (string Label, Rectangle IconSource, string? ItemId)[] Tabs =
        {
            ("鱼类", new Rectangle(640, 64, 16, 16), null),
            ("作物", new Rectangle(640, 80, 16, 16), null),
            ("矿物", new Rectangle(672, 64, 16, 16), null),
            ("古器物", new Rectangle(656, 64, 16, 16), null),
            ("料理", new Rectangle(688, 64, 16, 16), null),
            ("作物进度", default, "(T)WateringCan"),
            ("制造进度", default, "(BC)163"),
            ("Claude", new Rectangle(32, 368, 16, 16), null),
        };

        private readonly FishCollectionService fishService;
        private readonly CropCollectionService cropService;
        private readonly MineralCollectionService mineralService;
        private readonly ArtifactCollectionService artifactService;
        private readonly CookingCollectionService cookingService;
        private readonly InProgressService inProgressService;
        private readonly ClaudeApiClient claudeClient;
        private readonly IModHelper helper;
        private readonly List<string> chatHistory;

        private readonly List<ClickableTextureComponent> tabButtons = new();
        private readonly int tabY;
        private readonly TextBox chatInput;
        private readonly ClickableTextureComponent sendButton;

        private int selectedTab;
        private List<ReportLine> currentReportLines = new();
        private List<WrappedLine> wrappedLines = new();
        private int scrollOffset;
        private Task<string>? pendingResponse;
        private SpriteFont? contentFont;

        // 换行后的一行显示内容；ItemId/IsEntryStart 只在该逻辑条目的第一条物理行上设置，
        // 用来在这一行画图标，以及在这一行上方画分割线。
        private class WrappedLine
        {
            public string Text = "";
            public string? ItemId;
            public bool IsEntryStart;
        }

        public CompanionMenu(
            FishCollectionService fishService,
            CropCollectionService cropService,
            MineralCollectionService mineralService,
            ArtifactCollectionService artifactService,
            CookingCollectionService cookingService,
            InProgressService inProgressService,
            ClaudeApiClient claudeClient,
            IModHelper helper,
            List<string> chatHistory)
            : base(
                Game1.uiViewport.Width / 2 - WindowWidth / 2,
                Game1.uiViewport.Height / 2 - WindowHeight / 2,
                WindowWidth,
                WindowHeight,
                showUpperRightCloseButton: true)
        {
            this.fishService = fishService;
            this.cropService = cropService;
            this.mineralService = mineralService;
            this.artifactService = artifactService;
            this.cookingService = cookingService;
            this.inProgressService = inProgressService;
            this.claudeClient = claudeClient;
            this.helper = helper;
            this.chatHistory = chatHistory;

            this.tabY = this.yPositionOnScreen + IClickableMenu.tabYPositionRelativeToMenuY + 64;
            this.contentFont = this.TryLoadChineseFont(helper) ?? Game1.smallFont;
            for (int i = 0; i < Tabs.Length; i++)
            {
                this.tabButtons.Add(new ClickableTextureComponent(
                    i.ToString(),
                    new Rectangle(this.xPositionOnScreen + 64 + i * 64, this.tabY, 64, 64),
                    "",
                    Tabs[i].Label,
                    Game1.mouseCursors,
                    Tabs[i].IconSource,
                    4f,
                    drawShadow: false));
            }

            this.chatInput = new TextBox(null, null, this.contentFont, Game1.textColor)
            {
                X = this.xPositionOnScreen + 32,
                Y = this.yPositionOnScreen + WindowHeight - 80,
                Width = WindowWidth - 176,
                Height = 56
            };
            this.chatInput.OnEnterPressed += _ => this.SendChatMessage();

            this.sendButton = new ClickableTextureComponent(
                new Rectangle(this.chatInput.X + this.chatInput.Width + 16, this.chatInput.Y, 64, 64),
                Game1.mouseCursors,
                Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 46),
                1f);

            this.SelectTab(0);
        }

        private int ContentWidth => WindowWidth - 64;
        private int ContentAreaHeight => this.selectedTab == ChatTabIndex ? WindowHeight - 96 - 96 : WindowHeight - 96 - 32;
        private int VisibleLineCount => Math.Max(1, this.ContentAreaHeight / LineHeight);
        private int MaxScroll => Math.Max(0, this.wrappedLines.Count - this.VisibleLineCount);

        private SpriteFont? TryLoadChineseFont(IModHelper helper)
        {
            try
            {
                return helper.ModContent.Load<SpriteFont>("assets/ChineseFont.xnb");
            }
            catch
            {
                return null;
            }
        }

        private readonly Dictionary<string, Item?> iconItemCache = new();

        private void DrawItemIcon(SpriteBatch b, string itemId, Vector2 position)
        {
            if (!this.iconItemCache.TryGetValue(itemId, out var item))
            {
                try
                {
                    item = ItemRegistry.Create(itemId, allowNull: true);
                }
                catch
                {
                    item = null;
                }
                this.iconItemCache[itemId] = item;
            }

            // drawInMenu 以 position + (32,32) 为图标中心画图（不随 scaleSize 缩放），
            // 这里 scaleSize=0.5 时图标最终是 32x32，所以要把中心点往左上退 16px，
            // 才能让图标的左上角落在调用方传入的 position 上。
            item?.drawInMenu(b, position - new Vector2(16, 16), 0.5f, 1f, 0.9f, StackDrawType.Hide, Color.White, drawShadow: false);
        }

        // Tab图标专用：按物品贴图在源图集里的真实宽高做居中，而不是假定固定32x32——
        // 浇水壶这类工具类物品的sourceRect比例和普通Object不同，固定尺寸居中会视觉跑偏。
        private void DrawItemIconCentered(SpriteBatch b, string itemId, Vector2 center)
        {
            if (!this.iconItemCache.TryGetValue(itemId, out var item))
            {
                try
                {
                    item = ItemRegistry.Create(itemId, allowNull: true);
                }
                catch
                {
                    item = null;
                }
                this.iconItemCache[itemId] = item;
            }

            if (item == null)
                return;

            var data = ItemRegistry.GetDataOrErrorItem(item.QualifiedItemId);
            var sourceRect = data.GetSourceRect(0, item.ParentSheetIndex);

            const float scale = 2.5f;
            var origin = new Vector2(sourceRect.Width / 2f, sourceRect.Height / 2f);
            b.Draw(data.GetTexture(), center, sourceRect, Color.White, 0f, origin, scale, SpriteEffects.None, 0.9f);
        }

        private void SelectTab(int index)
        {
            this.selectedTab = index;
            this.scrollOffset = 0;

            if (index == ChatTabIndex)
            {
                Game1.keyboardDispatcher.Subscriber = this.chatInput;
                this.chatInput.Selected = true;
            }
            else
            {
                Game1.keyboardDispatcher.Subscriber = null;
                this.chatInput.Selected = false;

                this.currentReportLines = index switch
                {
                    0 => this.fishService.GetMissingFishReport(),
                    1 => this.cropService.GetMissingCropsReport(),
                    2 => this.mineralService.GetMissingMineralsReport(),
                    3 => this.artifactService.GetMissingArtifactsReport(),
                    4 => this.cookingService.GetMissingRecipesReport(),
                    GrowingCropsTabIndex => this.inProgressService.GetGrowingCropsReport(),
                    ProcessingMachinesTabIndex => this.inProgressService.GetProcessingMachinesReport(),
                    _ => new List<ReportLine>()
                };
            }

            this.RebuildWrappedLines();
        }

        private void RebuildWrappedLines()
        {
            var font = this.contentFont ?? Game1.smallFont;
            var lines = new List<WrappedLine>();

            if (this.selectedTab == ChatTabIndex)
            {
                foreach (var chatLine in this.chatHistory)
                {
                    // Claude 的回复可能自带换行(Markdown 列表/多段落)，DrawString 每行只能画一条物理行，
                    // 必须先按 \n 拆开，再对每条物理行分别做宽度折行，否则一个 WrappedLine 里
                    // 藏着好几行文字，但外层只按一行的高度步进，会导致后面的消息画到它上面、发生重叠。
                    foreach (var physicalLine in chatLine.Split('\n'))
                    {
                        string clean = StripMarkdown(physicalLine);
                        foreach (var wrapped in WrapLine(clean, font, this.ContentWidth))
                            lines.Add(new WrappedLine { Text = wrapped });
                    }
                }

                if (this.pendingResponse != null)
                    lines.Add(new WrappedLine { Text = "Claude 正在思考..." });
            }
            else
            {
                // 有图标的行要给文字预留左边距，换行宽度相应变窄
                int iconTextWidth = this.ContentWidth - IconGutter;

                foreach (var reportLine in this.currentReportLines)
                {
                    int wrapWidth = reportLine.ItemId != null ? iconTextWidth : this.ContentWidth;
                    var wrapped = WrapLine(reportLine.Text, font, wrapWidth);
                    for (int i = 0; i < wrapped.Count; i++)
                    {
                        lines.Add(new WrappedLine
                        {
                            Text = wrapped[i],
                            ItemId = i == 0 ? reportLine.ItemId : null,
                            IsEntryStart = i == 0 && reportLine.IsEntryStart
                        });
                    }
                }
            }

            this.wrappedLines = lines;
            this.scrollOffset = Math.Min(this.scrollOffset, this.MaxScroll);
        }

        // 去掉 Claude 回复里常见的 Markdown 标记符号（加粗/标题/列表项），
        // 我们是纯文本渲染，这些符号只会原样显示成一堆星号和井号，不会真的加粗或变成标题。
        private static string StripMarkdown(string text)
        {
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\*\*(.+?)\*\*", "$1");
            text = System.Text.RegularExpressions.Regex.Replace(text, @"^#{1,6}\s*", "");
            text = System.Text.RegularExpressions.Regex.Replace(text, @"^[-*]\s+", "• ");
            return text;
        }

        private static List<string> WrapLine(string line, SpriteFont font, int maxWidth)
        {
            var result = new List<string>();
            if (font.MeasureString(line).X <= maxWidth)
            {
                result.Add(line);
                return result;
            }

            // 把文本切成不可再拆的"单元"：西文按空格分词（单词整体不切断），中文/符号逐字符独立
            var units = new List<string>();
            var word = new System.Text.StringBuilder();
            foreach (char c in line)
            {
                bool isAscii = c < 128 && !char.IsWhiteSpace(c);
                if (isAscii)
                {
                    word.Append(c);
                }
                else
                {
                    if (word.Length > 0)
                    {
                        units.Add(word.ToString());
                        word.Clear();
                    }
                    units.Add(c.ToString());
                }
            }
            if (word.Length > 0)
                units.Add(word.ToString());

            var currentLine = new System.Text.StringBuilder();
            foreach (var unit in units)
            {
                string candidate = currentLine.ToString() + unit;
                if (currentLine.Length > 0 && font.MeasureString(candidate).X > maxWidth)
                {
                    result.Add(currentLine.ToString());
                    currentLine.Clear();
                    currentLine.Append(unit.TrimStart());
                }
                else
                {
                    currentLine.Append(unit);
                }
            }

            if (currentLine.Length > 0)
                result.Add(currentLine.ToString());

            return result;
        }

        private void SendChatMessage()
        {
            string question = this.chatInput.Text.Trim();
            if (string.IsNullOrEmpty(question) || this.pendingResponse != null)
                return;

            this.chatHistory.Add($"你: {question}");
            this.chatInput.Text = "";

            string context = GameSnapshotBuilder.Build(this.helper) + "\n\n" + this.BuildCollectionSummary(question) + "\n\n" + InventorySnapshotBuilder.Build();
            this.pendingResponse = Task.Run(() => this.claudeClient.AskAsync(question, context));

            this.RebuildWrappedLines();
            this.scrollOffset = this.MaxScroll;
        }

        private static readonly (string[] Keywords, string Title)[] CollectionTopics =
        {
            (new[] { "鱼", "钓", "fish" }, "鱼类收集"),
            (new[] { "作物", "种", "菜", "水果", "crop", "plant" }, "作物收集"),
            (new[] { "矿", "宝石", "mineral", "gem" }, "矿物收集"),
            (new[] { "古器物", "古物", "文物", "artifact" }, "古器物收集"),
            (new[] { "料理", "做菜", "食谱", "配方", "cook", "recipe" }, "料理收集"),
        };

        // 把本地已经算好的收集缺失清单转成纯文字，附加到发给 Claude 的上下文里，
        // 这样问答能直接引用"还缺什么/怎么获得"，不需要 Claude 自己重新比对。
        // 只加载和问题相关的分类，避免每次提问都把全部5类缺失清单(可能几千 token)塞进去。
        private string BuildCollectionSummary(string question)
        {
            var sb = new System.Text.StringBuilder();

            void AppendSection(string title, List<ReportLine> lines)
            {
                sb.Append("【").Append(title).Append("】\n");
                foreach (var line in lines)
                    sb.Append(line.Text).Append('\n');
                sb.Append('\n');
            }

            bool matchedAny = false;
            foreach (var topic in CollectionTopics)
            {
                if (!topic.Keywords.Any(k => question.Contains(k, System.StringComparison.OrdinalIgnoreCase)))
                    continue;

                matchedAny = true;
                var lines = topic.Title switch
                {
                    "鱼类收集" => this.fishService.GetMissingFishReport(),
                    "作物收集" => this.cropService.GetMissingCropsReport(),
                    "矿物收集" => this.mineralService.GetMissingMineralsReport(),
                    "古器物收集" => this.artifactService.GetMissingArtifactsReport(),
                    "料理收集" => this.cookingService.GetMissingRecipesReport(),
                    _ => new List<ReportLine>()
                };
                AppendSection(topic.Title, lines);
            }

            // 问题没命中任何收集类关键词（比如单纯问库存物品），就不额外附加，
            // InventorySnapshotBuilder 提供的物品清单已经够用。
            return matchedAny ? sb.ToString() : "";
        }

        public override void update(GameTime time)
        {
            base.update(time);
            this.chatInput.Update();

            if (this.pendingResponse != null && this.pendingResponse.IsCompleted)
            {
                string answer = this.pendingResponse.IsCompletedSuccessfully
                    ? this.pendingResponse.Result
                    : "[请求失败，请稍后重试]";
                this.chatHistory.Add($"Claude: {answer}");
                this.pendingResponse = null;

                if (this.selectedTab == ChatTabIndex)
                {
                    this.RebuildWrappedLines();
                    this.scrollOffset = this.MaxScroll;
                }
            }
        }

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            base.receiveLeftClick(x, y, playSound);

            for (int i = 0; i < this.tabButtons.Count; i++)
            {
                if (this.tabButtons[i].containsPoint(x, y))
                {
                    this.SelectTab(i);
                    Game1.playSound("smallSelect");
                    return;
                }
            }

            if (this.selectedTab == ChatTabIndex && this.sendButton.containsPoint(x, y))
            {
                this.SendChatMessage();
            }
        }

        public override void receiveScrollWheelAction(int direction)
        {
            base.receiveScrollWheelAction(direction);
            if (direction > 0)
                this.scrollOffset = Math.Max(0, this.scrollOffset - 1);
            else
                this.scrollOffset = Math.Min(this.MaxScroll, this.scrollOffset + 1);
        }

        public override void receiveKeyPress(Keys key)
        {
            // 聊天框输入时不能让 ESC/关闭菜单快捷键把整个面板关掉，只能用右上角的关闭按钮
            if (this.selectedTab == ChatTabIndex && this.chatInput.Selected)
                return;

            base.receiveKeyPress(key);
        }

        public override void performHoverAction(int x, int y)
        {
            base.performHoverAction(x, y);
            this.chatInput.Hover(x, y);
        }

        public override void draw(SpriteBatch b)
        {
            Game1.drawDialogueBox(this.xPositionOnScreen, this.yPositionOnScreen, this.width, this.height, speaker: false, drawOnlyBox: true);

            for (int i = 0; i < this.tabButtons.Count; i++)
            {
                var tab = this.tabButtons[i];
                bool selected = i == this.selectedTab;
                tab.bounds.Y = this.tabY + (selected ? 8 : 0);

                string? itemId = Tabs[i].ItemId;
                if (itemId != null)
                {
                    // 用真实游戏物品(浇水壶/橡木桶)画Tab图标，先画个木框背景和其他Tab保持一致，
                    // 图标居中在tab的64x64范围里
                    IClickableMenu.drawTextureBox(b, tab.bounds.X, tab.bounds.Y, tab.bounds.Width, tab.bounds.Height, Color.White);
                    var iconCenter = new Vector2(tab.bounds.X + tab.bounds.Width / 2f, tab.bounds.Y + tab.bounds.Height / 2f);
                    // 浇水壶(工具类)贴图在格子里的视觉重心偏上，单独往下微调
                    float extraYOffset = itemId == "(T)WateringCan" ? 8f : 0f;
                    this.DrawItemIcon(b, itemId, iconCenter - new Vector2(16, 16 - extraYOffset));
                }
                else
                {
                    tab.draw(b);
                }
            }

            int contentX = this.xPositionOnScreen + 32;
            int contentY = this.yPositionOnScreen + 96;
            if (this.contentFont != null)
            {
                const int EntryGap = 10; // 条目之间的额外竖直间距
                const int IconLeftPad = 12; // 图标离窗口内框的左边距
                int contentBottom = contentY + this.ContentAreaHeight;
                int extraGap = 0;
                for (int i = 0; i + this.scrollOffset < this.wrappedLines.Count; i++)
                {
                    var line = this.wrappedLines[i + this.scrollOffset];

                    // 新物品条目开始前留出一点间距，把不同物品的多行信息隔开
                    if (line.IsEntryStart && i > 0)
                        extraGap += EntryGap;

                    int rowY = contentY + i * LineHeight + extraGap;
                    if (rowY + LineHeight > contentBottom)
                        break;

                    int textX = contentX;
                    if (line.ItemId != null)
                    {
                        DrawItemIcon(b, line.ItemId, new Vector2(contentX + IconLeftPad, rowY - (IconSize - LineHeight) / 2));
                        textX = contentX + IconLeftPad + IconGutter;
                    }

                    b.DrawString(this.contentFont, line.Text, new Vector2(textX, rowY), Game1.textColor);
                }
            }

            if (this.selectedTab == ChatTabIndex)
            {
                // TextBox 在没有传入贴图时(我们传的是 null)不会画任何背景框，
                // 这里手动画一个，不然输入框在视觉上完全没有边界。
                IClickableMenu.drawTextureBox(b, this.chatInput.X - 8, this.chatInput.Y - 8, this.chatInput.Width + 16, this.chatInput.Height + 16, Color.White);

                this.chatInput.Draw(b);
                this.sendButton.draw(b);
            }

            base.draw(b);

            this.drawMouse(b);
        }
    }
}
