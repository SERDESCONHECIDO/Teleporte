using System;
using System.Linq;
using HarmonyLib;
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
            
            // Evento para substituir os itens no fim do dia
            helper.Events.GameLoop.DayEnding += OnDayEnding;

            // Aplica todos os patches Harmony de uma vez
            var harmony = new Harmony(this.ModManifest.UniqueID);
            harmony.PatchAll();
        }

        private void OnDayEnding(object sender, DayEndingEventArgs e)
        {
            // REGRA DE OURO 1.6: Evitar crash no Android verificando player
            if (Game1.player == null) return;
            
            var bin = Game1.getFarm()?.getShippingBin(Game1.player);
            if (bin == null) return;

            // IInventory na 1.6 usa .Count e [i]
            for (int i = 0; i < bin.Count; i++)
            {
                var item = bin[i];
                if (item != null && IsEquipment(item))
                {
                    // Troca o item real pelo fake para o jogo calcular o preço corretamente
                    bin[i] = new FakeShippableObject(item);
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

    // --- PATCH 2: Permite colocar na caixa de venda (CORRIGIDO PARA 1.6) ---
    [HarmonyPatch]
    public static class CanAcceptItemPatch
    {
        static System.Reflection.MethodBase TargetMethod()
        {
            // CORREÇÃO DO CRASH: Procura em Chest primeiro, se não achar, tenta em Object
            var typesToCheck = new[] { typeof(Chest), typeof(StardewValley.Object) };
            
            foreach (var type in typesToCheck)
            {
                var method = AccessTools.GetDeclaredMethods(type)
                    .FirstOrDefault(m => m.Name == "CanAcceptItem" 
                                      && m.GetParameters().Length >= 1 
                                      && m.GetParameters()[0].ParameterType == typeof(Item));
                if (method != null)
                {
                    ModEntry.Instance?.Monitor.Log($"[VenderEquipamentos] CanAcceptItem encontrado em {type.Name}", LogLevel.Trace);
                    return method;
                }
            }
            throw new Exception("CanAcceptItem não encontrado (1.6.15 mudou a assinatura novamente).");
        }

        // REGRA DE OURO 1.6: Usar object[] __args para não quebrar assinaturas
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
}
