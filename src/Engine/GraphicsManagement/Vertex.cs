using System.Runtime.InteropServices;
using OpenTK.Mathematics;

namespace MiniEngine.GraphicsManagement
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Vertex
    {
        public Vector3 position;
        public Vector3 normal;
        public Vector2 uv;
    }
}