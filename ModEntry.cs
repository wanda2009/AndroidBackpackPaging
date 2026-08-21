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
        private bool isPage2 = false;
        private Rectangle pageBtnBox;

        public override void Entry(IModHelper helper)
        {
            helper.Events.Display.RenderedWorld += OnRenderedWorld;
            helper.Events.Display.MenuChanged += (s, e) => { ResetToPage1(); };
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

        // FUNGSI PENDETEKSI INVENTORY DI SEMUA MENU (UNIVERSAL)
        private InventoryMenu GetActiveInventory(IClickableMenu menu)
        {
            if (menu is GameMenu gm && gm.currentTab == 0 && gm.GetCurrentPage() is InventoryPage ip)
                return ip.inventory;
            if (menu is ItemGrabMenu grab)
                return grab.inventory;
            if (menu is ShopMenu shop)
                return shop.inventory;
            if (menu is JunimoNoteMenu jnote)
                return jnote.inventory;
            return null;
        }

        private void OnRenderedActiveMenu(object s, RenderedActiveMenuEventArgs e)
        {
            if (Game1.player.MaxItems != 48) return;
            Ensure48Slots();

            // Tampilkan tombol di SEMUA menu yang memiliki tas
            var inv = GetActiveInventory(Game1.activeClickableMenu);
            if (inv != null && inv.inventory.Count >= 12)
            {
                int btnX = inv.inventory[11].bounds.Right + 8;
                int btnY = inv.inventory[0].bounds.Y;
                DrawBtn(e.SpriteBatch, btnX, btnY);
            }
        }

        private void DrawBtn(SpriteBatch b, int x, int y)
        {
            pageBtnBox = new Rectangle(x, y, 52, 52);
            IClickableMenu.drawTextureBox(b, Game1.menuTexture, new Rectangle(0, 256, 60, 60), x, y, 52, 52, new Color(235, 60, 50), 1f, false);

            string label = isPage2 ? "2" : "1";
            SpriteFont font = Game1.dialogueFont;
            Vector2 sz = font.MeasureString(label);
            Vector2 pos = new Vector2(x + (52 - sz.X) / 2, y + (52 - sz.Y) / 2 - 2);

            b.DrawString(font, label, new Vector2(pos.X + 2, pos.Y + 2), Color.Black);
            b.DrawString(font, label, pos, Color.White);
        }

        private void Ensure48Slots()
        {
            if (Game1.player.MaxItems == 48)
            {
                while (Game1.player.Items.Count < 48)
                    Game1.player.Items.Add(null);
            }
        }

        private void TogglePage()
        {
            if (Game1.player.MaxItems != 48) return;
            Ensure48Slots();

            Game1.playSound("shwip");

            // Tukar isi Baris 3 (index 24..35) dengan Baris 4 (index 36..47)
            for (int i = 0; i < 12; i++)
            {
                Item temp = Game1.player.Items[24 + i];
                Game1.player.Items[24 + i] = Game1.player.Items[36 + i];
                Game1.player.Items[36 + i] = temp;
            }

            isPage2 = !isPage2;
        }

        private void ResetToPage1()
        {
            if (isPage2 && Game1.player.MaxItems == 48)
            {
                Ensure48Slots();
                for (int i = 0; i < 12; i++)
                {
                    Item temp = Game1.player.Items[24 + i];
                    Game1.player.Items[24 + i] = Game1.player.Items[36 + i];
                    Game1.player.Items[36 + i] = temp;
                }
                isPage2 = false;
            }
        }

        private void ForceOrganize48()
        {
            ResetToPage1();
            Ensure48Slots();

            List<Item> tools = new List<Item>();
            List<Item> otherItems = new List<Item>();

            for (int i = 0; i < 48; i++)
            {
                Item it = Game1.player.Items[i];
                if (it == null) continue;

                if (it is Tool || it is MeleeWeapon || it is Slingshot || it is FishingRod)
                {
                    tools.Add(it);
                }
                else
                {
                    otherItems.Add(it);
                }
            }

            otherItems.Sort((a, b) =>
            {
                int c = b.Category.CompareTo(a.Category);
                if (c != 0) return c;
                return string.Compare(a.QualifiedItemId, b.QualifiedItemId, StringComparison.OrdinalIgnoreCase);
            });

            for (int i = 0; i < 48; i++)
                Game1.player.Items[i] = null;

            int targetIndex = 0;
            foreach (var t in tools)
            {
                if (targetIndex < 48) Game1.player.Items[targetIndex++] = t;
            }
            foreach (var o in otherItems)
            {
                if (targetIndex < 48) Game1.player.Items[targetIndex++] = o;
            }

            Game1.playSound("Ship");
            Game1.showGlobalMessage("Inventory Organized (48 Slots)!");
        }

        private void OnButtonPressed(object s, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady || e.Button != SButton.MouseLeft) return;

            Point mousePos = Game1.getMousePosition();
            Vector2 scaled = Utility.ModifyCoordinatesForUIScale(new Vector2(mousePos.X, mousePos.Y));
            Point uiPos = new Point((int)scaled.X, (int)scaled.Y);

            // 1. KLIK TOMBOL MERAH DI SEMUA MENU (UNIVERSAL)
            if (Game1.player.MaxItems == 48 && GetActiveInventory(Game1.activeClickableMenu) != null)
            {
                Rectangle touch = new Rectangle(pageBtnBox.X - 12, pageBtnBox.Y - 12, pageBtnBox.Width + 24, pageBtnBox.Height + 24);
                if (touch.Contains(mousePos) || touch.Contains(uiPos))
                {
                    Helper.Input.Suppress(e.Button);
                    TogglePage();
                    return;
                }
            }

            // 2. KLIK TOMBOL SORTIR DI MENU TAS UTAMA
            if (Game1.activeClickableMenu is GameMenu gm && gm.currentTab == 0 && gm.GetCurrentPage() is InventoryPage ip && ip.organizeButton != null)
            {
                Vector2 orgCenter = new Vector2(ip.organizeButton.bounds.Center.X, ip.organizeButton.bounds.Center.Y);
                if (Vector2.Distance(new Vector2(mousePos.X, mousePos.Y), orgCenter) <= 45f || Vector2.Distance(scaled, orgCenter) <= 45f)
                {
                    Helper.Input.Suppress(e.Button);
                    ForceOrganize48();
                    return;
                }
            }
            // 3. KLIK TOMBOL SORTIR DI MENU PETI (CHEST)
            else if (Game1.activeClickableMenu is ItemGrabMenu grab && grab.organizeButton != null)
            {
                Vector2 orgCenter = new Vector2(grab.organizeButton.bounds.Center.X, grab.organizeButton.bounds.Center.Y);
                if (Vector2.Distance(new Vector2(mousePos.X, mousePos.Y), orgCenter) <= 45f || Vector2.Distance(scaled, orgCenter) <= 45f)
                {
                    ResetToPage1();
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
