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

    // --- PATCH 1: Permite que o item seja colocado na caixa de venda ---
    [HarmonyPatch(typeof(Item), nameof(Item.canBeShipped))]
    public static class ItemCanBeShippedPatch
    {
        static void Postfix(Item __instance, ref bool __result)
        {
            if (__result) return;

            // Libera armas, botas e anéis
            if (__instance is MeleeWeapon || __instance is Slingshot || 
                __instance is Boots || __instance is Ring)
            {
                __result = true;
                return;
            }

            // Libera trinkets via prefixo de ID (evita usar a classe Trinket que não existe no Android)
            if (__instance.QualifiedItemId?.StartsWith("(TR)") == true)
            {
                __result = true;
            }
        }
    }

    // --- Objeto "Falso" para exibir equipamentos no menu de resumo ---
    public class FakeShippableObject : StardewValley.Object
    {
        private readonly Item originalItem;

        // Construtor com ID válido (Pedra) para evitar erro de item inválido
        public FakeShippableObject(Item item) : base("390", item.Stack)
        {
            this.originalItem = item;
            
            // Atribui as propriedades básicas
            this.Name = item.Name;
            this.Price = item.salePrice(); 
            this.Category = -1; // Misc
        }

        public override void drawInMenu(SpriteBatch spriteBatch, Vector2 location, float scaleSize, float transparency, float layerDepth, StackDrawType drawStackNumber, Color color, bool drawShadow)
        {
            // Desenha o item original no lugar do objeto falso
            this.originalItem.drawInMenu(spriteBatch, location, scaleSize, transparency, layerDepth, drawStackNumber, color, drawShadow);
        }
    }

    // --- PATCH 2: Adiciona os equipamentos ao menu de resumo do final do dia ---
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
