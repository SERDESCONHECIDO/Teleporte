using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Objects;
using Microsoft.Xna.Framework;

namespace TeleportSigns
{
    public class ModEntry : Mod
    {
        internal static IMonitor ModMonitor;
        // Dicionário: Texto da placa -> Lista de posições (Localização, Tile)
        internal static Dictionary<string, List<Tuple<string, Vector2>>> SignRegistry = new();

        public override void Entry(IModHelper helper)
        {
            ModMonitor = this.Monitor;
            var harmony = new Harmony(this.ModManifest.UniqueID);
            harmony.PatchAll();
        }

        // Limpa o registro ao carregar um save para evitar dados antigos
        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            SignRegistry.Clear();
        }
    }

    // Patch para interceptar a interação do jogador com objetos no mapa
    [HarmonyPatch(typeof(GameLocation), nameof(GameLocation.checkAction))]
    public static class CheckActionPatch
    {
        static bool Prefix(GameLocation __instance, Location tileLocation, Farmer who, ref bool __result)
        {
            // Verifica se o objeto na posição é uma Placa de Texto (Sign)
            if (__instance.Objects.TryGetValue(tileLocation, out var obj) && obj is Sign sign)
            {
                string text = sign.text?.Trim();
                if (string.IsNullOrWhiteSpace(text))
                    return true; // Se estiver vazia, deixa o comportamento normal (abrir edição)

                // Garante que o texto não exceda 60 caracteres (limite do jogo)
                if (text.Length > 60) text = text.Substring(0, 60);

                // Remove entradas antigas desta placa para evitar duplicatas
                foreach (var list in ModEntry.SignRegistry.Values)
                {
                    list.RemoveAll(entry => entry.Item1 == __instance.NameOrUniqueName && entry.Item2 == new Vector2(tileLocation.X, tileLocation.Y));
                }

                // Adiciona esta placa ao registro
                if (!ModEntry.SignRegistry.ContainsKey(text))
                    ModEntry.SignRegistry[text] = new List<Tuple<string, Vector2>>();

                ModEntry.SignRegistry[text].Add(new Tuple<string, Vector2>(__instance.NameOrUniqueName, new Vector2(tileLocation.X, tileLocation.Y)));

                // Verifica se há exatamente duas placas com este texto
                var locations = ModEntry.SignRegistry[text];
                if (locations.Count == 1)
                {
                    Game1.showRedMessage($"Placa '{text}' registrada. Coloque outra placa com o mesmo texto para conectar.");
                    __result = true;
                    return false; // Impede a abertura da edição da placa
                }
                else if (locations.Count == 2)
                {
                    // Encontra a "outra" placa (a que não é a atual)
                    var target = locations.FirstOrDefault(l => l.Item1 != __instance.NameOrUniqueName || l.Item2 != new Vector2(tileLocation.X, tileLocation.Y));

                    if (target != null)
                    {
                        // Teleporta o jogador para a outra placa
                        Game1.warpFarmer(target.Item1, (int)target.Item2.X, (int)target.Item2.Y, 2); // 2 = direção para baixo
                        Game1.showGlobalMessage($"Teleportado para '{text}'!");
                    }
                    __result = true;
                    return false;
                }
                else if (locations.Count > 2)
                {
                    Game1.showRedMessage($"Erro: Existem mais de duas placas com o texto '{text}'. Remova o excesso.");
                    __result = true;
                    return false;
                }
            }

            return true; // Executa o comportamento original para outros objetos
        }
    }
}
