﻿using Microsoft.Xna.Framework;
using System;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Void.Projectiles.Rognir;
using static Terraria.ModLoader.ModContent;

namespace Void.NPCs.Rognir
{
	/// <summary>
	/// Class Rognir is the base class of the Rognir boss. 
	/// It defines the defaults and AI for the boss.
	/// </summary>
	[AutoloadBossHead]
    class RognirBoss : ModNPC
    {
        #region Tunables
        private const float rogMaxSpeedOne = 5.0f;			// Rognir's max speed in stage one.
		private const float rogMaxSpeedTwo = 7.5f;			// Rognir's max speed in stage two.
		private const float rogAcceleration = 2000f;		// Rognir's acceleration divider.  A smaller number means a faster acceleration.
		private const float rogDashSpeedOne = 15f;			// Rognir's max dash speed in stage one.
		private const float rogDashSpeedTwo = 25f;          // Rognir's max dash speed in stage two.
		private const float rogSecondDashChance = 0.75f;	// Rognir's chance that he will do another dash in stage two.
		private const float rogSecondDashReduction = 0.25f;	// Rognir's change in dash chance each dash.  Limits the number of dashes Rognir can do.
		private const float rogShardVelocity = 9.0f;        // Rognir's ice shard velocity.
		private const float rogShardSprayMultiplier = 2.5f;      // Sets the rotation mulplier of each shard that is shot when switching stages.  


		private const int rogMinMoveTimer = 60;				// Rognir's minimum move timer
		private const int rogMaxMoveTimer = 90;				// Rognir's maximum move timer.
		private const int rogAttackCoolOne = 105;			// Rognir's attack cooldown for stage one.
		private const int rogAttackCoolTwo = 75;            // Rognir's attack cooldown for stage two.
		private const int rogDashLenght = 60;               // Rognir's dash timer to set the lenght of the dash.
		private const int rogNextDashDelay = 25;			// Sets what the spinTimer will be set to when another dash is going to happen.
		private const int rogChilledLenghtOne = 120;		// Rognir's chilled buff length for stage one.
		private const int rogChilledLenghtTwo = 120;        // Rognir's chilled buff length for stage two.
		private const int rogShardDamage = 10;              // Rognir's ice shard damage.
		private const int rogShardSprayCount = 5;           // Sets the number of shards rognir will shoot out at a time while switching stages.
		private const int rogShardSprayModulus = 10;        // Sets how ofter the shards will be sprayed when switching stages.  A smaller number means more shards.
		private const int rogVikingSpawnCool = 300;         // Rognir's time until next viking spawn.
		private const int rogMaxRange = 500;				// Rognir's max distance he can be from a player before dispawning. Units in feet.
		#endregion

		#region Variables
		private float MoveTimer				// Stores the time until a new movement offset is chosen.
		{
			get => npc.ai[0];
			set => npc.ai[0] = value;
		}
		private float Target				// 0 is targeting above the player.  1 is targeting left of the player. 2 is targeting right of the player.
		{
			get => npc.ai[1];
			set => npc.ai[1] = value;
		}
		private float AnchorID				// Stores the y movement offset.
		{
			get => npc.ai[2];
			set => npc.ai[2] = value;
		}
		private float Stage                 // Stores the current boss fight stage.
		{
			get => npc.ai[3];
			set => npc.ai[3] = value;
		}
		
		private float DashCounter
		{
			get => npc.localAI[0];
			set => npc.localAI[0] = value;
		}

		private int attackCool = 240;		// Stores the cooldown until the next attack.
		private int attack = 0;				// Selects the attack to use.
		private int dashTimer = 0;          // Stores the countdown untl the dash is complete.
		private int vikingCool = 0;			// Cooldown between spawing Undead Vikings
		private int stageTimer = 0;			// Used for when Rognir switches stages.
		private int nextDashCooldown = 0;	// Cooldown for when Rognir will dash again in stage two.
		private Vector2 dashDirection;      // Direction of the current dash attack.
		private Vector2 targetOffset;       // Target position for movement.
        #endregion

        #region Set Defaults and send/receive ai
        /// <summary>
        ///  Method SetStaticDefaults overrides the default SetStaticDefaults from the ModNPC class.
        /// The method sets the DisplayName to Rognir.
        /// </summary>
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Rognir");
            Main.npcFrameCount[npc.type] = 21;
        }

