using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace AndroidBackpackPaging
{
    public class ModEntry : Mod
    {
        public override void Entry(IModHelper helper)
        {
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        }

        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            // Kembalikan kapasitas tas ke 36 slot normal
            Game1.player.MaxItems = 36;
            while (Game1.player.Items.Count > 36)
            {
                Game1.player.Items.RemoveAt(Game1.player.Items.Count - 1);
            }
            Game1.showGlobalMessage("Status: Tas kembali NORMAL 36 Slot! Silakan TIDUR 1 malam untuk menyimpan data.");
        }
    }
}
