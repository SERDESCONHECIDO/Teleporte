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
        public static ModEntry Instance { get; private set; }

        public override void Entry(IModHelper helper)
        {
            Instance = this;
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

    // --- Objeto "Falso" para o menu de resumo ---
    public class FakeShippableObject : StardewValley.Object
    {
        private readonly Item originalItem;

        public FakeShippableObject(Item item) : base("390", item.Stack)
        {
            this.originalItem = item;
            this.Name = item.Name;
            this.Price = Math.Max(1, item.salePrice());
            this.Category = -1; // Misc
            this.Stack = item.Stack;
        }

        public override void drawInMenu(SpriteBatch spriteBatch, Vector2 location, float scaleSize, float transparency, float layerDepth, StackDrawType drawStackNumber, Color color, bool drawShadow)
        {
            this.originalItem.drawInMenu(spriteBatch, location, scaleSize, transparency, layerDepth, drawStackNumber, color, drawShadow);
        }
    }

    // --- PATCH 2: Adiciona os equipamentos ao menu de resumo ---
    [HarmonyPatch(typeof(ShippingMenu), "parseItems")]
    public static class ShippingMenuParseItemsPatch
    {
        // CORREÇÃO: IList items -> IList<Item> items
        static void Postfix(IList<Item> items, ref List<List<Item>> ___categoryItems, ref List<int> ___categoryTotals, ref Dictionary<Item, int> ___itemValues, ref Dictionary<Item, int> ___singleItemValues)
        {
            const int miscCategory = 4; // Categoria "Diversos"
            const int totalCategory = 5; // Categoria "Total"

            foreach (var item in items)
            {
                if (item is StardewValley.Object) continue; // Já processado pelo jogo
                if (!ModEntry.IsEquipment(item)) continue;

                try
                {
                    var fake = new FakeShippableObject(item);
                    int sellPrice = fake.salePrice();
                    int totalPrice = sellPrice * fake.Stack;

                    ___categoryItems[miscCategory].Add(fake);
                    ___categoryItems[totalCategory].Add(fake);
                    ___categoryTotals[miscCategory] += totalPrice;
                    ___categoryTotals[totalCategory] += totalPrice;
                    ___itemValues[fake] = totalPrice;
                    ___singleItemValues[fake] = sellPrice;

                    Game1.stats.ItemsShipped += (uint)fake.Stack;
                    if (fake.countsForShippedCollection())
                        Game1.player.shippedBasic(fake.ItemId, fake.Stack);
                }
                catch (Exception ex)
                {
                    ModEntry.Instance.Monitor.Log($"Erro ao processar equipamento: {ex}", LogLevel.Error);
                }
            }
        }
    }

    // --- PATCH 3: Adiciona o dinheiro extra na virada do dia ---
    [HarmonyPatch(typeof(Game1), "_newDayAfterFade")]
    public static class NewDayAfterFadePatch
    {
        static void Prefix()
        {
            if (Game1.player == null) return;

            var shippingBin = Game1.getFarm()?.getShippingBin(Game1.player);
            if (shippingBin == null) return;

            int extraGained = 0;
            foreach (var item in shippingBin)
            {
                if (item == null || item is StardewValley.Object) continue;
                if (!ModEntry.IsEquipment(item)) continue;

                try
                {
                    extraGained += Math.Max(1, item.salePrice()) * item.Stack;
                }
                catch { }
            }

            if (extraGained > 0)
            {
                Game1.player.Money += extraGained;
            }
        }
    }
}