		/// <summary>
		/// Method SetDefaults declares the default settings for the boss.
		/// </summary>
		public override void SetDefaults()
		{
			npc.aiStyle = -1;
			npc.lifeMax = 4750;
			npc.damage = 32;
			npc.defense = 14;
			npc.knockBackResist = 0f;
			npc.width = 204;
			npc.height = 310;
			npc.value = Item.buyPrice(0, 8, 0, 0);
			npc.npcSlots = 15f;
			npc.boss = true;
			npc.lavaImmune = true;
			npc.noGravity = true;
			npc.noTileCollide = true;
			npc.HitSound = SoundID.NPCHit1;
			npc.DeathSound = SoundID.NPCDeath1;
			npc.buffImmune[24] = true;

			//TODO Replace Boos_Fight_2 with final music.
			// Sets the music that plays when the boss spawns in and the priority of the music.  
			music = mod.GetSoundSlot(SoundType.Music, "Sounds/Music/RognirStage1");
			musicPriority = MusicPriority.BossMedium;
			bossBag = ItemType<Items.Rognir.RognirBag>();
		}

		public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
		{
			if (numPlayers > 1)
			{
				for (int i = 0; i < numPlayers; i++)
				{
					npc.lifeMax = (int)(npc.lifeMax * 1.35);
				}
			}
		}

		/// <summary>
		/// Sends extra ai variables over the network.
		/// </summary>
		/// <param name="writer">Writer to send the variables through.</param>
		public override void SendExtraAI(BinaryWriter writer)
		{
			writer.Write(attackCool);
			writer.Write(dashTimer);
			writer.Write(vikingCool);
			writer.Write(attack);
			writer.Write(stageTimer);
			writer.Write(nextDashCooldown);
			writer.Write(dashDirection.X);
			writer.Write(dashDirection.Y);
			writer.Write(targetOffset.X);
			writer.Write(targetOffset.Y);
		}

		/// <summary>
		/// Receives data sent through <c>SendExtraAI</c>.
		/// </summary>
		/// <param name="reader">Reader to read the variables from.</param>
		public override void ReceiveExtraAI(BinaryReader reader)
		{
			attackCool = reader.ReadInt32();
			dashTimer = reader.ReadInt32();
			vikingCool = reader.ReadInt32();
			attack = reader.ReadInt32();
			stageTimer = reader.ReadInt32();
			nextDashCooldown = reader.ReadInt32();
			float dashX = reader.ReadSingle();
			float dashY = reader.ReadSingle();

			dashDirection = new Vector2(dashX, dashY);

			float targetX = reader.ReadSingle();
			float targetY = reader.ReadSingle();

			targetOffset = new Vector2(targetX, targetY);
		}
        #endregion

        #region Visuals
        /// <summary>
        /// Updates frames for Rognir. To change frames, set the %= to how many ticks you want (numberOfTicks).
        /// Then change the frameCounter / n so that n = numberOfTicks / numberOfFrames
        /// </summary>
        /// <param name="frameHeight">Height of each frame in the sprite sheet.</param>
        public override void FindFrame(int frameHeight)
		{
			if (dashTimer <= 0 && Stage == 1)
			{
				npc.frameCounter += 1.0;						//This makes the animation run. Don't change this
				npc.frameCounter %= 60.0;						//This makes it so that after NUMBER ticks, the animation resets to the beginning.
																//To help you with timing, there are 60 ticks in one second.
				int frame = (int)(npc.frameCounter / 5) + 9;	//Chooses an animation frame based on frameCounter.
				npc.frame.Y = frame * frameHeight;				//Actually sets the frame
			}
			else if (Stage == 2)
			{
				npc.frameCounter += 1.0;						//This makes the animation run. Don't change this
				npc.frameCounter %= 64.0;						//This makes it so that after NUMBER ticks, the animation resets to the beginning.
																//To help you with timing, there are 60 ticks in one second.
				int frame = (int)(npc.frameCounter / 8) + 1;	//Chooses an animation frame based on frameCounter.
				npc.frame.Y = frame * frameHeight;				//Actually sets the frame
			}
			else if (dashTimer > 0)
			{
				npc.frame.Y = 0;
			}
			npc.spriteDirection = npc.direction; //Makes Rognir turn in the direction of his target.
		}
        #endregion

