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
        private Texture2D greenBackpackTexture;
        private const int UPGRADE_PRICE = 50000;

        public override void Entry(IModHelper helper)
        {
            helper.Events.Display.RenderedWorld += OnRenderedWorld;
            helper.Events.Display.RenderedActiveMenu += OnRenderedActiveMenu;
            helper.Events.Input.ButtonPressed += OnButtonPressed;

            // Buat Sprite Tas Hijau Terang Murni Otomatis
            greenBackpackTexture = CreateGreenBackpackTexture();
        }

        // Generator Pixel Art Tas Hijau Asli (Dijamin Hijau Cerah)
        private Texture2D CreateGreenBackpackTexture()
        {
            Texture2D tex = new Texture2D(Game1.graphics.GraphicsDevice, 12, 14);
            Color G = new Color(80, 210, 60);   // Hijau Terang Cerah
            Color D = new Color(25, 90, 18);    // Hijau Tua (Garis Tepi)
            Color Y = new Color(255, 220, 0);   // Emas / Gesper
            Color O = new Color(230, 120, 30);  // Aksen Tali
            Color _ = Color.Transparent;

            Color[] pixels = new Color[]
            {
                _, _, D, D, _, _, _, _, D, D, _, _,
                _, D, G, G, D, _, _, D, G, G, D, _,
                D, G, G, G, G, D, D, G, G, G, G, D,
                D, G, G, G, G, G, G, G, G, G, G, D,
                D, G, G, G, G, G, G, G, G, G, G, D,
                D, G, G, G, G, G, G, G, G, G, G, D,
                D, D, D, D, D, D, D, D, D, D, D, D,
                D, G, G, G, G, Y, Y, G, G, G, G, D,
                D, G, G, G, G, Y, O, G, G, G, G, D,
                D, G, G, G, G, O, O, G, G, G, G, D,
                D, G, G, G, G, G, G, G, G, G, G, D,
                D, G, G, G, G, G, G, G, G, G, G, D,
                _, D, G, G, G, G, G, G, G, G, D, _,
                _, _, D, D, D, D, D, D, D, D, _, _
            };

            tex.SetData(pixels);
            return tex;
        }

        private void OnRenderedWorld(object sender, RenderedWorldEventArgs e)
        {
            // Gambar TAS HIJAU CERAH di atas meja kasir Pierre
            if (Game1.currentLocation?.Name == "SeedShop" && Game1.player.MaxItems == 36)
            {
                Vector2 worldPos = new Vector2(7 * 64 + 10, 18 * 64 - 36);
                Vector2 screenPos = Game1.GlobalToLocal(Game1.viewport, worldPos);

                e.SpriteBatch.Draw(
                    greenBackpackTexture,
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
        }

        private void OnRenderedActiveMenu(object sender, RenderedActiveMenuEventArgs e)
        {
            // GAMBAR BARIS KE-4 YANG RAPI & TIDAK MENABRAK PROFIL
            if (Game1.activeClickableMenu is GameMenu gameMenu && gameMenu.currentTab == 0)
            {
                if (Game1.player.MaxItems == 36)
                {
                    if (gameMenu.GetCurrentPage() is InventoryPage invPage)
                    {
                        var invMenu = invPage.inventory;
                        if (invMenu != null && invMenu.inventory.Count >= 36)
                        {
                            int rowHeight = invMenu.inventory[12].bounds.Y - invMenu.inventory[0].bounds.Y;
                            
                            // Offset disesuaikan agar posisinya rapat dan tidak menabrak teks nama karakter di bawah
                            int offsetY = rowHeight - 6;

                            // 1. Gambar background kotak krem agar menyatu dengan menu atas
                            var firstSlot = invMenu.inventory[24];
                            var lastSlot = invMenu.inventory[35];
                            Rectangle bgPanel = new Rectangle(
                                firstSlot.bounds.X - 8,
                                firstSlot.bounds.Y + offsetY - 4,
                                (lastSlot.bounds.X + lastSlot.bounds.Width) - firstSlot.bounds.X + 16,
                                rowHeight + 8
                            );

                            IClickableMenu.drawTextureBox(
                                e.SpriteBatch,
                                Game1.menuTexture,
                                new Rectangle(0, 256, 60, 60),
                                bgPanel.X,
                                bgPanel.Y,
                                bgPanel.Width,
                                bgPanel.Height,
                                new Color(245, 207, 148), // Warna krem menu Stardew
                                1f,
                                false
                            );

                            // 2. Gambar 12 slot baris ke-4 terkunci/gelap
                            for (int i = 0; i < 12; i++)
                            {
                                var row3Slot = invMenu.inventory[24 + i];
                                Rectangle row4Slot = new Rectangle(
                                    row3Slot.bounds.X,
                                    row3Slot.bounds.Y + offsetY,
                                    row3Slot.bounds.Width,
                                    row3Slot.bounds.Height
                                );

                                e.SpriteBatch.Draw(
                                    Game1.menuTexture,
                                    row4Slot,
                                    new Rectangle(128, 128, 64, 64),
                                    Color.Black * 0.40f
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

                                            // AKTIFKAN 48 SLOT PENUH
                                            farmer.MaxItems = 48;

                                            Game1.playSound("reward");
                                            Game1.showGlobalMessage("Peningkatan Tas Selesai! Tas kamu sekarang 4 Baris (48 Slot).");
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
