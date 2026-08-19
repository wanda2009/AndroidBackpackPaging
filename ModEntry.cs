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
        
        private int rowOffset = 0; // 0 = Baris 1-3, 1 = Baris 2-4
        private int touchStartY = -1;
        private bool isSwiping = false;

        public override void Entry(IModHelper helper)
        {
            helper.Events.Display.RenderedWorld += OnRenderedWorld;
            helper.Events.Display.MenuChanged += OnMenuChanged;
            helper.Events.Display.RenderedActiveMenu += OnRenderedActiveMenu;
            helper.Events.Input.ButtonPressed += OnButtonPressed;
            helper.Events.Input.ButtonReleased += OnButtonReleased;

            // Muat file gambar asli backpack.png
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
            // TAMPILKAN TAS DI MEJA KASIR PIERRE (Player otomatis di depan tas)
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

                if (Game1.player.Tile.Y >= 17 && Math.Abs(Game1.player.Tile.X - 7) <= 2)
                {
                    Game1.player.draw(e.SpriteBatch);
                }
            }
        }

        private void OnMenuChanged(object sender, MenuChangedEventArgs e)
        {
            rowOffset = 0; // Reset ke baris atas saat menu baru dibuka
            UpdateSlotMapping(e.NewMenu);
        }

        private void OnRenderedActiveMenu(object sender, RenderedActiveMenuEventArgs e)
        {
            UpdateSlotMapping(Game1.activeClickableMenu);

            // Indikator Posisi Scroll & Efek Terkunci
            if (Game1.activeClickableMenu is GameMenu gameMenu && gameMenu.currentTab == 0)
            {
                if (gameMenu.GetCurrentPage() is InventoryPage invPage && invPage.inventory != null)
                {
                    var invMenu = invPage.inventory;
                    int rightX = invMenu.inventory[11].bounds.X + invMenu.inventory[11].bounds.Width + 10;
                    int topY = invMenu.inventory[0].bounds.Y + 20;

                    // Gambar Indikator Scroll Kecil (Dot Titik) di samping kanan tas
                    e.SpriteBatch.Draw(Game1.staminaRect, new Rectangle(rightX, topY, 6, 6), (rowOffset == 0) ? Color.Gold : Color.DimGray * 0.5f);
                    e.SpriteBatch.Draw(Game1.staminaRect, new Rectangle(rightX, topY + 16, 6, 6), (rowOffset == 1) ? Color.Gold : Color.DimGray * 0.5f);

                    // Jika berada di baris ke-4 dan BELUM beli tas 48 slot: gambar arsiran gelap pada baris paling bawah
                    if (rowOffset == 1 && Game1.player.MaxItems == 36)
                    {
                        for (int i = 24; i < 36; i++)
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

        private void UpdateSlotMapping(IClickableMenu menu)
        {
            if (menu is GameMenu gameMenu && gameMenu.currentTab == 0)
            {
                if (gameMenu.GetCurrentPage() is InventoryPage invPage && invPage.inventory != null)
                {
                    var invMenu = invPage.inventory;
                    if (invMenu.inventory.Count == 36)
                    {
                        // Petakan indeks slot sesuai posisi scroll (0-35 atau 12-47)
                        for (int i = 0; i < 36; i++)
                        {
                            invMenu.inventory[i].name = (rowOffset * 12 + i).ToString();
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
                // Rekam titik awal sentuhan untuk mendeteksi Swipe di menu tas
                if (Game1.activeClickableMenu is GameMenu gameMenu && gameMenu.currentTab == 0)
                {
                    touchStartY = Game1.getMousePosition().Y;
                    isSwiping = true;

                    // Kunci baris ke-4 jika belum beli
                    if (rowOffset == 1 && Game1.player.MaxItems == 36)
                    {
                        if (gameMenu.GetCurrentPage() is InventoryPage invPage && invPage.inventory != null)
                        {
                            Point touchPos = Game1.getMousePosition();
                            for (int i = 24; i < 36; i++)
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
                }

                // Interaksi beli tas 48 slot di meja Pierre
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

        private void OnButtonReleased(object sender, ButtonReleasedEventArgs e)
        {
            // DETEKSI GESTUR SWIPE (USAP LAYAR)
            if (e.Button == SButton.MouseLeft && isSwiping && touchStartY >= 0)
            {
                if (Game1.activeClickableMenu is GameMenu gameMenu && gameMenu.currentTab == 0)
                {
                    int deltaY = touchStartY - Game1.getMousePosition().Y;

                    // Swipe UP (Usap ke Atas minimal 35 piksel) -> Scroll ke Baris 2-4
                    if (deltaY > 35 && rowOffset == 0)
                    {
                        rowOffset = 1;
                        Game1.playSound("shwip");
                        UpdateSlotMapping(gameMenu);
                    }
                    // Swipe DOWN (Usap ke Bawah minimal 35 piksel) -> Scroll ke Baris 1-3
                    else if (deltaY < -35 && rowOffset == 1)
                    {
                        rowOffset = 0;
                        Game1.playSound("shwip");
                        UpdateSlotMapping(gameMenu);
                    }
                }

                isSwiping = false;
                touchStartY = -1;
            }
        }
    }
}
