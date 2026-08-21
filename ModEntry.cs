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

                    // 5. PENUTUP ATAP ATAS: Tutup bagian atas agar item tidak meluber ke tab
                    int topCoverHeight = startY - gameMenu.yPositionOnScreen + 8;
                    e.SpriteBatch.Draw(
                        Game1.staminaRect,
                        new Rectangle(gameMenu.xPositionOnScreen + 16, gameMenu.yPositionOnScreen - 60, gameMenu.width - 32, topCoverHeight + 60),
                        new Color(245, 207, 148)
                    );

                    // Gambar tomb
