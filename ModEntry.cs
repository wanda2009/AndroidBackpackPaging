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
            helper.Events.Display.RenderingWorld += OnRenderingWorld;
            helper.Events.Display.MenuChanged += OnMenuChanged;
            helper.Events.Display.RenderedActiveMenu += OnRenderedActiveMenu;
            helper.Events.Input.ButtonPressed += OnButtonPressed;

            // Muat gambar backpack.png asli
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

        private void OnRenderingWorld(object sender, RenderingWorldEventArgs e)
        {
            // GAMBAR TAS DI MEJA DENGAN KEDALAMAN YANG BENAR (Di belakang badan player)
            if (Game1.currentLocation?.Name == "SeedShop" && Game1.player.MaxItems == 36 && backpackTexture != null)
            {
                Vector2 worldPos = new Vector2(7 * 64 + 14, 18 * 64 - 42);
                Vector2 screenPos = Game1.GlobalToLocal(Game1.viewport, worldPos);

                // Menggunakan layerDepth meja (0.118f) agar karakter di Y=19 otomatis berada di depannya
                e.SpriteBatch.Draw(
                    backpackTexture,
                    screenPos,
                    null,
                    Color.White,
                    0f,
                    Vector2.Zero,
                    3.2f,
                    SpriteEffects.None,
                    (18f * 64f + 20f) / 10000f
                );
            }
        }

        private void OnMenuChanged(object sender, MenuChangedEventArgs e)
        {
            // BUKA 4 BARIS (48 KOTAK) DI MENU TAS SAAT SUDAH BELI 48 SLOT
            if (e.NewMenu is GameMenu gameMenu && Game1.player.MaxItems == 48)
            {
                if (gameMenu.GetCurrentPage() is InventoryPage invPage)
                {
                    var invMenu = invPage.inventory;
                    if (invMenu != null && invMenu.inventory.Count == 36)
                    {
                        invMenu.capacity = 48;
                        invMenu.rows = 4;

                        int rowHeight = invMenu.inventory[12].bounds.Y - invMenu.inventory[0].bounds.Y;

                        // Tambahkan 12 komponen kotak baris ke-4 ke dalam menu
                        for (int i = 0; i < 12; i++)
                        {
                            var row3Slot = invMenu.inventory[24 + i];
                            Rectangle row4SlotBounds = new Rectangle(
                                row3Slot.bounds.X,
                                row3Slot.bounds.Y + rowHeight,
                                row3Slot.bounds.Width,
                                row3Slot.bounds.Height
                            );

                            invMenu.inventory.Add(new ClickableComponent(row4SlotBounds, (36 + i).ToString()));
                        }
                    }
                }
            }
        }

        private void OnRenderedActiveMenu(object sender, RenderedActiveMenuEventArgs e)
        {
            // Pastikan baris ke-4 tetap aktif terisi jika menu di-render ulang
            if (Game1.activeClickableMenu is GameMenu gameMenu && Game1.player.MaxItems == 48)
            {
                if (gameMenu.GetCurrentPage() is InventoryPage invPage)
                {
                    var invMenu = invPage.inventory;
                    if (invMenu != null && invMenu.inventory.Count == 36)
                    {
                        invMenu.capacity = 48;
                        invMenu.rows = 4;

                        int rowHeight = invMenu.inventory[12].bounds.Y - invMenu.inventory[0].bounds.Y;

                        for (int i = 0; i < 12; i++)
                        {
                            var row3Slot = invMenu.inventory[24 + i];
                            Rectangle row4SlotBounds = new Rectangle(
                                row3Slot.bounds.X,
                                row3Slot.bounds.Y + rowHeight,
                                row3Slot.bounds.Width,
                                row3Slot.bounds.Height
                            );

                            invMenu.inventory.Add(new ClickableComponent(row4SlotBounds, (36 + i).ToString()));
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
                                            farmer.MaxItems = 48; // Buka 48 slot

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
