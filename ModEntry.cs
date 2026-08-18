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
            if (Game1.activeClickableMenu is GameMenu gameMenu && gameMenu.currentTab == 0)
            {
                if (gameMenu.GetCurrentPage() is InventoryPage invPage)
                {
                    var invMenu = invPage.inventory;
                    if (invMenu != null && invMenu.inventory.Count >= 36)
                    {
                        int startY = invMenu.yPositionOnScreen - 8;
                        int slotW = invMenu.inventory[0].bounds.Width;
                        int slotH = 50;
                        int stepY = 52;

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

                        if (Game1.player.MaxItems == 36)
                        {
                            int row4Y = startY + (3 * stepY);

                            for (int c = 0; c < 12; c++)
                            {
                                int slotX = invMenu.inventory[c].bounds.X;
                                Rectangle row4Slot = new Rectangle(slotX, row4Y, slotW, slotH);

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
