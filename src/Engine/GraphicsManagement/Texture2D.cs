using System;
using OpenTK.Graphics.OpenGL;

namespace MiniEngine.GraphicsManagement
{
    public sealed class Texture2D
    {
        private int id;
        private int width;
        private int height;
        private static Texture2D diffuseTexture;

        public int Id => id;

        public int Width
        {
            get
            {
                return width;
            }
        }

        public int Height
        {
            get
            {
                return height;
            }
        }

        public Texture2D()
        {
            id = 0;
            width = 0;
            height = 0;
        }

        public Texture2D(int id, int width, int height)
        {
            this.id = id;
            this.width = width;
            this.height = height;
        }

        public void Generate(Image image)
        {
            byte[] data = image.Data;

            if(data != null)
            {
                width = image.Width;
                height = image.Height;

                GL.GenTextures(1, ref id);
                GL.BindTexture(TextureTarget.Texture2d, id);

                GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
                GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
                GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
                GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
                GL.PixelStorei(PixelStoreParameter.UnpackAlignment, 1);

                int channels = image.Channels;

                switch(channels)
                {
                    case 1:
                    {
                        GL.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.Rgba, (int)image.Width, (int)image.Height, 0, PixelFormat.Red, PixelType.UnsignedByte, data);    
                        break;
                    }
                    case 2:
                    {
                        GL.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.Rgba, (int)image.Width, (int)image.Height, 0, PixelFormat.Rg, PixelType.UnsignedByte, data);
                        break;
                    }
                    case 3:
                    {
                        GL.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.Rgba, (int)image.Width, (int)image.Height, 0, PixelFormat.Rgb, PixelType.UnsignedByte, data);
                        break;
                    }
                    case 4:
                    {
                        GL.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.Rgba, (int)image.Width, (int)image.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, data);
                        break;
                    }
                    default:
                    {
                        GL.BindTexture(TextureTarget.Texture2d, 0);
                        GL.DeleteTextures(1, id);
                        id = 0;
                        string error = "Failed to load texture: Unsupported number of channels: " + image.Channels;
                        throw new Exception(error);
                    }
                }
                
                GL.GenerateMipmap(TextureTarget.Texture2d);
                GL.BindTexture(TextureTarget.Texture2d, 0);
            } 
            else 
            {
                throw new Exception("Failed to load texture: No valid data passed");
            }
        }

        public void Generate(int width, int height, Color color)
        {
            this.width = 0;
            this.height = 0;

            if(width == 0 || height == 0)
                throw new Exception("Failed to load texture: Texture width and height must be greater than 0");

            int channels = 4;
            int size = width * height * channels;
            byte[] data = new byte[size];

            if(data != null)
            {
                this.width = width;
                this.height = height;

                for(int i = 0; i < size; i += channels)
                {
                    byte r = (byte)(Math.Clamp(color.r * 255, 0.0, 255.0));
                    byte g = (byte)(Math.Clamp(color.g * 255, 0.0, 255.0));
                    byte b = (byte)(Math.Clamp(color.b * 255, 0.0, 255.0));
                    byte a = (byte)(Math.Clamp(color.a * 255, 0.0, 255.0));

                    data[i+0] = r;
                    data[i+1] = g;
                    data[i+2] = b;
                    data[i+3] = a;
                }

                GL.GenTextures(1, ref id);
                GL.BindTexture(TextureTarget.Texture2d, id);

                GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
                GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
                GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
                GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

                GL.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.Rgba, (int)width, (int)height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, data);
                
                GL.GenerateMipmap(TextureTarget.Texture2d);
                GL.BindTexture(TextureTarget.Texture2d, 0);

            } 
            else 
            {
                throw new Exception("Failed to load texture: Failed to allocate memory");
            }
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
            GL.BindTexture( TextureTarget.Texture2d, id);
        }

        public void Unbind()
        {
            GL.BindTexture(TextureTarget.Texture2d, 0);
        }

        public void SetMaxAnisotropyLevel(float level)
        {
            if(level > OpenGL.MaxAnisotropy)
                level = OpenGL.MaxAnisotropy;
            GL.BindTexture(TextureTarget.Texture2d, id);
            GL.TexParameterf(TextureTarget.Texture2d, (TextureParameterName)OpenGL.GL_TEXTURE_MAX_ANISOTROPY_EXT, level);
            GL.BindTexture(TextureTarget.Texture2d, 0);
        }
        
        public static Texture2D GetDiffuseTexture()
        {
            if(diffuseTexture != null)
                return diffuseTexture;

            diffuseTexture = new Texture2D();
            diffuseTexture.Generate(2, 2, Color.White);
            return diffuseTexture;
        }
    }
}