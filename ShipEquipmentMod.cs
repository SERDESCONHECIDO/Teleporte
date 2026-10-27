using System;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Inventories;
using StardewValley.Objects;
using StardewValley.Tools;

namespace ShipEquipment
{
    public class ModEntry : Mod
    {
        internal static IMonitor ModMonitor;

        public override void Entry(IModHelper helper)
        {
            ModMonitor = this.Monitor;
            var harmony = new Harmony(this.ModManifest.UniqueID);

            harmony.Patch(
                original: AccessTools.Method(typeof(Item), nameof(Item.canBeShipped)),
                postfix: new HarmonyMethod(typeof(ModEntry), nameof(CanBeShipped_Postfix))
            );

            harmony.Patch(
                original: AccessTools.Method(typeof(Chest), nameof(Chest.addItem)),
                prefix: new HarmonyMethod(typeof(ModEntry), nameof(ChestAddItem_Prefix))
            );

            harmony.Patch(
                original: AccessTools.Method(typeof(Utility), nameof(Utility.highlightShippableObjects)),
                postfix: new HarmonyMethod(typeof(ModEntry), nameof(HighlightShippable_Postfix))
            );
        }

        private static void CanBeShipped_Postfix(Item __instance, ref bool __result)
        {
            if (Game1.player == null || __result) return;
            if (__instance is Ring || __instance is Boots || __instance is MeleeWeapon || __instance is Tool)
                __result = true;
        }

        private static bool ChestAddItem_Prefix(Chest __instance, Item item, ref Item __result)
        {
            if (Game1.player == null || item == null) return true;

            if (__instance == Game1.getFarm().getShippingBin(Game1.player))
            {
                if (item is Ring || item is Boots || item is MeleeWeapon || item is Tool)
                {
                    int price = item.sellToStorePrice();
                    if (price <= 0) price = 100; // fallback para equipamentos sem preço base

                    var fakeItem = new Object(item.ParentSheetIndex, item.Stack)
                    {
                        Price = price,
                        Name = item.DisplayName
                    };

                    __result = __instance.addItem(fakeItem);
                    return false;
                }
            }
            return true;
        }

        private static void HighlightShippable_Postfix()
        {
            if (Game1.player == null) return;

            var shippingBin = Game1.getFarm().getShippingBin(Game1.player);
            if (shippingBin == null) return;

            var highlightField = AccessTools.FieldRefAccess<Item, bool>("isHighlightedInShop");

            for (int i = 0; i < shippingBin.Count; i++)
            {
                Item item = shippingBin[i];
                if (item is Ring || item is Boots || item is MeleeWeapon || item is Tool)
                    highlightField(item) = true;
            }
        }
    }
}
