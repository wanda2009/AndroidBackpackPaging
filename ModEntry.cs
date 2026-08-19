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
        private Texture2D backpackTexture;
        private const int UPGRADE_PRICE = 50000;

        public override void Entry(IModHelper helper)
        {
            helper.Events.Display.RenderedWorld += OnRenderedWorld;
            helper.Events.Display.RenderedActiveMenu += OnRenderedActiveMenu;
            helper.Events.Input.ButtonPressed += OnButtonPressed;

            // Load the original backpack.png texture
            try
            {
                backpackTexture = helper.ModContent.Load<Texture2D>("backpack.png");
            }
            catch (Exception ex)
            {
                Monitor.Log($"Could not load backpack.png: {ex.Message}", LogLevel.Warn);
                backpackTexture = null;
            }
        }

        private void OnRenderedWorld(object sender, RenderedWorldEventArgs e)
        {
            // Draw the green backpack resting flat on Pierre's counter
            if (Game1.currentLocation?.Name == "SeedShop" && Game1.player.MaxItems == 36)
            {
                Vector2 worldPos = new Vector2(7 * 64 + 12, 18 * 64 - 34);
                Vector2 screenPos = Game1.GlobalToLocal(Game1.viewport, worldPos);

                if (backpackTexture != null)
                {
                    e.SpriteBatch.Draw(
                        backpackTexture,
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
        }

        private void OnRenderedActiveMenu(object sender, RenderedActiveMenuEventArgs e)
        {
            // Compact 4-row layout to prevent overlapping character profile
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

                        // Align rows 1 to 3 compactly
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

                        // Draw locked Row 4 slots if not yet purchased
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
                // Interaction with the green backpack on Pierre's counter
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
                                new Response("Purchase", $"Purchase ({UPGRADE_PRICE:N0}g)"),
                                new Response("NotNow", "Not now")
                            };

                            // Dialogue box
                            Game1.currentLocation.createQuestionDialogue(
                                "Backpack Upgrade -- 48 slots",
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
                                            Game1.showGlobalMessage("Backpack Upgrade Complete! You now have 48 slots.");
                                        }
                                        else
                                        {
                                            Game1.drawObjectDialogue("You don't have enough money (Costs 50,000g).");
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
