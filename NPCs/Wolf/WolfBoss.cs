using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Void.NPCs.Wolf
{
    [AutoloadBossHead]
    class WolfBoss : ModNPC
    {
		#region Tunables
		#endregion

		#region Variables
		private float Mode
		{
			get => npc.ai[0];
			set => npc.ai[0] = value;
		}
		private float Direction
		{
			get => npc.ai[1];
			set => npc.ai[1] = value;
		}
		#endregion

		#region Set Defaults and send/receive ai
		/// <summary>
		///  Method SetStaticDefaults overrides the default SetStaticDefaults from the ModNPC class.
		/// The method sets the DisplayName to Rognir.
		/// </summary>
		public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Wolf");
            Main.npcFrameCount[npc.type] = 1;
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
			npc.noGravity = false;
			npc.noTileCollide = false;
			npc.HitSound = SoundID.NPCHit1;
			npc.DeathSound = SoundID.NPCDeath1;
			npc.buffImmune[24] = true;

			// Sets the music that plays when the boss spawns in and the priority of the music.  
			music = MusicID.Boss1;
			musicPriority = MusicPriority.BossMedium;
			//bossBag = ItemType<Items.Rognir.RognirBag>();
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
		#endregion

		#region AI
		public override void AI()
        {
			Mode = 1f;
			Direction = 1f;
			npc.netUpdate = true;
			//this.JumpCheck();
			if (Mode == 1f)
            {
				float speed = Direction;
                //if (npc.collideX && npc.collideY && !Collision.sloping)
                if (this.JumpCheck())
                {
					npc.velocity.Y = -20;
				}
				npc.velocity.X = speed;

            }
        }

		private bool JumpCheck()
        {
			bool col = false;
			bool standing = Collision.SolidTiles((int)npc.Left.X / 16, (int)npc.Right.X / 16, (int)npc.Bottom.Y / 16, (int)npc.Bottom.Y / 16 + 1);
			//Vector2 vertCollision = Collision.Soli(npc.position, new Vector2(0f, 0f), npc.width, npc.height);



			Collision.AnyCollision(npc.position, npc.velocity, npc.width, npc.height);
			//Tile right = Main.tile[((int)npc.Center.X / 16 + npc.width / 2) + 1, (int)npc.Center.Y / 16 + npc.height / 2];
			//Tile left = Main.tile[((int)npc.Center.X / 16 - npc.width / 2) - 1, (int)npc.Center.Y / 16 + npc.height / 2];
			if (standing)
			{
				Main.NewText(standing);
				if (npc.velocity.X > 0)
                {
					for (int i = (int)npc.Bottom.Y / 16 - 2; i >= npc.Top.Y / 16; i--)
					{
						Tile right = Main.tile[(int)npc.Right.X / 16, i];

						if (Main.tileSolid[right.type] && right.type != -1 && right.active())
						{
							col = true;
							break;
						}

					}
				}
				else if (npc.velocity.X < 0)
                {
					for (int i = (int)npc.Bottom.Y / 16 - 2; i >= npc.Top.Y / 16; i--)
					{
						Tile left = Main.tile[(int)npc.Left.X / 16, i];

						if (Main.tileSolid[left.type] && left.type != -1 && left.active())
						{
							col = true;
							break;
						}
					}
				}
			}
			//if ((Main.tileSolid[npc.Bott.type] && right.type != -1 && right.active()) || (Main.tileSolid[left.type] && left.type != -1 && left.active()))
			return col;
        }

		//private bool CheckCollision()
  //      {
		//	if (npc.width % 2 == 0)
  //          {

  //          }
		//	for (int i = 0; i < npc.width; i++)
  //          {
		//		for (int j = 0; j < npc.height; j++)
  //              {
		//			Tile tile = Main.tile[((int)npc.Center.X / 16) + i - (npc.width / 2), ((int)npc.Center.Y / 16) + j];
		//		}
  //          }
		//	return false;
  //      }
		#endregion
	}
}
