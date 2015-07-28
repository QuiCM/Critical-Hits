using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Streams;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Terraria;
using TerrariaApi.Server;
using TShockAPI;

namespace CriticalHit
{
	[ApiVersion(1, 20)]
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
				return new Version(1, 0);
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

			if (config.Messages.Count == 0)
			{
				AddDefaultsToConfig();
			}

			ServerApi.Hooks.NetGetData.Register(this, OnGetData);
		}

		private void OnGetData(GetDataEventArgs args)
		{
			if (args.MsgID != PacketTypes.NpcStrike)
			{
				return;
			}

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
					KeyValuePair<string, int[]> message = config.Messages.ElementAt(r.Next(0, config.Messages.Count));
					Color c = new Color(message.Value[0], message.Value[1], message.Value[2]);

					NetMessage.SendData((int)PacketTypes.CreateCombatText,
						-1, -1, message.Key, (int)c.PackedValue, Main.npc[id].position.X, Main.npc[id].position.Y);
				}
			}
		}

		private void AddDefaultsToConfig()
		{
			config.Messages.Add("Pow!", new int[] { 255, 120, 0 });
			config.Messages.Add("Bam!", new int[] { 255, 40, 50 });
			config.Messages.Add("Smack!", new int[] { 255, 255, 0 });
			config.Messages.Add("Thump!", new int[] { 255, 0, 0 });
			config.Messages.Add("Womp!", new int[] { 0, 255, 0 });
			config.Messages.Add("Boom!", new int[] { 0, 120, 255 });

			config.Write(path);
		}
	}
}
