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

            // 1. Libera a validação lógica do item para envio
            harmony.Patch(
                original: AccessTools.Method(typeof(Item), nameof(Item.canBeShipped)),
                postfix: new HarmonyMethod(typeof(ModEntry), nameof(CanBeShipped_Postfix))
            );

            // 2. Permite a inserção física na Caixa de Venda (convertendo para Object falso)
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

        // --- PATCH 2: Inserção na Caixa (converte equipamento em Object falso vendável) ---
        private static bool ChestAddItem_Prefix(Chest __instance, Item item, ref Item __result)
        {
            if (Game1.player == null || item == null) return true;

            if (__instance == Game1.getFarm().getShippingBin(Game1.player))
            {
                if (item is Ring || item is Boots || item is MeleeWeapon || item is Tool)
                {
                    int price = item.sellToStorePrice();
                    if (price <= 0) price = 100; // fallback para equipamentos sem preço base

                    try
                    {
                        // CORRIGIDO: 1.6 usa string itemId, não int ParentSheetIndex
                        var fakeItem = new StardewValley.Object(item.ItemId, item.Stack)
                        {
                            Price = price,
                            Name = item.DisplayName
                        };

                        if (fakeItem != null)
                        {
                            __result = __instance.addItem(fakeItem);
                            return false;
                        }
                    }
                    catch (Exception ex)
                    {
                        ModMonitor?.Log($"Falha ao converter {item.DisplayName} em Object vendável: {ex.Message}", LogLevel.Warn);
                    }
                }
            }
            return true;
        }

        // --- PATCH 3: Destaque visual (via reflexão, pois o campo não é público na 1.6.15) ---
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
                {
                    highlightField(item) = true;
                }
            }
        }
    }
}
