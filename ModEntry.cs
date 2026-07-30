using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace StardewClaudeCompanion
{
    public class ModEntry : Mod
    {
        private Config Config = null!;
        private ClaudeApiClient? claudeClient;
        private FishCollectionService fishService = null!;
        private CropCollectionService cropService = null!;
        private MineralCollectionService mineralService = null!;
        private ArtifactCollectionService artifactService = null!;
        private CookingCollectionService cookingService = null!;

        public override void Entry(IModHelper helper)
        {
            this.Config = this.Helper.ReadConfig<Config>();
            this.claudeClient = new ClaudeApiClient(this.Config.AnthropicApiKey);
            this.fishService = new FishCollectionService(this.Helper, this.Monitor);
            this.cropService = new CropCollectionService(this.Helper, this.Monitor);
            this.mineralService = new MineralCollectionService(this.Helper, this.Monitor);
            this.artifactService = new ArtifactCollectionService(this.Helper, this.Monitor);
            this.cookingService = new CookingCollectionService(this.Helper, this.Monitor);
            this.Monitor.Log("Stardew Claude Companion loaded successfully!", LogLevel.Info);
            helper.Events.Input.ButtonPressed += this.OnButtonPressed;
        }

        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;

            if (e.Button == SButton.F5)
                this.fishService.PrintCaughtFish();
            else if (e.Button == SButton.F6)
                this.fishService.PrintMissingFishWithDetails();
            else if (e.Button == SButton.F9)
            {
                this.cropService.PrintMissingCrops();
            }
            else if (e.Button == SButton.F10)
            {
                this.mineralService.PrintMissingMinerals();
            }
            else if (e.Button == SButton.F1)
            {
                this.artifactService.PrintMissingArtifacts();
            }
            else if (e.Button == SButton.F2)
            {
                this.cookingService.PrintMissingRecipes();
            }
        }
    }
}
