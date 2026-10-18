using System;
using System.Collections.Generic;
using System.Linq;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Objects;
using Microsoft.Xna.Framework;

namespace TeleportPairs
{
    public class ModEntry : Mod
    {
        // O ID qualificado do item no jogo é "(BC)Teleportador_Item"
        private const string ItemId = "(BC)Teleportador_Item";

        // Usamos uma string como chave: "Localização|X|Y"
        private List<string> _unpairedPads = new();
        private Dictionary<string, string> _connectedPairs = new();

        public override void Entry(IModHelper helper)
        {
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            helper.Events.World.ObjectListChanged += OnObjectListChanged;
            helper.Events.Input.ButtonPressed += OnButtonPressed;
        }

        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            _unpairedPads.Clear();
            _connectedPairs.Clear();
        }

        private void OnObjectListChanged(object sender, ObjectListChangedEventArgs e)
        {
            // Objetos adicionados (colocados no chão)
            foreach (var added in e.Added)
            {
                if (added.Value.QualifiedItemId == ItemId)
                {
                    string key = $"{e.Location.NameOrUniqueName}|{added.Key.X}|{added.Key.Y}";
                    RegisterNewPad(key);
                }
            }

            // Objetos removidos (quebrados)
            foreach (var removed in e.Removed)
            {
                if (removed.Value.QualifiedItemId == ItemId)
                {
                    string key = $"{e.Location.NameOrUniqueName}|{removed.Key.X}|{removed.Key.Y}";
                    UnregisterPad(key);
                }
            }
        }

        private void RegisterNewPad(string newPadKey)
        {
            if (_unpairedPads.Count > 0)
            {
                string partnerKey = _unpairedPads[0];
                _unpairedPads.RemoveAt(0);

                _connectedPairs[newPadKey] = partnerKey;
                _connectedPairs[partnerKey] = newPadKey;
                Game1.showGlobalMessage("Dois teletransportadores foram conectados!");
            }
            else
            {
                _unpairedPads.Add(newPadKey);
                Game1.showGlobalMessage("Teletransportador aguardando conexão...");
            }
        }

        private void UnregisterPad(string brokenPadKey)
        {
            if (_unpairedPads.Contains(brokenPadKey))
            {
                _unpairedPads.Remove(brokenPadKey);
                return;
            }

            if (_connectedPairs.TryGetValue(brokenPadKey, out var partnerKey))
            {
                _connectedPairs.Remove(brokenPadKey);
                _connectedPairs.Remove(partnerKey);

                _unpairedPads.Add(partnerKey);
                Game1.showGlobalMessage("Um teletransportador foi destruído. O par voltou a ficar solteiro.");
            }
        }

        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (!e.Button.IsActionButton() || !Context.IsWorldReady) return;

            Vector2 tile = this.Helper.Input.GetCursorPosition().GrabTile;
            string clickedKey = $"{Game1.currentLocation.NameOrUniqueName}|{tile.X}|{tile.Y}";

            if (Game1.currentLocation.Objects.TryGetValue(tile, out var obj) && obj.QualifiedItemId == ItemId)
            {
                if (_connectedPairs.TryGetValue(clickedKey, out var targetKey))
                {
                    // Separa a chave para obter a localização e as coordenadas
                    var parts = targetKey.Split('|');
                    string locationName = parts[0];
                    int x = int.Parse(parts[1]);
                    int y = int.Parse(parts[2]);

                    Game1.warpFarmer(locationName, x, y, 2);
                    Game1.showGlobalMessage("Teleportado!");
                    this.Helper.Input.Suppress(e.Button);
                }
                else
                {
                    Game1.showRedMessage("Este teletransportador ainda não está conectado.");
                    this.Helper.Input.Suppress(e.Button);
                }
            }
        }
    }
}