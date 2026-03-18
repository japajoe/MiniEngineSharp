using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using MiniEngine.Core;
using OpenTK.Mathematics;

namespace MiniEngine.GraphicsManagement
{
    public enum LightType : int
    {
        Directional = 0,
        Point = 1
    }

	public enum FallOffMode : int
	{
		Linear = 0,
		Exponential = 1
	}

    public sealed class Light : Entity
    {
        public static readonly uint UBO_BINDING_INDEX = 0;
        public static readonly string UBO_NAME = "Lights";
        public static readonly uint MAX_LIGHTS = 32;

        private LightType type;
        private FallOffMode fallOffMode;
        private Color color;
        private float strength;
        private float range; //point lights
        private bool castShadows;

        private static UniformBuffer ubo;

        public LightType Type
        {
            get
            {
                return type;
            }
            set
            {
                type = value;
            }
        }

        public FallOffMode FallOffMode
        {
            get
            {
                return fallOffMode;
            }
            set
            {
                fallOffMode = value;
            }
        }

        public Color Color
        {
            get
            {
                return color;
            }
            set
            {
                color = value;
            }
        }

        public float Strength
        {
            get
            {
                return strength;
            }
            set
            {
                strength = value;
            }
        }

        public float Range
        {
            get
            {
                return range;
            }
            set
            {
                range = value;
            }
        }

        public bool CastShadows
        {
            get => castShadows;
            set => castShadows = value;
        }

        public Light() : base()
        {
            type = LightType.Directional;
            fallOffMode = FallOffMode.Linear;
            range = 10.0f;
            color = Color.White;
            strength = 1.0f;
        }

        internal static void CreateUniformBuffer()
        {
            if(ubo != null)
                return;

            uint headerSize = (uint)(4 * Marshal.SizeOf<UInt32>());
            uint size = (uint)(headerSize + (MAX_LIGHTS * Marshal.SizeOf<UniformLightInfo>()));
            ubo = UniformBuffer.Create(UBO_BINDING_INDEX, size);
            ubo.ObjectLabel(UBO_NAME);
        }

        internal static UniformBuffer GetUniformBuffer()
        {
            return ubo;
        }

        private static List<Light> lightsSorted = new List<Light>();

        internal static void UpdateUniformBuffer(Camera camera, List<Light> lights)
        {
            if(lights == null)
                return;

            if(ubo == null)
                return;

            Frustum frustum = camera != null ? camera.Frustum : null;
            int activeLights = 0;

            if(lightsSorted.Count != lights.Count)
            {
                lightsSorted.Clear();
                for(int i = 0; i < lights.Count; i++)
                    lightsSorted.Add(null);
            }

            for(int i = 0; i < lights.Count; i++)
            {
                lightsSorted[i] = lights[i];

                if(lights[i].isActive)
                {
                    bool cull = false;

                    if(lights[i].type == LightType.Point && frustum != null)
                    {
                        Vector3 position = lights[i].transform.position;
                        BoundingBox bounds = new BoundingBox();
                        
                        if(lights[i].fallOffMode == FallOffMode.Linear)
                        {
                            float radius = lights[i].range * 0.5f;
                            Vector3 min = position - new Vector3(radius, radius, radius);
                            Vector3 max = position + new Vector3(radius, radius, radius);
                            bounds.Grow(min, max);
                        }
                        else // Exponential fall off
                        {
                            // Calculate a theoretical radius where the light becomes too dim to see.
                            float threshold = 0.002f;
                            float radius = (float)Math.Sqrt(1.0f / threshold);
                            Vector3 min = position - new Vector3(radius, radius, radius);
                            Vector3 max = position + new Vector3(radius, radius, radius);
                            bounds.Grow(min, max);
                        }

                        if(!frustum.Contains(bounds))
                            cull = true;
                    }

                    if(cull)
                        continue;

                    activeLights++;
                }
            }

            if(activeLights >= MAX_LIGHTS)
                activeLights = (int)MAX_LIGHTS;

            int startOffset = 4 * Marshal.SizeOf<Int32>();

            Span<int> pActiveLights = new Span<int>(ref activeLights);

            ubo.Bind();
		    ubo.BufferSubData(pActiveLights, 0);

            if(lights.Count > 0)
            {
                for(int i = 0; i < activeLights; i++)
                {
                    UniformLightInfo info = lightsSorted[i].GetUniformInfo();
                    var pInfo = new ReadOnlySpan<UniformLightInfo>(ref info);
                    int offset = startOffset + (i * Marshal.SizeOf<UniformLightInfo>());
                    ubo.BufferSubData(pInfo, offset);
                }
            }

            ubo.Unbind();
        }

        private UniformLightInfo GetUniformInfo()
        {
            UniformLightInfo info = new UniformLightInfo();
            info.type = (int)type;
            info.fallOffMode = (int)fallOffMode;
            info.strength = strength;
            info.range = range;
            info.position = new Vector4(m_transform.position);
            info.direction = new Vector4(m_transform.forward);
            info.color = color;
            return info;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct UniformLightInfo
    {
		public int type;            //4
		public int fallOffMode;	    //4
		public float strength;      //4
		public float range;         //4
		public Vector4 position;    //12
		public Vector4 direction;   //12
		public Color color;         //16
    }
}