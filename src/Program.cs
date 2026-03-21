using System;
using System.Text;
using System.Threading.Tasks;
using MiniEngine.Utilities;

namespace MiniEngine
{
	class Program
	{
		static async Task Main2(string[] args)
		{
			HttpClient client = new HttpClient();
			var request = new HttpClient.Request("https://directory.shoutcast.com/Home/BrowseByGenre", HttpClient.Method.Post);

            request.AddHeader("User-Agent", "Mozilla/5.0 (X11; Linux x86_64; rv:143.0) Gecko/20100101 Firefox/143.0");
            request.AddHeader("Accept", "*/*");
            request.AddHeader("Accept-Language", "en-US,en;q=0.5");
            request.AddHeader("Accept-Encoding", "gzip, deflate, br, zstd");
            request.AddHeader("X-Requested-With", "XMLHttpRequest");
            request.AddHeader("Origin", " https://directory.shoutcast.com");
            request.AddHeader("Sec-GPC", "1");
            request.AddHeader("Connection", "keep-alive");
            request.AddHeader("Referer", "https://directory.shoutcast.com/");
            request.AddHeader("Sec-Fetch-Dest", "empty");
            request.AddHeader("Sec-Fetch-Mode", "cors");
            request.AddHeader("Sec-Fetch-Site", "same-origin");
            request.AddHeader("Priority", "u=0");
			request.SetContent("genrename=Rap", "application/x-www-form-urlencoded; charset=UTF-8");
			
			var response = await client.Send(request);

			if(response.content.ReadAsString(out string text, response.contentLength))
			{
				Console.WriteLine(text);
			}
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