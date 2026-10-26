using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace VenderEquipamentos
{
    public class FakeShippableObject : StardewValley.Object
    {
        private readonly Item originalItem;

        // Construtor usa ID base válido (390 = Pedra) para não gerar erro de item
        public FakeShippableObject(Item item) : base("390", 1) 
        {
            this.originalItem = item;
            this.Name = item.Name;
            this.Stack = 1; // Equipamentos não empilham na venda
            this.Category = -1; // Misc
            this.Price = System.Math.Max(1, item.sellToStorePrice()); // Valor real de venda
        }

        public override string DisplayName => originalItem.DisplayName;
        public override string getDescription() => originalItem.getDescription();

        // CORREÇÃO DO PREÇO DOBRADO: Delega o cálculo de venda diretamente para o item original.
        // Assim o jogo não aplica multiplicadores de Object duas vezes.
        public override int sellToStorePrice(long specificPlayerID = -1)
        {
            return System.Math.Max(1, originalItem.sellToStorePrice(specificPlayerID));
        }

        public override void drawInMenu(SpriteBatch spriteBatch, Vector2 location, float scaleSize, float transparency, float layerDepth, StackDrawType drawStackNumber, Color color, bool drawShadow)
        {
            originalItem.drawInMenu(spriteBatch, location, scaleSize, transparency, layerDepth, drawStackNumber, color, drawShadow);
        }
    }
}
