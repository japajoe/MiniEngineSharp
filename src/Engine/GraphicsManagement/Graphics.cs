using System;
using System.Collections.Generic;
using MiniEngine.Core;
using MiniEngine.GraphicsManagement.PostProcessing;
using MiniEngine.GraphicsManagement.Renderers;
using MiniEngine.GraphicsManagement.Shaders;
using MiniEngine.GraphicsManagement.Shaders.PostProcessing;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace MiniEngine.GraphicsManagement
{
    public enum FrameBufferTextureTarget
    {
        Color,
        Brightness
    }

    public class BloomSettings
    {
        public bool enabled;
        public float threshold;
        public float filterRadius;
        public float intensity;

        public BloomSettings()
        {
            enabled = true;
            threshold = 0.0f;
            filterRadius = 0.005f;
            intensity = 1.0f;
        }
    }

    public class AmbientOcclusionSettings
    {
        public bool globalEnabled = false;
        public float value;

        public AmbientOcclusionSettings()
        {
            globalEnabled = false;
            value = 10.0f;
        }
    }

    public enum ShaderName : int
    {
        Bloom = 0,
        BloomFilter,
        Line,
        Screen,
        Standard,
        ProceduralSkybox,
        ShadowDepth,
        COUNT
    }

    internal struct GraphicsContext
    {
        public Shader[] shaders;
        public FrameBuffer[] frameBuffers;
        public Camera camera;
        public List<Light> lights;
        public List<Renderer> renderers;
        public List<PostProcessingEffect> postProcessing;
        public BloomPostProcessor bloom;
        public PingPongBuffer pingpongBuffer;
        public BloomSettings bloomSettings;
        public AmbientOcclusionSettings ambientOcclusion;
        public LineRenderer lineRenderer;
        public ImGuiController imGuiController;
        public int width;
        public int height;
        public int screenVAO;
        public bool dirty;
        public bool bypassColorPass = false;

        public GraphicsContext(int width, int height)
        {
            this.width = width;
            this.height = height;
            
            OpenGL.Initialize();
            
            imGuiController = new ImGuiController();
            
            camera = null;
            
            lights = new List<Light>();
            
            renderers = new List<Renderer>();

            pingpongBuffer = new PingPongBuffer();
            
            postProcessing = new List<PostProcessingEffect>();
            bloom = new BloomPostProcessor();

            bloomSettings = new BloomSettings();
            ambientOcclusion = new AmbientOcclusionSettings();
            
            shaders = new Shader[(int)ShaderName.COUNT];
            for(int i = 0; i < shaders.Length; i++)
                shaders[i] = new Shader();
            
            Shadow.Generate();

            Graphics2D.Initialize();
            
            lineRenderer = new LineRenderer();
        }

        public Shader GetShader(ShaderName name)
        {
            int index = (int)name;
            if(index >= shaders.Length)
                return null;
            return shaders[index];
        }
    }

    public delegate void ResizeEvent(int width, int height);

    public static class Graphics
    {
        private static GraphicsContext context;
        private const int FRAMEBUFFER_MAIN = 0;
        private const int FRAMEBUFFER_SCREEN1 = 1;
        private const int FRAMEBUFFER_SCREEN2 = 2;

        public static bool BypassColorPass
        {
            get => context.bypassColorPass;
            set => context.bypassColorPass = value;
        }

        internal static void Initialize(int width, int height)
        {
            context = new GraphicsContext(width, height);

            CreateFrameBuffers();
            CreateUniformBuffers();
            CreateShaders();
        }

        internal static void Destroy()
        {
            context.imGuiController.Dispose();
        }

        internal static void NewFrame()
        {
            InvalidateFrameBuffers();
            UpdateUniformBuffers();

            FrameBuffer fboMain = context.frameBuffers[FRAMEBUFFER_MAIN];
            FrameBuffer fboScreen1 = context.frameBuffers[FRAMEBUFFER_SCREEN1];

            if(!context.bypassColorPass)
            {
                RenderShadowPass();
                RenderColorPass();
                fboMain.Blit(fboScreen1, BlitOption.Color | BlitOption.Depth);
                fboMain.Blit(fboScreen1, BlitOption.Color, 1, 1);
            }

            FrameBuffer outputFBO = RenderPostProcessingPass();

            GL.Disable(EnableCap.DepthTest);
            GL.Disable(EnableCap.Blend);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.Viewport(0, 0, context.width, context.height);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            Shader screenShader = context.GetShader(ShaderName.Screen);

            screenShader.Use();

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2d, outputFBO.GetColorAttachment((int)FrameBufferTextureTarget.Color));
            screenShader.SetInt(UniformName.Texture, 0);
            
            GL.BindVertexArray(context.screenVAO);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 3);
            GL.BindVertexArray(0);
        }

        internal static void BeginGUI()
        {
            context.imGuiController.NewFrame();
        }

        internal static void EndGUI()
        {
            Graphics2D.NewFrame();
            context.imGuiController.EndFrame();
        }

        internal static void SetViewport(int x, int y, int width, int height)
        {
            if(width != context.width || height != context.height)
            {
                context.width = width;
                context.height = height;
                context.dirty = true;
            }
        }

        private static void CreateFrameBuffers()
        {
            FrameBufferTextureSpecification colorAttachment = new FrameBufferTextureSpecification() {
                format = FrameBufferTextureFormat.RGBA16F,
                wrap = TextureWrapMode.ClampToEdge,
                filter = TextureFilterMode.Linear
            };

            FrameBufferTextureSpecification depthAttachment = new FrameBufferTextureSpecification() {
                format = FrameBufferTextureFormat.Depth24Stencil8,
                wrap = TextureWrapMode.ClampToEdge,
                filter = TextureFilterMode.Linear
            };

            FrameBufferSpecification specMain = new FrameBufferSpecification() {
                width = context.width,
                height = context.height,
                samples = 4,
                resizable = true,
                attachments = {
                    colorAttachment, // Main color
                    colorAttachment, // Brightness
                    depthAttachment
                }
            };

            FrameBufferSpecification specScreen1 = new FrameBufferSpecification() {
                width = context.width,
                height = context.height,
                samples = 1,
                resizable = true,
                attachments = {
                    colorAttachment, // Main Color
                    colorAttachment, // Brightness
                    depthAttachment
                }
            };

            FrameBufferSpecification specScreen2 = new FrameBufferSpecification() {
                width = context.width,
                height = context.height,
                samples = 1,
                resizable = true,
                attachments = {
                    colorAttachment
                }
            };

            context.frameBuffers = new FrameBuffer[3];
            
            for(int i = 0; i < context.frameBuffers.Length; i++)
                context.frameBuffers[i] = new FrameBuffer();

            context.frameBuffers[FRAMEBUFFER_MAIN].Generate(specMain);
            context.frameBuffers[FRAMEBUFFER_SCREEN1].Generate(specScreen1);
            context.frameBuffers[FRAMEBUFFER_SCREEN2].Generate(specScreen2);

            GL.GenVertexArrays(1, ref context.screenVAO);
        }

        private static void CreateUniformBuffers()
        {
            Camera.CreateUniformBuffer();
            Light.CreateUniformBuffer();
            World.CreateUniformBuffer();
            Shadow.CreateUniformBuffer();
        }

        private static void CreateShaders()
        {
            Shader screenShader = context.GetShader(ShaderName.Screen);
            Shader standardShader = context.GetShader(ShaderName.Standard);
            Shader proceduralSkyboxShader = context.GetShader(ShaderName.ProceduralSkybox);
            Shader shadowDepthShader = context.GetShader(ShaderName.ShadowDepth);
            Shader bloomShader = context.GetShader(ShaderName.Bloom);
            Shader bloomFilterShader = context.GetShader(ShaderName.BloomFilter);
            Shader lineShader = context.GetShader(ShaderName.Line);

            try
            {
                screenShader.Generate(ScreenShader.vertexSource, ScreenShader.fragmentSource);
                standardShader.Generate(StandardShader.vertexSource, StandardShader.fragmentSource);
                proceduralSkyboxShader.Generate(ProceduralSkyboxShader.vertexSource, ProceduralSkyboxShader.fragmentSource);
                shadowDepthShader.Generate(ShadowDepthShader.vertexSource, ShadowDepthShader.fragmentSource);
                bloomShader.Generate(PostProcessingShader.vertexSource, BloomShader.fragmentSource);
                bloomFilterShader.Generate(PostProcessingShader.vertexSource, BloomFilterShader.fragmentSource);
                lineShader.Generate(LineShader.vertexSource, LineShader.fragmentSource);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

			UniformBuffer uboCamera = Camera.GetUniformBuffer();
            UniformBuffer uboLights = Light.GetUniformBuffer();
            UniformBuffer uboWorld = World.GetUniformBuffer();
            UniformBuffer uboShadow = Shadow.GetUniformBuffer();

            for(int i = 0; i < context.shaders.Length; i++)
            {
			    uboCamera.BindBlockToShader(context.shaders[i], Camera.UBO_BINDING_INDEX, Camera.UBO_NAME);	
                uboLights.BindBlockToShader(context.shaders[i], Light.UBO_BINDING_INDEX, Light.UBO_NAME);
                uboWorld.BindBlockToShader(context.shaders[i], World.UBO_BINDING_INDEX, World.UBO_NAME);
                uboShadow.BindBlockToShader(context.shaders[i], Shadow.UBO_BINDING_INDEX, Shadow.UBO_NAME);
            }
        }

        private static void InvalidateFrameBuffers()
        {
            if(!context.dirty)
                return;

            for(int i = 0; i < context.frameBuffers.Length; i++)
            {
                context.frameBuffers[i].Resize(context.width, context.height);
            }

            context.imGuiController.InvalidateDeviceObjects();

            context.dirty = false;
        }

        private static void UpdateUniformBuffers()
        {
            Camera.UpdateUniformBuffer(context.camera);
            Light.UpdateUniformBuffer(context.camera, context.lights);
            World.UpdateUniformBuffer();
            Shadow.UpdateUniformBuffer(context.camera, context.lights.Count > 0 ? context.lights[0] : null);
        }

        private static void RenderShadowPass()
        {
            if(context.lights.Count == 0)
                return;

            if(context.camera == null)
                return;

            if(context.renderers.Count == 0)
                return;

            //TODO: find more robust way to select shadow caster
            if(context.lights[0].Type == LightType.Directional && !context.lights[0].CastShadows)
                return;

            Shadow.Bind();

            Matrix4[] lightSpaceMatrices = Shadow.GetLightSpaceMatrices();

            Shader shader = context.GetShader(ShaderName.ShadowDepth);
            shader.Use();

            for(int n = 0; n < Shadow.CascadeCount; n++)
            {
                shader.SetMat4(UniformName.LightSpaceMatrix, lightSpaceMatrices[n]);
                Shadow.Clear(n);

                for(int j = 0; j < context.renderers.Count; j++)
                {
                    context.renderers[j].OnRenderDepth();
                }
            }

            Shadow.Unbind();
        }

        private static void RenderColorPass()
        {
            FrameBuffer fbo = context.frameBuffers[FRAMEBUFFER_MAIN];
            
            fbo.Bind();

            if(context.camera == null || context.renderers.Count == 0)
            {
                fbo.Clear(Color.Black);
                fbo.Unbind();
                return;
            }

            fbo.Clear(context.camera.ClearColor);

            Matrix4 projection = context.camera.GetProjectionMatrix();
            Matrix4 view = context.camera.GetViewMatrix();

            for(int i = 0; i < context.renderers.Count; i++)
            {
                context.renderers[i].OnRender(projection, view, context.camera.Frustum);
            }

            context.lineRenderer.OnRender(projection, view, context.camera.Frustum);

            fbo.Unbind();
        }

        private static FrameBuffer RenderPostProcessingPass()
        {
		    if (context.camera == null)
		 	    return context.frameBuffers[FRAMEBUFFER_SCREEN1];

            if(context.renderers.Count == 0)
                return context.frameBuffers[FRAMEBUFFER_SCREEN1];

            context.pingpongBuffer.sourceFBO = context.frameBuffers[FRAMEBUFFER_SCREEN1];
            context.pingpongBuffer.destinationFBO = context.frameBuffers[FRAMEBUFFER_SCREEN2];

            Matrix4 projection = context.camera.GetProjectionMatrix();
            Matrix4 view = context.camera.GetViewMatrix();
            int depthTexture = context.pingpongBuffer.sourceFBO.GetDepthAttachment();

            if(context.bloomSettings.enabled)
            {
                context.bloom.SetBrightnessTexture(context.frameBuffers[FRAMEBUFFER_SCREEN1].GetColorAttachment((int)FrameBufferTextureTarget.Brightness));
                context.bloom.SetThreshold(context.bloomSettings.threshold);
                context.bloom.SetFilterRadius(context.bloomSettings.filterRadius);
                context.bloom.SetIntensity(context.bloomSettings.intensity);
                context.bloom.vao = context.screenVAO;
                context.bloom.buffer = context.pingpongBuffer;
                context.bloom.OnProcess(projection, view);
            }

            if(context.postProcessing.Count == 0)
                return context.pingpongBuffer.sourceFBO;

            for (int i = 0; i < context.postProcessing.Count; i++)
            {
                if(!context.postProcessing[i].IsActive)
                    continue;
                context.postProcessing[i].vao = context.screenVAO;
                context.postProcessing[i].buffer = context.pingpongBuffer;
                context.postProcessing[i].depthTexture = depthTexture;
                context.postProcessing[i].OnProcess(projection, view);
            }

            return context.pingpongBuffer.sourceFBO;
        }

        public static int GetScreenWidth()
        {
            return context.width;
        }

        public static int GetScreenHeight()
        {
            return context.height;
        }

        public static void Add(Camera camera)
        {
            if(camera == null)
                return;
            context.camera = camera;
        }

        public static void Add(Light light)
        {
            if(light == null)
                return;
            
            for(int i = 0; i < context.lights.Count; i++)
            {
                if(context.lights[i] == light)
                    return;
            }

            context.lights.Add(light);
        }

        public static void Add(Renderer renderer)
        {
            if(renderer == null)
                return;
            
            for(int i = 0; i < context.renderers.Count; i++)
            {
                if(context.renderers[i] == renderer)
                    return;
            }

            context.renderers.Add(renderer);
        }

        public static void Add(PostProcessingEffect postProcessingEffect)
        {
            if(postProcessingEffect == null)
                return;
            
            for(int i = 0; i < context.postProcessing.Count; i++)
            {
                if(context.postProcessing[i] == postProcessingEffect)
                    return;
            }

            postProcessingEffect.Initialize();

            context.postProcessing.Add(postProcessingEffect);
        }

        public static void Remove(Camera camera)
        {
            if(context.camera == camera)
                context.camera = null;
        }

        public static void Remove(Light light)
        {
            if(light == null)
                return;
            
            int index = -1;

            for(int i = 0; i < context.lights.Count; i++)
            {
                if(context.lights[i] == light)
                {
                    index = i;
                    break;
                }
            }

            if(index >= 0)
                context.lights.RemoveAt(index);
        }

        public static void Remove(Renderer renderer)
        {
            if(renderer == null)
                return;
            
            int index = -1;

            for(int i = 0; i < context.renderers.Count; i++)
            {
                if(context.renderers[i] == renderer)
                {
                    index = i;
                    break;
                }
            }

            if(index >= 0)
                context.renderers.RemoveAt(index);
        }

        public static void Remove(PostProcessingEffect postProcessingEffect)
        {
            if(postProcessingEffect == null)
                return;
            
            int index = -1;

            for(int i = 0; i < context.postProcessing.Count; i++)
            {
                if(context.postProcessing[i] == postProcessingEffect)
                {
                    index = i;
                    break;
                }
            }

            if(index >= 0)
                context.postProcessing.RemoveAt(index);
        }

        public static Shader GetShader(ShaderName name)
        {
            return context.GetShader(name);
        }

        public static BloomSettings GetBloomSettings()
        {
            return context.bloomSettings;
        }

        public static AmbientOcclusionSettings GetAmbientOcclusionSettings()
        {
            return context.ambientOcclusion;
        }

        public static void DrawLine(Vector3 from, Vector3 to, Color color)
        {
            context.lineRenderer.DrawLine(from, to, color);
        }
    }
}