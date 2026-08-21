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
        private const int THUMB_HEIGHT = 44;

        // Kontrol Scroll & Touch
        private float currentScroll = 0f;
        private float targetScroll = 0f;
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
            targetScroll = 0f;
            currentScroll = 0f;
            isTouchHeld = false;
            isDraggingScrollbar = false;
            isSwiping = false;
        }

        private void OnRenderedActiveMenu(object sender, RenderedActiveMenuEventArgs e)
        {
            if (Game1.activeClickableMenu is GameMenu gameMenu && gameMenu.currentTab == 0)
            {
                if (gameMenu.GetCurrentPage() is InventoryPage invPage && invPage.inventory != null)
                {
                    var invMenu = invPage.inventory;

                    // Gunakan koordinat paten dari menu bawaan game (ANTI-HILANG)
                    int slotSize = 64;
                    int startX = invMenu.xPositionOnScreen;
                    int startY = invMenu.yPositionOnScreen;

                    // 1. UPDATE DRAG SCROLLBAR REAL-TIME
                    if (isTouchHeld && isDraggingScrollbar)
                    {
                        int currentTouchY = Game1.getMousePosition().Y;
                        int maxTravel = trackBounds.Height - THUMB_HEIGHT;
                        if (maxTravel > 0)
                        {
                            float progress = (float)(currentTouchY - trackBounds.Y - (THUMB_HEIGHT / 2)) / maxTravel;
                            targetScroll = MathHelper.Clamp(progress, 0f, 1f);
                            currentScroll = targetScroll;

                            // Geser isi data tas
                            if (targetScroll >= 0.5f && Game1.player.MaxItems == 48)
                                ShiftToRow(1);
                            else if (targetScroll < 0.5f && Game1.player.MaxItems == 48)
                                ShiftToRow(0);
                        }
                    }

                    // 2. GAMBAR SCROLLBAR ASLI VANILLA (KOORDINAT PATEN)
                    int trackX = startX + (12 * slotSize) + 8;
                    int trackY = startY + 2;
                    int trackWidth = 20;
                    int trackHeight = (3 * slotSize) - 4;
                    int thumbY = trackY + (int)(currentScroll * (trackHeight - THUMB_HEIGHT));

                    trackBounds = new Rectangle(trackX - 6, trackY, trackWidth + 12, trackHeight);
                    thumbBounds = new Rectangle(trackX, thumbY, trackWidth, THUMB_HEIGHT);

                    // Jalur Abu-abu Resmi
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

                    // Balok Slider Kayu/Emas Resmi
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

        private void ShiftToRow(int targetOffset)
        {
            if (Game1.player.MaxItems != 48 || Game1.player.Items.Count < 48) return;

            // Geser ke Baris 2-4
            if (targetOffset == 1 && currentScroll < 0.5f)
            {
                List<Item> row1 = new List<Item>();
                for (int i = 0; i < 12; i++)
                    row1.Add(Game1.player.Items[i]);

                for (int i = 12; i < 48; i++)
                    Game1.player.Items[i - 12] = Game1.player.Items[i];

                for (int i = 0; i < 12; i++)
                    Game1.player.Items[36 + i] = row1[i];
            }
            // Geser balik ke Baris 1-3
            else if (targetOffset == 0 && currentScroll >= 0.5f)
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

                    // Proteksi Tab Atas (Bebas untuk Tab Hati, Map, Tombol X)
                    if (touchPos.Y < gameMenu.yPositionOnScreen)
                    {
                        return;
                    }

                    // Sentuh Slider Scrollbar untuk Drag Langsung
                    if (trackBounds.Contains(touchPos) || thumbBounds.Contains(touchPos))
                    {
                        Helper.Input.Suppress(e.Button);
                        isTouchHeld = true;
                        isDraggingScrollbar = true;
                        return;
                    }

                    // Sentuh Area Tas untuk Swipe
                    if (gameMenu.GetCurrentPage() is InventoryPage invPage && invPage.inventory != null)
                    {
                        var invMenu = invPage.inventory;
                        int slotSize = 64;
                        Rectangle invArea = new Rectangle(invMenu.xPositionOnScreen, invMenu.yPositionOnScreen, (12 * slotSize), 3 * slotSize);

                        if (invArea.Contains(touchPos))
                        {
                            touchStartY = touchPos.Y;
                            isTouchHeld = true;
                            isSwiping = true;
                        }
                    }
                }

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
                        if (deltaY > 25 && targetScroll < 1f && Game1.player.MaxItems == 48)
                        {
                            targetScroll = 1f;
                            currentScroll = 1f;
                            Game1.playSound("shwip");

                            List<Item> row1 = new List<Item>();
                            for (int i = 0; i < 12; i++)
                                row1.Add(Game1.player.Items[i]);

                            for (int i = 12; i < 48; i++)
                                Game1.player.Items[i - 12] = Game1.player.Items[i];

                            for (int i = 0; i < 12; i++)
                                Game1.player.Items[36 + i] = row1[i];
                        }
                        // Swipe DOWN -> Scroll ke Baris 1-3
                        else if (deltaY < -25 && targetScroll > 0f && Game1.player.MaxItems == 48)
                        {
                            targetScroll = 0f;
                            currentScroll = 0f;
                            Game1.playSound("shwip");

                            List<Item> row4 = new List<Item>();
                            for (int i = 36; i < 48; i++)
                                row4.Add(Game1.player.Items[i]);

                            for (int i = 35; i >= 0; i--)
                                Game1.player.Items[i + 12] = Game1.player.Items[i];

                            for (int i = 0; i < 12; i++)
                                Game1.player.Items[i] = row4[i];
                        }
                    }

                    isSwiping = false;
                    touchStartY = -1;
                }
            }
        }
    }
}
