using System;
using System.IO;
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
            helper.Events.Display.MenuChanged += OnMenuChanged;
            helper.Events.Display.RenderedActiveMenu += OnRenderedActiveMenu;
            helper.Events.Input.ButtonPressed += OnButtonPressed;

            try
            {
                string imagePath = Path.Combine(helper.DirectoryPath, "backpack.png");
                if (File.Exists(imagePath))
                {
                    using (FileStream stream = File.OpenRead(imagePath))
                    {
                        backpackTexture = Texture2D.FromStream(Game1.graphics.GraphicsDevice, stream);
                    }
                }
                else
                {
                    backpackTexture = helper.ModContent.Load<Texture2D>("backpack.png");
                }
            }
            catch
            {
                backpackTexture = null;
            }
        }

        private void OnRenderedWorld(object sender, RenderedWorldEventArgs e)
        {
            if (Game1.currentLocation?.Name == "SeedShop" && Game1.player.MaxItems == 36 && backpackTexture != null)
            {
                Vector2 worldPos = new Vector2(7 * 64 + 14, 18 * 64 - 56);
                Vector2 screenPos = Game1.GlobalToLocal(Game1.viewport, worldPos);

                e.SpriteBatch.Draw(
                    backpackTexture,
                    screenPos,
                    null,
                    Color.White,
                    0f,
                    Vector2.Zero,
                    3.0f,
                    SpriteEffects.None,
                    0.86f
                );
            }
        }

        private void OnMenuChanged(object sender, MenuChangedEventArgs e)
        {
            FormatInventoryGridEvenly(e.NewMenu);
        }

        private void OnRenderedActiveMenu(object sender, RenderedActiveMenuEventArgs e)
        {
            FormatInventoryGridEvenly(Game1.activeClickableMenu);

            if (Game1.activeClickableMenu is GameMenu gameMenu && gameMenu.currentTab == 0 && Game1.player.MaxItems == 36)
            {
                if (gameMenu.GetCurrentPage() is InventoryPage invPage && invPage.inventory != null)
                {
                    var invMenu = invPage.inventory;
                    if (invMenu.inventory.Count >= 48)
                    {
                        for (int i = 36; i < 48; i++)
                        {
                            var slot = invMenu.inventory[i];
                            
                            IClickableMenu.drawTextureBox(
                                e.SpriteBatch,
                                Game1.menuTexture,
                                new Rectangle(0, 256, 60, 60),
                                slot.bounds.X,
                                slot.bounds.Y,
                                slot.bounds.Width,
                                slot.bounds.Height,
                                Color.DimGray * 0.40f,
                                1f,
                                false
                            );
                        }
                    }
                }
            }
        }

        private void FormatInventoryGridEvenly(IClickableMenu menu)
        {
            if (menu is GameMenu gameMenu)
            {
                if (gameMenu.GetCurrentPage() is InventoryPage invPage)
                {
                    var invMenu = invPage.inventory;
                    if (invMenu != null)
                    {
                        int slotSize = 48;
                        int gapX = 4;
                        int gapY = 9;
                        int stepY = slotSize + gapY;

                        int totalWidth = 12 * (slotSize + gapX) - gapX;
                        int startX = invMenu.xPositionOnScreen + (invMenu.width - totalWidth) / 2;
                        int startY = invMenu.yPositionOnScreen - 2;

                        for (int r = 0; r < 3; r++)
                        {
                            for (int c = 0; c < 12; c++)
                            {
                                int idx = r * 12 + c;
                                if (idx < invMenu.inventory.Count)
                                {
                                    invMenu.inventory[idx].bounds = new Rectangle(
                                        startX + c * (slotSize + gapX),
                                        startY + (r * stepY),
                                        slotSize,
                                        slotSize
                                    );
                                }
                            }
                        }

                        if (invMenu.inventory.Count == 36)
                        {
                            invMenu.capacity = 48;
                            invMenu.rows = 4;

                            int row4Y = startY + (3 * stepY);

                            for (int c = 0; c < 12; c++)
                            {
                                int slotX = startX + c * (slotSize + gapX);
                                Rectangle row4Bounds = new Rectangle(slotX, row4Y, slotSize, slotSize);
                                invMenu.inventory.Add(new ClickableComponent(row4Bounds, (36 + c).ToString()));
                            }
                        }
                        else if (invMenu.inventory.Count >= 48)
                        {
                            int row4Y = startY + (3 * stepY);
                            for (int c = 0; c < 12; c++)
                            {
                                int idx = 36 + c;
                                if (idx < invMenu.inventory.Count)
                                {
                                    invMenu.inventory[idx].bounds = new Rectangle(
                                        startX + c * (slotSize + gapX),
                                        row4Y,
                                        slotSize,
                                        slotSize
                                    );
                                }
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
                if (Game1.activeClickableMenu is GameMenu gameMenu && gameMenu.currentTab == 0 && Game1.player.MaxItems == 36)
                {
                    if (gameMenu.GetCurrentPage() is InventoryPage invPage && invPage.inventory != null)
                    {
                        Point touchPos = Game1.getMousePosition();
                        for (int i = 36; i < invPage.inventory.inventory.Count; i++)
                        {
                            if (invPage.inventory.inventory[i].bounds.Contains(touchPos))
                            {
                                Helper.Input.Suppress(e.Button);
                                Game1.playSound("cancel");
                                return;
                            }
                        }
                    }
                }

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
