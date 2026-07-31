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
        private List<string> chatHistory = new();

        public override void Entry(IModHelper helper)
        {
            this.Config = this.Helper.ReadConfig<Config>();
            this.claudeClient = new ClaudeApiClient(this.Config.AnthropicApiKey);
            this.fishService = new FishCollectionService(this.Helper);
            this.cropService = new CropCollectionService(this.Helper);
            this.mineralService = new MineralCollectionService(this.Helper);
            this.artifactService = new ArtifactCollectionService(this.Helper);
            this.cookingService = new CookingCollectionService(this.Helper);
            this.Monitor.Log("Stardew Claude Companion loaded successfully!", LogLevel.Info);
            helper.Events.Input.ButtonPressed += this.OnButtonPressed;
        }

        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;

            if (e.Button == SButton.F8 && Game1.activeClickableMenu == null)
            {
                Game1.activeClickableMenu = new CompanionMenu(
                    this.fishService,
                    this.cropService,
                    this.mineralService,
                    this.artifactService,
                    this.cookingService,
                    this.claudeClient,
                    this.Helper,
                    this.chatHistory);
            }
        }
    }
}
