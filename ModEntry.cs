using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace AndroidBackpackPaging
{
    // Kelas data penyimpanan khusus untuk SMAPI
    public class ModSaveData
    {
        public bool HasBoughtBackpackPage2 { get; set; } = false;
        public List<Item> BackpackPage2_Items { get; set; } = new List<Item>();
    }

    public class ModEntry : Mod
    {
        private ModSaveData saveData = new ModSaveData();
        private int currentPage = 1;
        private Rectangle switchButtonBounds;
        private const int UPGRADE_PRICE = 50000;

        public override void Entry(IModHelper helper)
        {
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            helper.Events.GameLoop.Saving += OnSaving;
            helper.Events.Display.RenderedHud += OnRenderedHud;
            helper.Events.Input.ButtonPressed += OnButtonPressed;
        }

        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            saveData = Helper.Data.ReadSaveData<ModSaveData>("AndroidBackpackData") ?? new ModSaveData();
            switchButtonBounds = new Rectangle(20, 180, 75, 75);
            currentPage = 1;
        }

        private void OnSaving(object sender, SavingEventArgs e)
        {
            Helper.Data.WriteSaveData("AndroidBackpackData", saveData);
        }

        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady) return;

            if (e.Button == SButton.MouseLeft)
            {
                Point touchPos = Game1.getMousePosition();

                if (switchButtonBounds.Contains(touchPos) && Game1.activeClickableMenu == null)
                {
                    Helper.Input.Suppress(e.Button);

                    if (!saveData.HasBoughtBackpackPage2)
                    {
                        if (Game1.player.MaxItems < 36)
                        {
                            Game1.drawObjectDialogue("Kamu harus memiliki Tas 36 Slot (Deluxe) terlebih dahulu.");
                            return;
                        }

                        if (Game1.player.Money >= UPGRADE_PRICE)
                        {
                            Game1.player.Money -= UPGRADE_PRICE;
                            saveData.HasBoughtBackpackPage2 = true;
                            Game1.playSound("reward");
                            Game1.showGlobalMessage("Selamat! Tas Halaman 2 (36 Slot) berhasil dibeli!");
                        }
                        else
                        {
                            Game1.drawObjectDialogue($"Uangmu tidak cukup untuk membeli Tas Halaman 2 (Harga: {UPGRADE_PRICE:N0}g).");
                        }
                    }
                    else
                    {
                        SwapInventoryPages();
                    }
                }
            }
        }

        private void SwapInventoryPages()
        {
            Game1.playSound("shwip");
            List<Item> currentActiveItems = new List<Item>(Game1.player.Items);

            Game1.player.Items.Clear();
            for (int i = 0; i < 36; i++)
            {
                if (i < saveData.BackpackPage2_Items.Count && saveData.BackpackPage2_Items[i] != null)
                    Game1.player.Items.Add(saveData.BackpackPage2_Items[i]);
                else
                    Game1.player.Items.Add(null);
            }

            saveData.BackpackPage2_Items = currentActiveItems;
            currentPage = (currentPage == 1) ? 2 : 1;
            Game1.showGlobalMessage($"Tas Aktif: Halaman {currentPage}");
        }

        private void OnRenderedHud(object sender, RenderedHudEventArgs e)
        {
            if (Context.IsWorldReady && Game1.activeClickableMenu == null)
            {
                e.SpriteBatch.Draw(Game1.staminaRect, switchButtonBounds, Color.Black * 0.6f);

                string text = saveData.HasBoughtBackpackPage2 ? $"P{currentPage}" : "BUY";
                Color textColor = saveData.HasBoughtBackpackPage2 ? Color.Gold : Color.LightGray;

                Vector2 textSize = Game1.smallFont.MeasureString(text);
                Vector2 textPos = new Vector2(
                    switchButtonBounds.X + (switchButtonBounds.Width - textSize.X) / 2,
                    switchButtonBounds.Y + (switchButtonBounds.Height - textSize.Y) / 2
                );

                e.SpriteBatch.DrawString(Game1.smallFont, text, textPos, textColor);
            }
        }
    }
}
