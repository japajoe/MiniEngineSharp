using System;
using System.Runtime.InteropServices;

namespace MiniEngine.GraphicsManagement
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Color
    {
        public float r;
        public float g;
        public float b;
        public float a;

        public static Color White => new Color(1, 1, 1, 1);
        public static Color Black => new Color(0, 0, 0, 1);
        public static Color Red => new Color(1, 0, 0, 1);
        public static Color Green => new Color(0, 1, 0, 1);
        public static Color Blue => new Color(0, 0, 1, 1);
        public static Color RayWhite => Color.FromInt(245, 245, 245, 255);
        public static Color Orange => Color.FromInt(255, 161, 0, 255);
        public static Color Yellow => new Color(1, 1, 0, 1);
        public static Color Cyan => new Color(0, 1, 1, 1);
        public static Color Magenta => new Color(1, 0, 1, 1);
        public static Color Gray => Color.FromInt(130, 130, 130, 255);
        public static Color DarkGray => Color.FromInt(80, 80, 80, 255);
        public static Color LightGray => Color.FromInt(200, 200, 200, 255);
        public static Color Gold => Color.FromInt(255, 203, 0, 255);
        public static Color Pink => Color.FromInt(255, 109, 194, 255);
        public static Color Maroon => Color.FromInt(190, 33, 55, 255);
        public static Color Lime => Color.FromInt(0, 158, 47, 255);
        public static Color DarkGreen => Color.FromInt(0, 117, 44, 255);
        public static Color SkyBlue => Color.FromInt(102, 191, 255, 255);
        public static Color DarkBlue => Color.FromInt(0, 82, 172, 255);
        public static Color Purple => Color.FromInt(200, 122, 255, 255);
        public static Color Violet => Color.FromInt(135, 60, 190, 255);
        public static Color DarkPurple => Color.FromInt(112, 31, 126, 255);
        public static Color Beige => Color.FromInt(211, 176, 131, 255);
        public static Color Brown => Color.FromInt(127, 106, 79, 255);
        public static Color DarkBrown => Color.FromInt(76, 63, 47, 255);
        public static Color Transparent => new Color(0, 0, 0, 0);

        public Color()
        {
            r = 0.0f;
            g = 0.0f;
            b = 0.0f;
            a = 1.0f;
        }

        public Color(float r, float g, float b, float a)
        {
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = a;
        }

        public static Color FromInt(int r, int g, int b, int a)
        {
            float rf = Math.Clamp(r, 0, 255) / 255.0f;
            float gf = Math.Clamp(g, 0, 255) / 255.0f;
            float bf = Math.Clamp(b, 0, 255) / 255.0f;
            float af = Math.Clamp(a, 0, 255) / 255.0f;

            return new Color(rf, gf, bf, af);
        }
    }

}