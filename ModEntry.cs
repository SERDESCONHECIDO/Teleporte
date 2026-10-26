using System;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;
using StardewValley.Tools;

namespace VenderEquipamentos
{
    public class ModEntry : Mod
    {
        public override void Entry(IModHelper helper)
        {
            var harmony = new Harmony(this.ModManifest.UniqueID);
            harmony.PatchAll();
        }
    }

    // --- PATCH 1: Permite que o item seja reconhecido como vendável ---
    [HarmonyPatch(typeof(Item), nameof(Item.canBeShipped))]
    public static class ItemCanBeShippedPatch
    {
        static void Postfix(Item __instance, ref bool __result)
        {
            if (__result) return;

            if (__instance is MeleeWeapon || __instance is Slingshot || 
                __instance is Boots || __instance is Ring ||
                __instance.QualifiedItemId?.StartsWith("(TR)") == true)
            {
                __result = true;
            }
        }
    }

    // --- PATCH 2: Força a colocação do item no baú de venda ---
    [HarmonyPatch(typeof(Chest), nameof(Chest.addItem))]
    public static class ChestAddItemPatch
    {
        // CORREÇÃO: O parâmetro __result agora é do tipo Item, como no jogo 1.6
        static bool Prefix(Chest __instance, Item item, ref Item __result)
        {
            var shippingBin = Game1.getFarm().getShippingBin(Game1.player) as Chest;
            if (shippingBin == null || __instance != shippingBin || item == null) return true;

            if (item is MeleeWeapon || item is Slingshot || item is Boots || item is Ring || item.QualifiedItemId?.StartsWith("(TR)") == true)
            {
                // Tenta empilhar (embora equipamentos raramente empilhem)
                for (int i = 0; i < __instance.Items.Count; i++)
                {
                    if (__instance.Items[i] != null && __instance.Items[i].canStackWith(item))
                    {
                        int amount = Math.Min(item.Stack, __instance.Items[i].maximumStackSize() - __instance.Items[i].Stack);
                        if (amount > 0)
                        {
                            __instance.Items[i].Stack += amount;
                            item.Stack -= amount;
                            if (item.Stack <= 0)
                            {
                                __result = null; // null significa sucesso (item totalmente adicionado)
                                return false; 
                            }
                        }
                    }
                }
                
                // Procura um slot vazio no baú
                for (int i = 0; i < __instance.Items.Count; i++)
                {
                    if (__instance.Items[i] == null)
                    {
                        __instance.Items[i] = item;
                        __result = null; // null significa sucesso
                        return false; 
                    }
                }
                
                __result = item; // Retorna o item se o baú estiver cheio
                return false;
            }
            return true;
        }
    }
}
