using MiniEngine.Core;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MiniEngine.GraphicsManagement
{
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct UniformShadowInfo
    {
        public int cascadeCount;
        public float shadowBias;
        public float farPlane;
        public int enabled;
        public fixed byte lightSpaceMatrices[16*16*4]; // 16 matrices of 4x4 (16 floats each)
        public fixed byte cascadePlaneDistances[16*16]; //16 floats with 12 bytes padding each
    }

    public static class Shadow
    {
        public static readonly uint UBO_BINDING_INDEX = 3;
        public static readonly string UBO_NAME = "Shadow";
        private static int fbo;
        private static int cascadeCount;
        private static float bias;
        private static float lambda;
        private static Texture2DArray texture;
        private static float[] cascadeLevels;
        private static Matrix4[] lightSpaceMatrices;
        private static Vector3[] worldBounds;
        private static Vector4[] frustumCorners;
        private static UniformBuffer ubo;

        public static Texture2DArray Texture => texture;
        public static int CascadeCount => cascadeCount;

        internal static Matrix4[] GetLightSpaceMatrices()
        {
            return lightSpaceMatrices;
        }

        internal static void Generate()
        {
            if(fbo > 0)
                return;

            cascadeCount = 4;
            bias = 0.0005f;
            lambda = 0.5f;

            texture = new Texture2DArray();
            texture.Generate(2048, 2048, cascadeCount);

            cascadeLevels = new float[cascadeCount];
            lightSpaceMatrices = new Matrix4[cascadeCount+1];
            frustumCorners = new Vector4[8];

            for(int i = 0; i < frustumCorners.Length; i++)
            {
                frustumCorners[i] = new Vector4(0, 0, 0, 1);
            }
            
            float halfWidth = 1024.0f;
            float height = 100.0f;
            
            worldBounds = new Vector3[] {
                new Vector3(-halfWidth, -1.0f, -halfWidth), new Vector3(halfWidth, -1.0f, -halfWidth),
                new Vector3(-halfWidth, -1.0f,  halfWidth), new Vector3(halfWidth, -1.0f,  halfWidth),
                new Vector3(-halfWidth, height, -halfWidth), new Vector3(halfWidth, height, -halfWidth),
                new Vector3(-halfWidth, height,  halfWidth), new Vector3(halfWidth, height,  halfWidth)
            };

            fbo = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);
            GL.DrawBuffer(DrawBufferMode.None);
            GL.ReadBuffer(ReadBufferMode.None);
            GL.FramebufferTextureLayer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, texture.Id, 0, 0);

            if (GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferStatus.FramebufferComplete)
            {
                throw new Exception("Shadow Framebuffer is incomplete");
            }

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        internal static void Bind()
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);
            GL.Viewport(0, 0, texture.Width, texture.Height);
		    GL.Enable(EnableCap.CullFace);
		    GL.CullFace(TriangleFace.Front); // Render back-faces only to the shadow map
        }

        internal static void Unbind()
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.Viewport(0, 0, Graphics.GetScreenHeight(), Graphics.GetScreenHeight());
            GL.CullFace(TriangleFace.Back);
        }

        internal static void Clear(int cascadeIndex)
        {
            GL.FramebufferTextureLayer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, texture.Id, 0, cascadeIndex);
            GL.Clear(ClearBufferMask.DepthBufferBit);
        }

        private static void UpdateCascadeSplits(float near, float far)
        {
            for (int i = 0; i < cascadeCount; i++)
            {
                float p = (i + 1) / (float)cascadeCount;
                float log = near * MathF.Pow(far / near, p);
                float uniform = near + (far - near) * p;
                cascadeLevels[i] = lambda * log + (1.0f - lambda) * uniform;
            }
        }

        private static Matrix4 GetLightSpaceMatrix(Camera camera, Light light, float n, float f)
        {
            float screenWidth = (float)Graphics.GetScreenWidth();
            float screenHeight = (float)Graphics.GetScreenHeight();

            Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(camera.FieldOfView), screenWidth / screenHeight, n, f);
            Matrix4 view = Matrix4.LookAt(camera.transform.position, camera.transform.position + camera.transform.forward, camera.transform.up);
            Matrix4 inv = Matrix4.Invert(view * projection);

            int frustumIndex = 0;

            for (int x = 0; x < 2; ++x) 
            {
                for (int y = 0; y < 2; ++y) 
                {
                    for (int z = 0; z < 2; ++z) 
                    {
                        Vector4 pt = new Vector4(
                            2.0f * x - 1.0f,
                            2.0f * y - 1.0f,
                            2.0f * z - 1.0f,
                            1.0f) * inv;
                        frustumCorners[frustumIndex] = (pt / pt.W);
                        frustumIndex++;
                    }
                }
            }

            // Find the center of the frustum split
            Vector3 center = new Vector3(0, 0, 0);
            for (int i = 0; i < frustumCorners.Length; i++)
            {
                center += new Vector3(frustumCorners[i].X, frustumCorners[i].Y, frustumCorners[i].Z);
            }
            center /= 8.0f;

            // Create the Light View matrix
            // Looking at the center of the frustum split from the direction of the light
            Matrix4 lightView = Matrix4.LookAt(center + light.transform.forward, center, new Vector3(0.0f, 1.0f, 0.0f));

            // Find the min/max X and Y in Light Space from the Frustum Corners
            // This ensures the shadow map resolution is focused tightly on the viewable area
            float minX = float.MaxValue;
            float maxX = float.MinValue;
            float minY = float.MaxValue;
            float maxY = float.MinValue;

            for (int i = 0; i < frustumCorners.Length; i++)
            {
                Vector4 trf = frustumCorners[i] * lightView;
                minX = (float)Math.Min(minX, trf.X);
                maxX = (float)Math.Max(maxX, trf.X);
                minY = (float)Math.Min(minY, trf.Y);
                maxY = (float)Math.Max(maxY, trf.Y);
            }

            float minZ = float.MaxValue;
            float maxZ = float.MinValue;

            // Z-bounds based on the Scene (to prevent chopping)
            for(int i = 0; i < worldBounds.Length; i++)
            {
                Vector4 trf = new Vector4(worldBounds[i], 1.0f) * lightView;
                minZ = (float)Math.Min(minZ, trf.Z);
                maxZ = (float)Math.Max(maxZ, trf.Z);
            }

            // Return the final light space transformation matrix
            // Note: minZ and maxZ define the near and far planes of the shadow map
            Matrix4 lightProjection = Matrix4.CreateOrthographicOffCenter(minX, maxX, minY, maxY, minZ, maxZ);
            return lightView * lightProjection;
        }

        internal static void CreateUniformBuffer()
        {
            ubo = UniformBuffer.Create<UniformShadowInfo>(UBO_BINDING_INDEX, 1, UBO_NAME);
            ubo.ObjectLabel(UBO_NAME);
        }

        internal static UniformBuffer GetUniformBuffer()
        {
            return ubo;
        }

        internal static void UpdateUniformBuffer(Camera camera, Light light)
        {
            if(camera == null)
                return;
            if(light == null)
                return;
            if(ubo == null)
                return;
            if(texture == null)
                return;

            UpdateCascadeSplits(camera.NearClippingPlane, camera.FarClippingPlane);

            for (int i = 0; i < cascadeCount; i++)
            {
                float near = (i == 0) ? camera.NearClippingPlane : cascadeLevels[i - 1];
                float far = cascadeLevels[i];
                lightSpaceMatrices[i] = GetLightSpaceMatrix(camera, light, near, far);
            }

            bool enabled = false;

            if(light.Type == LightType.Directional)
                enabled = light.CastShadows;

            UniformShadowInfo info = new UniformShadowInfo();
			info.farPlane = camera.FarClippingPlane;
			info.shadowBias = bias;
			info.cascadeCount = cascadeCount;
			info.enabled = enabled ? 1 : -1;

            unsafe
            {
                for(int i = 0; i < lightSpaceMatrices.Length; i++)
                {
                    int index = i * Marshal.SizeOf<Matrix4>();
                    Matrix4 matrix = lightSpaceMatrices[i];
                    float *pSrc = &matrix.Row0.X;
                    byte *pDst = &info.lightSpaceMatrices[index];
                    Unsafe.CopyBlock(pDst, pSrc, (uint)Marshal.SizeOf<Matrix4>());
                }

                for(int i = 0; i < cascadeLevels.Length; i++)
                {
                    int index = i * Marshal.SizeOf<Vector4>();

                    byte *pData = &info.cascadePlaneDistances[index];
                    float *pFloat = (float*)pData;
                    *pFloat = cascadeLevels[i];
                }
            }

            var pInfo = new ReadOnlySpan<UniformShadowInfo>(ref info);

			ubo.Bind();
			ubo.BufferSubData(pInfo, 0);
			ubo.Unbind();
        }

        public static void SetWorldBounds(float halfWidth, float height)
        {
            worldBounds = new Vector3[] {
                new Vector3(-halfWidth, -1.0f, -halfWidth), new Vector3(halfWidth, -1.0f, -halfWidth),
                new Vector3(-halfWidth, -1.0f,  halfWidth), new Vector3(halfWidth, -1.0f,  halfWidth),
                new Vector3(-halfWidth, height, -halfWidth), new Vector3(halfWidth, height, -halfWidth),
                new Vector3(-halfWidth, height,  halfWidth), new Vector3(halfWidth, height,  halfWidth)
            };
        }
    }
}