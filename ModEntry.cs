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
        private const int THUMB_HEIGHT = 48;

        // Kontrol Scroll & Touch
        private float currentScroll = 0f;
        private float targetScroll = 0f;
        private int currentRowState = 0; // 0 = Baris 1-3, 1 = Baris 2-4
        private int touchStartY = -1;
        private bool isTouchHeld = false;
        private bool isDraggingScrollbar = false;
        private bool isSwiping = false;

        private Rectangle trackBounds;
        private Rectangle thumbBounds;

        public override void Entry(IModHelper helper)
        {
            helper.Events.Display.RenderedWorld += OnRenderedWorld;
            helper.Events.Display.MenuChanged += OnMenuChanged;
            helper.Events.Display.RenderedActiveMenu += OnRenderedActiveMenu;
            helper.Events.Input.ButtonPressed += OnButtonPressed;
            helper.Events.Input.ButtonReleased += OnButtonReleased;

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

                if (Game1.player.Tile.Y >= 17 && Math.Abs(Game1.player.Tile.X - 7) <= 2)
                {
                    Game1.player.draw(e.SpriteBatch);
                }
            }
        }

        private void OnMenuChanged(object sender, MenuChangedEventArgs e)
        {
            // Reset ke posisi atas saat menu baru dibuka
            if (currentRowState == 1 && Game1.player.MaxItems == 48)
            {
                ShiftRows(false);
            }
            currentRowState = 0;
            targetScroll = 0f;
            currentScroll = 0f;
            isTouchHeld = false;
            isDraggingScrollbar = false;
            isSwiping = false;

            Ensure48ItemSlots();
        }

        private void OnRenderedActiveMenu(object sender, RenderedActiveMenuEventArgs e)
        {
            if (Game1.activeClickableMenu is GameMenu gameMenu && gameMenu.currentTab == 0)
            {
                if (gameMenu.GetCurrentPage() is InventoryPage invPage && invPage.inventory != null)
                {
                    var invMenu = invPage.inventory;
                    Ensure48ItemSlots();

                    if (invMenu.inventory.Count >= 36)
                    {
                        // POSISI SCROLLBAR PRESISI (DI SEBELAH KANAN KOLOM KE-12)
                        int trackX = invMenu.inventory[11].bounds.Right + 8;
                        int trackY = invMenu.inventory[0].bounds.Y;
                        int trackWidth = 20;
                        int trackHeight = invMenu.inventory[24].bounds.Bottom - trackY; // Panjang penuh 3 baris
                        int thumbY = trackY + (int)(currentScroll * (trackHeight - THUMB_HEIGHT));

                        trackBounds = new Rectangle(trackX - 10, trackY, trackWidth + 20, trackHeight);
                        thumbBounds = new Rectangle(trackX - 6, thumbY, trackWidth + 12, THUMB_HEIGHT);

                        // 1. UPDATE DRAG SCROLLBAR REAL-TIME
                        if (isTouchHeld && isDraggingScrollbar)
                        {
                            int currentTouchY = Game1.getMousePosition().Y;
                            int maxTravel = trackHeight - THUMB_HEIGHT;
                            if (maxTravel > 0)
                            {
                                float progress = (float)(currentTouchY - trackY - (THUMB_HEIGHT / 2)) / maxTravel;
                                targetScroll = MathHelper.Clamp(progress, 0f, 1f);
                                currentScroll = targetScroll;

                                if (targetScroll >= 0.5f && currentRowState == 0)
                                {
                                    ShiftRows(true);
                                    currentRowState = 1;
                                }
                                else if (targetScroll < 0.5f && currentRowState == 1)
                                {
                                    ShiftRows(false);
                                    currentRowState = 0;
                                }
                            }
                        }

                        // 2. ANIMASI INTERPOLASI HALUS
                        if (!isDraggingScrollbar)
                        {
                            currentScroll = MathHelper.Lerp(currentScroll, targetScroll, 0.25f);
                            if (Math.Abs(currentScroll - targetScroll) < 0.005f)
                                currentScroll = targetScroll;
                        }

                        // 3. GAMBAR SCROLLBAR ASLI VANILLA
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

                        IClickableMenu.drawTextureBox(
                            e.SpriteBatch,
                            Game1.mouseCursors,
                            new Rectangle(435, 463, 6, 10),
                            trackX,
                            thumbY,
                            trackWidth,
                            THUMB_HEIGHT,
                            Color.White,
                            4f,
                            false
                        );
                    }
                }
            }
        }

        private void Ensure48ItemSlots()
        {
            if (Game1.player.MaxItems == 48)
            {
                while (Game1.player.Items.Count < 48)
                {
                    Game1.player.Items.Add(null);
                }
            }
        }

        private void ShiftRows(bool down)
        {
            if (Game1.player.MaxItems != 48) return;
            Ensure48ItemSlots();

            Game1.playSound("shwip");

            if (down) // Geser turun -> Tampilkan Baris 2-4
            {
                List<Item> row1 = new List<Item>();
                for (int i = 0; i < 12; i++)
                    row1.Add(Game1.player.Items[i]);

                for (int i = 12; i < 48; i++)
                    Game1.player.Items[i - 12] = Game1.player.Items[i];

                for (int i = 0; i < 12; i++)
                    Game1.player.Items[36 + i] = row1[i];
            }
            else // Geser naik -> Kembali ke Baris 1-3
            {
                List<Item> row4 = new List<Item>();
                for (int i = 36; i < 48; i++)
                    row4.Add(Game1.player.Items[i]);

                for (int i = 35; i >= 0; i--)
                    Game1.player.Items[i + 12] = Game1.player.Items[i];

                for (int i = 0; i < 12; i++)
                    Game1.player.Items[i] = row4[i];
            }
        }

        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady) return;

            if (e.Button == SButton.MouseLeft)
            {
                if (Game1.activeClickableMenu is GameMenu gameMenu && gameMenu.currentTab == 0)
                {
                    Point touchPos = Game1.getMousePosition();

                    // Proteksi Tab Atas
                    if (touchPos.Y < gameMenu.yPositionOnScreen)
                    {
                        return;
                    }

                    // 1. Sentuh Slider Scrollbar
                    if (trackBounds.Contains(touchPos) || thumbBounds.Contains(touchPos))
                    {
                        Helper.Input.Suppress(e.Button);
                        isTouchHeld = true;
                        isDraggingScrollbar = true;
                        return;
                    }

                    // 2. Sentuh Area Tas untuk Swipe
                    if (gameMenu.GetCurrentPage() is InventoryPage invPage && invPage.inventory != null)
                    {
                        var invMenu = invPage.inventory;
                        if (invMenu.inventory.Count >= 36)
                        {
                            Rectangle invArea = new Rectangle(
                                invMenu.inventory[0].bounds.X,
                                invMenu.inventory[0].bounds.Y,
                                invMenu.inventory[11].bounds.Right - invMenu.inventory[0].bounds.X,
                                invMenu.inventory[24].bounds.Bottom - invMenu.inventory[0].bounds.Y
                            );

                            if (invArea.Contains(touchPos))
                            {
                                touchStartY = touchPos.Y;
                                isTouchHeld = true;
                                isSwiping = true;
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
            if (e.Button == SButton.MouseLeft)
            {
                isTouchHeld = false;
                isDraggingScrollbar = false;

                // Gestur Swipe
                if (isSwiping && touchStartY >= 0)
                {
                    if (Game1.activeClickableMenu is GameMenu gameMenu && gameMenu.currentTab == 0)
                    {
                        int deltaY = touchStartY - Game1.getMousePosition().Y;

                        // Swipe UP -> Scroll ke Baris 2-4
                        if (deltaY > 25 && currentRowState == 0 && Game1.player.MaxItems == 48)
                        {
                            targetScroll = 1f;
                            ShiftRows(true);
                            currentRowState = 1;
                        }
                        // Swipe DOWN -> Scroll ke Baris 1-3
                        else if (deltaY < -25 && currentRowState == 1 && Game1.player.MaxItems == 48)
                        {
                            targetScroll = 0f;
                            ShiftRows(false);
                            currentRowState = 0;
                        }
                    }

                    isSwiping = false;
                    touchStartY = -1;
                }
            }
        }
    }
}
