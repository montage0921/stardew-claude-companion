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
        private const int ChatTabIndex = 5;

        // 复用游戏收藏页(CollectionsPage)/游戏菜单(GameMenu)里真实的图标坐标，视觉上和游戏自带菜单保持一致
        private static readonly (string Label, Rectangle IconSource)[] Tabs =
        {
            ("鱼类", new Rectangle(640, 64, 16, 16)),
            ("作物", new Rectangle(640, 80, 16, 16)),
            ("矿物", new Rectangle(672, 64, 16, 16)),
            ("古器物", new Rectangle(656, 64, 16, 16)),
            ("料理", new Rectangle(688, 64, 16, 16)),
            ("Claude", new Rectangle(32, 368, 16, 16)),
        };

        private readonly FishCollectionService fishService;
        private readonly CropCollectionService cropService;
        private readonly MineralCollectionService mineralService;
        private readonly ArtifactCollectionService artifactService;
        private readonly CookingCollectionService cookingService;
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

            this.chatInput = new TextBox(null, null, Game1.smallFont, Game1.textColor)
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
                    foreach (var wrapped in WrapLine(chatLine, font, this.ContentWidth))
                        lines.Add(new WrappedLine { Text = wrapped });
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

            string context = GameSnapshotBuilder.Build(this.helper);
            this.pendingResponse = Task.Run(() => this.claudeClient.AskAsync(question, context));

            this.RebuildWrappedLines();
            this.scrollOffset = this.MaxScroll;
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
                tab.draw(b);
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
                this.chatInput.Draw(b);
                this.sendButton.draw(b);
            }

            base.draw(b);

            this.drawMouse(b);
        }
    }
}
