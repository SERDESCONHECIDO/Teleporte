using System;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Inventories; // <-- ADICIONADO
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

            // 1. Libera a validação lógica do item para envio
            harmony.Patch(
                original: AccessTools.Method(typeof(Item), nameof(Item.canBeShipped)),
                postfix: new HarmonyMethod(typeof(ModEntry), nameof(CanBeShipped_Postfix))
            );

            // 2. Permite a inserção física no inventário da Caixa de Venda
            harmony.Patch(
                original: AccessTools.Method(typeof(Chest), nameof(Chest.addItem)),
                prefix: new HarmonyMethod(typeof(ModEntry), nameof(ChestAddItem_Prefix))
            );

            // 3. PATCH DE UI REMOVIDO (isHighlightedInShop não existe publicamente na 1.6.15)
        }

        // --- PATCH 1: Lógica de envio ---
        private static void CanBeShipped_Postfix(Item __instance, ref bool __result)
        {
            if (Game1.player == null || __result) return;

            if (__instance is Ring || __instance is Boots || __instance is MeleeWeapon || __instance is Tool)
            {
                __result = true;
            }
        }

        // --- PATCH 2: Inserção na Caixa ---
        private static bool ChestAddItem_Prefix(Chest __instance, Item item, ref Item __result)
        {
            if (Game1.player == null || item == null) return true;

            // CORRIGIDO: usa Game1.getFarm().getShippingBin(...)
            if (__instance == Game1.getFarm().getShippingBin(Game1.player))
            {
                if (item is Ring || item is Boots || item is MeleeWeapon || item is Tool)
                {
                    Item itemToAdd = item.getOne();
                    itemToAdd.Stack = item.Stack;
                    __result = __instance.addItem(itemToAdd);
                    return false;
                }
            }
            return true;
        }
    }
}
