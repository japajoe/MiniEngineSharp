using System;
using System.Collections.Generic;
using MiniEngine.GraphicsManagement;

namespace MiniEngine.Utilities
{
    public static class TextureHelper
    {
        public static Texture2D CreateGroundTexture()
        {
            Color grassColor = Color.Green;
            grassColor.g = 0.7f;
            return CreateGroundTexture(512, 512, grassColor, Color.Brown, 0.85f, 0);
        }

        public static Texture2D CreateBrickTexture()
        {
			Color brickRed = new Color(0.45f, 0.35f, 0.35f, 1.0f);
			Color mortarGrey = new Color(0.7f, 0.7f, 0.7f, 1.0f);
			return CreateBrickTexture(512, 512, brickRed, mortarGrey, 10, 5, 3.0f, 0.5f, 12345);	
        }

        public static Texture2D CreateGroundTexture(int width, int height, Color colorGrass, Color colorGround, float mixFactor, int randomSeed)
        {
            Texture2D target = new Texture2D();
            byte[] imageData = new byte[width * height * 4];

            Noise.SetType(NoiseType.OpenSimplex2S);
            Noise.SetFractalType(FractalType.FBm);
            Noise.SetFrequency(0.02f);
            Noise.SetLacunarity(2.0f);
            Noise.SetSeed(randomSeed);

            byte[] dirtColor = { (byte)(colorGround.r * 255), (byte)(colorGround.g * 255), (byte)(colorGround.b * 255) };
            byte[] grassColor = { (byte)(colorGrass.r * 255), (byte)(colorGrass.g * 255), (byte)(colorGrass.b * 255) };

            float coverage = mixFactor; // 0.0 = Pure Dirt, 1.0 = Pure Grass, 0.5 = Even Mix
            float sharpness = 0.1f;     // Lower = blurrier transitions, Higher = sharper edges
            float margin = 0.1f;

            for (int y = 0; y < height; ++y)
            {
                for (int x = 0; x < width; ++x)
                {
                    float fx = (float)x;
                    float fy = (float)y;
                    float fw = (float)width;
                    float fh = (float)height;

                    float sample = Noise.GetSample(fx, fy);

                    // Edge tiling logic
                    if (x > (1.0f - margin) * fw)
                    {
                        float weight = (fx - (1.0f - margin) * fw) / (margin * fw);
                        sample = sample * (1.0f - weight) + Noise.GetSample(fx - fw, fy) * weight;
                    }

                    if (y > (1.0f - margin) * fh)
                    {
                        float weight = (fy - (1.0f - margin) * fh) / (margin * fh);
                        sample = sample * (1.0f - weight) + Noise.GetSample(fx, fy - fh) * weight;
                    }

                    // Normalize sample to 0-1
                    float blend = (sample + 1.0f) * 0.5f;

                    // Remap blend based on coverage and sharpness
                    // We create a window around the 'coverage' value
                    float lowerBound = (1.0f - coverage) - sharpness;
                    float upperBound = (1.0f - coverage) + sharpness;

                    if (blend < lowerBound)
                    {
                        blend = 0.0f;
                    }
                    else if (blend > upperBound)
                    {
                        blend = 1.0f;
                    }
                    else
                    {
                        blend = (blend - lowerBound) / (upperBound - lowerBound);
                    }

                    int index = (y * width + x) * 4;
                    for (int i = 0; i < 3; ++i)
                    {
                        float colorValue = dirtColor[i] + blend * (grassColor[i] - dirtColor[i]);
                        float finalPixel = colorValue + (sample * 10.0f);

                        if (finalPixel > 255.0f)
                        {
                            finalPixel = 255.0f;
                        }
                        if (finalPixel < 0.0f)
                        {
                            finalPixel = 0.0f;
                        }

                        imageData[index + i] = (byte)finalPixel;
                    }

                    imageData[index + 3] = 255;
                }
            }

            Image image = new Image(imageData, width, height, 4);

            if (image.IsLoaded)
            {
                target.Generate(image);
            }

            return target;
        }

