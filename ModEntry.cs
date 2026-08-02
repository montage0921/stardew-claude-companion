using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System.Collections.Generic;

namespace StardewClaudeCompanion
{
    public class ModEntry : Mod
    {
        private Config Config = null!;
        private ClaudeApiClient claudeClient = null!;
        private FishCollectionService fishService = null!;
        private CropCollectionService cropService = null!;
        private MineralCollectionService mineralService = null!;
        private ArtifactCollectionService artifactService = null!;
        private CookingCollectionService cookingService = null!;
        private InProgressService inProgressService = null!;
        private AchievementCollectionService achievementService = null!;
        private List<string> chatHistory = new();
        private List<ChatTurn> apiChatHistory = new();
        private CompanionHudButton hudButton = null!;

        public override void Entry(IModHelper helper)
        {
            this.Config = this.Helper.ReadConfig<Config>();
            this.claudeClient = new ClaudeApiClient(this.Config.AnthropicApiKey);
            this.fishService = new FishCollectionService(this.Helper);
            this.cropService = new CropCollectionService(this.Helper);
            this.mineralService = new MineralCollectionService(this.Helper);
            this.artifactService = new ArtifactCollectionService(this.Helper);
            this.cookingService = new CookingCollectionService(this.Helper);
            this.inProgressService = new InProgressService();
            this.achievementService = new AchievementCollectionService(this.Helper);
            this.hudButton = new CompanionHudButton(this.Helper);
            this.Monitor.Log("Stardew Claude Companion loaded successfully!", LogLevel.Info);
            helper.Events.Input.ButtonPressed += this.OnButtonPressed;
            helper.Events.Display.RenderedHud += this.OnRenderedHud;
            helper.Events.Input.ButtonPressed += this.OnHudButtonClicked;
        }

        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;

            if (e.Button == SButton.F8 && Game1.activeClickableMenu == null)
            {
                this.OpenCompanionMenu();
            }

            // 临时诊断：验证候选物品ID对应的真实名称，确认后会删除
            if (e.Button == SButton.F9)
            {
                string[] candidates = { "(O)74", "(O)168", "(O)166", "(O)771", "(O)797", "(O)789", "(O)373", "(BC)102" };
                foreach (var id in candidates)
                {
                    var item = StardewValley.ItemRegistry.Create(id, allowNull: true);
                    this.Monitor.Log($"{id} -> {(item == null ? "NULL" : item.DisplayName)}", LogLevel.Alert);
                }
            }
        }

        private void OnRenderedHud(object? sender, RenderedHudEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;

            this.hudButton.Draw(e.SpriteBatch);
        }

        private void OnHudButtonClicked(object? sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady || e.Button != SButton.MouseLeft)
                return;

            // 显式用 ui_scale:true 换算，和 Draw() 里 Game1.getMouseX/Y() 在 HUD 阶段的换算方式保持一致，
            // 不依赖 Game1.uiMode 这个在输入事件触发时可能还没切到位的全局状态。
            int x = Game1.getMouseX(ui_scale: true);
            int y = Game1.getMouseY(ui_scale: true);
            if (this.hudButton.TryHandleClick(x, y))
            {
                this.OpenCompanionMenu();
                this.Helper.Input.Suppress(e.Button);
            }
        }

        private void OpenCompanionMenu()
        {
            Game1.activeClickableMenu = new CompanionMenu(
                this.fishService,
                this.cropService,
                this.mineralService,
                this.artifactService,
                this.cookingService,
                this.inProgressService,
                this.achievementService,
                this.claudeClient,
                this.Helper,
                this.chatHistory,
                this.apiChatHistory);
        }
    }
}
