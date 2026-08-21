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

        // Kontrol Scroll Halus & Real-time Touch
        private float currentScroll = 0f;
        private float targetScroll = 0f;
        private float dragStartScroll = 0f;
        private int touchStartY = -1;
        private bool isTouchHeld = false;
        private bool isDraggingScrollbar = false;
        private bool isDraggingInventory = false;

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
            isDraggingInventory = false;
            SetupInventory48(e.NewMenu);
        }

        private void OnRenderedActiveMenu(object sender, RenderedActiveMenuEventArgs e)
        {
            if (Game1.activeClickableMenu is GameMenu gameMenu && gameMenu.currentTab == 0)
            {
                if (gameMenu.GetCurrentPage() is InventoryPage invPage && invPage.inventory != null)
                {
                    var invMenu = invPage.inventory;
                    SetupInventory48(gameMenu);

                    int slotSize = invMenu.inventory[0].bounds.Width;
                    int startX = invMenu.inventory[0].bounds.X;
                    int startY = invMenu.yPositionOnScreen;

                    // 1. UPDATE DRAG SECARA REAL-TIME 60 FPS (TANPA TAP-TAP)
                    if (isTouchHeld)
                    {
                        int currentTouchY = Game1.getMousePosition().Y;

                        if (isDraggingScrollbar)
                        {
                            int thumbHeight = 44;
                            int maxTravel = trackBounds.Height - thumbHeight;
                            if (maxTravel > 0)
                            {
                                float progress = (float)(currentTouchY - trackBounds.Y - (thumbHeight / 2)) / maxTravel;
                                targetScroll = MathHelper.Clamp(progress, 0f, 1f);
                                currentScroll = targetScroll;
                            }
                        }
                        else if (isDraggingInventory && touchStartY >= 0)
                        {
                            float deltaProgress = (float)(touchStartY - currentTouchY) / slotSize;
                            targetScroll = MathHelper.Clamp(dragStartScroll + deltaProgress, 0f, 1f);
                        }
                    }

                    // 2. ANIMASI INTERPOLASI HALUS
                    if (!isDraggingScrollbar)
                    {
                        currentScroll = MathHelper.Lerp(currentScroll, targetScroll, 0.28f);
                        if (Math.Abs(currentScroll - targetScroll) < 0.002f)
                            currentScroll = targetScroll;
                    }

                    int pixelShift = (int)(currentScroll * slotSize);

                    // 3. GESER KOORDINAT 48 SLOT DI LAPISAN BELAKANG
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

                    // 4. PENUTUP LANTAI BAWAH: Tutup area profil di bawah & gambar ulang profil di depan
                    int dividerY = startY + (3 * slotSize) + 2;
                    int bottomHeight = (gameMenu.yPositionOnScreen + gameMenu.height) - dividerY;

                    IClickableMenu.drawTextureBox(
                        e.SpriteBatch,
                        Game1.menuTexture,
                        new Rectangle(0, 256, 60, 60),
                        gameMenu.xPositionOnScreen + 16,
                        dividerY,
                        gameMenu.width - 32,
                        bottomHeight,
                        new Color(245, 207, 148),
                        1f,
                        false
                    );

                    e.SpriteBatch.Draw(
                        Game1.staminaRect,
                        new Rectangle(gameMenu.xPositionOnScreen + 16, dividerY, gameMenu.width - 32, 6),
                        new Color(185, 95, 25)
                    );

                    invPage.draw(e.SpriteBatch);

                    // 5. PENUTUP ATAP ATAS: Tutup bagian atas dan gambar ulang tab ikon di depan
                    int topCoverHeight = startY - gameMenu.yPositionOnScreen + 8;
                    e.SpriteBatch.Draw(
                        Game1.staminaRect,
                        new Rectangle(gameMenu.xPositionOnScreen + 16, gameMenu.yPositionOnScreen - 60, gameMenu.width - 32, topCoverHeight + 60),
                        new Color(245, 207, 148)
                    );

                    // Gambar ulang seluruh tab ikon resmi (Tas, Map, Hati, Tombol X)
                    for (int i = 0; i < gameMenu.tabs.Count; i++)
                    {
                        gameMenu.tabs[i].draw(e.SpriteBatch);
                    }
                    if (gameMenu.upperRightCloseButton != null)
                    {
                        gameMenu.upperRightCloseButton.draw(e.SpriteBatch);
                    }

                    // 6. GAMBAR SCROLLBAR ASLI VANILLA (LEBAR 24px PROPORSIONAL)
                    int trackX = invMenu.inventory[11].bounds.X + slotSize + 10;
                    int trackY = startY + 4;
                    int trackWidth = 24;
                    int trackHeight = (3 * slotSize) - 8;
                    int thumbHeight = 44;
                    int thumbY = trackY + (int)(currentScroll * (trackHeight - thumbHeight));

                    trackBounds = new Rectangle(trackX - 6, trackY, trackWidth + 12, trackHeight);
                    thumbBounds = new Rectangle(trackX, thumbY, trackWidth, thumbHeight);

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
                        thumbHeight,
                        Color.White,
                        4f,
                        false
                    );
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
                if (Game1.activeClickableMenu is GameMenu gameMenu && gameMenu.currentTab == 0)
                {
                    Point touchPos = Game1.getMousePosition();

                    // 1. Sentuh Scrollbar untuk Drag Langsung
                    if (trackBounds.Contains(touchPos) || thumbBounds.Contains(touchPos))
                    {
                        Helper.Input.Suppress(e.Button);
                        isTouchHeld = true;
                        isDraggingScrollbar = true;
                        return;
                    }

                    // 2. Sentuh Area Tas untuk Drag / Swipe
                    if (gameMenu.GetCurrentPage() is InventoryPage invPage && invPage.inventory != null)
                    {
                        var invMenu = invPage.inventory;
                        int slotSize = invMenu.inventory[0].bounds.Width;
                        Rectangle invArea = new Rectangle(invMenu.inventory[0].bounds.X, invMenu.yPositionOnScreen, (12 * slotSize), 3 * slotSize);

                        if (invArea.Contains(touchPos))
                        {
                            touchStartY = touchPos.Y;
                            dragStartScroll = currentScroll;
                            isTouchHeld = true;
                            isDraggingInventory = true;
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

                // Kunci posisi akhir saat jari dilepas (Snap mentok atas atau mentok bawah)
                if (isDraggingInventory)
                {
                    targetScroll = (targetScroll > 0.5f) ? 1f : 0f;
                    isDraggingInventory = false;
                    touchStartY = -1;
                }
            }
        }
    }
}
