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

            // 3. Força o destaque visual (obrigatório para arrastar no Android)
            harmony.Patch(
                original: AccessTools.Method(typeof(Utility), nameof(Utility.highlightShippableObjects)),
                postfix: new HarmonyMethod(typeof(ModEntry), nameof(HighlightShippable_Postfix))
            );
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

            // Verifica se é a caixa de venda do jogador (global ou pessoal)
            if (__instance == Game1.player.getShippingBin(Game1.player) || 
                (__instance.modData.ContainsKey("furyx639.BetterChests/AutoStore") && 
                 __instance.modData["furyx639.BetterChests/AutoStore"] == "ShippingBin"))
            {
                if (item is Ring || item is Boots || item is MeleeWeapon || item is Tool)
                {
                    // Clona o item e força a entrada no contêiner
                    Item itemToAdd = item.getOne();
                    itemToAdd.Stack = item.Stack;
                    
                    // Usa o método nativo para evitar conflitos com outros mods de caixa
                    __result = __instance.addItem(itemToAdd);
                    return false; 
                }
            }
            return true;
        }

        // --- PATCH 3: Destaque na UI ---
        private static void HighlightShippable_Postfix(Utility __instance, Farmer farmer)
        {
            if (Game1.player == null) return;

            // IInventory da caixa de venda usa .Count e [i] (Regra do Prompt Mestre)
            IInventory shippingBin = Game1.player.getShippingBin(Game1.player);
            if (shippingBin == null) return;

            for (int i = 0; i < shippingBin.Count; i++)
            {
                Item item = shippingBin[i];
                if (item is Ring || item is Boots || item is MeleeWeapon || item is Tool)
                {
                    item.isHighlightedInShop = true;
                }
            }
        }
    }
}
