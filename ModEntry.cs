using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Tools;

namespace AndroidBackpackPaging
{
    public class ModEntry : Mod
    {
        private Texture2D backpackTexture;
        private const int Price = 50000;
        private int currentOffset = 0; // 0 = Baris 1-3, 1 = Baris 2-4
        private Rectangle pageBtnBox;

        public override void Entry(IModHelper helper)
        {
            helper.Events.Display.RenderedWorld += OnRenderedWorld;
            helper.Events.Display.MenuChanged += OnMenuChanged;
            helper.Events.Display.RenderedActiveMenu += OnRenderedActiveMenu;
            helper.Events.Input.ButtonPressed += OnButtonPressed;

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

        private void OnMenuChanged(object s, MenuChangedEventArgs e)
        {
            if (currentOffset == 1 && Game1.player.MaxItems == 48)
            {
                ShiftInventory(false);
                currentOffset = 0;
            }
            Ensure48();
        }

        private void OnRenderedActiveMenu(object s, RenderedActiveMenuEventArgs e)
        {
            if (Game1.player.MaxItems != 48) return;
            Ensure48();

            if (Game1.activeClickableMenu is GameMenu gm && gm.currentTab == 0 && gm.GetCurrentPage() is InventoryPage ip && ip.inventory != null)
            {
                if (ip.inventory.inventory.Count >= 12)
                    DrawBtn(e.SpriteBatch, ip.inventory.inventory[11].bounds.Right + 8, ip.inventory.inventory[0].bounds.Y);
            }
            else if (Game1.activeClickableMenu is ItemGrabMenu grab && grab.inventory != null)
            {
                if (grab.inventory.inventory.Count >= 12)
                    DrawBtn(e.SpriteBatch, grab.inventory.inventory[11].bounds.Right + 8, grab.inventory.inventory[0].bounds.Y);
            }
        }

        private void DrawBtn(SpriteBatch b, int x, int y)
        {
            pageBtnBox = new Rectangle(x, y, 52, 52);
            IClickableMenu.drawTextureBox(b, Game1.menuTexture, new Rectangle(0, 256, 60, 60), x, y, 52, 52, new Color(235, 60, 50), 1f, false);

            string label = (currentOffset == 1) ? "2" : "1";
            SpriteFont font = Game1.dialogueFont;
            Vector2 sz = font.MeasureString(label);
            Vector2 pos = new Vector2(x + (52 - sz.X) / 2, y + (52 - sz.Y) / 2 - 2);

            b.DrawString(font, label, new Vector2(pos.X + 2, pos.Y + 2), Color.Black);
            b.DrawString(font, label, pos, Color.White);
        }

        private void Ensure48()
        {
            if (Game1.player.MaxItems == 48)
            {
                while (Game1.player.Items.Count < 48)
                    Game1.player.Items.Add(null);
            }
        }

        private void ShiftInventory(bool forward)
        {
            if (Game1.player.MaxItems != 48) return;
            Ensure48();

            Game1.playSound("shwip");

            if (forward) // Pindah ke Baris 2-4
            {
                List<Item> row1 = new List<Item>();
                for (int i = 0; i < 12; i++)
                    row1.Add(Game1.player.Items[i]);

                for (int i = 12; i < 48; i++)
                    Game1.player.Items[i - 12] = Game1.player.Items[i];

                for (int i = 0; i < 12; i++)
                    Game1.player.Items[36 + i] = row1[i];
            }
            else // Kembali ke Baris 1-3
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

        // FUNGSI SORTIR YANG DIPERBAIKI SANGAT STABIL
        private void ForceOrganize48()
        {
            // Reset offset dulu ke tampilan awal (Halaman 1)
            if (currentOffset == 1)
            {
                ShiftInventory(false);
                currentOffset = 0;
            }

            Ensure48();

            // Gunakan logika bawaan Stardew Valley untuk merapikan item seluruh 48 slot
            ItemGrabMenu.organizeItemsInList(Game1.player.Items);

            Game1.playSound("Ship");
            Game1.showGlobalMessage("Inventory Organized!");

            // Refresh UI Menu secara aman
            if (Game1.activeClickableMenu is GameMenu)
            {
                Game1.activeClickableMenu = new GameMenu(0);
            }
        }

        private void OnButtonPressed(object s, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady || e.Button != SButton.MouseLeft) return;

            Point mousePos = Game1.getMousePosition();
            Vector2 scaled = Utility.ModifyCoordinatesForUIScale(new Vector2(mousePos.X, mousePos.Y));
            Point uiPos = new Point((int)scaled.X, (int)scaled.Y);

            // 1. KLIK TOMBOL MERAH HALAMAN [1] & [2]
            if (Game1.player.MaxItems == 48 && (Game1.activeClickableMenu is GameMenu || Game1.activeClickableMenu is ItemGrabMenu))
            {
                Rectangle touch = new Rectangle(pageBtnBox.X - 12, pageBtnBox.Y - 12, pageBtnBox.Width + 24, pageBtnBox.Height + 24);
                if (touch.Contains(mousePos) || touch.Contains(uiPos))
                {
                    Helper.Input.Suppress(e.Button);

                    if (currentOffset == 0)
                    {
                        ShiftInventory(true);
                        currentOffset = 1;
                    }
                    else
                    {
                        ShiftInventory(false);
                        currentOffset = 0;
                    }

                    if (Game1.activeClickableMenu is GameMenu)
                    {
                        Game1.activeClickableMenu = new GameMenu(0);
                    }
                    return;
                }
            }

            // 2. KLIK TOMBOL SORTIR DI MENU TAS UTAMA (INVENTORY PAGE)
            if (Game1.activeClickableMenu is GameMenu gm && gm.currentTab == 0 && gm.GetCurrentPage() is InventoryPage ip && ip.organizeButton != null)
            {
                if (ip.organizeButton.containsPoint(mousePos.X, mousePos.Y) || ip.organizeButton.containsPoint((int)scaled.X, (int)scaled.Y))
                {
                    Helper.Input.Suppress(e.Button);
                    ForceOrganize48();
                    return;
                }
            }

            // 3. KLIK TOMBOL SORTIR DI MENU PETI (CHEST / ITEM GRAB MENU)
            if (Game1.activeClickableMenu is ItemGrabMenu grab && grab.organizeButton != null)
            {
                if (grab.organizeButton.containsPoint(mousePos.X, mousePos.Y) || grab.organizeButton.containsPoint((int)scaled.X, (int)scaled.Y))
                {
                    if (currentOffset == 1)
                    {
                        ShiftInventory(false);
                        currentOffset = 0;
                    }
                    // Biarkan game native memproses sortir chest bawaan tanpa Suppress
                }
            }

            // 4. BELI TAS 48 SLOT DI MEJA PIERRE
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
    }
}
