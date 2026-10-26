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

        public static bool IsEquipment(Item item)
        {
            if (item == null) return false;
            return item is MeleeWeapon || item is Slingshot || item is Boots || item is Ring 
                || item.QualifiedItemId?.StartsWith("(TR)") == true;
        }
    }

    // --- PATCH 1: Permite que o item seja reconhecido como vendável ---
    [HarmonyPatch(typeof(Item), nameof(Item.canBeShipped))]
    public static class ItemCanBeShippedPatch
    {
        static void Postfix(Item __instance, ref bool __result)
        {
            if (__result) return;
            if (ModEntry.IsEquipment(__instance)) __result = true;
        }
    }

    // --- PATCH 2: Permite que o inventário do baú aceite equipamentos (via reflexão) ---
    [HarmonyPatch]
    public static class InventoryCanAcceptItemPatch
    {
        static IEnumerable<System.Reflection.MethodBase> TargetMethods()
        {
            var invType = AccessTools.TypeByName("StardewValley.Inventories.Inventory");
            if (invType == null) yield break;
            
            var method = AccessTools.GetDeclaredMethods(invType)
                .FirstOrDefault(m => m.Name == "CanAcceptItem" 
                    && m.GetParameters().Length >= 2 
                    && m.GetParameters()[0].ParameterType == typeof(Item));
            
            if (method != null) yield return method;
        }

        static void Postfix(object[] __args, ref bool __result)
        {
            if (__result) return;
            
            Item item = __args.OfType<Item>().FirstOrDefault();
            if (item == null || !ModEntry.IsEquipment(item)) return;

            if (Game1.activeClickableMenu is ItemGrabMenu menu && menu.context is Chest chest)
            {
                var shippingBin = Game1.getFarm()?.getShippingBin(Game1.player) as Chest;
                if (shippingBin != null && chest == shippingBin)
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
            var farm = Game1.getFarm();
            if (farm == null) return true;
            
            var shippingBin = farm.getShippingBin(Game1.player) as Chest;
            if (shippingBin == null || __instance != shippingBin || item == null) return true;
            if (!ModEntry.IsEquipment(item)) return true;

            var inv = __instance.Items;
            for (int i = 0; i < inv.Count; i++)
            {
                if (inv[i] != null && inv[i].canStackWith(item))
                {
                    int amount = Math.Min(item.Stack, inv[i].maximumStackSize() - inv[i].Stack);
                    if (amount > 0)
                    {
                        inv[i].Stack += amount;
                        item.Stack -= amount;
                        if (item.Stack <= 0) { __result = null; return false; }
                    }
                }
            }

            for (int i = 0; i < inv.Count; i++)
            {
                if (inv[i] == null)
                {
                    inv[i] = item;
                    __result = null;
                    return false;
                }
            }

            __result = item;
            return false;
        }
    }

    // --- PATCH 4: Substitui equipamentos por objetos "falsos" no menu de resumo ---
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
                        if (ModEntry.IsEquipment(items[i]))
                        {
                            items[i] = new FakeShippableObject(items[i]);
                        }
                    }
                    break;
                }
            }
        }
    }

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
}
