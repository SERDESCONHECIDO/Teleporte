using System;
using System.Linq;
using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.Objects;

namespace AnelDoPescador
{
    public class ModEntry : Mod
    {
        private const string RingName = "AnelDoPescador_Item";
        private const string RingQualifiedId = "(O)" + RingName;

        public override void Entry(IModHelper helper)
        {
            helper.Events.Content.AssetRequested += OnAssetRequested;
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;

            var harmony = new Harmony(this.ModManifest.UniqueID);
            harmony.PatchAll();
        }

        private void OnAssetRequested(object sender, AssetRequestedEventArgs e)
        {
            if (e.NameWithoutLocale.IsEquivalentTo("Data/Objects"))
            {
                e.Edit(asset =>
                {
                    var data = asset.AsDictionary<string, ObjectData>().Data;
                    data[RingName] = new ObjectData
                    {
                        Name = RingName,
                        DisplayName = "Anel do Pescador",
                        Description = "Duplica a quantidade de peixes pescados.",
                        Type = "Ring",
                        Category = -96, 
                        Price = 5000,
                        Texture = "Maps/springobjects",
                        SpriteIndex = 526, // Textura do Anel do Ladrão
                        CanBeGivenAsGift = true,
                        CanBeTrashed = true
                    };
                });
            }
        }

        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            // Dá o anel ao jogador caso ele não tenha (para testes)
            if (!Game1.player.Items.Any(i => i?.QualifiedItemId == RingQualifiedId))
            {
                var ring = new StardewValley.Objects.Ring(RingName);
                Game1.player.addItemToInventory(ring);
                this.Monitor.Log("Anel do Pescador adicionado ao inventario.", LogLevel.Info);
            }
        }

        internal static bool HasRingEquipped()
        {
            if (Game1.player == null) return false;
            return Game1.player.leftRing.Value?.QualifiedItemId == RingQualifiedId || 
                   Game1.player.rightRing.Value?.QualifiedItemId == RingQualifiedId;
        }
    }

    // --- PATCH: Duplica o peixe ao terminar a pescaria ---
    [HarmonyPatch]
    public static class FishingDuplicationPatch
    {
        static System.Reflection.MethodBase TargetMethod()
        {
            // Encontra o método 'doDoneFishing' na classe FishingRod
            return AccessTools.GetDeclaredMethods(typeof(StardewValley.Tools.FishingRod))
                .FirstOrDefault(m => m.Name == "doDoneFishing");
        }

        static void Postfix(StardewValley.Tools.FishingRod __instance, object[] __args)
        {
            // Usa object[] para ser imune a mudanças de assinatura na 1.6
            Farmer who = __args.OfType<Farmer>().FirstOrDefault();
            Item fish = __args.OfType<Item>().FirstOrDefault();

            // Proteções básicas
            if (who == null || fish == null) return;
            if (!ModEntry.HasRingEquipped()) return;

            // Categoria -4 é a categoria oficial de "Peixe" no jogo
            if (fish.Category == -4)
            {
                // Adiciona uma cópia do peixe no inventário
                who.addItemToInventory(fish.getOne());
                Game1.showGlobalMessage("O Anel do Pescador duplicou o seu peixe!");
            }
        }
    }
}
