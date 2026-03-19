using System;
using MiniAudioEx.Native;

namespace MiniEngine
{
	class Program
	{
		static void Main2(string[] args)
		{
			ma_sound_ptr sound = new ma_sound_ptr(true);
			if(MiniAudioNative.ma_sound_is_playing(sound) > 0)
			{
				Console.WriteLine("Is playing");
			}
			sound.Free();
		}
		
		static void Main(string[] args)
		{
			Core.Configuration config = new Core.Configuration();
			config.iconData = Embedded.Logo.GetData();
			config.flags = Core.WindowFlags.VSync;
			config.width = 512;
			config.height = 512;
			config.title = "MiniEngine";
			App application = new App(config);
			application.Run();
		}
	}
}