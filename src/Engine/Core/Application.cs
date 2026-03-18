// MIT License

// Copyright (c) 2025 W.M.R Jap-A-Joe

// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.Runtime.InteropServices;
using GLFWNet;
using MiniEngine.AudioManagement;
using MiniEngine.GraphicsManagement;
using MiniEngine.Utilities;

namespace MiniEngine.Core
{
    [Flags]
    public enum WindowFlags
    {
        None = 1 << 0,
        VSync = 1 << 1,
        FullScreen = 1 << 2,
        Maximize = 1 << 3
    }

    public struct Configuration
    {
        public string title;
        public int width;
        public int height;
        public WindowFlags flags;
        public byte[] iconData;
    }

    public class Application
    {
        private Configuration config;
        private IntPtr window;
        private static IntPtr nativeWindow;

        public static IntPtr NativeWindow
        {
            get
            {
                return nativeWindow;
            }
        }

        public Application(int width, int height, string title, WindowFlags flags = WindowFlags.VSync)
        {
            config.width = width;
            config.height = height;
            config.title = title;
            config.flags = flags;
            config.iconData = null;
            window = IntPtr.Zero;
            nativeWindow = IntPtr.Zero;
        }

        public Application(Configuration config)
        {
            this.config = config;
            window = IntPtr.Zero;
            nativeWindow = IntPtr.Zero;
        }

        public void Run()
        {
            if(window != IntPtr.Zero)
            {
                Console.WriteLine("Window is already initialized");
                return;
            }

            if(GLFW.Init() == 0)
            {
                Console.WriteLine("Failed to initialize GLFW");
                return;
            }

            GLFW.WindowHint(GLFW.CONTEXT_VERSION_MAJOR, 3);
            GLFW.WindowHint(GLFW.CONTEXT_VERSION_MINOR, 3);
            GLFW.WindowHint(GLFW.OPENGL_PROFILE, GLFW.OPENGL_CORE_PROFILE);
            GLFW.WindowHint(GLFW.VISIBLE, GLFW.FALSE);

            if((config.flags & WindowFlags.Maximize) != 0)
                GLFW.WindowHint(GLFW.MAXIMIZED, GLFW.TRUE);

            if((config.flags & WindowFlags.FullScreen) != 0)
            {
                IntPtr monitor =  GLFW.GetPrimaryMonitor();

                if(GLFW.GetVideoMode(monitor, out GLFWvidmode mode))
                    window = GLFW.CreateWindow(mode.width, mode.height, config.title, monitor, IntPtr.Zero);
                else
                    window = GLFW.CreateWindow(config.width, config.height, config.title, IntPtr.Zero, IntPtr.Zero);
            }
            else
            {
                window = GLFW.CreateWindow(config.width, config.height, config.title, IntPtr.Zero, IntPtr.Zero);
            }

            if(window == IntPtr.Zero)
            {
                GLFW.Terminate();
                Console.WriteLine("Failed to create window");
                return;
            }

            if(config.iconData != null)
            {
                Image image = new Image(config.iconData);

                if(image.IsLoaded)
                {
                    GLFWimage windowIcon = new GLFWimage();
                    windowIcon.width = (int)image.Width;
                    windowIcon.height = (int)image.Height;
                    
                    windowIcon.pixels = Marshal.AllocHGlobal(image.Data.Length);
                    
                    if(windowIcon.pixels != IntPtr.Zero)
                    {
                        Marshal.Copy(image.Data, 0, windowIcon.pixels, image.Data.Length);
                        
                        GLFWimage[] images = new GLFWimage[1]
                        {
                            windowIcon
                        };
                        
                        GLFW.SetWindowIcon(window, images);

                        Marshal.FreeHGlobal(windowIcon.pixels);
                    }
                }
            }

            nativeWindow = window;

            GLFW.MakeContextCurrent(window);

            GLFW.SwapInterval((config.flags & WindowFlags.VSync) != 0 ? 1 : 0);

            GLFW.SetFramebufferSizeCallback(window, OnWindowResize);
            GLFW.SetWindowPosCallback(window, OnWindowMove);
            GLFW.SetKeyCallback(window, OnKeyPress);
            GLFW.SetCharCallback(window, OnCharPress);
            GLFW.SetMouseButtonCallback(window, OnMouseButtonPress);
            GLFW.SetScrollCallback(window, OnMouseScroll);

            OnInitialize();

            GLFW.ShowWindow(window);

            while(GLFW.WindowShouldClose(window) == 0)
            {
                //MemoryProfiler.Begin();

                OnNewFrame();
                OnEndFrame();
                GLFW.PollEvents();
                GLFW.SwapBuffers(window);

                //MemoryProfiler.End();

                // long allocatedThisFrame = MemoryProfiler.GetAllocatedBytes();
                // if (allocatedThisFrame > 0)
                //     Console.WriteLine($"Allocated {allocatedThisFrame} bytes this frame");
            }

            OnClosing();

            GLFW.DestroyWindow(window);

            window = IntPtr.Zero;
            nativeWindow = IntPtr.Zero;

            GLFW.Terminate();
        }

        private void OnInitialize()
        {
            Audio.Initialize();
            Graphics.Initialize(config.width, config.height);
            Input.Initialize();
            OnLoad();
        }        

        private void OnNewFrame()
        {
            Time.NewFrame();
            Input.NewFrame();

            OnUpdate();
            OnLateUpdate();
            
            Audio.NewFrame();

            Graphics.NewFrame();
            Graphics.BeginGUI();
            OnGUI();
            Graphics.EndGUI();
        }

        private void OnEndFrame()
        {
            Input.EndFrame();
        }

        private void OnClosing()
        {
            OnClose();
            Audio.Destroy();
            Graphics.Destroy();
        }

        public static void Quit()
        {
            GLFW.SetWindowShouldClose(NativeWindow, GLFW.TRUE);
        }

        private void OnWindowResize(IntPtr window, int width, int height)
        {
            if(width < 10 || height < 10)
                return;
            Graphics.SetViewport(0, 0, width, height);
        }

        private void OnWindowMove(IntPtr window, int x, int y)
        {
            Input.SetWindowPosition(x, y);
        }

        private void OnKeyPress(IntPtr window, int key, int scancode, int action, int mods)
        {
            Input.SetKeyState((KeyCode)key, action > 0 ? 1 : 0);
        }

        private void OnCharPress(IntPtr window, uint codepoint)
        {
            Input.AddInputCharacter(codepoint);
        }

        private void OnMouseButtonPress(IntPtr window, int button, int action, int mods)
        {
            Input.SetButtonState((ButtonCode)button, action > 0 ? 1 : 0);
        }

        private void OnMouseScroll(IntPtr window, double xoffset, double yoffset)
        {
            Input.SetScrollDirection(xoffset, yoffset);
        }

        protected virtual void OnLoad(){}
        protected virtual void OnClose(){}
        protected virtual void OnUpdate(){}
        protected virtual void OnLateUpdate(){}
        protected virtual void OnGUI(){}
    }
}