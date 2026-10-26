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

    // --- PATCH 2: Permite que a UI do baú aceite o item (via Object.canAcceptItem) ---
    [HarmonyPatch(typeof(StardewValley.Object), nameof(StardewValley.Object.canAcceptItem))]
    public static class ObjectCanAcceptItemPatch
    {
        static void Postfix(StardewValley.Object __instance, Item item, Farmer who, ref bool __result)
        {
            if (__result) return;

            // Verifica se o objeto é o baú de venda
            if (__instance is Chest chest && chest == Game1.getFarm().getShippingBin(Game1.player) as Chest)
            {
                if (item is MeleeWeapon || item is Slingshot || item is Boots || item is Ring || item.QualifiedItemId?.StartsWith("(TR)") == true)
                {
                    __result = true;
                }
            }
        }
    }

    // --- PATCH 3: Força a colocação do item no baú (Automate/UI) ---
    [HarmonyPatch(typeof(Chest), nameof(Chest.addItem))]
    public static class ChestAddItemPatch
    {
        static bool Prefix(Chest __instance, Item item, ref Item __result)
        {
            var shippingBin = Game1.getFarm().getShippingBin(Game1.player) as Chest;
            if (shippingBin == null || __instance != shippingBin || item == null) return true;

            if (item is MeleeWeapon || item is Slingshot || item is Boots || item is Ring || item.QualifiedItemId?.StartsWith("(TR)") == true)
            {
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
                                __result = null; 
                                return false; 
                            }
                        }
                    }
                }
                
                for (int i = 0; i < __instance.Items.Count; i++)
                {
                    if (__instance.Items[i] == null)
                    {
                        __instance.Items[i] = item;
                        __result = null; 
                        return false; 
                    }
                }
                
                __result = item; 
                return false;
            }
            return true;
        }
    }

    // --- Objeto "Falso" para exibir equipamentos no menu de resumo ---
    public class FakeShippableObject : StardewValley.Object
    {
        private readonly Item originalItem;

        public FakeShippableObject(Item item) : base("390", item.Stack)
        {
            this.originalItem = item;
            this.Name = item.Name;
            this.Price = item.salePrice(); 
            this.Category = -1; 
        }

        public override void drawInMenu(SpriteBatch spriteBatch, Vector2 location, float scaleSize, float transparency, float layerDepth, StackDrawType drawStackNumber, Color color, bool drawShadow)
        {
            this.originalItem.drawInMenu(spriteBatch, location, scaleSize, transparency, layerDepth, drawStackNumber, color, drawShadow);
        }
    }

    // --- PATCH 4: Substitui os equipamentos por objetos falsos ANTES do menu ser criado ---
    [HarmonyPatch]
    public static class ShippingMenuCtorPatch
    {
        static System.Reflection.MethodBase TargetMethod()
        {
            return AccessTools.GetDeclaredConstructors(typeof(ShippingMenu))
                .FirstOrDefault(c => c.GetParameters().Any(p => typeof(IList<Item>).IsAssignableFrom(p.ParameterType)));
        }

        static void Prefix(object[] __args)
        {
            foreach (var arg in __args)
            {
                if (arg is IList<Item> items)
                {
                    for (int i = 0; i < items.Count; i++)
                    {
                        var item = items[i];
                        if (item is MeleeWeapon || item is Slingshot || item is Boots || item is Ring || item.QualifiedItemId?.StartsWith("(TR)") == true)
                        {
                            items[i] = new FakeShippableObject(item);
                        }
                    }
                    break;
                }
            }
        }
    }
}