        #region AI
        //TODO Make boss AI less dumb.
        /// <summary>
        /// Method AI defines the AI for the boss.
        /// <c>AI</c> Starts out by checking if the the stage should be set to stage two.
        /// Then it gets the npc's target player and checks if the player is still alive.
        /// If the player is dead the npc targets the player closest to the npc and does the same check.
        /// If there are no players left the npc despawns after ten seconds.
        /// A random move timer is set and the npc moves to one of three locations unless the npc is dashing.
        /// If not dashing the 
        /// </summary>
        public override void AI()
		{
			// player is the current player that Rognir is targeting.
			Player player = Main.player[npc.target];

			// Checks to see if Rognir should despawn
			DespawnHandler(player);

			// Set the current stage based on current health.
			if ((Stage != 1) && (npc.life > npc.lifeMax / 2))
			{
				Stage = 1;
			}
			else if (Stage != 2 && (npc.life < npc.lifeMax / 2))
			{
				SwitchStage();
				if (Main.netMode != NetmodeID.MultiplayerClient)
				{
					Stage = 2;
					npc.netUpdate = true;
				}
			}

			// Target the closest player and turn towards it.
			npc.TargetClosest(true);
			// player is the currently targeted player.
			player = Main.player[npc.target];

			/*
			 * Checks if running on singleplayer, client, or server.
			 * True if not on client.
			 */
			if (Main.netMode != NetmodeID.MultiplayerClient)
			{
				// Check if it is time to reupdate the movement offset.
				if (MoveTimer <= 0)
				{
					if (Main.rand.NextFloat() > 0.8f)
					{
						NewPosition();
					}

					// Store a random amount of ticks until next update of the movement offset.
					MoveTimer = (int)Main.rand.NextFloat(rogMinMoveTimer, rogMaxMoveTimer);

					// Update network.
					npc.netUpdate = true;
				}
			}
			
			// Check if Rognir is spinning while he swiches stages.
			if (stageTimer <= 0)
			{
				if (nextDashCooldown <= 0)
				{
					if (dashTimer <= 0)
					{
						Vector2 targetPosition = player.Center + targetOffset;

						// Apply a velocity based on the distance between moveTo and the bosses current position and scale down the velocity.
						npc.velocity += (targetPosition - npc.Center) / rogAcceleration;

						/*
						 * Check if the velocity is above the maximum. 
						 * If so set the velocity to max.
						 */
						float speed = npc.velocity.Length();
						npc.velocity.Normalize();
						if (speed > (Stage == 1 ? rogMaxSpeedOne : rogMaxSpeedTwo))
						{
							speed = (Stage == 1 ? rogMaxSpeedOne : rogMaxSpeedTwo);
						}
						npc.velocity *= speed;

						/*
						 * Rotate Rognir based on his velocity.
						 */
						npc.rotation = npc.velocity.X / 50;
						if (npc.rotation > 0.1f)
							npc.rotation = 0.1f;
						else if (npc.rotation < -0.1f)
							npc.rotation = -0.1f;

						DoAttack();
					}
					else
						Dash();
				}
				else
				{
					nextDashCooldown--;
				}
			}
			// Spin.
			else
			{
				if (Main.rand.NextBool())
					Dust.NewDust(npc.Center, npc.width, npc.height, 230, 0, -2f);

				npc.velocity = Vector2.Zero; 
				stageTimer--;
				if (stageTimer % rogShardSprayModulus == 0 && Main.netMode != NetmodeID.MultiplayerClient)
				{
					for (float i = 0; i < Math.PI * 2; i += (float)Math.PI * 2f / rogShardSprayCount)
					{
						Vector2 projVelocity = new Vector2(-1, 0);
						projVelocity = projVelocity.RotatedBy(i + stageTimer * rogShardSprayMultiplier);
						projVelocity.Normalize();
						projVelocity *= rogShardVelocity;
						ShootShard(projVelocity);
					}
				}

				if (stageTimer == 0)
				{
					npc.dontTakeDamage = false;
					Main.PlaySound(SoundID.ZombieMoan);
				}
			}

			npc.ai[0]--;
		}

		/// <summary>
		/// Selects a target position for Rognir.  
		/// The position can be above, to the left of, or to the right of the player.
		/// </summary>
		private void NewPosition()
		{
			Vector2 above = new Vector2(0, -300);
			Vector2 left = new Vector2(-300, -100);
			Vector2 right = new Vector2(300, -100);
			if (Target == 0)
			{
				if (Main.rand.NextFloat() > 0.5f)
				{
					targetOffset = left;
					Target = 1;
				}
				else
				{
					targetOffset = right;
					Target = 2;
				}
			}
			else
			{
				targetOffset = above;
				Target = 0;
			}
		}

