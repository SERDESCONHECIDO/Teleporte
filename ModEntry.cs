using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Objects;
using Microsoft.Xna.Framework;
using xTile.Dimensions;

namespace TeleportSigns
{
    public class ModEntry : Mod
    {
        internal static IMonitor ModMonitor;
        internal static Dictionary<string, List<Tuple<string, Vector2>>> SignRegistry = new();

        public override void Entry(IModHelper helper)
        {
            ModMonitor = this.Monitor;
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            var harmony = new Harmony(this.ModManifest.UniqueID);
            harmony.PatchAll();
        }

        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            SignRegistry.Clear();
        }

        // Método auxiliar para obter o texto da placa de forma robusta
        internal static string GetSignText(Sign sign)
        {
            // Tenta a propriedade pública 'SignText' (padrão na 1.6)
            var prop = typeof(Sign).GetProperty("SignText", BindingFlags.Public | BindingFlags.Instance);
            if (prop != null && prop.PropertyType == typeof(string))
                return (string)prop.GetValue(sign);

            // Fallback: tenta o campo privado 'signText'
            var field = typeof(Sign).GetField("signText", BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null && field.FieldType == typeof(string))
                return (string)field.GetValue(sign);

            // Fallback: tenta a propriedade 'displayText' (algumas versões)
            var prop2 = typeof(Sign).GetProperty("displayText", BindingFlags.Public | BindingFlags.Instance);
            if (prop2 != null && prop2.PropertyType == typeof(string))
                return (string)prop2.GetValue(sign);

            // Fallback: tenta o campo 'text' (versões anteriores)
            var field2 = typeof(Sign).GetField("text", BindingFlags.NonPublic | BindingFlags.Instance);
            if (field2 != null && field2.FieldType == typeof(string))
                return (string)field2.GetValue(sign);

            return null;
        }
    }

    [HarmonyPatch(typeof(GameLocation), nameof(GameLocation.checkAction))]
    public static class CheckActionPatch
    {
        // A assinatura correta na 1.6 inclui o parâmetro 'Rectangle viewport'
        static bool Prefix(GameLocation __instance, Location tileLocation, Rectangle viewport, Farmer who, ref bool __result)
        {
            Vector2 tileVec = new Vector2(tileLocation.X, tileLocation.Y);

            if (__instance.Objects.TryGetValue(tileVec, out var obj) && obj is Sign sign)
            {
                string text = ModEntry.GetSignText(sign)?.Trim();
                if (string.IsNullOrWhiteSpace(text))
                    return true; // Se estiver vazia, deixa o comportamento normal (abrir edição)

                if (text.Length > 60) text = text.Substring(0, 60);

                // Limpa registros antigos desta mesma placa
                foreach (var list in ModEntry.SignRegistry.Values)
                {
                    list.RemoveAll(entry => entry.Item1 == __instance.NameOrUniqueName && entry.Item2 == tileVec);
                }

                if (!ModEntry.SignRegistry.ContainsKey(text))
                    ModEntry.SignRegistry[text] = new List<Tuple<string, Vector2>>();

                ModEntry.SignRegistry[text].Add(new Tuple<string, Vector2>(__instance.NameOrUniqueName, tileVec));

                var locations = ModEntry.SignRegistry[text];
                if (locations.Count == 1)
                {
                    Game1.showRedMessage($"Placa '{text}' registrada. Coloque outra placa com o mesmo texto.");
                    __result = true;
                    return false; // Impede a abertura da edição da placa
                }
                else if (locations.Count == 2)
                {
                    // Encontra a "outra" placa (a que não é a atual)
                    var target = locations.FirstOrDefault(l => l.Item1 != __instance.NameOrUniqueName || l.Item2 != tileVec);

                    if (target != null)
                    {
                        // Teleporta o jogador para a outra placa
                        Game1.warpFarmer(target.Item1, (int)target.Item2.X, (int)target.Item2.Y, 2);
                        Game1.showGlobalMessage($"Teleportado!");
                    }
                    __result = true;
                    return false;
                }
                else if (locations.Count > 2)
                {
                    Game1.showRedMessage($"Erro: Mais de duas placas com o texto '{text}'.");
                    __result = true;
                    return false;
                }
            }
            return true; // Executa o comportamento original para outros objetos
        }
    }
}