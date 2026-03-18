namespace MiniEngine.AudioManagement
{
    public static class Audio
    {
        private static AudioContext context;

        internal static void Initialize()
        {
            AudioDevice[] devices = AudioDevice.GetDevices(AudioDeviceType.Playback);
            AudioDevice device = null;

            for(int i = 0; i < devices?.Length; i++)
            {
                if(devices[i].IsDefault)
                {
                    device = devices[i];
                    break;
                }
            }

            context = new AudioContext(44100, 2, 2048, device);
            context.Log += OnLog;
            context.Create();
            context.MakeCurrent();
        }

        internal static void Destroy()
        {
            context.Dispose();
        }

        internal static void NewFrame()
        {
            context.Update();
        }

        private static void OnLog(uint level, string message)
        {
            // message = message.Replace("\r", "").Replace("\n", " ");
            // Console.WriteLine("AudioContext [{0}]: {1}", level, message);
        }
    }
}