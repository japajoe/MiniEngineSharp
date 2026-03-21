using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using MiniEngine.Core;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace MiniEngine.GraphicsManagement.Renderers
{
    public sealed class LineRenderer : Renderer
    {
		private LineVertex[] lines;
		private int numLines;
		private int pointIndex;
		private int maxLines;
		private int VAO;
		private int VBO;
        private static Shader shader;

        public LineRenderer() : base()
        {
            numLines = 0;
            pointIndex = 0;
            maxLines = 128;
            VAO = 0;
            VBO = 0;
            int maxVertices = maxLines * 2;
            lines = new LineVertex[maxVertices];
        }

        private void Initialize()
        {
            if(shader == null)
                shader = Graphics.GetShader(ShaderName.Line);

            GL.GenVertexArrays(1, ref VAO);
            GL.BindVertexArray(VAO);

            GL.GenBuffers(1, ref VBO);
            GL.BindBuffer(BufferTargetARB.ArrayBuffer, VBO);

            GL.BufferData(BufferTargetARB.ArrayBuffer, lines.Length * Marshal.SizeOf<LineVertex>(), IntPtr.Zero, BufferUsageARB.DynamicDraw);

            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, Marshal.SizeOf(typeof(LineVertex)), Marshal.OffsetOf(typeof(LineVertex), "position"));
            GL.EnableVertexAttribArray(0);

            GL.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, Marshal.SizeOf(typeof(LineVertex)), Marshal.OffsetOf(typeof(LineVertex), "color"));
            GL.EnableVertexAttribArray(1);

            GL.BindVertexArray(0);
            GL.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
        }

        public override void OnRender(Matrix4 projection, Matrix4 view, Frustum frustum)
        {
            if(VAO == 0)
            {
                Initialize();
            }

            if(numLines == 0)
                return;

            shader.Use();

            Matrix4 model = Matrix4.Identity;
            Matrix4 mvp = model * view * projection;

            shader.SetMat4(UniformName.MVP, mvp);

            int numVertices = numLines * 2;

            GL.BindBuffer(BufferTargetARB.ArrayBuffer, VBO);
            GL.BufferSubData(BufferTargetARB.ArrayBuffer, 0, lines);
            GL.BindBuffer(BufferTargetARB.ArrayBuffer, 0);

            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.Blend);

            GL.BindVertexArray(VAO);
            GL.DrawArrays(PrimitiveType.Lines, 0, numVertices);
            
            GL.Disable(EnableCap.DepthTest);
            GL.Disable(EnableCap.Blend);

            //Clear
            pointIndex = 0;
            numLines = 0;
        }

        public void DrawLine(Vector3 from, Vector3 to, Color color)
        {
            AddToDrawList(from, to, color);
        }

        private void AddToDrawList(Vector3 p1, Vector3 p2, Color color)
        {
            if(Graphics.BypassColorPass)
                return;
                
            if(numLines >= maxLines)
            {
                //Double the capacity if we run out of space
                maxLines = maxLines * 2;
                int maxVertices = maxLines * 2;
                Array.Resize(ref lines, maxVertices);
                GL.BindBuffer(BufferTargetARB.ArrayBuffer, VBO);
                GL.BufferData(BufferTargetARB.ArrayBuffer, lines, BufferUsageARB.DynamicCopy);
                GL.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
            }

            lines[pointIndex+0] = new LineVertex(p1, color);
            lines[pointIndex+1] = new LineVertex(p2, color);
            
            pointIndex += 2;
            numLines++;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct LineVertex
    {
        public Vector3 position;
        public Color color;
        
		public LineVertex()
		{
			this.position = new Vector3(0.0f, 0.0f, 0.0f);
			this.color = new Color(255, 255, 255, 255);
		}
        
		public LineVertex(Vector3 position, Color color)
        {
            this.position = position;
			this.color = color;
		}
    }

}