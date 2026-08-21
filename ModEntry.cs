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
        private bool isPage2Active = false; // false = Halaman 1, true = Halaman 2
        private Rectangle pageButtonBounds;

        public override void Entry(IModHelper helper)
        {
            helper.Events.Display.RenderedWorld += OnRenderedWorld;
            helper.Events.Display.MenuChanged += OnMenuChanged;
            helper.Events.Display.RenderedActiveMenu += OnRenderedActiveMenu;
            helper.Events.Input.ButtonPressed += OnButtonPressed;

            try
            {
                string imagePath = Path.Combine(helper.DirectoryPath, "backpack.png");
                backpackTexture = File.Exists(imagePath)
                    ? Texture2D.FromStream(Game1.graphics.GraphicsDevice, File.OpenRead(imagePath))
                    : helper.ModContent.Load<Texture2D>("backpack.png");
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

                e.SpriteBatch.Draw(backpackTexture, screenPos, null, Color.White, 0f, Vector2.Zero, 3.0f, SpriteEffects.None, 0.86f);

                if (Game1.player.Tile.Y >= 17 && Math.Abs(Game1.player.Tile.X - 7) <= 2)
                {
                    Game1.player.draw(e.SpriteBatch);
                }
            }
        }

        private void OnMenuChanged(object sender, MenuChangedEventArgs e)
        {
            ResetToPage1();
            Ensure48Slots();
        }

        private void OnRenderedActiveMenu(object sender, RenderedActiveMenuEventArgs e)
        {
            if (Game1.player.MaxItems != 48) return;
            Ensure48Slots();

            // 1. Menu Tas Utama (GameMenu)
            if (Game1.activeClickableMenu is GameMenu gameMenu && gameMenu.currentTab == 0)
            {
                if (gameMenu.GetCurrentPage() is InventoryPage invPage && invPage.inventory != null)
                {
                    var inv = invPage.inventory;
                    if (inv.inventory.Count >= 12)
                    {
                        int btnX = inv.inventory[11].bounds.Right + 8;
                        int btnY = inv.inventory[0].bounds.Y;
                        DrawPageButton(e.SpriteBatch, btnX, btnY);
                    }
                }
            }
            // 2. Menu Peti / Chest / Kulkas (ItemGrabMenu)
            else if (Game1.activeClickableMenu is ItemGrabMenu grabMenu && grabMenu.inventory != null)
            {
                var inv = grabMenu.inventory;
                if (inv.inventory.Count >= 12)
                {
                    int btnX = inv.inventory[11].bounds.Right + 8;
                    int btnY = inv.inventory[0].bounds.Y;
                    DrawPageButton(e.SpriteBatch, btnX, btnY);
                }
            }
        }

        private void DrawPageButton(SpriteBatch b, int x, int y)
        {
            int btnSize = 52;
            pageButtonBounds = new Rectangle(x, y, btnSize, btnSize);

            // Kotak tombol merah
            IClickableMenu.drawTextureBox(
                b,
                Game1.menuTexture,
                new Rectangle(0, 256, 60, 60),
                pageButtonBounds.X,
                pageButtonBounds.Y,
                pageButtonBounds.Width,
                pageButtonBounds.Height,
                new Color(235, 60, 50),
                1f,
                false
            );

            string label = isPage2Active ? "2" : "1";
            SpriteFont font = Game1.dialogueFont;
            Vector2 textSize = font.MeasureString(label);
            Vector2 textPos = new Vector2(
                pageButtonBounds.X + (btnSize - textSize.X) / 2,
                pageButtonBounds.Y + (btnSize - textSize.Y) / 2 - 2
            );

            b.DrawString(font, label, new Vector2(textPos.X + 2, textPos.Y + 2), Color.Black);
            b.DrawString(font, label, textPos, Color.White);
        }

        private void Ensure48Slots()
        {
            if (Game1.player.MaxItems == 48)
            {
                while (Game1.player.Items.Count < 48)
                    Game1.player.Items.Add(null);
            }
        }

        // SISTEM TUKAR 2 ARAH MURNI (100% AMAN DARI ITEM HILANG)
        private void TogglePage()
        {
            if (Game1.player.MaxItems != 48) return;
            Ensure48Slots();

            Game1.playSound("shwip");

            // Tukar langsung isi Baris 3 (index 24..35) dengan Baris 4 (index 36..47)
            for (int i = 0; i < 12; i++)
            {
                Item temp = Game1.player.Items[24 + i];
                Game1.player.Items[24 + i] = Game1.player.Items[36 + i];
                Game1.player.Items[36 + i] = temp;
            }

            isPage2Active = !isPage2Active;
        }

        private void ResetToPage1()
        {
            if (isPage2Active && Game1.player.MaxItems == 48)
            {
                Ensure48Slots();
                for (int i = 0; i < 12; i++)
                {
                    Item temp = Game1.player.Items[24 + i];
                    Game1.player.Items[24 + i] = Game1.player.Items[36 + i];
                    Game1.player.Items[36 + i] = temp;
                }
                isPage2Active = false;
            }
        }

        private void OrganizeAll48Slots()
        {
            ResetToPage1();
            Ensure48Slots();

            // Sortir resmi seluruh 48 slot
            ItemGrabMenu.organizeItemsInList(Game1.player.Items);
            Game1.playSound("Ship");
            Game1.showGlobalMessage("Inventory Organized!");
        }

        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady || e.Button != SButton.MouseLeft) return;

            Point mousePos = Game1.getMousePosition();
            Vector2 scaledVec = Utility.ModifyCoordinatesForUIScale(new Vector2(mousePos.X, mousePos.Y));
            Point uiPos = new Point((int)scaledVec.X, (int)scaledVec.Y);

            // 1. KLIK TOMBOL MERAH HALAMAN (DUAL DETEKSI)
            if (Game1.player.MaxItems == 48 && (Game1.activeClickableMenu is GameMenu || Game1.activeClickableMenu is ItemGrabMenu))
            {
                Rectangle touchArea = new Rectangle(pageButtonBounds.X - 10, pageButtonBounds.Y - 10, pageButtonBounds.Width + 20, pageButtonBounds.Height + 20);
                if (touchArea.Contains(mousePos) || touchArea.Contains(uiPos))
                {
                    Helper.Input.Suppress(e.Button);
                    TogglePage();
                    return;
                }
            }

            // 2. KLIK TOMBOL SORTIR DI MENU TAS UTAMA
            if (Game1.activeClickableMenu is GameMenu gm && gm.currentTab == 0)
            {
                if (gm.GetCurrentPage() is InventoryPage invPage && invPage.organizeButton != null)
                {
                    Rectangle orgArea = new Rectangle(invPage.organizeButton.bounds.X - 12, invPage.organizeButton.bounds.Y - 12, invPage.organizeButton.bounds.Width + 24, invPage.organizeButton.bounds.Height + 24);
                    if (orgArea.Contains(mousePos) || orgArea.Contains(uiPos))
                    {
                        Helper.Input.Suppress(e.Button);
                        OrganizeAll48Slots();
                        return;
                    }
                }
            }

            // 3. KLIK TOMBOL SORTIR DI MENU PETI (CHEST)
            if (Game1.activeClickableMenu is ItemGrabMenu grabMenu && grabMenu.organizeButton != null)
            {
                Rectangle orgArea = new Rectangle(grabMenu.organizeButton.bounds.X - 12, grabMenu.organizeButton.bounds.Y - 12, grabMenu.organizeButton.bounds.Width + 24, grabMenu.organizeButton.bounds.Height + 24);
                if (orgArea.Contains(mousePos) || orgArea.Contains(uiPos))
                {
                    Helper.Input.Suppress(e.Button);
                    OrganizeAll48Slots();
                    return;
                }
            }

            // 4. Beli Tas 48 Slot di Meja Pierre
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
