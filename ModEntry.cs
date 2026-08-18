using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace AndroidBackpackPaging
{
    public class ModSaveData
    {
        public bool HasBoughtBackpack48 { get; set; } = false;
        public List<Item> BackpackMain_Items { get; set; } = new List<Item>();
        public List<Item> BackpackExtra12_Items { get; set; } = new List<Item>();
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
            helper.Events.Display.RenderedWorld += OnRenderedWorld;
            helper.Events.Input.ButtonPressed += OnButtonPressed;
        }

        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            saveData = Helper.Data.ReadSaveData<ModSaveData>("AndroidBackpack48Data") ?? new ModSaveData();
            switchButtonBounds = new Rectangle(85, 20, 65, 65);
            currentPage = 1;
        }

        private void OnSaving(object sender, SavingEventArgs e)
        {
            Helper.Data.WriteSaveData("AndroidBackpack48Data", saveData);
        }

        private void OnRenderedWorld(object sender, RenderedWorldEventArgs e)
        {
            // Gambar TAS HIJAU 48 SLOT di meja Pierre jika belum dibeli
            if (Game1.currentLocation?.Name == "SeedShop" && Game1.player.MaxItems >= 36 && !saveData.HasBoughtBackpack48)
            {
                Vector2 worldPos = new Vector2(7 * 64 + 10, 18 * 64 - 12);
                Vector2 screenPos = Game1.GlobalToLocal(Game1.viewport, worldPos);

                e.SpriteBatch.Draw(
                    Game1.mouseCursors,
                    screenPos,
                    new Rectangle(257, 1436, 11, 13),
                    new Color(50, 220, 80), // Warna Tas Hijau
                    0f,
                    Vector2.Zero,
                    4.2f,
                    SpriteEffects.None,
                    0.85f
                );
            }
        }

        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady) return;

            if (e.Button == SButton.MouseLeft)
            {
                // 1. Klik Tas Hijau di Meja Kasir Pierre untuk beli 48 Slot
                if (Game1.currentLocation?.Name == "SeedShop" && Game1.player.MaxItems >= 36 && !saveData.HasBoughtBackpack48)
                {
                    Vector2 clickedTile = e.Cursor.Tile;

                    if (clickedTile.X == 7 && (clickedTile.Y == 18 || clickedTile.Y == 17))
                    {
                        if (Vector2.Distance(Game1.player.Tile, new Vector2(7, 18)) <= 3.5f)
                        {
                            Helper.Input.Suppress(e.Button);

                            var responses = new Response[]
                            {
                                new Response("Purchase", $"Beli ({UPGRADE_PRICE:N0}g)"),
                                new Response("NotNow", "Nanti saja")
                            };

                            // Kotak dialog persis foto
                            Game1.currentLocation.createQuestionDialogue(
                                "Peningkatan Tas -- 48 slot",
                                responses,
                                new GameLocation.afterQuestionBehavior((farmer, answer) =>
                                {
                                    if (answer == "Purchase")
                                    {
                                        if (farmer.Money >= UPGRADE_PRICE)
                                        {
                                            farmer.Money -= UPGRADE_PRICE;
                                            saveData.HasBoughtBackpack48 = true;
                                            Game1.playSound("reward");
                                            Game1.showGlobalMessage("Peningkatan Tas Selesai! Kamu sekarang memiliki 48 Slot.");
                                        }
                                        else
                                        {
                                            Game1.drawObjectDialogue("Uangmu tidak cukup.");
                                        }
                                    }
                                })
                            );
                            return;
                        }
                    }
                }

                // 2. Tombol di layar untuk menukar tas (36 Utama <-> 12 Ekstra)
                if (saveData.HasBoughtBackpack48)
                {
                    Point touchPos = Game1.getMousePosition();
                    if (switchButtonBounds.Contains(touchPos) && Game1.activeClickableMenu == null)
                    {
                        Helper.Input.Suppress(e.Button);
                        SwapInventory48();
                    }
                }
            }
        }

        private void SwapInventory48()
        {
            Game1.playSound("shwip");

            if (currentPage == 1)
            {
                // Simpan 36 Slot Utama
                saveData.BackpackMain_Items = new List<Item>(Game1.player.Items);

                // Muat 12 Slot Tambahan (Slot 37-48)
                Game1.player.Items.Clear();
                for (int i = 0; i < 12; i++)
                {
                    if (i < saveData.BackpackExtra12_Items.Count && saveData.BackpackExtra12_Items[i] != null)
                        Game1.player.Items.Add(saveData.BackpackExtra12_Items[i]);
                    else
                        Game1.player.Items.Add(null);
                }

                currentPage = 2;
                Game1.showGlobalMessage("Tas Ekstra: 12 Slot Tambahan (Slot 37 - 48)");
            }
            else
            {
                // Simpan 12 Slot Tambahan
                saveData.BackpackExtra12_Items = new List<Item>();
                for (int i = 0; i < 12; i++)
                {
                    if (i < Game1.player.Items.Count)
                        saveData.BackpackExtra12_Items.Add(Game1.player.Items[i]);
                    else
                        saveData.BackpackExtra12_Items.Add(null);
                }

                // Kembalikan 36 Slot Utama
                Game1.player.Items.Clear();
                for (int i = 0; i < 36; i++)
                {
                    if (i < saveData.BackpackMain_Items.Count && saveData.BackpackMain_Items[i] != null)
                        Game1.player.Items.Add(saveData.BackpackMain_Items[i]);
                    else
                        Game1.player.Items.Add(null);
                }

                currentPage = 1;
                Game1.showGlobalMessage("Tas Utama: 36 Slot (Slot 1 - 36)");
            }
        }

        private void OnRenderedHud(object sender, RenderedHudEventArgs e)
        {
            // Tombol [P1] / [+12] hanya muncul setelah tas 48 slot dibeli
            if (Context.IsWorldReady && saveData.HasBoughtBackpack48 && Game1.activeClickableMenu == null)
            {
                e.SpriteBatch.Draw(Game1.staminaRect, switchButtonBounds, Color.Black * 0.65f);

                string text = (currentPage == 1) ? "P1" : "+12";
                Color textColor = (currentPage == 1) ? Color.Gold : Color.LimeGreen;

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
