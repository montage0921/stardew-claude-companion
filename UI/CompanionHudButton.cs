using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using System;

namespace StardewClaudeCompanion
{
    // 屏幕右侧常驻的图标按钮，贴在原版 DayTimeMoneyBox(日历/时钟/金钱那块UI)下方，
    // 尺寸和位置参照游戏自带的任务(QuestLog)图标(44x46)，点击效果和打开逻辑一致。
    public class CompanionHudButton
    {
        private const int ButtonWidth = 44;
        private const int ButtonHeight = 46;
        private const float IconScale = 2.75f;

        private readonly ClickableTextureComponent button;
        private readonly SpriteFont? hoverFont;

        public CompanionHudButton(IModHelper helper)
        {
            this.button = new ClickableTextureComponent(
                new Rectangle(0, 0, ButtonWidth, ButtonHeight),
                Game1.mouseCursors,
                new Rectangle(640, 64, 16, 16), // 鱼类图标，和 CompanionMenu 标签栏用的是同一块贴图区域(已验证可见)
                2.75f);

            try
            {
                this.hoverFont = helper.ModContent.Load<SpriteFont>("assets/ChineseFont.xnb");
            }
            catch
            {
                this.hoverFont = null;
            }
        }

        private void Reposition()
        {
            // 贴在 DayTimeMoneyBox(位置固定为 uiViewport.Width - 300, 8, 宽300高284) 下方，右对齐，
            // 并和其下方的任务图标之间留出一段间距，避免两个图标挤在一起。
            int x = Game1.uiViewport.Width - 300 + 220;
            int y = 8 + 284 + 8 + ButtonHeight + 24;
            this.button.bounds.X = x;
            this.button.bounds.Y = y;
        }

        public void Draw(SpriteBatch b)
        {
            if (!Context.IsWorldReady || Game1.activeClickableMenu != null)
                return;

            this.Reposition();
            bool hovering = this.button.containsPoint(Game1.getMouseX(), Game1.getMouseY());

            IClickableMenu.drawTextureBox(b, this.button.bounds.X - 8, this.button.bounds.Y - 8, this.button.bounds.Width + 16, this.button.bounds.Height + 16, Color.White);

            float scale = hovering ? IconScale * 1.1f : IconScale;
            var origin = new Vector2(this.button.sourceRect.Width / 2f, this.button.sourceRect.Height / 2f);
            var center = new Vector2(this.button.bounds.X + this.button.bounds.Width / 2f, this.button.bounds.Y + this.button.bounds.Height / 2f);
            b.Draw(Game1.mouseCursors, center, this.button.sourceRect, Color.White, 0f, origin, scale, SpriteEffects.None, 0.9f);

            if (this.hoverFont != null && this.button.containsPoint(Game1.getMouseX(), Game1.getMouseY()))
            {
                const string label = "收集助手";
                var size = this.hoverFont.MeasureString(label);
                int x = Game1.getMouseX() - (int)size.X - 24;
                int y = Game1.getMouseY() - (int)size.Y - 8;

                b.Draw(Game1.staminaRect, new Rectangle(x - 8, y - 4, (int)size.X + 16, (int)size.Y + 8), Color.Black * 0.75f);
                b.DrawString(this.hoverFont, label, new Vector2(x, y), Color.White);
            }
        }

        public bool TryHandleClick(int x, int y)
        {
            if (!Context.IsWorldReady || Game1.activeClickableMenu != null)
                return false;

            // 点击事件可能先于本帧的 Draw() 触发，这里主动刷新一次坐标，
            // 避免用上一帧(或初始值 0,0)的 bounds 做命中判定。
            this.Reposition();

            if (this.button.containsPoint(x, y))
            {
                Game1.playSound("bigSelect");
                return true;
            }

            return false;
        }
    }
}
