using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ContentMod.Items
{
    class VikingCrown : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Makes undead in the frozen biome friendly.");
        }

        public override void SetDefaults()
        {
            item.width = 20;
            item.height = 20;
            item.accessory = true;
            item.value = Item.sellPrice(silver: 30);
            item.rare = ItemRarityID.Expert;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<VoidPlayer>().vikingCrown = true;
        }
    }
}
