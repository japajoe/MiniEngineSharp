using System;
using System.Runtime.InteropServices;
using MiniEngine.GraphicsManagement;

namespace MiniEngine.Core
{
	public struct FogSettings
	{
		public Color color;
		public float density;
		public bool enabled;

        public FogSettings()
        {
		    color = new Color(0.887f, 0.887f, 0.887f, 1.000f);
		    density = 0.001f;
		    enabled = true;
        }
	}

    [StructLayout(LayoutKind.Sequential)]
    public struct UniformWorldInfo
    {
        public Color fogColor;
        public float fogDensity;
        public int fogEnabled;
        public float time;
        public float padding;
    };

    public static class World
    {
        private static FogSettings fogSettings = new FogSettings();
        private static UniformBuffer ubo;
        public static readonly uint UBO_BINDING_INDEX = 2;
        public static readonly string UBO_NAME = "World";

        public static Color FogColor
        {
            get => fogSettings.color;
            set => fogSettings.color = value;
        }

        public static float FogDensity
        {
            get => fogSettings.density;
            set => fogSettings.density = value;
        }

        public static bool FogEnabled
        {
            get => fogSettings.enabled;
            set => fogSettings.enabled = value;
        }

        internal static UniformBuffer GetUniformBuffer()
        {
            return ubo;
        }

        internal static void CreateUniformBuffer()
        {
            if(ubo != null)
                return;

            ubo = UniformBuffer.Create<UniformWorldInfo>(UBO_BINDING_INDEX, 1, UBO_NAME);
            ubo.ObjectLabel(UBO_NAME);
        }

        internal static void UpdateUniformBuffer()
        {
            if(ubo == null)
                return;
            
            UniformWorldInfo info = new UniformWorldInfo();
            info.fogColor = fogSettings.color;
            info.fogDensity = fogSettings.density;
            info.fogEnabled = fogSettings.enabled ? 1 : 0;
            info.time = Time.Elapsed;

            var pInfo = new ReadOnlySpan<UniformWorldInfo>(ref info);

            ubo.Bind();
            ubo.BufferSubData(pInfo, 0);
            ubo.Unbind();
        }
    }
}