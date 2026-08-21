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

        private float currentScroll = 0f;
        private float targetScroll = 0f;
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
            // TAMPILKAN TAS DI MEJA PIERRE & PLAYER DI DEPAN TAS
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
            targetScroll = 0f;
            currentScroll = 0f;
            SetupInventory48(e.NewMenu);
        }

        private void OnRenderedActiveMenu(object sender, RenderedActiveMenuEventArgs e)
        {
            // ANIMASI SCROLL MELUNCUR HALUS
            if (Math.Abs(currentScroll - targetScroll) > 0.001f)
            {
                currentScroll = MathHelper.Lerp(currentScroll, targetScroll, 0.22f);
                if (Math.Abs(currentScroll - targetScroll) < 0.005f)
                    currentScroll = targetScroll;
            }

            if (Game1.activeClickableMenu is GameMenu gameMenu && gameMenu.currentTab == 0)
            {
                if (gameMenu.GetCurrentPage() is InventoryPage invPage && invPage.inventory != null)
                {
                    var invMenu = invPage.inventory;
                    SetupInventory48(gameMenu);

                    int slotSize = invMenu.inventory[0].bounds.Width; // Ukuran asli layar HP (tidak mengecil)
                    int startX = invMenu.inventory[0].bounds.X;
                    int startY = invMenu.yPositionOnScreen;
                    int pixelShift = (int)(currentScroll * slotSize);

                    // Reposisi koordinat 48 slot di dalam menu
                    for (int r = 0; r < 4; r++)
                    {
                        for (int c = 0; c < 12; c++)
                        {
                            int idx = r * 12 + c;
                            if (idx < invMenu.inventory.Count)
                            {
                                invMenu.inventory[idx].bounds = new Rectangle(
                                    startX + (c * slotSize),
                                    startY + (r * slotSize) - pixelShift,
                                    slotSize,
                                    slotSize
                                );
                            }
                        }
                    }

                    // 1. GAMBAR SCROLLBAR 100% VANILLA ASLI GAME
                    int trackX = startX + (12 * slotSize) + 10;
                    int trackY = startY + 6;
                    int trackWidth = 24;
                    int trackHeight = (3 * slotSize) - 12;
                    int thumbHeight = 44;
                    int thumbY = trackY + (int)(currentScroll * (trackHeight - thumbHeight));

                    // Jalur Scrollbar Bawaan Game (Abu-abu Vanilla)
                    IClickableMenu.drawTextureBox(
                        e.SpriteBatch,
                        Game1.mouseCursors,
                        new Rectangle(403, 383, 6, 6),
                        trackX,
                        trackY,
                        trackWidth,
                        trackHeight,
                        Color.White,
                        4f,
                        false
                    );

                    // Slider Kotak Emas/Kayu Bawaan Game (Vanilla Thumb)
                    IClickableMenu.drawTextureBox(
                        e.SpriteBatch,
                        Game1.mouseCursors,
                        new Rectangle(435, 463, 6, 10),
                        trackX,
                        thumbY,
                        trackWidth,
                        thumbHeight,
                        Color.White,
                        4f,
                        false
                    );

                    // 2. SEBELUM BELI (36 SLOT): Berikan efek arsiran terkunci pada Baris ke-4
                    if (Game1.player.MaxItems == 36 && invMenu.inventory.Count >= 48)
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

        private void SetupInventory48(IClickableMenu menu)
        {
            if (menu is GameMenu gameMenu && gameMenu.currentTab == 0)
            {
                if (gameMenu.GetCurrentPage() is InventoryPage invPage && invPage.inventory != null)
                {
                    var invMenu = invPage.inventory;
                    if (invMenu.inventory.Count == 36)
                    {
                        invMenu.capacity = 48;
                        invMenu.rows = 4;

                        int slotSize = invMenu.inventory[0].bounds.Width;
                        int startX = invMenu.inventory[0].bounds.X;
                        int startY = invMenu.yPositionOnScreen;

                        for (int i = 0; i < 12; i++)
                        {
                            Rectangle row4Bounds = new Rectangle(startX + (i * slotSize), startY + (3 * slotSize), slotSize, slotSize);
                            invMenu.inventory.Add(new ClickableComponent(row4Bounds, (36 + i).ToString()));
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
                // Deteksi sentuhan di menu tas untuk Swipe & Kunci Anti-Ngezoom
                if (Game1.activeClickableMenu is GameMenu gameMenu && gameMenu.currentTab == 0)
                {
                    Point touchPos = Game1.getMousePosition();
                    if (gameMenu.GetCurrentPage() is InventoryPage invPage && invPage.inventory != null)
                    {
                        var invMenu = invPage.inventory;
                        int slotSize = invMenu.inventory[0].bounds.Width;
                        Rectangle invArea = new Rectangle(invMenu.inventory[0].bounds.X, invMenu.yPositionOnScreen, (12 * slotSize) + 40, 3 * slotSize);

                        if (invArea.Contains(touchPos))
                        {
                            touchStartY = touchPos.Y;
                            isSwiping = true;

                            // Kunci sentuhan pada baris ke-4 jika belum dibeli
                            if (Game1.player.MaxItems == 36)
                            {
                                for (int i = 36; i < invMenu.inventory.Count; i++)
                                {
                                    if (invMenu.inventory[i].bounds.Contains(touchPos))
                                    {
                                        Helper.Input.Suppress(e.Button);
                                        Game1.playSound("cancel");
                                        return;
                                    }
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
            // DETEKSI GESTUR SWIPE DENGAN SISTEM MENTOK
            if (e.Button == SButton.MouseLeft && isSwiping && touchStartY >= 0)
            {
                if (Game1.activeClickableMenu is GameMenu gameMenu && gameMenu.currentTab == 0)
                {
                    int deltaY = touchStartY - Game1.getMousePosition().Y;

                    // Swipe UP (Usap ke Atas minimal 25 piksel) -> Meluncur ke Baris 2-4
                    if (deltaY > 25 && targetScroll < 1f)
                    {
                        targetScroll = 1f; // Mentok Bawah
                        Game1.playSound("shwip");
                    }
                    // Swipe DOWN (Usap ke Bawah minimal 25 piksel) -> Meluncur ke Baris 1-3
                    else if (deltaY < -25 && targetScroll > 0f)
                    {
                        targetScroll = 0f; // Mentok Atas
                        Game1.playSound("shwip");
                    }
                }

                isSwiping = false;
                touchStartY = -1;
            }
        }
    }
}
