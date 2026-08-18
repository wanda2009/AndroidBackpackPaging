using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;

namespace AndroidBackpackPaging
{
    public class ModEntry : Mod
    {
        private bool hasBoughtUpgrade = false;
        private int currentPage = 1;
        private List<Item> page2Items = new List<Item>();
        private Rectangle switchButtonBounds;
        private const int UPGRADE_PRICE = 50000;

        public override void Entry(IModHelper helper)
        {
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            helper.Events.GameLoop.Saving += OnSaving;
            helper.Events.Display.RenderedHud += OnRenderedHud;
            helper.Events.Input.ButtonPressed += OnButtonPressed;
            helper.Events.Display.MenuChanged += OnMenuChanged;
        }

        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            hasBoughtUpgrade = Helper.Data.ReadSaveData<bool>("HasBoughtBackpackPage2");
            var savedItems = Helper.Data.ReadSaveData<List<Item>>("BackpackPage2_Items");
            if (savedItems != null)
                page2Items = savedItems;

            switchButtonBounds = new Rectangle(20, 180, 70, 70);
            currentPage = 1;
        }

        private void OnSaving(object sender, SavingEventArgs e)
        {
            Helper.Data.WriteSaveData("HasBoughtBackpackPage2", hasBoughtUpgrade);
            Helper.Data.WriteSaveData("BackpackPage2_Items", page2Items);
        }

        private void OnMenuChanged(object sender, MenuChangedEventArgs e)
        {
            if (e.NewMenu is ShopMenu shop && shop.ShopId == "SeedShop")
            {
                if (Game1.player.MaxItems >= 36 && !hasBoughtUpgrade)
                {
                    var item = ItemRegistry.Create("(O)BackpackUpgrade", 1);
                    item.DisplayName = "Buku Tas Tambahan (Halaman 2 - 36 Slot)";
                    shop.forSale.Add(item);
                    shop.itemPriceAndStock.Add(item, new ItemStockInformation(UPGRADE_PRICE, 1));
                }
            }
        }

        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady) return;

            if (Game1.activeClickableMenu is ShopMenu shop && shop.ShopId == "SeedShop")
            {
                if (shop.heldItem != null && shop.heldItem.DisplayName.Contains("Buku Tas Tambahan"))
                {
                    hasBoughtUpgrade = true;
                    Game1.player.Money -= UPGRADE_PRICE;
                    shop.heldItem = null;
                    Game1.playSound("reward");
                    Game1.showGlobalMessage("Berhasil membeli Tas Halaman 2!");
                    return;
                }
            }

            if (hasBoughtUpgrade && (e.Button == SButton.MouseLeft || e.Button == SButton.Android_Tap))
            {
                Point touchPos = Game1.getMousePosition();
                if (switchButtonBounds.Contains(touchPos) && Game1.activeClickableMenu == null)
                {
                    Helper.Input.Suppress(e.Button);
                    SwapInventoryPages();
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
                if (i < page2Items.Count && page2Items[i] != null)
                    Game1.player.Items.Add(page2Items[i]);
                else
                    Game1.player.Items.Add(null);
            }

            page2Items = currentActiveItems;
            currentPage = (currentPage == 1) ? 2 : 1;
            Game1.showGlobalMessage($"Tas Aktif: Halaman {currentPage}");
        }

        private void OnRenderedHud(object sender, RenderedHudEventArgs e)
        {
            if (Context.IsWorldReady && hasBoughtUpgrade && Game1.activeClickableMenu == null)
            {
                e.SpriteBatch.Draw(Game1.staminaRect, switchButtonBounds, Color.Black * 0.6f);
                string text = $"P{currentPage}";
                Vector2 textSize = Game1.smallFont.MeasureString(text);
                Vector2 textPos = new Vector2(
                    switchButtonBounds.X + (switchButtonBounds.Width - textSize.X) / 2,
                    switchButtonBounds.Y + (switchButtonBounds.Height - textSize.Y) / 2
                );
                e.SpriteBatch.DrawString(Game1.smallFont, text, textPos, Color.Gold);
            }
        }
    }
}
