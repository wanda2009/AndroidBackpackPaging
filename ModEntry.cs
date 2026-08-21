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
            // Tampilkan tas di meja Pierre & player di depan tas
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

                    // 1. UPDATE DRAG SECARA REAL-TIME 60 FPS
                    if (isTouchHeld)
                    {
                        int currentTouchY = Game1.getMousePosition().Y;

                        if (isDraggingScrollbar)
                        {
                            int maxTravel = trackBounds.Height - THUMB_HEIGHT;
                            if (maxTravel > 0)
                            {
                                float progress = (float)(currentTouchY - trackBounds.Y - (THUMB_HEIGHT / 2)) / maxTravel;
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
                    int safeTop = startY - 4;
                    int safeBottom = startY + (3 * slotSize) + 4;

                    // 3. ATUR POSISI 48 SLOT & HILANGKAN SLOT YANG KELUAR BATAS (IDE USER)
                    for (int r = 0; r < 4; r++)
                    {
                        int rowY = startY + (r * slotSize) - pixelShift;

                        for (int c = 0; c < 12; c++)
                        {
                            int idx = r * 12 + c;
                            if (idx < invMenu.inventory.Count)
                            {
                                // Jika slot berada di dalam batas aman: posisikan normal
                                if (rowY >= safeTop && (rowY + slotSize) <= safeBottom)
                                {
                                    invMenu.inventory[idx].bounds = new Rectangle(
                                        startX + (c * slotSize),
                                        rowY,
                                        slotSize,
                                        slotSize
                                    );
                                }
                                // Jika keluar batas atas/bawah: lempar ke luar layar agar tidak terlihat & tidak bisa disentuh
                                else
                                {
                                    invMenu.inventory[idx].bounds = new Rectangle(-1000, -1000, 0, 0);
                                }
                            }
                        }
                    }

                    // 4. GAMBAR SCROLLBAR ASLI VANILLA (LEBAR 24px)
                    int trackX = startX + (12 * slotSize) + 10;
                    int trackY = startY + 4;
                    int trackWidth = 24;
                    int trackHeight = (3 * slotSize) - 8;
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

                    // Slider Balok Kayu/Emas Resmi
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

                    // PROTEKSI TAB ATAS: Jika menyentuh area atap tab, biarkan game asli yang memproses
                    if (touchPos.Y < gameMenu.yPositionOnScreen)
                    {
                        return; // 100% responsif untuk tab Hati, Map, Tas, dan tombol X!
                    }

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
