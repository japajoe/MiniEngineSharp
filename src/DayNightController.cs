using System;
using ImGuiNET;
using MiniEngine.Core;
using MiniEngine.GraphicsManagement;
using MiniEngine.GraphicsManagement.Renderers;
using OpenTK.Mathematics;

namespace MiniEngine
{
    public sealed class DayNightController
    {
        private ProceduralSkybox skybox;
        private Light light;
        private float timeOfDay = 0.35f;
        private int dayLengthInSeconds = 1440;
        private float timer = 0.0f;
        private readonly char[] timeBuffer = new char[5] { '0', '0', ':', '0', '0' };

        public float TimeOfDay
        {
            get => timeOfDay;
            set => timeOfDay = Math.Clamp(value, 0.0f, 1.0f);
        }

        public DayNightController(Light directionalLight)
        {
            light = directionalLight;
            skybox = new ProceduralSkybox();
            skybox.SetSunPositionFromDirection(light.transform.forward);
            skybox.Rayleigh = 3.339f;
            skybox.Turbidity = 1.0f;
            skybox.MieCoefficient = 0.0075f;
            skybox.MieDirectionalG = 0.4f;
            skybox.Exposure = 0.25f;
            skybox.CloudCoverage = 0.39f;
            skybox.CloudDensity = 0.6f;

            World.FogColor = skybox.SkyColor;
            World.FogDensity = 0.0032f;

            Graphics.Add(skybox);
        }
        
        public void OnUpdate()
        {
            if(light == null)
                return;

            float progress = Time.DeltaTime / (float)dayLengthInSeconds;

            timeOfDay += progress;

            float sunAngle = GetSunAngle();

            ToggleShadows(sunAngle);

            Transform transform = light.transform;
            transform.rotation = Quaternion.FromEulerAngles(new Vector3(MathHelper.DegreesToRadians(sunAngle), 0, 0));

            float brightness = 1.0f - (float)Math.Pow((sunAngle - 90.0f) / 90.0f, 2.0f);
            brightness = Math.Max(0.025f, brightness);
            light.Strength = brightness;

            Graphics.GetAmbientOcclusionSettings().value = light.Strength * 10.0f;

            skybox.SetSunPositionFromDirection(transform.forward);
            World.FogColor = skybox.SkyColor;

            if(timeOfDay >= 1.0f)
                timeOfDay -= 1.0f;
            
            SetTimeString();
        }
        
        public void OnGUI()
        {
            if(light == null)
                return;

            Span<char> span = timeBuffer;

            ImGui.Begin("Time");
            ImGui.Text(span);
            ImGui.End();
        }
        
        private float GetSunAngle()
        {
            // Multiply by 360 to get the full circle
            float angle = timeOfDay * 360.0f;

            // Offset by 90 degrees so that 0.5 results in 90.0
            // (0.5 * 360) - 90 = 180 - 90 = 90
            float offsetAngle = angle - 90.0f;

            // Wrap the angle to keep it between 0 and 360 for cleanliness
            if (offsetAngle < 0.0f) 
                offsetAngle += 360.0f;

            return offsetAngle;
        }

        void ToggleShadows(float sunAngle)
        {
            if (sunAngle <= 0.0f || sunAngle >= 180.0f)
            {
                if(light.CastShadows)
                    light.CastShadows = false;
            }
            else
            {
                if(!light.CastShadows)
                    light.CastShadows = true;
            }
        }

        void SetTimeString()
        {
            timer += Time.DeltaTime;

            if (timer < 0.5f)
                return;
            
            timer -= 0.5f;

            int totalMinutesPassed = (int)(timeOfDay * dayLengthInSeconds);
            int hours = (totalMinutesPassed / 60) % 24;
            int minutes = totalMinutesPassed % 60;

            // Manually write digits to the buffer to avoid formatting allocations
            timeBuffer[0] = (char)('0' + (hours / 10));
            timeBuffer[1] = (char)('0' + (hours % 10));
            // _timeBuffer[2] is already ':'
            timeBuffer[3] = (char)('0' + (minutes / 10));
            timeBuffer[4] = (char)('0' + (minutes % 10));

            // HOW TO USE THE BUFFER:
            // If you do: currentTime = new string(_timeBuffer); // THIS ALLOCATES.
        }

    }
}