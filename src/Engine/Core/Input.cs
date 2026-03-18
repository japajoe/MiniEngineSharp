using System.Collections.Generic;
using GLFWNet;
using OpenTK.Mathematics;

namespace MiniEngine.Core
{
    public static class Input
    {
        private static Keyboard keyboard = new Keyboard();
        private static Mouse mouse = new Mouse();
        private static Dictionary<string, AxisInfo> keyToAxisDictionary = new Dictionary<string, AxisInfo>();

        public static Keyboard Keyboard
        {
            get
            {
                return keyboard;
            }
        }

        public static Mouse Mouse
        {
            get
            {
                return mouse;
            }
        }

        internal static void Initialize()
        {
            AxisInfo axisHorizontal = new AxisInfo("Horizontal");
            AxisInfo axisVertical = new AxisInfo("Vertical");
            AxisInfo axisPanning = new AxisInfo("Panning");

            axisHorizontal.AddKeys(KeyCode.D, KeyCode.A);
            axisVertical.AddKeys(KeyCode.W, KeyCode.S);
            axisPanning.AddKeys(KeyCode.R, KeyCode.F);
            
            RegisterAxis(axisHorizontal);
            RegisterAxis(axisVertical);
            RegisterAxis(axisPanning);
        }

        internal static void NewFrame()
        {
            //Cursorpos callback is not called when mouse is outside the bounds of the window
            GLFW.GetCursorPos(Application.NativeWindow, out double cursorPosX, out double cursorPosY);
            SetMousePosition(cursorPosX, cursorPosY);

            keyboard.NewFrame();
            mouse.NewFrame();
        }

        internal static void EndFrame()
        {
            mouse.EndFrame();
        }

        internal static void SetMousePosition(double x, double y)
        {
            mouse.SetPosition(x, y);
        }

        internal static void SetWindowPosition(double x, double y)
        {
            mouse.SetWindowPosition(x, y);
        }

        internal static void SetKeyState(KeyCode keycode, int state)
        {
            keyboard.SetState(keycode, state);
        }

        internal static void AddInputCharacter(uint codepoint)
        {
            keyboard.AddInputCharacter(codepoint);
        }

        internal static void SetButtonState(ButtonCode buttoncode, int state)
        {
            mouse.SetState(buttoncode, state);
        }

        internal static void SetScrollDirection(double x, double y)
        {
            mouse.SetScrollDirection(x, y);
        }

        public static void RegisterAxis(AxisInfo axisInfo)
        {
            if(keyToAxisDictionary.ContainsKey(axisInfo.name))
                return;
            keyToAxisDictionary.Add(axisInfo.name, axisInfo);
        }

        public static float GetAxis(string axis)
        {
            if (keyToAxisDictionary.ContainsKey(axis))
            {
                for (int i = 0; i < keyToAxisDictionary[axis].keys.Count; i++)
                {
                    if (GetKey(keyToAxisDictionary[axis].keys[i].positive))
                        return 1.0f;
                    else if (GetKey(keyToAxisDictionary[axis].keys[i].negative))
                        return -1.0f;
                }
            }
            else
            {
                System.Console.WriteLine("The Input Axis '" + axis + "' has not been set up");
            }

            return 0.0f;
        }

        public static bool GetKey(KeyCode keycode)
        {
            return keyboard.GetKey(keycode);
        }

        public static bool GetKeyDown(KeyCode keycode)
        {
            return keyboard.GetKeyDown(keycode);
        }

        public static bool GetKeyUp(KeyCode keycode)
        {
            return keyboard.GetKeyUp(keycode);
        }

        public static bool GetAnyKeyDown(out KeyCode keycode)
        {
            return keyboard.GetAnyKeyDown(out keycode);
        }

        public static bool GetButton(ButtonCode buttoncode)
        {
            return mouse.GetButton(buttoncode);
        }

        public static bool GetButtonDown(ButtonCode buttoncode)
        {
            return mouse.GetButtonDown(buttoncode);
        }

        public static bool GetButtonUp(ButtonCode buttoncode)
        {
            return mouse.GetButtonUp(buttoncode);
        }

        public static bool GetAnyButtonDown(out ButtonCode buttoncode)
        {
            return mouse.GetAnyButtonDown(out buttoncode);
        }

        public static Vector2 GetScrollDirection()
        {
            return mouse.GetScroll();
        }

        public static Vector2 GetMousePosition()
        {
            return mouse.GetPosition();
        }

        public static Vector2 GetMouseDelta()
        {
            return mouse.GetDelta();
        }

        public static void SetMouseCursor(bool visible)
        {
            mouse.SetCursor(visible);
        }

        public static bool IsCursorVisible()
        {
            return mouse.IsCursorVisible();
        }
    }

    [System.Serializable]
    public sealed class AxisInfo
    {
        public string name;
        public List<AxisKeys> keys;
        
        public AxisInfo(string name)
        {
            this.name = name;
            keys = new List<AxisKeys>();
        }

        public void AddKeys(KeyCode positive, KeyCode negative)
        {
            keys.Add(new AxisKeys(positive, negative));
        }
    }

    [System.Serializable]
    public sealed class AxisKeys
    {
        public KeyCode positive;
        public KeyCode negative;

        public AxisKeys(KeyCode positive, KeyCode negative)
        {
            this.positive = positive;
            this.negative = negative;
        }
    }

    public enum AxisDirection
    {
        Positive,
        Negative
    }
}