		/// <summary>
		/// <c>DoAttack</c> selects which attack to do randomly and then calls the apropriate function.
		/// </summary>
		private void DoAttack()
		{
			// Get next attack ten tick before attack happens to avoid desync.
			if (attackCool == 10)
			{
				if (Main.netMode != NetmodeID.MultiplayerClient)
				{
					attack = Main.rand.Next(3);     // Choose what attack to do.  Shards happen twice as often as a dash.
					npc.netUpdate = true;
				}
			}

			if (Stage == 2)
				SpawnViking();

			// If attack cooldown is still active then subtract one from it and exit.
			if (attackCool > 0)
			{
				attackCool -= 1;
				return;
			}

			// Check if in stage 1 or 2.
			if (Stage == 1)
				attackCool = rogAttackCoolOne;                   // Reset attack cooldown to 60.
			else
				attackCool = rogAttackCoolTwo;

			switch (attack)
			{
				case 0:
					Dash();						// Perform a dash attack.
					break;
				case 1:							// Shoot out a shard.
				case 2:
					Shards();					// Shoot out a shard.  Same as case 1.
					break;
				default:
					return;
			}
		}

		/// <summary>
		/// Causes Rognir to perform a quick dash attack.
		/// Normal movement needs to be stopped durring the dash in AI().
		/// </summary>
		private void Dash()
		{
			Player player = Main.player[npc.target];
			DespawnHandler(player);
			if (dashTimer <= 0)
			{
				//npc.rotation = 0f;
				npc.velocity = Vector2.Zero;
				if (Main.netMode != NetmodeID.MultiplayerClient)
				{
					// dashTimer is the number of ticks the dash will last.  Increase dashTimer to increase the lenght of the dash.
					dashTimer = rogDashLenght;
					// Direction to dash in.
					dashDirection = Main.player[npc.target].Center - npc.Center;
					npc.netUpdate = true;
				}

				Main.PlaySound(SoundID.ForceRoar);
			}

			else
			{
				dashTimer--;

				// Get the speed of the dash and limit it.
				float speed = dashTimer;
				if (speed > rogDashSpeedOne && Stage == 1)
				{
					speed = rogDashSpeedOne;
				}
				else if (speed > rogDashSpeedTwo && Stage == 2)
				{
					speed = rogDashSpeedTwo;
				}

				// Normalize the direction, add the speed, and then update position.  
				dashDirection.Normalize();
				dashDirection *= speed;
				npc.position += dashDirection;

				// Face in the direction of the dash.
				if (dashDirection.X < 0)
				{
					npc.direction = 0;
				}
				else
				{
					npc.direction = 1;
				}

				if (dashTimer <= 0 && Stage == 2 && Main.netMode != NetmodeID.MultiplayerClient)
				{
					if (Main.rand.NextFloat() < rogSecondDashChance - (rogSecondDashReduction * DashCounter))
					{
						DashCounter++;
						nextDashCooldown = rogNextDashDelay;
						Dash();
					}
					else
					{
						DashCounter = 0;
					}
				}
			}
		}

		/// <summary>
		/// Shoots out an ice shard that attacks the player.
		/// </summary>
		private void Shards()
		{
			// player is the current player that Rognir is targeting.
			Player player = Main.player[npc.target];

			// Updates target to next closest npc if current target is dead
			DespawnHandler(player);

			// Target the closest player and turn towards it.
			npc.TargetClosest(true);
			// player is the currently targeted player.
			player = Main.player[npc.target];

			Vector2 projVelocity = player.Center - npc.Center;
			projVelocity.Normalize();

			projVelocity *= rogShardVelocity;

			if (Main.netMode != NetmodeID.MultiplayerClient)
			{
				ShootShard(projVelocity);
				// Shoot out an ice shard 15 degrees offset
				ShootShard(projVelocity.RotatedBy((Math.PI / 180) * 15));
				// Shoot out an ice shard 345 degrees offset
				ShootShard(projVelocity.RotatedBy((Math.PI / 180) * 345));
			}
		}
		/// <summary>
		/// Shoots out a shard at the specified velocity.
		/// </summary>
		/// <param name="velocity">Velocity of the shard to shoot.</param>
		private void ShootShard(Vector2 velocity)
		{
			Projectile.NewProjectile(npc.Center, velocity, ProjectileType<RognirBossIceShard>(), rogShardDamage, 0f, Main.myPlayer, 0f, Main.rand.Next(0, 1000));
		}

		/// <summary>
		/// Spawns an Undead Viking on Rognir unless Rognir is inside of tiles.
		/// </summary>
		private void SpawnViking()
		{
			Player player = Main.player[npc.target];
			DespawnHandler(player);

			if (vikingCool > 0)
			{
				vikingCool--;
				return;
			}

			/*
			 * Checks a 3 by 3 area arround the center of rognir to see if 
			 * an undead viking can be spawned in.
			 */
			bool canSpawn = true;
			for (int i = -1; i < 2; i++)
			{
				for (int j = -1; j < 2; j++)
				{
					Tile tile = Main.tile[((int)npc.Center.X / 16) + i, ((int)npc.Center.Y / 16) + j];
					// Check if block is type 0 (air or dirt) or is not active and is solid.
					if ((tile.type != 0 || tile.active()) && Main.tileSolid[tile.type])
						canSpawn = false;
				}
			}
			if (canSpawn)
				NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, 167, 0, 0f, 0f, 0f, 0f, npc.target);		//Spawn undead viking

