using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
        internal static Dictionary<string, List<Tuple<string, Vector2>>> SignRegistry = new();

        public override void Entry(IModHelper helper)
        {
            ModMonitor = this.Monitor;
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            helper.Events.Input.ButtonPressed += OnButtonPressed;
        }

        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            SignRegistry.Clear();
        }

        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            // Verifica se é o botão de ação (clique esquerdo / botão A)
            if (!e.Button.IsActionButton()) return;
            if (!Context.IsWorldReady) return;

            // Usa o campo estático do jogo, que é mais confiável
            Vector2 tile = Game1.currentCursorTile;

            if (Game1.currentLocation.Objects.TryGetValue(tile, out var obj) && obj is Sign sign)
            {
                string text = GetSignText(sign)?.Trim();
                if (string.IsNullOrWhiteSpace(text)) return;

                if (text.Length > 60) text = text.Substring(0, 60);

                // Remove registros antigos desta mesma placa
                foreach (var list in SignRegistry.Values)
                {
                    list.RemoveAll(entry => entry.Item1 == Game1.currentLocation.NameOrUniqueName && entry.Item2 == tile);
                }

                if (!SignRegistry.ContainsKey(text))
                    SignRegistry[text] = new List<Tuple<string, Vector2>>();

                SignRegistry[text].Add(new Tuple<string, Vector2>(Game1.currentLocation.NameOrUniqueName, tile));

                var locations = SignRegistry[text];
                if (locations.Count == 1)
                {
                    Game1.showRedMessage($"Placa '{text}' registrada. Coloque outra placa com o mesmo texto.");
                    this.Helper.Input.Suppress(e.Button);
                }
                else if (locations.Count == 2)
                {
                    var target = locations.FirstOrDefault(l => l.Item1 != Game1.currentLocation.NameOrUniqueName || l.Item2 != tile);
                    if (target != null)
                    {
                        Game1.warpFarmer(target.Item1, (int)target.Item2.X, (int)target.Item2.Y, 2);
                        Game1.showGlobalMessage("Teleportado!");
                    }
                    this.Helper.Input.Suppress(e.Button);
                }
                else if (locations.Count > 2)
                {
                    Game1.showRedMessage($"Erro: Mais de duas placas com o texto '{text}'.");
                    this.Helper.Input.Suppress(e.Button);
                }
            }
        }

        internal static string GetSignText(Sign sign)
        {
            var prop = typeof(Sign).GetProperty("text", BindingFlags.Public | BindingFlags.Instance);
            if (prop != null && prop.PropertyType == typeof(string))
                return (string)prop.GetValue(sign);

            var field = typeof(Sign).GetField("text", BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null && field.FieldType == typeof(string))
                return (string)field.GetValue(sign);

            var field2 = typeof(Sign).GetField("signText", BindingFlags.NonPublic | BindingFlags.Instance);
            if (field2 != null && field2.FieldType == typeof(string))
                return (string)field2.GetValue(sign);

            return null;
        }
    }
}