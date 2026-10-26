using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
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
            
            // Substitui os itens no exato momento em que o dia termina
            helper.Events.GameLoop.DayEnding += OnDayEnding;

            var harmony = new Harmony(this.ModManifest.UniqueID);
            harmony.PatchAll();
        }

        private void OnDayEnding(object sender, DayEndingEventArgs e)
        {
            if (Game1.player == null) return;
            
            // Pega a caixa de venda do jogador
            var bin = Game1.getFarm()?.getShippingBin(Game1.player);
            if (bin == null) return;

            for (int i = 0; i < bin.Items.Count; i++)
            {
                var item = bin.Items[i];
                if (item != null && IsEquipment(item))
                {
                    // Substitui a arma/anel por um objeto falso que o jogo entende
                    bin.Items[i] = new FakeShippableObject(item);
                }
            }
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

    // --- PATCH 2: Permite que o baú de venda aceite o item na UI ---
    [HarmonyPatch]
    public static class CanAcceptItemPatch
    {
        static System.Reflection.MethodBase TargetMethod()
        {
            // Procura o método 'CanAcceptItem' na classe Object (ignora a assinatura exata)
            return AccessTools.GetDeclaredMethods(typeof(StardewValley.Object))
                .FirstOrDefault(m => m.Name == "CanAcceptItem" && m.GetParameters().Length >= 1 && m.GetParameters()[0].ParameterType == typeof(Item));
        }

        static void Postfix(StardewValley.Object __instance, object[] __args, ref bool __result)
        {
            if (__result) return;
            
            Item item = __args.OfType<Item>().FirstOrDefault();
            if (item == null || !ModEntry.IsEquipment(item)) return;

            if (__instance is Chest chest && chest == Game1.getFarm()?.getShippingBin(Game1.player))
            {
                __result = true;
            }
        }
    }

    // --- PATCH 3: Força a colocação do item no baú (para o Automate) ---
    [HarmonyPatch(typeof(Chest), nameof(Chest.addItem))]
    public static class ChestAddItemPatch
    {
        static bool Prefix(Chest __instance, Item item, ref Item __result)
        {
            var bin = Game1.getFarm()?.getShippingBin(Game1.player);
            if (bin == null || __instance != bin || item == null) return true;
            if (!ModEntry.IsEquipment(item)) return true;

            // Tenta empilhar
            for (int i = 0; i < __instance.Items.Count; i++)
            {
                if (__instance.Items[i] != null && __instance.Items[i].canStackWith(item))
                {
                    int amount = Math.Min(item.Stack, __instance.Items[i].maximumStackSize() - __instance.Items[i].Stack);
                    if (amount > 0)
                    {
                        __instance.Items[i].Stack += amount;
                        item.Stack -= amount;
                        if (item.Stack <= 0) { __result = null; return false; }
                    }
                }
            }

            // Procura slot vazio
            for (int i = 0; i < __instance.Items.Count; i++)
            {
                if (__instance.Items[i] == null)
                {
                    __instance.Items[i] = item;
                    __result = null;
                    return false;
                }
            }

            __result = item; // Baú cheio
            return false;
        }
    }

    // --- Objeto Falso: Faz o jogo 1.6 calcular o preço e mostrar a textura corretamente ---
    public class FakeShippableObject : StardewValley.Object
    {
        private readonly Item originalItem;

        public FakeShippableObject(Item item) : base("390", item.Stack) // 390 = Pedra (ID base válido)
        {
            this.originalItem = item;
            this.Name = item.Name;
            this.Price = Math.Max(1, item.salePrice());
            this.Category = -1; // Misc
            this.Stack = item.Stack;
        }

        public override string DisplayName => originalItem.DisplayName;

        public override void drawInMenu(SpriteBatch spriteBatch, Vector2 location, float scaleSize, float transparency, float layerDepth, StackDrawType drawStackNumber, Color color, bool drawShadow)
        {
            this.originalItem.drawInMenu(spriteBatch, location, scaleSize, transparency, layerDepth, drawStackNumber, color, drawShadow);
        }
    }
}
