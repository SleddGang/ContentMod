using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using static Terraria.ModLoader.ModContent;
using Terraria.ID;

namespace Void.Items
{
	/* The internal class that defines the stats of the Frozen Hook grappling hook item and
	   establishes the hook projectile that this grappling hook shoots*/
	internal class FrozenHookItem : ModItem
    {

        private const int maxHooks = 3;
        private const float maxRange = 420f;
        private const float pullSpeed = 12f;

        /// <summary>
        /// Sets the static default values for the hook projectile shot by the grappling hook item.
        /// </summary>
        public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Frozen Hook");
		}

		/// <summary>
		/// Sets the default values for the hook projectile shot by the grappling hook item.
		/// </summary>
		public override void SetDefaults()
        {
            item.shootSpeed = 20f;
            item.shoot = ProjectileType<FrozenHookProjectile>();
            item.damage = 9;
            item.knockBack = 13;
			item.rare = ItemRarityID.LightPurple;
			item.melee = true;
		}

		public override void ModifyTooltips(List<TooltipLine> tooltips)
		{
			TooltipLine reach = new TooltipLine(mod, "Reach", "Reach: " + (maxRange/16).ToString());
			TooltipLine totalHooks = new TooltipLine(mod, "TotalHooks", "Max Hooks: " + maxHooks.ToString());
			TooltipLine launchVelocity = new TooltipLine(mod, "LaunchVelocity", "Launch Velocity: " + item.shootSpeed);
			TooltipLine pullVelocity = new TooltipLine(mod, "PullVelocity", "Pull Velocity: " + pullSpeed.ToString());
			tooltips.Add(totalHooks);
			tooltips.Add(reach);
			tooltips.Add(launchVelocity);
			tooltips.Add(pullVelocity);
		}
	}

	/* The internal class that defines the stats and behavior of the hook that is shot by the
	   Frozen Hook grappling hook item*/
    internal class FrozenHookProjectile : ModProjectile
    {
		// The hook's defined stats. Initilaized as variables here to make later stat updates easier
		private const int maxHooks		 = 3;
		private const float maxRange	 = 420f;
		private const float pullSpeed	 = 12f;
		private const float retreatSpeed = 16f;

		/// <summary>
		/// Sets the static default values for the hook projectile shot by the grappling hook item.
		/// </summary>
        public override void SetStaticDefaults()
        {
            Main.projHook[projectile.type] = true;
            DisplayName.SetDefault("${ProjectileName.FrozenAnchorHook}");
        }
		/// <summary>
		/// Sets the default values for the hook projectile shot by the grappling hook item.
		/// </summary>
        public override void SetDefaults()
        {
			projectile.netImportant = true;			// Updates server when a new player joins so that the new player can see the active projectile
            projectile.width = 36;					// Width of hook hitbox
            projectile.height = 45;					// Height of hook hitbox
            projectile.timeLeft *= 10;				// Time left before the projectile dies
            projectile.friendly = true;				// Does not hurt other players/friendly npcs
            projectile.ignoreWater = true;			// Ignores water
            projectile.tileCollide = false;			// Collides with tiles
            projectile.penetrate = -1;				// Penetrates through infinite enemies
            projectile.usesLocalNPCImmunity = true;	// Makes NPCs immune to only the active projectile that hit them rather than to the whole item
            projectile.localNPCHitCooldown = -1;    // Immunity per NPC per projectile lasts until the projectile dies
            projectile.aiStyle = 7;
        }

		/// <summary>
		/// A tModLoader hook method that is called whenever the player presses the grapple button.
		/// This method prevents the player from sending out more hooks than the grappling hook's
		/// maximum number of hooks. The player can always shoot out one additional hook beyond the
		/// grappling hook's maximum, but if that hook grapples a block, the ShouldKillOldestHook
		/// method will kill the oldest hook.
		/// </summary>
		/// <param name="player"> The player who owns the grappling hook</param>
		/// <returns></returns>
		public override bool? CanUseGrapple(Player player)
		{
			int hooksOut = 0;
			for (int i = 0; i < 1000; i++)
			{
				if (Main.projectile[i].active && Main.projectile[i].owner == Main.myPlayer && Main.projectile[i].type == projectile.type)
				{
					hooksOut++;
				}
			}
			return hooksOut <= maxHooks;
		}

        /// <summary>
        /// A tModLoader hook method that is used to set the pull speed of the grappling hook. This will
        /// override any pre-set values for the grappling hook pull speed (hence why the "speed" input
        /// variable is a ref)
        /// </summary>
        /// <param name="player"> The player who owns the grappling hook</param>
        /// <param name="speed"> The grappling hook's speed variable</param>
        public override void GrapplePullSpeed(Player player, ref float speed) => speed = pullSpeed;

        /// <summary>
        /// A tModLoader hook method that is used to set the retreat speed of the grappling hook. This will
        /// override any pre-set values for the grappling hook retreat speed (hence why the "speed" input
        /// variable is a ref)
        /// </summary>
        /// <param name="player"> The player who owns the grappling hook</param>
        /// <param name="speed"> The grappling hook's speed variable</param>
		public override void GrappleRetreatSpeed(Player player, ref float speed) => speed = retreatSpeed;

        /// <summary>
        /// A tModLoader hook method that is used to set the range of the grappling hook.
        /// </summary>
        public override float GrappleRange() => maxRange;

        /// <summary>
        /// This method simply draws the chain between the player and the hook while the hook is active
        /// </summary>
        /// <param name="spriteBatch"> The sprite batch that defines what the chain will look like</param>
        /// <param name="lightColor"> The color of the light if the projectile emits light</param>
        /// <returns></returns>
        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Vector2 playerCenter = Main.player[projectile.owner].MountedCenter;
            Vector2 center = projectile.Center;
            Vector2 distToProj = playerCenter - projectile.Center;
            float projRotation = distToProj.ToRotation() - 1.57f;
            float distance = distToProj.Length();
            while (distance > 30f && !float.IsNaN(distance))
            {
                distToProj.Normalize();                 // get unit vector
                distToProj *= retreatSpeed;             // speed = 16
                center += distToProj;                   // update draw position
                distToProj = playerCenter - center;     // update distance
                distance = distToProj.Length();
                Color drawColor = lightColor;

                // Draws chain
                spriteBatch.Draw(mod.GetTexture("Items/FrozenHookChain"), new Vector2(center.X - Main.screenPosition.X, center.Y - Main.screenPosition.Y),
                    new Rectangle(0,0, Main.chain30Texture.Width, Main.chain30Texture.Height), drawColor, projRotation,
                    new Vector2(Main.chain30Texture.Width * 0.5f, Main.chain30Texture.Height * 0.5f), 1f, SpriteEffects.None, 0f);
            }
            return true;
        }
	}

}