			vikingCool = rogVikingSpawnCool;	
		}

		/// <summary>
		/// Gets called when Rognir switches to stage two.
		/// Put code that needs to run at the start of stage two here.
		/// </summary>
		private void SwitchStage()
		{
			music = mod.GetSoundSlot(SoundType.Music, "Sounds/Music/RognirStage2");
			npc.height = 222;
			npc.width = 156;

			Player player = Main.player[npc.target];
			DespawnHandler(player);

			if (AnchorID == 0)
			{
				AnchorID = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, NPCType<RognirBossAnchor>(), 0, npc.whoAmI);
			}

			stageTimer = 60;				// Start spinning
			npc.dontTakeDamage = true;  //Don't take damage while spinning.
		}

		/// <summary>
		/// Handle's the despawning requirements for Rognir.
		/// Checks at the beginning of the AI() method.
		/// </summary>
		/// <param name="target"> The npc currently being targeted</param>
		/// 
		private void DespawnHandler(Player target)
		{
			/*
			 * Checks if the current player target is alive and active.  
			 * If not then the boss will run away and despawn.
			 */
			RefreshTarget(target);
			if (!target.active || target.dead)
			{
				npc.velocity = new Vector2(0f, 10f);
				if (npc.timeLeft > 10)
				{
					npc.timeLeft = 10;
				}
				return;
			}
			else if (Vector2.Distance(target.Center, npc.Center) > 8 * rogMaxRange)
			{
				npc.velocity = new Vector2(0f, 10f);
				if (npc.timeLeft > 10)
				{
					npc.timeLeft = 10;
				}
				return;
			}

			// Checks if it is daytime. If so, boss despawns
			if (Main.dayTime)
			{
				npc.velocity = new Vector2(0f, 10f);
				if (npc.timeLeft > 10)
				{
					npc.timeLeft = 10;
				}
				return;
			}
		}

		/// <summary>
		/// Simply updates target to the next closest npc
		/// if the current target is dead.
		/// </summary>
		/// <param name="target"> The npc currently being targeted</param>
		private void RefreshTarget(Player target)
		{
			if (!target.active || target.dead)
			{
				npc.TargetClosest(true);
				_ = Main.player[npc.target];

			}
		}
        #endregion

        #region Overrides
        /// <summary>
        /// Defines what happens when Rognir hits a player.
        /// </summary>
        /// <param name="target"> Player who has been hit. </param>
        /// <param name="damage"> The amout of damage the player should take. </param>
        /// <param name="crit"> Whether or not the damage is a crit hit. </param>
        public override void OnHitPlayer(Player target, int damage, bool crit)
		{
			if (!target.HasBuff(BuffID.Chilled))
				target.AddBuff(BuffID.Chilled, Stage == 1 ? rogChilledLenghtOne : rogChilledLenghtTwo);        // Chilled buff.
		}

		/// <summary>
		/// <c>NPCLoot</c> selects what loot Rognir will drop.
		/// </summary>
		public override void NPCLoot()
		{
			// Drops boss bags if world is in Expert mode
			if (Main.expertMode)
			{
				npc.DropBossBags();

			// If world is in Normal mode, Rognir will drop his Frozen Hook
			} else
			{
				// For normal mode, Rognir drops one of either Rognir's Frozen Hook or Rognir's Anchor with a 50% chance each.
				if (Main.rand.NextFloat() > 0.5f)
				{
					Item.NewItem(npc.getRect(), ItemType<Items.FrozenHookItem>());
				} else
				{
					Item.NewItem(npc.getRect(), ItemType<Items.RognirsAnchor>());
				}
			}

			if (!VoidWorld.downedRognir)
			{
				VoidWorld.downedRognir = true;
				if (Main.netMode == NetmodeID.Server)
				{
					NetMessage.SendData(MessageID.WorldData); // Immediately inform clients of new world state.
				}
			}
		}

		/// <summary>
		/// Allows customization of boss name in defeat message as well as what potions he drops.
		/// </summary>
		/// <param name="name"> Custom boss name. We leave it as is for Rognir</param>
		/// <param name="potionType">Potion type. Defaults to 5-15. We set potion type to Healing Potion</param>
		public override void BossLoot(ref string name, ref int potionType)
		{
			potionType = ItemID.HealingPotion;
		}
        #endregion
    }
}
