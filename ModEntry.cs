using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System.Threading.Tasks;

namespace StardewClaudeCompanion
{
    public class ModEntry : Mod
    {
        private Config Config = null!;
        private ClaudeApiClient? claudeClient;
        private FishCollectionService fishService = null!;
        private CropCollectionService cropService = null!;
        private MineralCollectionService mineralService = null!;

        public override void Entry(IModHelper helper)
        {
            this.Config = this.Helper.ReadConfig<Config>();
            this.claudeClient = new ClaudeApiClient(this.Config.AnthropicApiKey);
            this.fishService = new FishCollectionService(this.Helper, this.Monitor);
            this.cropService = new CropCollectionService(this.Helper, this.Monitor);
            this.mineralService = new MineralCollectionService(this.Helper, this.Monitor);
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
            else if (e.Button == SButton.F8)
            {
                this.Monitor.Log("正在问 Claude...", LogLevel.Info);
                Task.Run(async () =>
                {
                    string context = GameSnapshotBuilder.Build(this.Helper);
                    string answer = await this.claudeClient!.AskAsync("我现在钓鱼进度怎么样？", context);
                    this.Monitor.Log($"Claude 回答: {answer}", LogLevel.Info);
                });
            }
            else if (e.Button == SButton.F9)
            {
                this.cropService.PrintMissingCrops();
            }
            else if (e.Button == SButton.F10)
            {
                this.mineralService.PrintMissingMinerals();
            }
        }
    }
}
