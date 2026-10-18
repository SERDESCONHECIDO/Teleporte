using System;
using System.Collections.Generic;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using Microsoft.Xna.Framework;

namespace PressurePlateTeleporter
{
    public class ModEntry : Mod
    {
        private const string ItemId = "(BC)PressurePlate_Item";

        private readonly List<string> _orderedPlates = new();
        private int _warpCooldown = 0;

        public override void Entry(IModHelper helper)
        {
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            helper.Events.World.ObjectListChanged += OnObjectListChanged;
            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
        }

        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            _orderedPlates.Clear();
        }

        // O tipo correto é ObjectListChangedEventArgs, e não WorldObjectListChangedEventArgs
        private void OnObjectListChanged(object sender, ObjectListChangedEventArgs e)
        {
            // Quando uma placa é colocada no chão
            foreach (var added in e.Added)
            {
                if (added.Value.QualifiedItemId == ItemId)
                {
                    string key = $"{e.Location.NameOrUniqueName}|{added.Key.X}|{added.Key.Y}";
                    _orderedPlates.Add(key); // Adiciona no final da fila
                }
            }

            // Quando uma placa é quebrada/retirada
            foreach (var removed in e.Removed)
            {
                if (removed.Value.QualifiedItemId == ItemId)
                {
                    string key = $"{e.Location.NameOrUniqueName}|{removed.Key.X}|{removed.Key.Y}";
                    _orderedPlates.Remove(key); // Remove da fila
                }
            }
        }

        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (_warpCooldown > 0)
            {
                _warpCooldown--;
                return;
            }

            if (!Context.IsWorldReady || !Context.IsPlayerFree) return;

            string currentKey = $"{Game1.currentLocation.NameOrUniqueName}|{(int)Game1.player.Tile.X}|{(int)Game1.player.Tile.Y}";

            int index = _orderedPlates.IndexOf(currentKey);
            
            if (index != -1)
            {
                int nextIndex = (index + 1) % _orderedPlates.Count;
                
                if (nextIndex != index)
                {
                    string targetKey = _orderedPlates[nextIndex];
                    var parts = targetKey.Split('|');
                    
                    string locationName = parts[0];
                    int x = int.Parse(parts[1]);
                    int y = int.Parse(parts[2]);

                    Game1.warpFarmer(locationName, x, y, 2);
                    Game1.showGlobalMessage("Zuum!");
                    
                    _warpCooldown = 60; 
                }
            }
        }
    }
}