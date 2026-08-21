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
        private int currentPage = 1; // 1 = Slot 1-36, 2 = Slot 13-48
        private Rectangle pageButtonBounds;

        public override void Entry(IModHelper helper)
        {
            helper.Events.Display.RenderedWorld += OnRenderedWorld;
            helper.Events.Display.MenuChanged += OnMenuChanged;
            helper.Events.Display.RenderedActiveMenu += OnRenderedActiveMenu;
            helper.Events.Input.ButtonPressed += OnButtonPressed;

            // Muat gambar backpack.png asli
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
            // Reset ke Halaman 1 saat membuka/menutup menu
            if (currentPage == 2 && Game1.player.MaxItems == 48)
            {
                SwitchPage(1);
            }
            currentPage = 1;
            Ensure48Slots();
        }

        private void OnRenderedActiveMenu(object sender, RenderedActiveMenuEventArgs e)
        {
            if (Game1.player.MaxItems != 48) return;
            Ensure48Slots();

            // 1. TAMPILKAN TOMBOL DI MENU TAS UTAMA (GameMenu)
            if (Game1.activeClickableMenu is GameMenu gameMenu && gameMenu.currentTab == 0)
            {
                if (gameMenu.GetCurrentPage() is InventoryPage invPage && invPage.inventory != null)
                {
                    var inv = invPage.inventory;
                    if (inv.inventory.Count >= 12)
                    {
                        int btnX = inv.inventory[11].bounds.Right + 12;
                        int btnY = inv.inventory[0].bounds.Y + 6;
                        DrawPageButton(e.SpriteBatch, btnX, btnY);
                    }
                }
            }
            // 2. TAMPILKAN TOMBOL DI MENU PETI / CHEST / KULKAS (ItemGrabMenu)
            else if (Game1.activeClickableMenu is ItemGrabMenu grabMenu && grabMenu.inventory != null)
            {
                var inv = grabMenu.inventory;
                if (inv.inventory.Count >= 12)
                {
                    int btnX = inv.inventory[11].bounds.Right + 12;
                    int btnY = inv.inventory[0].bounds.Y + 6;
                    DrawPageButton(e.SpriteBatch, btnX, btnY);
                }
            }
        }

        private void DrawPageButton(SpriteBatch b, int x, int y)
        {
            int btnWidth = 54;
            int btnHeight = 54;
            pageButtonBounds = new Rectangle(x, y, btnWidth, btnHeight);

            // Gambar kotak tombol kayu resmi Stardew Valley
            IClickableMenu.drawTextureBox(
                b,
                Game1.menuTexture,
                new Rectangle(0, 256, 60, 60),
                pageButtonBounds.X,
                pageButtonBounds.Y,
                pageButtonBounds.Width,
                pageButtonBounds.Height,
                Color.White,
                1f,
                false
            );

            // Gambar Teks Halaman [1/2] atau [2/2] Emas
            string label = (currentPage == 1) ? "1/2" : "2/2";
            Vector2 textSize = Game1.smallFont.MeasureString(label);
            Vector2 textPos = new Vector2(
                pageButtonBounds.X + (btnWidth - textSize.X) / 2,
                pageButtonBounds.Y + (btnHeight - textSize.Y) / 2
            );

            b.DrawString(Game1.smallFont, label, textPos, Color.Gold);
        }

        private void Ensure48Slots()
        {
            if (Game1.player.MaxItems == 48)
            {
                while (Game1.player.Items.Count < 48)
                    Game1.player.Items.Add(null);
            }
        }

        private void SwitchPage(int targetPage)
        {
            if (Game1.player.MaxItems != 48 || targetPage == currentPage) return;
            Ensure48Slots();

            Game1.playSound("shwip");

            if (targetPage == 2) // Pindah ke Halaman 2 (Baris 2-4 / Slot 13-48)
            {
                List<Item> row1 = new List<Item>();
                for (int i = 0; i < 12; i++)
                    row1.Add(Game1.player.Items[i]);

                for (int i = 12; i < 48; i++)
                    Game1.player.Items[i - 12] = Game1.player.Items[i];

                for (int i = 0; i < 12; i++)
                    Game1.player.Items[36 + i] = row1[i];

                currentPage = 2;
            }
            else if (targetPage == 1) // Kembali ke Halaman 1 (Baris 1-3 / Slot 1-36)
            {
                List<Item> row4 = new List<Item>();
                for (int i = 36; i < 48; i++)
                    row4.Add(Game1.player.Items[i]);

                for (int i = 35; i >= 0; i--)
                    Game1.player.Items[i + 12] = Game1.player.Items[i];

                for (int i = 0; i < 12; i++)
                    Game1.player.Items[i] = row4[i];

                currentPage = 1;
            }
        }

        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady || e.Button != SButton.MouseLeft) return;

            Point touchPos = Game1.getMousePosition();

            // 1. Deteksi Klik Tombol Halaman [1/2] / [2/2] di Menu Tas ATAU Menu Peti (Chest)
            if (Game1.player.MaxItems == 48 && (Game1.activeClickableMenu is GameMenu || Game1.activeClickableMenu is ItemGrabMenu))
            {
                if (pageButtonBounds.Contains(touchPos))
                {
                    Helper.Input.Suppress(e.Button);
                    int next = (currentPage == 1) ? 2 : 1;
                    SwitchPage(next);
                    return;
                }
            }

            // 2. Interaksi Beli Tas 48 Slot di Meja Pierre
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
}
