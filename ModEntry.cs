using System;
using System.IO;
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
        private const int Price = 50000;
        private const int ThumbH = 44;

        private float curScroll, tarScroll, dragStart;
        private int touchStartY = -1;
        private bool isTouchHeld, isDragBar, isDragInv;
        private Rectangle trackBox, thumbBox;

        public override void Entry(IModHelper helper)
        {
            helper.Events.Display.RenderedWorld += OnRenderedWorld;
            helper.Events.Display.MenuChanged += (s, e) => { curScroll = tarScroll = 0f; isTouchHeld = isDragBar = isDragInv = false; SetupInv(e.NewMenu); };
            helper.Events.Display.RenderedActiveMenu += OnRenderedActiveMenu;
            helper.Events.Input.ButtonPressed += OnButtonPressed;
            helper.Events.Input.ButtonReleased += OnButtonReleased;

            try
            {
                string path = Path.Combine(helper.DirectoryPath, "backpack.png");
                backpackTexture = File.Exists(path) ? Texture2D.FromStream(Game1.graphics.GraphicsDevice, File.OpenRead(path)) : helper.ModContent.Load<Texture2D>("backpack.png");
            }
            catch { backpackTexture = null; }
        }

        private void OnRenderedWorld(object s, RenderedWorldEventArgs e)
        {
            if (Game1.currentLocation?.Name == "SeedShop" && Game1.player.MaxItems == 36 && backpackTexture != null)
            {
                Vector2 pos = Game1.GlobalToLocal(Game1.viewport, new Vector2(7 * 64 + 14, 18 * 64 - 56));
                e.SpriteBatch.Draw(backpackTexture, pos, null, Color.White, 0f, Vector2.Zero, 3f, SpriteEffects.None, 0.86f);
                if (Game1.player.Tile.Y >= 17 && Math.Abs(Game1.player.Tile.X - 7) <= 2)
                    Game1.player.draw(e.SpriteBatch);
            }
        }

        private void OnRenderedActiveMenu(object s, RenderedActiveMenuEventArgs e)
        {
            if (Game1.activeClickableMenu is GameMenu menu && menu.currentTab == 0 && menu.GetCurrentPage() is InventoryPage page && page.inventory != null)
            {
                var inv = page.inventory;
                SetupInv(menu);

                int size = inv.inventory[0].bounds.Width;
                int startX = inv.inventory[0].bounds.X;
                int startY = inv.yPositionOnScreen;

                // Drag Real-time 60 FPS
                if (isTouchHeld)
                {
                    int my = Game1.getMousePosition().Y;
                    if (isDragBar && trackBox.Height > ThumbH)
                        curScroll = tarScroll = MathHelper.Clamp((float)(my - trackBox.Y - (ThumbH / 2)) / (trackBox.Height - ThumbH), 0f, 1f);
                    else if (isDragInv && touchStartY >= 0)
                        tarScroll = MathHelper.Clamp(dragStart + (float)(touchStartY - my) / size, 0f, 1f);
                }

                if (!isDragBar)
                {
                    curScroll = MathHelper.Lerp(curScroll, tarScroll, 0.28f);
                    if (Math.Abs(curScroll - tarScroll) < 0.002f) curScroll = tarScroll;
                }

                int shift = (int)(curScroll * size);

                for (int r = 0; r < 4; r++)
                {
                    for (int c = 0; c < 12; c++)
                    {
                        int idx = r * 12 + c;
                        if (idx < inv.inventory.Count)
                            inv.inventory[idx].bounds = new Rectangle(startX + c * size, startY + r * size - shift, size, size);
                    }
                }

                // Penutup Bawah (Profil di Lapisan Depan)
                int divY = startY + 3 * size + 2;
                int botH = (menu.yPositionOnScreen + menu.height) - divY;
                IClickableMenu.drawTextureBox(e.SpriteBatch, Game1.menuTexture, new Rectangle(0, 256, 60, 60), menu.xPositionOnScreen + 16, divY, menu.width - 32, botH, new Color(245, 207, 148), 1f, false);
                e.SpriteBatch.Draw(Game1.staminaRect, new Rectangle(menu.xPositionOnScreen + 16, divY, menu.width - 32, 6), new Color(185, 95, 25));
                page.draw(e.SpriteBatch);

                // Penutup Atas (Tab di Lapisan Depan)
                int topH = startY - menu.yPositionOnScreen + 8;
                e.SpriteBatch.Draw(Game1.staminaRect, new Rectangle(menu.xPositionOnScreen + 16, menu.yPositionOnScreen - 60, menu.width - 32, topH + 60), new Color(245, 207, 148));
                menu.upperRightCloseButton?.draw(e.SpriteBatch);

                // Scrollbar Asli
                int tx = inv.inventory[11].bounds.X + size + 10;
                int ty = startY + 4;
                int th = 3 * size - 8;
                int sy = ty + (int)(curScroll * (th - ThumbH));

                trackBox = new Rectangle(tx - 6, ty, 36, th);
                thumbBox = new Rectangle(tx, sy, 24, ThumbH);

                IClickableMenu.drawTextureBox(e.SpriteBatch, Game1.mouseCursors, new Rectangle(403, 383, 6, 6), tx, ty, 24, th, Color.White, 4f, false);
                IClickableMenu.drawTextureBox(e.SpriteBatch, Game1.mouseCursors, new Rectangle(435, 463, 6, 10), tx, sy, 24, ThumbH, Color.White, 4f, false);
            }
        }

        private void SetupInv(IClickableMenu menu)
        {
            if (menu is GameMenu gm && gm.currentTab == 0 && gm.GetCurrentPage() is InventoryPage p && p.inventory != null)
            {
                var inv = p.inventory;
                if (inv.inventory.Count == 36)
                {
                    inv.capacity = 48;
                    inv.rows = 4;
                    int size = inv.inventory[0].bounds.Width;
                    int sx = inv.inventory[0].bounds.X;
                    int sy = inv.yPositionOnScreen;
                    for (int i = 0; i < 12; i++)
                        inv.inventory.Add(new ClickableComponent(new Rectangle(sx + i * size, sy + 3 * size, size, size), (36 + i).ToString()));
                }
            }
        }

        private void OnButtonPressed(object s, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady || e.Button != SButton.MouseLeft) return;

            if (Game1.activeClickableMenu is GameMenu gm && gm.currentTab == 0 && gm.GetCurrentPage() is InventoryPage p && p.inventory != null)
            {
                Point pos = Game1.getMousePosition();
                if (trackBox.Contains(pos) || thumbBox.Contains(pos))
                {
                    Helper.Input.Suppress(e.Button);
                    isTouchHeld = isDragBar = true;
                    if (trackBox.Height > ThumbH)
                        curScroll = tarScroll = MathHelper.Clamp((float)(pos.Y - trackBox.Y - (ThumbH / 2)) / (trackBox.Height - ThumbH), 0f, 1f);
                    return;
                }

                int size = p.inventory.inventory[0].bounds.Width;
                Rectangle area = new Rectangle(p.inventory.inventory[0].bounds.X, p.inventory.yPositionOnScreen, 12 * size, 3 * size);
                if (area.Contains(pos))
                {
                    touchStartY = pos.Y;
                    dragStart = curScroll;
                    isTouchHeld = isDragInv = true;
                }
            }

            if (Game1.currentLocation?.Name == "SeedShop" && Game1.player.MaxItems == 36)
            {
                Vector2 t = e.Cursor.Tile;
                if (t.X == 7 && (t.Y == 18 || t.Y == 17) && Vector2.Distance(Game1.player.Tile, new Vector2(7, 18)) <= 3.5f)
                {
                    Helper.Input.Suppress(e.Button);
                    var res = new[] { new Response("Purchase", $"Purchase ({Price:N0}g)"), new Response("NotNow", "Not now") };
                    Game1.currentLocation.createQuestionDialogue("Backpack Upgrade -- 48 slots", res, new GameLocation.afterQuestionBehavior((who, ans) =>
                    {
                        if (ans == "Purchase")
                        {
                            if (who.Money >= Price)
                            {
                                who.Money -= Price;
                                who.MaxItems = 48;
                                Game1.playSound("reward");
                                Game1.showGlobalMessage("Backpack Upgrade Complete! You now have 48 slots.");
                            }
                            else Game1.drawObjectDialogue("You don't have enough money (Costs 50,000g).");
                        }
                    }));
                }
            }
        }

        private void OnButtonReleased(object s, ButtonReleasedEventArgs e)
        {
            if (e.Button == SButton.MouseLeft)
            {
                isTouchHeld = isDragBar = false;
                if (isDragInv)
                {
                    tarScroll = (tarScroll > 0.5f) ? 1f : 0f;
                    isDragInv = false;
                    touchStartY = -1;
                }
            }
        }
    }
}
