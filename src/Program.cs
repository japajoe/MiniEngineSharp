namespace MiniEngine
{
	class Program
	{
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