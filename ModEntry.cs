using System;
using System.Collections.Generic;
using System.Linq;
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
    }

    [HarmonyPatch(typeof(GameLocation), nameof(GameLocation.checkAction))]
    public static class CheckActionPatch
    {
        static bool Prefix(GameLocation __instance, Location tileLocation, Farmer who, ref bool __result)
        {
            // Converte a posição do tile para Vector2 para buscar o objeto no mapa
            Vector2 tileVec = new Vector2(tileLocation.X, tileLocation.Y);

            if (__instance.Objects.TryGetValue(tileVec, out var obj) && obj is Sign sign)
            {
                // Na versão 1.6, a propriedade de texto da placa é 'displayText'
                string text = sign.displayText?.Trim();
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