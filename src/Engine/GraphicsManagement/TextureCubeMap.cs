using System;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL;

namespace MiniEngine.GraphicsManagement
{
    public sealed class TextureCubeMap
    {
		private int id;
		private int width;
		private int height;

        public int Id => id;
        public int Width => width;
        public int Height => height;

        public TextureCubeMap()
        {
            id = 0;
            width = 0;
            height = 0;
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
            GL.ActiveTexture(TextureUnit.Texture0 + (uint)unit);
            GL.BindTexture(TextureTarget.TextureCubeMap, id);
        }

        public void Unbind()
        {
            GL.BindTexture(TextureTarget.TextureCubeMap, 0);
        }

        public void Generate(int width, int height, Color color)
        {
            this.width = width;
            this.height = height;

            int channels = 4;
            int size = width * height * channels;
            byte[] data = new byte[size];

            for(int i = 0; i < size; i += channels)
            {
                byte r = (byte)Math.Clamp(color.r * 255.0f, 0.0f, 255.0f);
                byte g = (byte)Math.Clamp(color.g * 255.0f, 0.0f, 255.0f);
                byte b = (byte)Math.Clamp(color.b * 255.0f, 0.0f, 255.0f);
                byte a = (byte)Math.Clamp(color.a * 255.0f, 0.0f, 255.0f);

                data[i+0] = r;
                data[i+1] = g;
                data[i+2] = b;
                data[i+3] = a;
            }

            GL.GenTexture(out id);
            GL.BindTexture(TextureTarget.TextureCubeMap, id);
            GL.PixelStorei(PixelStoreParameter.UnpackAlignment, 1);

            for(int i = 0; i < 6; i++)
            {
                GL.TexImage2D(TextureTarget.TextureCubeMapPositiveX + (uint)i, 0, InternalFormat.Rgba, width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, data);
            }

            GL.TexParameteri(TextureTarget.TextureCubeMap, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameteri(TextureTarget.TextureCubeMap, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameteri(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameteri(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameteri(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapR, (int)TextureWrapMode.ClampToEdge);

            GL.BindTexture(TextureTarget.TextureCubeMap, 0);
        }

        // Right, Left, Top, Bottom, Front, Back
        public void Generate(List<Image> images)
        {
            GL.GenTexture(out id);
            GL.BindTexture(TextureTarget.TextureCubeMap, id);
            GL.PixelStorei(PixelStoreParameter.UnpackAlignment, 1);

            for(int i = 0; i < images.Count; i++)
            {
                Image image = images[i];

                byte[] data = image.Data;
                int width = (int)image.Width;
                int height = (int)image.Height;

                this.width = width;
                this.height = height;
                
                if(data != null)
                {
                    int channels = (int)image.Channels;                    
                    TextureTarget target = TextureTarget.TextureCubeMapPositiveX + (uint)i;

                    switch(channels)
                    {
                        case 1:
                        {
                            GL.TexImage2D(target, 0, InternalFormat.Rgba, width, height, 0, PixelFormat.Red, PixelType.UnsignedByte, data);    
                            break;
                        }
                        case 2:
                        {
                            GL.TexImage2D(target, 0, InternalFormat.Rgba, width, height, 0, PixelFormat.Rg, PixelType.UnsignedByte, data);
                            break;
                        }
                        case 3:
                        {
                            GL.TexImage2D(target, 0, InternalFormat.Rgba, width, height, 0, PixelFormat.Rgb, PixelType.UnsignedByte, data);
                            break;
                        }
                        case 4:
                        {
                            GL.TexImage2D(target, 0, InternalFormat.Rgba, width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, data);
                            break;
                        }
                        default:
                        {
                            GL.BindTexture(TextureTarget.TextureCubeMap, 0);
                            GL.DeleteTexture(id);
                            id = 0;
                            string error = "Failed to load texture: Unsupported number of channels: " + image.Channels;
                            throw new Exception(error);
                        }
                    }
                }
                else 
                {
                    GL.BindTexture(TextureTarget.TextureCubeMap, 0);
                    GL.DeleteTexture(id);
                    id = 0;
                    throw new Exception("Failed to load texture: No valid data passed");
                }
            }

            GL.TexParameteri(TextureTarget.TextureCubeMap, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameteri(TextureTarget.TextureCubeMap, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameteri(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameteri(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameteri(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapR, (int)TextureWrapMode.ClampToEdge);

            GL.BindTexture(TextureTarget.TextureCubeMap, 0);
        }
    }
}