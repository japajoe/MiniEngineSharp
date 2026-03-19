using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using ImGuiNET;
using MiniAudioEx.Utilities;
using MiniEngine.AudioManagement;
using MiniEngine.Core;
using MiniEngine.GraphicsManagement;
using MiniEngine.GraphicsManagement.Renderers;
using MiniEngine.GraphicsManagent;
using MiniEngine.Utilities;
using OpenTK.Mathematics;

namespace MiniEngine
{
	public class CubeObject : IDisposable
	{
		public Model model;
		public AudioSource audioSource;

		public CubeObject()
		{
			model = ModelGenerator.Get(ModelName.Cube);
			audioSource = new AudioSource();
			audioSource.transform.SetParent(model.transform);
			audioSource.AttenuationModel = AttenuationModel.Linear;
			audioSource.Spatial = true;
		}

        public void Dispose()
        {
            audioSource.Dispose();
        }
    }

    public sealed class App : Application
    {
		private AudioStream stream;
		private AudioListener audioListener;
		private AudioClip audioClip;
		private Camera camera;
		private List<Light> lights; 
		private ProceduralSkybox skybox;
        private Model ground;
		private List<CubeObject> cubes;
		private CameraController cameraController;
		private Texture2D groundTexture;
		private string currentTrack = "Now Playing: Unknown";
		private int selectedStreamIndex = 2;
		private ShoutCast shoutCast;
		private string[] streams = {
			"http://ice1.somafm.com/groovesalad-128-mp3",
			"http://ice1.somafm.com/synphaera-128-mp3",
			"http://ice1.somafm.com/fluid-128-mp3",
			"http://stream.radioparadise.com/mp3-128",
			"http://kexp-mp3-128.streamguys1.com/kexp128.mp3",
			"http://stream0.wfmu.org/freeform-128k",
			"http://198.15.94.34:8006/stream",
			"http://50.31.185.139/1053?icy=https"
		};

		private List<ShoutCast.Station> stations; 

        public App(int width, int height, string title, WindowFlags flags = WindowFlags.VSync) 
            : base(width, height, title, flags)
        {
        }

        public App(Configuration config) 
            : base(config)
        {
        }

        protected override void OnLoad()
        {
			stream = new AudioStream();
			
			stream.MetadataReceived += (string metadata) => {
				currentTrack = "Now Playing: " + stream.CurrentTrack;
			};

			stream.Play(streams[selectedStreamIndex]);

			camera = new Camera();
			camera.ClearColor = Color.RayWhite;
			camera.FarClippingPlane = 512;
            camera.transform.position = new Vector3(0, 1, 2);

			Shadow.SetWorldBounds(camera.FarClippingPlane * 0.5f, 100);

			cameraController = new CameraController();
			cameraController.Initialize(camera);

			lights = new List<Light>();
			lights.Add(new Light());
			lights[0].Type = LightType.Directional;
			lights[0].CastShadows = true;
			lights[0].transform.rotation= Quaternion.FromAxisAngle(Vector3.UnitX, MathHelper.DegreesToRadians(56.0f));

			cubes = new List<CubeObject>();
			cubes.Add(new CubeObject());
			cubes.Add(new CubeObject());

			cubes[0].model.GetChild(0).Color = Color.Orange;
			
			cubes[1].model.GetChild(0).Color = Color.Purple;
			cubes[1].model.transform.position = new Vector3(10, 2, 3);
            
			ground = ModelGenerator.Get(ModelName.Plane);
            ground.transform.scale = new Vector3(1000, 1, 1000);
			
			groundTexture = TextureHelper.CreateGroundTexture();
			
			ground.GetChild(0).Texture = groundTexture;
			ground.GetChild(0).TextureTiling = new Vector2(500, 500);

			skybox = new ProceduralSkybox();
			skybox.Rayleigh = 3.339f;
			skybox.Turbidity = 1.0f;
			skybox.MieCoefficient = 0.0075f;
			skybox.MieDirectionalG = 0.4f;
			skybox.Exposure = 0.25f;
    		skybox.CloudCoverage = 0.39f;
    		skybox.CloudDensity = 0.6f;
			skybox.SetSunPositionFromDirection(lights[0].transform.forward);

			Graphics.Add(camera);
			Graphics.Add(lights[0]);
			Graphics.Add(skybox);
			Graphics.Add(ground);

			for(int i = 0; i < cubes.Count; i++)
            	Graphics.Add(cubes[i].model);

            World.FogColor = Color.RayWhite;
			World.FogDensity = 0.0032f;
			Graphics.GetAmbientOcclusionSettings().globalEnabled = true;

			audioListener = new AudioListener();
			audioListener.transform.SetParent(camera.transform);


			
			LoadStations();
        }

