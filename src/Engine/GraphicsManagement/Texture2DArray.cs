using System;
using OpenTK.Graphics.OpenGL;

namespace MiniEngine.GraphicsManagement
{
    public sealed class Texture2DArray
    {
        private int id;
        private int width;
        private int height;
        private int depth;

        public int Id => id;
        public int Width => width;
        public int Height => height;
        public int Depth => depth;

        public Texture2DArray()
        {
            id = 0;
            width = 0;
            height = 0;
            depth = 0;
        }

        public void Generate(int width, int height, int depth)
        {
            if(width == 0 || height == 0 || depth == 0)
                throw new Exception("Failed to generate texture:  width/height/depth must all be greater than 0");

            this.width = width;
            this.height = height;
            this.depth = depth;

            GL.GenTextures(1, ref id);
            GL.BindTexture(TextureTarget.Texture2dArray, id);
            GL.TexImage3D(TextureTarget.Texture2dArray, 0, InternalFormat.DepthComponent32f, width, height, depth, 0, PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero);
            GL.TexParameteri(TextureTarget.Texture2dArray, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameteri(TextureTarget.Texture2dArray, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.TexParameteri(TextureTarget.Texture2dArray, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToBorder);
            GL.TexParameteri(TextureTarget.Texture2dArray, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToBorder);

            float[] bordercolor = new float[] { 1.0f, 1.0f, 1.0f, 1.0f };
            
            unsafe
            {
                fixed(float *pBorderColor = &bordercolor[0])
                {
                    GL.TexParameterfv(TextureTarget.Texture2dArray, TextureParameterName.TextureBorderColor, pBorderColor);
                }
            }

            GL.BindTexture(TextureTarget.Texture2dArray, 0);
        }

        public void Destroy()
        {
            if(id > 0)
            {
                GL.DeleteTexture(id);
            }
            
            id = 0;
        }

        public void Bind(int unit)
        {
            if(id > 0)
            {
                GL.ActiveTexture(TextureUnit.Texture0 + (uint)unit);
                GL.BindTexture(TextureTarget.Texture2dArray, id);
            }
        }

        public void Unbind()
        {
            GL.BindTexture(TextureTarget.Texture2dArray, 0);
        }
    }
}