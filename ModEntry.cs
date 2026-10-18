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
        private const string ItemId = "(BC)Teleportador_Item";

        // Lista de teletransportadores que estão sozinhos no mapa
        private List<TeleportPad> _unpairedPads = new();
        
        // Dicionário que guarda os pares conectados
        private Dictionary<TeleportPad, TeleportPad> _connectedPairs = new();

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
            // Verifica se algum objeto foi ADICIONADO (colocado no chão)
            foreach (var added in e.Added)
            {
                if (added.Value.ItemId == ItemId)
                {
                    var newPad = new TeleportPad(e.Location.NameOrUniqueName, added.Key);
                    RegisterNewPad(newPad);
                }
            }

            // Verifica se algum objeto foi REMOVIDO (quebrado)
            foreach (var removed in e.Removed)
            {
                if (removed.Value.ItemId == ItemId)
                {
                    var brokenPad = new TeleportPad(e.Location.NameOrUniqueName, removed.Key);
                    UnregisterPad(brokenPad);
                }
            }
        }

        private void RegisterNewPad(TeleportPad newPad)
        {
            if (_unpairedPads.Count > 0)
            {
                // Pega o primeiro teletransportador solteiro da fila
                var partner = _unpairedPads[0];
                _unpairedPads.RemoveAt(0);

                // Conecta os dois
                _connectedPairs[newPad] = partner;
                _connectedPairs[partner] = newPad;
                Game1.showGlobalMessage("Dois teletransportadores foram conectados!");
            }
            else
            {
                // Se não tem ninguém solteiro, ele entra na fila de espera
                _unpairedPads.Add(newPad);
                Game1.showGlobalMessage("Teletransportador aguardando conexão...");
            }
        }

        private void UnregisterPad(TeleportPad brokenPad)
        {
            // Se a máquina quebrada estava sozinha, só a remove da fila
            if (_unpairedPads.Contains(brokenPad))
            {
                _unpairedPads.Remove(brokenPad);
                return;
            }

            // Se ela estava conectada, desconecta o par
            if (_connectedPairs.TryGetValue(brokenPad, out var partner))
            {
                _connectedPairs.Remove(brokenPad);
                _connectedPairs.Remove(partner);

                // O parceiro que sobrou volta para a fila de solteiros
                _unpairedPads.Add(partner);
                Game1.showGlobalMessage("Um teletransportador foi destruído. O par voltou a ficar solteiro.");
            }
        }

        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (!e.Button.IsActionButton() || !Context.IsWorldReady) return;

            Vector2 tile = this.Helper.Input.GetCursorPosition().GrabTile;

            if (Game1.currentLocation.Objects.TryGetValue(tile, out var obj) && obj.ItemId == ItemId)
            {
                var clickedPad = new TeleportPad(Game1.currentLocation.NameOrUniqueName, tile);

                if (_connectedPairs.TryGetValue(clickedPad, out var targetPad))
                {
                    // Teleporta o jogador para o par conectado
                    Game1.warpFarmer(targetPad.Location, (int)targetPad.Tile.X, (int)targetPad.Tile.Y, 2);
                    Game1.showGlobalMessage("Teleportado!");
                    this.Helper.Input.Suppress(e.Button); // Impede que a interface da máquina abra
                }
                else
                {
                    Game1.showRedMessage("Este teletransportador ainda não está conectado.");
                    this.Helper.Input.Suppress(e.Button);
                }
            }
        }
    }

    // Classe auxiliar para representar a posição exata da máquina no mapa
    public class TeleportPad
    {
        public string Location { get; set; }
        public Vector2 Tile { get; set; }

        public TeleportPad(string location, Vector2 tile)
        {
            Location = location;
            Tile = tile;
        }

        // Sobrescrevemos o Equals para que o Dicionário saiba comparar as posições corretamente
        public override bool Equals(object obj)
        {
            if (obj is not TeleportPad other) return false;
            return Location == other.Location && Tile == other.Tile;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Location, Tile);
        }
    }
}