		private void LoadStations()
		{
			shoutCast = new ShoutCast();

			_ = Task.Run(async () => {
				var json = await shoutCast.GetStationsByGenre("Rap");
				stations = JsonSerializer.Deserialize<List<ShoutCast.Station>>(json);
				streams = new string[stations.Count];
				for(int i = 0; i < stations.Count; i++)
				{
					streams[i] = stations[i].Name;
				}
			});			
		}

		private void ChangeStation()
		{
			_ = Task.Run(async () => {
				string selectedUrl = await shoutCast.GetStationUrl(stations[selectedStreamIndex].ID);
				stream.Stop();
				stream.Play(selectedUrl);
			});
		}

        protected override void OnClose()
        {
            stream.Dispose();
			shoutCast.Dispose();
        }

		private float testVolume = 0.5f;
		private Color buttonColor = new Color(0.2f, 0.4f, 0.8f, 1.0f);
    	private Color sliderColor = new Color(0.1f, 0.8f, 0.2f, 1.0f);

        protected override void OnUpdate()
        {
			cubes[0].model.transform.rotation = Quaternion.FromAxisAngle(new Vector3(0, 1, 0), MathHelper.DegreesToRadians(Time.Elapsed * 100.0f));
			float y = (float)Math.Sin(Time.Elapsed) * 1.0f;
			cubes[0].model.transform.position = new Vector3(0, 1.8f + y, 0);

			float sunAngle = MathHelper.RadiansToDegrees(lights[0].transform.rotation.ToEulerAngles().X);
			float brightness = 1.0f - (float)Math.Pow((sunAngle - 90.0f) / 90.0f, 2.0f);
			brightness = Math.Max(0.025f, brightness);
			lights[0].Strength = brightness;

			Graphics.GetAmbientOcclusionSettings().value = lights[0].Strength * 10.0f;

        }

        protected override void OnLateUpdate()
        {
            cameraController.OnLateUpdate();
        }

        protected override void OnGUI()
        {
			float volume = stream.Volume;
			float lightX = MathHelper.RadiansToDegrees(lights[0].transform.rotation.ToEulerAngles().X);

			ImGui.DockSpaceOverViewport(0, ImGui.GetMainViewport(), ImGuiDockNodeFlags.PassthruCentralNode);

			if(ImGui.Begin("Info"))
			{
				Span<char> span = stackalloc char[64];
				int charsWritten;

				int fps = (int)Math.Round(Time.FPS);
				
				if(span.TryWrite($"FPS: {fps}", out charsWritten))
				{
					ImGui.Text(span.Slice(0, charsWritten));
				}
				
				ImGui.Text(currentTrack);

				if(ImGui.SliderFloat("Volume", ref volume, 0.0f, 1.0f))
				{
					stream.Volume = volume;
				}

				if(ImGui.SliderFloat("Light", ref lightX, 0.0f, 180.0f))
				{
					Quaternion rotation = Quaternion.FromAxisAngle(Vector3.UnitX, MathHelper.DegreesToRadians(lightX));
					lights[0].transform.rotation = rotation;
					skybox.SetSunPositionFromDirection(lights[0].transform.forward);
					World.FogColor = skybox.SkyColor;
				}

				if (ImGui.Combo("Select Stream", ref selectedStreamIndex, streams, streams.Length))
				{
					if(stations?.Count > 0)
					{
						ChangeStation();
					}
					else
					{
						stream.Stop();
						stream.Play(streams[selectedStreamIndex]);
					}
				}
				
			}
			ImGui.End();
        }

		private void TestGUI()
		{
			GUI.Begin();
			Vector2 position = new Vector2(50.0f, 50.0f);
			
			// 2. Render a simple label
			GUI.Label(position, "System Settings", Color.White);

			// 3. Render a button
			// We use a unique ID (100) to track this specific widget's state
			if (GUI.Button(100, position + new Vector2(0, 40), new Vector2(120, 30), "Reset Volume", buttonColor))
			{
				testVolume = 0.5f;
			}

			// 4. Render a slider
			// We use a unique ID (101) for the slider
			Vector2 sliderPos = position + new Vector2(0, 80);
			Vector2 sliderSize = new Vector2(200, 20);
			
			if (GUI.Slider(101, sliderPos, sliderSize, ref testVolume, 0.0f, 1.0f, sliderColor))
			{
				
			}

			GUI.End();
		}
    }
}