using System;
using MiniEngine.Core;
using MiniEngine.Embedded;
using MiniEngine.GraphicsManagement;
using OpenTK.Mathematics;

namespace MiniEngine.GraphicsManagent
{
    public static class GUI
    {
        private static int hotId = 0;
        private static int activeId = 0;
        private static Font font;
        private static float fontSize = 14;

        public static void Begin()
        {
            if(font == null)
            {
                font = new Font();
                var data = RobotoMonoRegular.GetData();
                if(font.LoadFromMemory(data, data.Length, 32, FontRenderMethod.SDF))
                {
                    font.GenerateTexture();
                }
            }

            hotId = 0;
        }

        public static void End()
        {
            if (Input.GetButtonUp(ButtonCode.Left))
            {
                activeId = 0;
            }
        }

        public static void Label(Vector2 position, string text, Color color)
        {
            Graphics2D.AddText(position, font, text, fontSize, color, false);
        }

        public static bool Button(int id, Vector2 position, Vector2 size, string text, Color color)
        {
            bool clicked = false;
            Vector2 mousePos = Input.GetMousePosition();
            bool isInside = IsInside(mousePos, position, size);

            // Only allow this button to become hot if nothing else is active
            if (isInside && (activeId == 0 || activeId == id))
            {
                hotId = id;
            }

            if (activeId == id)
            {
                if (Input.GetButtonUp(ButtonCode.Left))
                {
                    if (hotId == id)
                    {
                        clicked = true;
                    }

                    activeId = 0;
                }
            }
            else if (hotId == id)
            {
                if (Input.GetButtonDown(ButtonCode.Left))
                {
                    activeId = id;
                }
            }

            Color renderColor = color;
            if (activeId == id)
            {
                renderColor = MultiplyColor(color, 0.8f);
            }
            else if (hotId == id)
            {
                renderColor = MultiplyColor(color, 1.2f);
            }

            Graphics2D.AddRectangleRounded(position, size, 0.0f, 5.0f, renderColor);

            float textWidth;
            float textHeight;
            font.CalculateBounds(text, text.Length, fontSize, out textWidth, out textHeight);

            float centerX = position.X + (size.X - textWidth) * 0.5f;
            float centerY = position.Y + (size.Y - textHeight) * 0.5f;

            Vector2 textPos = new Vector2(centerX, centerY);

            Graphics2D.AddText(textPos, font, text, fontSize, Color.White, false);

            return clicked;
        }

        public static bool Slider(int id, Vector2 position, Vector2 size, ref float value, float min, float max, Color color)
        {
            bool changed = false;
            Vector2 mousePos = Input.GetMousePosition();
            bool isInside = IsInside(mousePos, position, size);

            if (isInside && (activeId == 0 || activeId == id))
            {
                hotId = id;
            }

            if (activeId == id)
            {
                float mouseRelativeX = mousePos.X - position.X;
                float percentage = Math.Clamp(mouseRelativeX / size.X, 0.0f, 1.0f);
                value = min + (max - min) * percentage;
                changed = true;

                if (Input.GetButtonUp(ButtonCode.Left))
                {
                    activeId = 0;
                }
            }
            else if (hotId == id && Input.GetButtonDown(ButtonCode.Left))
            {
                activeId = id;
            }

            Graphics2D.AddRectangleRounded(position, size, 0.0f, 5.0f, new Color(0.2f, 0.2f, 0.2f, 1.0f));

            float handleWidth = 12.0f;
            float normalizedValue = (value - min) / (max - min);
            float scrollPos = normalizedValue * (size.X - handleWidth);
            
            Vector2 handlePos = new Vector2(position.X + scrollPos, position.Y);
            Vector2 handleSize = new Vector2(handleWidth, size.Y);
            
            Graphics2D.AddRectangle(handlePos, handleSize, 0.0f, color);

            return changed;
        }

        private static bool IsInside(Vector2 point, Vector2 pos, Vector2 size)
        {
            if (point.X < pos.X)
            {
                return false;
            }
            
            if (point.X > pos.X + size.X)
            {
                return false;
            }
            
            if (point.Y < pos.Y)
            {
                return false;
            }
            
            if (point.Y > pos.Y + size.Y)
            {
                return false;
            }
            
            return true;
        }

        private static Color MultiplyColor(Color color, float factor)
        {
            return new Color(
                Math.Clamp(color.r * factor, 0.0f, 1.0f),
                Math.Clamp(color.g * factor, 0.0f, 1.0f),
                Math.Clamp(color.b * factor, 0.0f, 1.0f),
                color.a
            );
        }

        private static void DrawCenteredText(Vector2 position, Vector2 size, string text, Color color)
        {
            if (font == null)
            {
                return;
            }

            float textWidth;
            float textHeight;
            font.CalculateBounds(text, text.Length, fontSize, out textWidth, out textHeight);

            float centerX = position.X + (size.X - textWidth) * 0.5f;
            float centerY = position.Y + (size.Y - textHeight) * 0.5f;

            Graphics2D.AddText(new Vector2(centerX, centerY), font, text, fontSize, color, false);
        }
    }
}