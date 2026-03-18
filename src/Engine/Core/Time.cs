using System.Diagnostics;

namespace MiniEngine.Core
{
    public static class Time
    {
        private static Timer timer = new Timer();
        private static float elapsed;
        private static double elapsedAsDouble;
        private static ulong frameCount;

        public static float DeltaTime
        {
            get => timer.GetDeltaTime();
        }

        public static float Elapsed
        {
            get => elapsed;
        }

        public static double ElapsedAsDouble
        {
            get => elapsedAsDouble;
        }

        public static float FPS
        {
            get => timer.GetFPS();
        }

        public static ulong FrameCount
        {
            get => frameCount;
        }

        internal static void NewFrame()
        {
            timer.Update();
            elapsed += timer.GetDeltaTime();
            elapsedAsDouble += timer.GetDeltaTime();
            frameCount++;
        }
    }

    public sealed class Timer
    {
        private Stopwatch sw;
        private float deltaTime;
        private float lastFrameTime;
        private float fpsTimer;
        private float averageFPS;
        private int fps;

        public Timer()
        {
            sw = Stopwatch.StartNew();
            deltaTime = 0;
            lastFrameTime = 0;
            fpsTimer = 0;
            averageFPS = 0;
            fps = 0;
        }

        public float GetDeltaTime()
        {
            return deltaTime;
        }

        public float GetFPS()
        {
            return averageFPS;
        }

        public void Update()
        {
            float currentFrameTime = (float)sw.Elapsed.TotalSeconds;
            deltaTime = currentFrameTime - lastFrameTime;
            lastFrameTime = currentFrameTime;

            fpsTimer += deltaTime;

            fps++;

            if (fpsTimer > 0.5f)
            {
                averageFPS = (float)fps / fpsTimer;
                fps = 0;
                fpsTimer = 0.0f;
            }
        }
    }
}