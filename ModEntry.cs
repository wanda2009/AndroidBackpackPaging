using System;
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
        }

        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            hasBoughtUpgrade = Helper.Data.ReadSaveData<bool>("HasBoughtBackpackPage2");
            var savedItems = Helper.Data.ReadSaveData<List<Item>>("BackpackPage2_Items");
            page2Items = savedItems ?? new List<Item>();

            // Posisi tombol [P1/P2] di layar HP
            switchButtonBounds = new Rectangle(20, 180, 75, 75);
            currentPage = 1;
        }

        private void OnSaving(object sender, SavingEventArgs e)
        {
            Helper.Data.WriteSaveData("HasBoughtBackpackPage2", hasBoughtUpgrade);
            Helper.Data.WriteSaveData("BackpackPage2_Items", page2Items);
        }

        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady) return;

            // Di Android SMAPI, sentuhan layar dibaca sebagai MouseLeft
            if (e.Button == SButton.MouseLeft)
            {
                Point touchPos = Game1.getMousePosition();

                if (switchButtonBounds.Contains(touchPos) && Game1.activeClickableMenu == null)
                {
                    Helper.Input.Suppress(e.Button);

                    // Jika BELUM dibeli, munculkan dialog pembelian
                    if (!hasBoughtUpgrade)
                    {
                        if (Game1.player.MaxItems < 36)
                        {
                            Game1.drawObjectDialogue("Kamu harus memiliki Tas 36 Slot (Deluxe) terlebih dahulu sebelum membeli halaman tambahan.");
                            return;
                        }

                        var responses = new Response[]
                        {
                            new Response("Yes", $"Beli seharga {UPGRADE_PRICE:N0}g"),
                            new Response("No", "Batal")
                        };

                        Game1.currentLocation.createQuestionDialogue(
                            $"Buka Tas Tambahan Halaman 2 (36 Slot Ekstra)?",
                            responses,
                            (farmer, answer) =>
                            {
                                if (answer == "Yes")
                                {
                                    if (farmer.Money >= UPGRADE_PRICE)
                                    {
                                        farmer.Money -= UPGRADE_PRICE;
                                        hasBoughtUpgrade = true;
                                        Game1.playSound("reward");
                                        Game1.showGlobalMessage("Selamat! Tas Halaman 2 berhasil diaktifkan!");
                                    }
                                    else
                                    {
                                        Game1.drawObjectDialogue("Uangmu tidak cukup untuk membeli tas ini.");
                                    }
                                }
                            }
                        );
                    }
                    else
                    {
                        // Jika SUDAH dibeli, ganti halaman tas
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
            if (Context.IsWorldReady && Game1.activeClickableMenu == null)
            {
                // Gambar tombol kotak hitam transparan di layar HP
                e.SpriteBatch.Draw(Game1.staminaRect, switchButtonBounds, Color.Black * 0.6f);

                string text = hasBoughtUpgrade ? $"P{currentPage}" : "BUY";
                Color textColor = hasBoughtUpgrade ? Color.Gold : Color.LightGray;

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
