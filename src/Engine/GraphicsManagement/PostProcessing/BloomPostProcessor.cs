using System;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace MiniEngine.GraphicsManagement.PostProcessing
{
    public struct BloomMip
    {
        public int texture;
        public int width;
        public int height;
    }

    public sealed class BloomPostProcessor : PostProcessingEffect
    {
        private Shader bloomFilterShader;
        private List<BloomMip> mipChain;
        private int bloomFBO;
        private int brightnessTexture;
        private int textureWidth;
        private int textureHeight;
        private float filterRadius;
        private float threshold;
        private float intensity;
        private int uBloomTexture;
        private int uIntensity;
        private int uSampleMode;
        private int uThreshold;
        private int uFilterRadius;
        private int uResolution;

        public BloomPostProcessor() : base()
        {
            bloomFilterShader = null;
            mipChain = new List<BloomMip>();
            bloomFBO = 0;
            brightnessTexture = 0;
            textureWidth = 0;
            textureHeight = 0;
            filterRadius = 0.005f;
            threshold = 0.0f;
            intensity = 1.0f;
            uBloomTexture = -1;
            uIntensity = -1;
            uSampleMode = -1;
            uThreshold = -1;
            uFilterRadius = -1;
            uResolution = -1;
        }

        public override void Initialize()
        {
            shader = Graphics.GetShader(ShaderName.Bloom);

            uBloomTexture = GL.GetUniformLocation(shader.Id, "uBloomTexture");
            uIntensity = GL.GetUniformLocation(shader.Id, "uIntensity");
            
            bloomFilterShader = Graphics.GetShader(ShaderName.BloomFilter);

            uSampleMode = GL.GetUniformLocation(bloomFilterShader.Id, "uSampleMode");
            uThreshold = GL.GetUniformLocation(bloomFilterShader.Id, "uThreshold");
            uFilterRadius = GL.GetUniformLocation(bloomFilterShader.Id, "uFilterRadius");
            uResolution = GL.GetUniformLocation(bloomFilterShader.Id, "uResolution");
            
            textureWidth = Graphics.GetScreenWidth();
            textureHeight = Graphics.GetScreenHeight();

            CreateFrameBuffer(textureWidth, textureHeight);
        }

        private void CreateFrameBuffer(int width, int height)
        {
            GL.GenFramebuffers(1, ref bloomFBO);

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, bloomFBO);

            float currentWidth = (float)width / 2.0f;
            float currentHeight = (float)height / 2.0f;

            int mipChainLength = 7;

            for (int i = 0; i < mipChainLength; i++)
            {
                BloomMip mip = new BloomMip();
                mip.width = (int)currentWidth;
                mip.height = (int)currentHeight;

                // Ensure we don't go below 1x1, though usually 
                // you'd stop much sooner for quality reasons.
                if (mip.width <= 1 || mip.height <= 1) 
                {
                    break;
                }

                GL.GenTextures(1, ref mip.texture);
                GL.BindTexture(TextureTarget.Texture2d, mip.texture);
                
                // Use R11G11B10F for high dynamic range without the alpha overhead
                GL.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.R11fG11fB10f, mip.width, mip.height, 0, PixelFormat.Rgb, PixelType.Float, IntPtr.Zero);
                
                // Bilinear filtering is mandatory for the downsample/upsample math to work
                GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
                GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
                GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

                mipChain.Add(mip);

                currentWidth /= 2.0f;
                currentHeight /= 2.0f;
            }

            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2d, mipChain[0].texture, 0);

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        private void ClearTextures()
        {
            for (int i = 0; i < mipChain.Count; i++)
            {
                GL.DeleteTextures(1, mipChain[i].texture);
            }
            mipChain.Clear();
        }

        private void Invalidate()
        {
            int width = buffer.sourceFBO.GetWidth();
            int height = buffer.sourceFBO.GetHeight();

            if(textureWidth != width || textureHeight != height)
            {
                textureWidth = width;
                textureHeight = height;
                ClearTextures();
                CreateFrameBuffer(width, height);
            }
        }

        private void DownAndUpSample()
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, bloomFBO);
            GL.Disable(EnableCap.Blend);

            bloomFilterShader.Use();

            // --- DOWNSAMPLING ---
            bloomFilterShader.SetIntEx(uSampleMode, 0);
            
            int inputTexture = brightnessTexture;
            Vector2 inputResolution = new Vector2(textureWidth, textureHeight);

            for (int i = 0; i < mipChain.Count; i++)
            {
                BloomMip mip = mipChain[i];
                GL.Viewport(0, 0, mip.width, mip.height);
                GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2d, mip.texture, 0);
                
                bloomFilterShader.SetFloat2Ex(uResolution, inputResolution);
                
                // Thresholding only applies to the very first downsample pass
                if (i == 0)
                    bloomFilterShader.SetFloatEx(uThreshold, threshold);
                else
                    bloomFilterShader.SetFloatEx(uThreshold, -1.0f);

                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2d, inputTexture);
                bloomFilterShader.SetInt(UniformName.Texture, 0);
                
                // Function to render a full-screen quad
                Render();

                inputTexture = mip.texture;
                inputResolution.X = (float)mip.width;
                inputResolution.Y = (float)mip.height;
            }

            // --- UPSAMPLING ---
            bloomFilterShader.SetIntEx(uSampleMode, 1);
            bloomFilterShader.SetFloatEx(uFilterRadius, filterRadius);
            
            // Use additive blending to combine mips
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.One, BlendingFactor.One);

            for (int i = mipChain.Count - 1; i > 0; i--)
            {
                BloomMip mip = mipChain[i];
                BloomMip nextMip = mipChain[i - 1];

                GL.Viewport(0, 0, nextMip.width, nextMip.height);
                GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2d, nextMip.texture, 0);

                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2d, mip.texture);
                bloomFilterShader.SetInt(UniformName.Texture, 0);
                Render();
            }

            GL.Disable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        public override void OnProcess(Matrix4 projection, Matrix4 view)
        {
            if(shader == null)
                Initialize();

            Invalidate();

            DownAndUpSample();

            Bind();
            
            shader.Use();

            shader.SetFloatEx(uIntensity, intensity);

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2d, buffer.sourceFBO.GetColorAttachment(0));
            shader.SetInt(UniformName.Texture, 0);

            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.Texture2d, mipChain[0].texture);
            shader.SetIntEx(uBloomTexture, 1);

            Render();
            
            SwapBuffers();
        }

        public void SetBrightnessTexture(int texture)
        {
            brightnessTexture = texture;
        }

        public void SetFilterRadius(float radius)
        {
            filterRadius = radius;
        }

        public float GetFilterRadius()
        {
            return filterRadius;
        }

        public void SetThreshold(float threshold)
        {
            this.threshold = threshold;
        }

        public float GetThreshold()
        {
            return threshold;
        }

        public void SetIntensity(float intensity)
        {
            this.intensity = intensity;
        }

        public float GetIntensity()
        {
            return intensity;
        }
    }
}