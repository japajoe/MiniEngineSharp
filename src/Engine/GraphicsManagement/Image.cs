using System;
using StbImageSharp;

namespace MiniEngine.GraphicsManagement
{
    public sealed class Image
    {
        private byte[] data;
        private int width;
        private int height;
        private int channels;
        private bool isLoaded;

        public byte[] Data
        {
            get
            {
                return data;
            }
        }

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

        public int Channels
        {
            get
            {
                return channels;
            }
        }

        public int DataSize
        {
            get
            {
                return width * height * channels;
            }
        }

        public bool IsLoaded
        {
            get
            {
                return isLoaded;
            }
        }

        public Image()
        {
            this.data = null;
            this.width = 0;
            this.height = 0;
            this.channels = 0;
            this.isLoaded = false;
        }

        public Image(string filepath)
        {
            this.data = null;
            this.width = 0;
            this.height = 0;
            this.channels = 0;
            this.isLoaded = false;

            if(LoadFromFile(filepath))
            {
                this.isLoaded = true;
            }
        }

        public Image(byte[] compressedData)
        {
            this.isLoaded = false;
            this.data = null;
            this.width = 0;
            this.height = 0;
            this.channels = 0;

            if(LoadFromMemory(compressedData))
            {
                this.isLoaded = true;
            }
        }

        public Image(byte[] uncompressedData, int width, int height, int channels)
        {
            this.width = width;
            this.height = height;
            this.channels = channels;
            this.data = new byte[uncompressedData.Length];
            Buffer.BlockCopy(uncompressedData, 0, this.data, 0, uncompressedData.Length);
            this.isLoaded = true;
        }

        public Image(int width, int height, int channels, Color color)
        {
            this.isLoaded = false;
            this.data = null;
            this.width = width;
            this.height = height;
            this.channels = channels;

            if(Load(width, height, channels, color))
            {
                this.isLoaded = true;
            }
        }

        private bool LoadFromFile(string filepath)
        {
            if (!System.IO.File.Exists(filepath))
            {
                Console.WriteLine("File does not exist: " + filepath);
                return false;
            }

            using (var stream = System.IO.File.OpenRead(filepath))
            {
                try
                {
                    ImageResult image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
                    width = image.Width;
                    height = image.Height;
                    channels = (int)image.Comp;
                    data = image.Data;
                    return true;
                }
                catch(System.Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    return false;
                }
            }
        }

        private bool LoadFromMemory(byte[] compressedImageData)
        {
            try
            {
                ImageResult image = ImageResult.FromMemory(compressedImageData, ColorComponents.RedGreenBlueAlpha);
                width = image.Width;
                height = image.Height;
                channels = (int)image.Comp;
                data = image.Data;
                return true;
            }
            catch(System.Exception ex)
            {
                width = 0;
                height = 0;
                channels = 0;
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        private bool Load(int width, int height, int channels, Color color)
        {
            if(channels < 3 || channels > 4)
                return false;

            int size = width * height * channels;

            if(size == 0)
                return false;

            this.data = new byte[size];

            if(channels == 3)
            {
                for(int i = 0; i < size; i += 3)
                {
                    byte r = (byte)(Math.Clamp(color.r * 255, 0.0, 255.0));
                    byte g = (byte)(Math.Clamp(color.g * 255, 0.0, 255.0));
                    byte b = (byte)(Math.Clamp(color.b * 255, 0.0, 255.0));

                    data[i+0] = r;
                    data[i+1] = g;
                    data[i+2] = b;
                }
            }
            else
            {
                for(int i = 0; i < size; i += 4)
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
            }
            
            return true;
        }
    }
}