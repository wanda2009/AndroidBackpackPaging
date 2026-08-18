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
        private Texture2D customBackpackTexture;
        private const int UPGRADE_PRICE = 50000;

        public override void Entry(IModHelper helper)
        {
            helper.Events.Display.RenderedWorld += OnRenderedWorld;
            helper.Events.Display.RenderedActiveMenu += OnRenderedActiveMenu;
            helper.Events.Input.ButtonPressed += OnButtonPressed;

            try
            {
                customBackpackTexture = helper.ModContent.Load<Texture2D>("backpack.png");
            }
            catch
            {
                customBackpackTexture = null;
            }
        }

        private void OnRenderedWorld(object sender, RenderedWorldEventArgs e)
        {
            // Tampilkan TAS HIJAU di meja Pierre jika pemain masih punya 36 slot (belum 48 slot)
            if (Game1.currentLocation?.Name == "SeedShop" && Game1.player.MaxItems == 36)
            {
                Vector2 worldPos = new Vector2(7 * 64 + 10, 18 * 64 - 36);
                Vector2 screenPos = Game1.GlobalToLocal(Game1.viewport, worldPos);

                if (customBackpackTexture != null)
                {
                    e.SpriteBatch.Draw(
                        customBackpackTexture,
                        screenPos,
                        null,
                        Color.White,
                        0f,
                        Vector2.Zero,
                        4f,
                        SpriteEffects.None,
                        0.86f
                    );
                }
                else
                {
                    e.SpriteBatch.Draw(
                        Game1.mouseCursors,
                        screenPos,
                        new Rectangle(257, 1436, 11, 13),
                        new Color(60, 240, 80),
                        0f,
                        Vector2.Zero,
                        4f,
                        SpriteEffects.None,
                        0.86f
                    );
                }
            }
        }

        private void OnRenderedActiveMenu(object sender, RenderedActiveMenuEventArgs e)
        {
            // GAMBAR BARIS KE-4 YANG GELAP & TERKUNCI DI MENU TAS (Sebelum dibeli)
            if (Game1.activeClickableMenu is GameMenu gameMenu && gameMenu.currentTab == 0)
            {
                if (Game1.player.MaxItems == 36)
                {
                    if (gameMenu.GetCurrentPage() is InventoryPage invPage)
                    {
                        var invMenu = invPage.inventory;
                        if (invMenu != null && invMenu.inventory.Count >= 36)
                        {
                            // Hitung jarak vertikal antar baris slot
                            int rowHeight = invMenu.inventory[12].bounds.Y - invMenu.inventory[0].bounds.Y;

                            // Gambar 12 slot baris ke-4 dengan warna gelap/transparan
                            for (int i = 0; i < 12; i++)
                            {
                                var row3Slot = invMenu.inventory[24 + i];
                                Rectangle row4SlotBounds = new Rectangle(
                                    row3Slot.bounds.X,
                                    row3Slot.bounds.Y + rowHeight,
                                    row3Slot.bounds.Width,
                                    row3Slot.bounds.Height
                                );

                                // Kotak slot baris ke-4 gelap (Terkunci)
                                e.SpriteBatch.Draw(
                                    Game1.menuTexture,
                                    row4SlotBounds,
                                    new Rectangle(128, 128, 64, 64),
                                    Color.Black * 0.45f
                                );
                            }
                        }
                    }
                }
            }
        }

        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady) return;

            if (e.Button == SButton.MouseLeft)
            {
                // Beli tas 48 slot saat memencet tas hijau di meja Pierre
                if (Game1.currentLocation?.Name == "SeedShop" && Game1.player.MaxItems == 36)
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

                                            // BUKA & AKTIFKAN BARIS KE-4 (48 SLOT)
                                            farmer.MaxItems = 48;

                                            Game1.playSound("reward");
                                            Game1.showGlobalMessage("Peningkatan Tas Selesai! Baris ke-4 (48 Slot) berhasil terbuka.");
                                        }
                                        else
                                        {
                                            Game1.drawObjectDialogue("Uangmu tidak cukup (Butuh 50.000g).");
                                        }
                                    }
                                })
                            );
                        }
                    }
                }
            }
        }
    }
}
