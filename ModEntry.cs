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
        private int touchStartY = -1;
        private bool isSwiping = false;

        public override void Entry(IModHelper helper)
        {
            helper.Events.Display.RenderedWorld += OnRenderedWorld;
            helper.Events.Input.ButtonPressed += OnButtonPressed;
            helper.Events.Input.ButtonReleased += OnButtonReleased;

            // Muat file gambar asli backpack.png
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
            // Tampilkan tas di atas meja Pierre & player di depan tas
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

        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady) return;

            if (e.Button == SButton.MouseLeft)
            {
                // Catat posisi awal sentuhan untuk deteksi Swipe di menu tas
                if (Game1.activeClickableMenu is GameMenu gameMenu && gameMenu.currentTab == 0)
                {
                    touchStartY = Game1.getMousePosition().Y;
                    isSwiping = true;
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
            // DETEKSI GESTUR SWIPE & GESER SELURUH BARIS ITEM
            if (e.Button == SButton.MouseLeft && isSwiping && touchStartY >= 0)
            {
                if (Game1.activeClickableMenu is GameMenu gameMenu && gameMenu.currentTab == 0)
                {
                    int deltaY = touchStartY - Game1.getMousePosition().Y;

                    // Swipe UP (Usap ke Atas minimal 35 piksel) -> Baris 1, 2, 3 bergeser naik
                    if (deltaY > 35)
                    {
                        ShiftInventoryRows(true);
                    }
                    // Swipe DOWN (Usap ke Bawah minimal 35 piksel) -> Baris 1, 2, 3 bergeser turun
                    else if (deltaY < -35)
                    {
                        ShiftInventoryRows(false);
                    }
                }

                isSwiping = false;
                touchStartY = -1;
            }
        }

        private void ShiftInventoryRows(bool forward)
        {
            // Pastikan tas sudah 48 slot sebelum bisa digeser
            if (Game1.player.MaxItems != 48 || Game1.player.Items.Count < 48) return;

            Game1.playSound("shwip");

            if (forward)
            {
                // Swipe UP: Geser semua baris naik (Baris 2, 3, 4 muncul di layar)
                List<Item> row1 = new List<Item>();
                for (int i = 0; i < 12; i++)
                    row1.Add(Game1.player.Items[i]);

                for (int i = 12; i < 48; i++)
                    Game1.player.Items[i - 12] = Game1.player.Items[i];

                for (int i = 0; i < 12; i++)
                    Game1.player.Items[36 + i] = row1[i];
            }
            else
            {
                // Swipe DOWN: Geser semua baris turun (Kembali ke Baris 1, 2, 3)
                List<Item> row4 = new List<Item>();
                for (int i = 36; i < 48; i++)
                    row4.Add(Game1.player.Items[i]);

                for (int i = 35; i >= 0; i--)
                    Game1.player.Items[i + 12] = Game1.player.Items[i];

                for (int i = 0; i < 12; i++)
                    Game1.player.Items[i] = row4[i];
            }
        }
    }
}
