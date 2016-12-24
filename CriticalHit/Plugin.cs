using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Streams;
using System.Linq;
using Microsoft.Xna.Framework;


using Terraria;
using Terraria.ID;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;

namespace CriticalHit
{
	[ApiVersion(2, 0)]
	public class Plugin : TerrariaPlugin
	{
		internal Config config = new Config();
		internal Random r = new Random();

		public override string Author
		{
			get
			{
				return "White";
			}
		}

		public override string Description
		{
			get
			{
				return "Gives critical hit pop-ups";
			}
		}

		public override string Name
		{
			get
			{
				return "CRITICAL HIT!";
			}
		}

		public override Version Version
		{
			get
			{
				return new Version(1, 1);
			}
		}

		public Plugin(Main game) : base(game)
		{
		}

		private string path = Path.Combine(TShock.SavePath, "CriticalConfig.json");

		public override void Initialize()
		{
			if (!File.Exists(path))
			{
				config.Write(path);
			}
			config.Read(path);

			if (config.CritMessages.Count == 0)
			{
				AddDefaultsToConfig();
			}

			GeneralHooks.ReloadEvent += OnReload;

			ServerApi.Hooks.NetGetData.Register(this, OnGetData);
		}

		private void OnReload(ReloadEventArgs e)
		{
			if (!File.Exists(path))
			{
				config.Write(path);
			}
			config.Read(path);
		}

		private void OnGetData(GetDataEventArgs args)
		{
			if (args.MsgID != PacketTypes.NpcStrike)
			{
				return;
			}

			if (args.Msg.whoAmI < 0 || args.Msg.whoAmI > Main.maxNetPlayers)
			{
				return;
			}

			Player player = Main.player[args.Msg.whoAmI];

			using (MemoryStream ms = new MemoryStream(args.Msg.readBuffer, args.Index, args.Length - 1))
			{
				short id = ms.ReadInt16();
				ms.ReadInt16();
				ms.ReadSingle();
				ms.ReadInt8();
				bool crit = Convert.ToBoolean(ms.ReadInt8());

				if (Main.npc[id] == null)
				{
					return;
				}

				if (crit)
				{
					Dictionary<string, int[]> messages;
					Item selected = player.inventory[player.selectedItem];
					
					if (selected.ranged && !ItemID.Sets.Explosives[selected.type])
					{
						messages = config.CritMessages[WeaponType.Range].Messages;
					}
					else if (selected.melee)
					{
						messages = config.CritMessages[WeaponType.Melee].Messages;
					}
					else if (selected.magic)
					{
						messages = config.CritMessages[WeaponType.Magic].Messages;
					}
					else
					{
						if (ItemID.Sets.Explosives[selected.type] 
							|| selected.type == ItemID.Grenade
							|| selected.type == ItemID.BouncyGrenade
							|| selected.type == ItemID.PartyGirlGrenade
							|| selected.type == ItemID.StickyGrenade)
						{
							messages = config.CritMessages[WeaponType.Explosive].Messages;
						}
						else
						{
							messages = config.CritMessages[WeaponType.Melee].Messages;
						}
					}

					KeyValuePair<string, int[]> message = messages.ElementAt(r.Next(0, messages.Count));


					Color c = new Color(message.Value[0], message.Value[1], message.Value[2]);

					NetMessage.SendData((int)PacketTypes.CreateCombatText,
						-1, -1, message.Key, (int)c.PackedValue, Main.npc[id].position.X, Main.npc[id].position.Y);
				}
			}
		}

		private void AddDefaultsToConfig()
		{
			CritMessage msg = new CritMessage();
			msg.Messages.Add("Pow!", new int[] { 255, 120, 0 });
			msg.Messages.Add("Bam!", new int[] { 255, 40, 50 });
			msg.Messages.Add("Smack!", new int[] { 255, 255, 0 });
			msg.Messages.Add("Thump!", new int[] { 255, 0, 0 });
			config.CritMessages.Add(WeaponType.Melee, msg);

			msg = new CritMessage();
			msg.Messages.Add("Boom!", new int[] { 255, 0, 0 });
			msg.Messages.Add("Bang!", new int[] { 255, 0, 0 });
			config.CritMessages.Add(WeaponType.Explosive, msg);

			msg = new CritMessage();
			msg.Messages.Add("Pew pew!", new int[] { 50, 255, 10 });
			config.CritMessages.Add(WeaponType.Range, msg);

			msg = new CritMessage();
			msg.Messages.Add("Splat!", new int[] { 10, 50, 255 });
			msg.Messages.Add("Whoosh!", new int[] { 0, 150, 255 });
			msg.Messages.Add("Whoomph!", new int[] { 0, 200, 255 });
			config.CritMessages.Add(WeaponType.Magic, msg);

			config.Write(path);
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				ServerApi.Hooks.NetGetData.Deregister(this, OnGetData);
				GeneralHooks.ReloadEvent -= OnReload;
			}

			base.Dispose(disposing);
		}
	}
}
