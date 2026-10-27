using System;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
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

            // 1. Permite que equipamentos sejam enviados (lógica de "shippable")
            harmony.Patch(
                original: AccessTools.Method(typeof(Item), nameof(Item.canBeShipped)),
                postfix: new HarmonyMethod(typeof(ModEntry), nameof(CanBeShipped_Postfix))
            );

            // 2. Força o destaque visual na mochila (essencial para arrastar no Android)
            harmony.Patch(
                original: AccessTools.Method(typeof(Utility), nameof(Utility.highlightShippableObjects)),
                postfix: new HarmonyMethod(typeof(ModEntry), nameof(HighlightShippable_Postfix))
            );

            // 3. Garante que o equipamento tenha um preço de venda válido no final do dia
            harmony.Patch(
                original: AccessTools.Method(typeof(Item), nameof(Item.sellToStorePrice)),
                postfix: new HarmonyMethod(typeof(ModEntry), nameof(SellToStorePrice_Postfix))
            );
        }

        // --- PATCH 1: Permite o envio ---
        private static void CanBeShipped_Postfix(Item __instance, ref bool __result)
        {
            if (Game1.player == null || __result) return;

            if (__instance is Ring || __instance is Boots || __instance is MeleeWeapon || __instance is Tool)
            {
                __result = true;
            }
        }

        // --- PATCH 2: Destaque na UI (arrastar) ---
        private static void HighlightShippable_Postfix()
        {
            if (Game1.player == null) return;

            var shippingBin = Game1.getFarm().getShippingBin(Game1.player);
            if (shippingBin == null) return;

            // Acessa o campo privado que controla o brilho do item
            var highlightField = AccessTools.FieldRefAccess<Item, bool>("isHighlightedInShop");

            for (int i = 0; i < shippingBin.Count; i++)
            {
                Item item = shippingBin[i];
                if (item is Ring || item is Boots || item is MeleeWeapon || item is Tool)
                {
                    highlightField(item) = true;
                }
            }
        }

        // --- PATCH 3: Preço de venda ---
        private static void SellToStorePrice_Postfix(Item __instance, ref int __result)
        {
            if (Game1.player == null) return;

            // Se for um equipamento e o preço calculado for 0, usa o salePrice como fallback.
            if (__instance is Ring || __instance is Boots || __instance is MeleeWeapon || __instance is Tool)
            {
                if (__result <= 0)
                {
                    __result = Math.Max(1, __instance.salePrice());
                }
            }
        }
    }
}
