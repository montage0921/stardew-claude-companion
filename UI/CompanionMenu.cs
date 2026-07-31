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
        private const int WindowWidth = 900;
        private const int WindowHeight = 640;
        private const int LineHeight = 28;
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
        private string? hoverText;
        private List<string> currentReportLines = new();
        private List<string> wrappedLines = new();
        private int scrollOffset;
        private Task<string>? pendingResponse;
        private SpriteFont? contentFont;

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
                    _ => new List<string>()
                };
            }

            this.RebuildWrappedLines();
        }

        private void RebuildWrappedLines()
        {
            var source = this.selectedTab == ChatTabIndex ? this.chatHistory : this.currentReportLines;
            var lines = new List<string>();
            var font = this.contentFont ?? Game1.smallFont;

            foreach (var line in source)
            {
                if (font.MeasureString(line).X <= this.ContentWidth)
                {
                    lines.Add(line);
                }
                else
                {
                    // 逐字符添加，超过宽度就换行
                    var currentLine = new System.Text.StringBuilder();
                    foreach (char c in line)
                    {
                        currentLine.Append(c);
                        if (font.MeasureString(currentLine.ToString()).X > this.ContentWidth)
                        {
                            // 删掉最后加的字符，保存当前行
                            currentLine.Length -= 1;
                            if (currentLine.Length > 0)
                                lines.Add(currentLine.ToString());

                            // 重新开始新行，包含刚才的字符
                            currentLine.Clear();
                            currentLine.Append(c);
                        }
                    }

                    if (currentLine.Length > 0)
                        lines.Add(currentLine.ToString());
                }
            }

            if (this.selectedTab == ChatTabIndex && this.pendingResponse != null)
                lines.Add("Claude 正在思考...");

            this.wrappedLines = lines;
            this.scrollOffset = Math.Min(this.scrollOffset, this.MaxScroll);
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

            this.hoverText = null;
            foreach (var tab in this.tabButtons)
            {
                if (tab.containsPoint(x, y))
                {
                    this.hoverText = tab.hoverText;
                    break;
                }
            }
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
                for (int i = 0; i < this.VisibleLineCount && i + this.scrollOffset < this.wrappedLines.Count; i++)
                {
                    string line = this.wrappedLines[i + this.scrollOffset];
                    b.DrawString(this.contentFont, line, new Vector2(contentX, contentY + i * LineHeight), Game1.textColor);
                }
            }

            if (this.selectedTab == ChatTabIndex)
            {
                this.chatInput.Draw(b);
                this.sendButton.draw(b);
            }

            base.draw(b);

            if (!string.IsNullOrEmpty(this.hoverText))
                IClickableMenu.drawHoverText(b, this.hoverText, Game1.smallFont);

            this.drawMouse(b);
        }
    }
}
