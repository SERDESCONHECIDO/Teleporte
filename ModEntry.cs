using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
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
        static bool Prefix(Chest __instance, Item item, ref bool __result)
        {
            // Se não for a caixa de venda ou o item for nulo, segue o fluxo normal
            if (!__instance.shippingBin || item == null) return true;

            // Se for um equipamento, nós o colocamos manualmente no baú
            if (item is MeleeWeapon || item is Slingshot || item is Boots || item is Ring || item.QualifiedItemId?.StartsWith("(TR)") == true)
            {
                // Tenta empilhar (embora equipamentos não empilhem, alguns trinkets podem)
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
                                __result = true;
                                return false; // Ignora o método original do jogo
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
                        __result = true;
                        return false; // Ignora o método original do jogo
                    }
                }
                
                __result = false; // O baú de venda está cheio
                return false;
            }
            return true; // Outros itens seguem o fluxo normal do jogo
        }
    }

    // --- Objeto "Falso" para exibir equipamentos no menu de resumo do final do dia ---
    public class FakeShippableObject : StardewValley.Object
    {
        private readonly Item originalItem;

        public FakeShippableObject(Item item) : base("390", item.Stack)
        {
            this.originalItem = item;
            this.Name = item.Name;
            this.Price = item.salePrice(); 
            this.Category = -1; // Misc
        }

        public override void drawInMenu(SpriteBatch spriteBatch, Vector2 location, float scaleSize, float transparency, float layerDepth, StackDrawType drawStackNumber, Color color, bool drawShadow)
        {
            this.originalItem.drawInMenu(spriteBatch, location, scaleSize, transparency, layerDepth, drawStackNumber, color, drawShadow);
        }
    }

    // --- PATCH 3: Adiciona os equipamentos ao menu de resumo do final do dia ---
    [HarmonyPatch(typeof(ShippingMenu), "parseItems")]
    public static class ShippingMenuParseItemsPatch
    {
        static void Prefix(ShippingMenu __instance, IList<Item> items)
        {
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                
                bool isEquipment = item is MeleeWeapon || item is Slingshot || 
                                   item is Boots || item is Ring ||
                                   item.QualifiedItemId?.StartsWith("(TR)") == true;

                if (isEquipment)
                {
                    items[i] = new FakeShippableObject(item);
                }
            }
        }
    }
}
