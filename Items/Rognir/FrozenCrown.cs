﻿using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using static Terraria.ModLoader.ModContent;


namespace Void.Items.Rognir
{
    class FrozenCrown : ModItem
	{
		public override void AddRecipes()
		{
			ModRecipe recipe = new ModRecipe(mod);
			recipe.AddIngredient(ItemID.GoldCrown, 1);
			recipe.AddIngredient(ItemID.IceBlock, 20);
			recipe.AddIngredient(ItemID.Bone, 50);
			recipe.AddTile(TileID.WorkBenches);
			recipe.SetResult(ItemType<FrozenCrown>());
			recipe.AddRecipe();

			recipe = new ModRecipe(mod);
			recipe.AddIngredient(ItemID.PlatinumCrown, 1);
			recipe.AddIngredient(ItemID.IceBlock, 20);
			recipe.AddIngredient(ItemID.Bone, 50);
			recipe.AddTile(TileID.WorkBenches);
			recipe.SetResult(ItemType<FrozenCrown>());
			recipe.AddRecipe();
		}
		public override void SetStaticDefaults()
		{
			Tooltip.SetDefault("The crown yearns for the cold.");
			ItemID.Sets.SortingPriorityBossSpawns[item.type] = 13; // This helps sort inventory know this is a boss summoning item
		}

		public override void SetDefaults()
		{
			item.width = 20;
			item.height = 20;
			item.maxStack = 20;
			item.rare = ItemRarityID.Cyan;
			item.useTime = 45;
			item.useAnimation = 45;
			item.useStyle = ItemUseStyleID.HoldingUp;
			item.UseSound = SoundID.Item44;
			item.consumable = true;
		}

		// We use the CanUseItem hook to prevent a player from using this item while the boss is present in the world
		public override bool CanUseItem(Player player)
		{
			if (NPC.AnyNPCs(NPCType<NPCs.Rognir.RognirBoss>()) | !player.ZoneSnow | Main.dayTime)
			{
				return false;
			}
			return true;
		}

		// Defines what happens when the item is used
		public override bool UseItem(Player player)
		{
			NPC.SpawnOnPlayer(player.whoAmI, NPCType<NPCs.Rognir.RognirBoss>());
			Main.PlaySound(SoundID.Roar, player.position, 0);
			return true;
		}
	}
}
