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

        // Lista que guarda a ORDEM em que as placas foram colocadas
        private readonly List<string> _orderedPlates = new();
        
        // Trava de tempo para evitar loop infinito de teleporte
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
                    _orderedPlates.Remove(key); // Remove da fila, e a próxima assume o lugar
                }
            }
        }

        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            // Se o jogo acabou de teleportar, conta 1 segundo (60 ticks) antes de checar de novo
            if (_warpCooldown > 0)
            {
                _warpCooldown--;
                return;
            }

            if (!Context.IsWorldReady || !Context.IsPlayerFree) return;

            // Pega a posição exata onde o jogador está pisando
            string currentKey = $"{Game1.currentLocation.NameOrUniqueName}|{(int)Game1.player.Tile.X}|{(int)Game1.player.Tile.Y}";

            // Verifica se ele pisou em uma das nossas placas
            int index = _orderedPlates.IndexOf(currentKey);
            
            if (index != -1)
            {
                // Se pisou na última placa, volta para a primeira. Caso contrário, vai para a próxima.
                int nextIndex = (index + 1) % _orderedPlates.Count;
                
                if (nextIndex != index) // Só teleporta se houver outra placa
                {
                    string targetKey = _orderedPlates[nextIndex];
                    var parts = targetKey.Split('|');
                    
                    string locationName = parts[0];
                    int x = int.Parse(parts[1]);
                    int y = int.Parse(parts[2]);

                    // Teleporta o jogador para a próxima placa
                    Game1.warpFarmer(locationName, x, y, 2);
                    Game1.showGlobalMessage("Zuum!");
                    
                    // Ativa a trava para o jogador não teleportar em loop infinito
                    _warpCooldown = 60; 
                }
            }
        }
    }
}