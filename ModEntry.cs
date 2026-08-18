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

            greenBackpackTexture = CreateGreenBackpackTexture();
        }

        // Generator Pixel Art Tas Hijau Pierre
        private Texture2D CreateGreenBackpackTexture()
        {
            Texture2D tex = new Texture2D(Game1.graphics.GraphicsDevice, 12, 14);
            Color G = new Color(80, 210, 60);
            Color D = new Color(25, 90, 18);
            Color Y = new Color(255, 220, 0);
            Color O = new Color(230, 120, 30);
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
            // Gambar TAS HIJAU di atas meja Pierre
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
            // RAMPATKAN 4 BARIS AGAR MENYATU RAPI DI DALAM MENU ATAS
            if (Game1.activeClickableMenu is GameMenu gameMenu && gameMenu.currentTab == 0)
            {
                if (gameMenu.GetCurrentPage() is InventoryPage invPage)
                {
                    var invMenu = invPage.inventory;
                    if (invMenu != null && invMenu.inventory.Count >= 36)
                    {
                        int startX = invMenu.inventory[0].bounds.X;
                        int startY = invMenu.yPositionOnScreen - 8; // Geser sedikit ke atas
                        int slotW = invMenu.inventory[0].bounds.Width;
                        int slotH = 50;  // Tinggi slot dibuat lebih ramping
                        int stepY = 52;  // Jarak antar baris rapat

                        // 1. Reposisi baris 1, 2, 3 agar lebih rapat dan muat 4 baris
                        for (int r = 0; r < 3; r++)
                        {
                            for (int c = 0; c < 12; c++)
                            {
                                int idx = r * 12 + c;
                                if (idx < invMenu.inventory.Count)
                                {
                                    invMenu.inventory[idx].bounds = new Rectangle(
                                        invMenu.inventory[idx].bounds.X,
                                        startY + (r * stepY),
                                        slotW,
                                        slotH
                                    );
                                }
                            }
                        }

                        // 2. Gambar baris ke-4 menyatu di bawah baris ke-3 (Sebelum dibeli = Gelap)
                        if (Game1.player.MaxItems == 36)
                        {
                            int row4Y = startY + (3 * stepY);

                            for (int c = 0; c < 12; c++)
                            {
                                int slotX = invMenu.inventory[c].bounds.X;
                                Rectangle row4Slot = new Rectangle(slotX, row4Y, slotW, slotH);

                                // Gambar slot baris ke-4 terkunci (menyatu dalam menu krem)
                                e.SpriteBatch.Draw(
                                    Game1.menuTexture,
                                    row4Slot,
                                    new Rectangle(128, 128, 64, 64),
                                    Color.Black * 0.38f
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
                // Beli tas 48 slot di meja Pierre
                if (Game1.currentLocation?.Name == "SeedShop" && Game1.player.MaxItems == 36)
                {
