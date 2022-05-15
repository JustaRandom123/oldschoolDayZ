using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiscordRPC;
using DiscordRPC.Logging;

namespace Oldschool_DayZ_Launcher
{
   
    internal class Discord
    {
        public static DiscordRpcClient client;

		public static void Initialize()
		{
			
			client = new DiscordRpcClient("975134151814570034");

			//Set the logger
			client.Logger = new ConsoleLogger() { Level = LogLevel.Warning };

			//Subscribe to events
			client.OnReady += (sender, e) =>
			{
				Console.WriteLine("Received Ready from user {0}", e.User.Username);
			};

			client.OnPresenceUpdate += (sender, e) =>
			{
				Console.WriteLine("Received Update! {0}", e.Presence);
			};

			//Connect to the RPC
			client.Initialize();

			changeDiscordRPC("Running Launcher","","OSD Launcher","logo");
		}


		public static void changeDiscordRPC(string status, string details, string imageText,string imageKey)
        {
			client.SetPresence(new RichPresence()
			{		
				Details = details,
				State = status,
				Assets = new Assets()
				{
					LargeImageKey = imageKey,
					LargeImageText = imageText					
				}
			});
		}
	}
}