        public static Texture2D CreateBrickTexture(int width, int height, Color colorBrick, Color colorMortar, int brickRows, int bricksPerRow, float mortarThickness, float weatheringFactor, int randomSeed)
        {
            Texture2D target = new Texture2D();
            byte[] imageData = new byte[width * height * 4];

            Noise.SetType(NoiseType.OpenSimplex2S);
            Noise.SetSeed(randomSeed);
            Noise.SetFrequency(0.05f);

            byte[] brickRGB = { (byte)(colorBrick.r * 255), (byte)(colorBrick.g * 255), (byte)(colorBrick.b * 255) };
            byte[] mortarRGB = { (byte)(colorMortar.r * 255), (byte)(colorMortar.g * 255), (byte)(colorMortar.b * 255) };

            float brickHeight = (float)height / brickRows;
            float brickWidth = (float)width / bricksPerRow;
            float margin = 0.15f; // Margin for blending the noise at edges

            for (int y = 0; y < height; ++y)
            {
                for (int x = 0; x < width; ++x)
                {
                    float fx = (float)x;
                    float fy = (float)y;
                    float fw = (float)width;
                    float fh = (float)height;

                    // Tileable Noise Sampling (The "Grit")
                    float gritNoise = Noise.GetSample(fx, fy);
                    float stainNoise = Noise.GetSample(fx * 0.1f, fy * 0.1f);

                    // Horizontal Blend
                    if (x > (1.0f - margin) * fw)
                    {
                        float weight = (fx - (1.0f - margin) * fw) / (margin * fw);
                        gritNoise = gritNoise * (1.0f - weight) + Noise.GetSample(fx - fw, fy) * weight;
                        stainNoise = stainNoise * (1.0f - weight) + Noise.GetSample((fx - fw) * 0.1f, fy * 0.1f) * weight;
                    }
                    // Vertical Blend
                    if (y > (1.0f - margin) * fh)
                    {
                        float weight = (fy - (1.0f - margin) * fh) / (margin * fh);
                        gritNoise = gritNoise * (1.0f - weight) + Noise.GetSample(fx, fy - fh) * weight;
                        stainNoise = stainNoise * (1.0f - weight) + Noise.GetSample(fx * 0.1f, (fy - fh) * 0.1f) * weight;
                    }

                    int rowIndex = (int)(fy / brickHeight);
                    float xOffset = (rowIndex % 2 == 1) ? brickWidth * 0.5f : 0.0f;

                    // Wrap x for colIndex calculation
                    float wrappedX = (fx + xOffset) % fw;
                    if (wrappedX < 0)
                    {
                        wrappedX += fw;
                    }

                    int colIndex = (int)(wrappedX / brickWidth);
                    float localX = wrappedX % brickWidth;
                    float localY = fy % brickHeight;

                    // Sample noise at the brick's center to get a solid color per brick
                    float brickVarX = ((float)colIndex * brickWidth) + (brickWidth * 0.5f);
                    float brickVarY = ((float)rowIndex * brickHeight) + (brickHeight * 0.5f);
                    float brickVariation = Noise.GetSample(brickVarX * 10.0f, brickVarY * 10.0f);

                    bool isMortar = false;

                    if (localY < mortarThickness || localY > (brickHeight - mortarThickness))
                    {
                        isMortar = true;
                    }

                    if (localX < mortarThickness || localX > (brickWidth - mortarThickness))
                    {
                        isMortar = true;
                    }

                    int index = (y * width + x) * 4;

                    for (int i = 0; i < 3; ++i)
                    {
                        float baseColor;
                        float finalPixel;

                        if (isMortar)
                        {
                            baseColor = mortarRGB[i];
                            finalPixel = baseColor + (gritNoise * 12.0f) + (stainNoise * 45.0f * weatheringFactor);
                        }
                        else
                        {
                            baseColor = brickRGB[i];
                            float variationAmount = brickVariation * 35.0f * weatheringFactor;
                            float stainAmount = stainNoise * 30.0f * weatheringFactor;
                            finalPixel = baseColor + (gritNoise * 20.0f) + variationAmount + stainAmount;
                        }

                        if (finalPixel > 255.0f)
                        {
                            finalPixel = 255.0f;
                        }

                        if (finalPixel < 0.0f)
                        {
                            finalPixel = 0.0f;
                        }

                        imageData[index + i] = (byte)finalPixel;
                    }

                    imageData[index + 3] = 255;
                }
            }

            Image image = new Image(imageData, width, height, 4);

            if (image.IsLoaded)
            {
                target.Generate(image);
            }

            return target;
        }

        public static TextureCubeMap CreateStarsTexture(int width, int height)
        {
            Random random = new Random();

            List<Image> starImages = new List<Image>();
            for(int i = 0; i < 6; i++)
                starImages.Add(null);
            
            int channels = 4;
            byte[] data = new byte[width * height * channels];

            for(int i = 0; i < starImages.Count; i++)
            {
                Array.Fill<byte>(data, 0);

                int starCount = 200;
                for (int s = 0; s < starCount; s++)
                {
                    int x = (int)random.Next(0, width -1);
                    int y = (int)random.Next(0, height -1);
                    int index = (y * width + x) * channels;

                    // Random brightness between 150 and 255
                    byte brightness = (byte)(150 + (random.Next(0, 105)));

                    data[index + 0] = brightness; // R
                    data[index + 1] = brightness; // G
                    data[index + 2] = brightness; // B
                    data[index + 3] = 255;        // A
                }

                starImages[i] = new Image(data, width, height, channels);
            }

            TextureCubeMap texture = new TextureCubeMap();
            texture.Generate(starImages);
            return texture;
        }
    }
}