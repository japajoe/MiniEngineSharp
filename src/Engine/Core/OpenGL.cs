using System;
using System.Runtime.InteropServices;
using GLFWNet;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace MiniEngine.GraphicsManagement
{
    public static class OpenGL
    {
        public static readonly int Major;
        public static readonly int Minor;
        public static readonly float MaxAnisotropy;

        public static readonly int GL_TEXTURE_MAX_ANISOTROPY_EXT = 0x84FE;
        public static readonly int GL_MAX_TEXTURE_MAX_ANISOTROPY_EXT = 0x84FF;
    
        public static unsafe void Initialize()
        {
            GLLoader.LoadBindings(new GLFWBindingsContext());

            string version = GL.GetString(StringName.Version);

            if(!string.IsNullOrEmpty(version))
                Console.WriteLine("OpenGL Version: " + version);

            fixed(int *pMajor = &Major)
            {
                GL.GetIntegerv(GetPName.MajorVersion, pMajor);
            }
            fixed(int *pMinor = &Minor)
            {
                GL.GetIntegerv(GetPName.MinorVersion, pMinor);
            }

            fixed(float *pMaxAnisotropy = &MaxAnisotropy)
            {
                GL.GetFloatv((GetPName)GL_MAX_TEXTURE_MAX_ANISOTROPY_EXT, pMaxAnisotropy);
            }

            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.Multisample);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        }

    }

    internal sealed class GLFWBindingsContext : IBindingsContext
    {
        public IntPtr GetProcAddress(string procName)
        {
            return Marshal.GetFunctionPointerForDelegate(GLFW.GetProcAddress(procName));
        }
    }
}