using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL;

namespace MiniEngine.GraphicsManagement
{
    public sealed class UniformBuffer
    {
        private int id;

        public int Id => id;

        public UniformBuffer()
        {
            
        }

        public void Generate()
        {
            GL.GenBuffers(1, ref id);
        }

        public void Delete()
        {
            if(id > 0)
            {
                GL.DeleteBuffers(1, id);
                id = 0;
            }
        }

        public void Bind()
        {
            GL.BindBuffer(BufferTargetARB.UniformBuffer, id);
        }

        public void Unbind()
        {
            GL.BindBuffer(BufferTargetARB.UniformBuffer, 0);
        }

        public void BufferData<T>(List<T> data, BufferUsageARB usage) where T : unmanaged
        {
            ReadOnlySpan<T> span = CollectionsMarshal.AsSpan(data);
            BufferData<T>(span, usage);
        }

        public void BufferData<T>(ReadOnlySpan<T> data, BufferUsageARB usage) where T : unmanaged
        {            
            GL.BufferData(BufferTargetARB.UniformBuffer, data, usage);
        }

        public void BufferSubData<T>(List<T> data, int offset) where T : unmanaged
        {
            ReadOnlySpan<T> span = CollectionsMarshal.AsSpan(data);
            BufferSubData(span, offset);
        }

        public void BufferSubData<T>(ReadOnlySpan<T> data, int offset) where T : unmanaged
        {
            GL.BufferSubData(BufferTargetARB.UniformBuffer, new IntPtr(offset), data);
        }

        public void BindBufferBase(uint index)
        {
            GL.BindBufferBase(BufferTargetARB.UniformBuffer, index, id);
        }

        public uint GetUniformBlockIndex(int shaderProgram, string uniformBlockName)
        {
            return GL.GetUniformBlockIndex(shaderProgram, uniformBlockName);
        }

        public void UniformBlockBinding(int shaderProgram, uint uniformBlockIndex, uint uniformBlockBinding)
        {
            GL.UniformBlockBinding(shaderProgram, uniformBlockIndex, uniformBlockBinding);
        }

        public void BindBlockToShader(Shader shader, uint bindingIndex, string blockName)
        {
            uint blockIndex = GL.GetUniformBlockIndex(shader.Id, blockName);
            GL.UniformBlockBinding(shader.Id, blockIndex, bindingIndex);
        }

        public static UniformBuffer Create<T>(uint bindingIndex, uint numItems, string name) where T : unmanaged
        {
            UniformBuffer ubo = new UniformBuffer();
            ubo.Generate();
            ubo.Bind();

            T[] data = new T[numItems];
            
            ubo.BufferData<T>(data, BufferUsageARB.DynamicDraw);
            ubo.BindBufferBase(bindingIndex);
            ubo.Unbind();

            return ubo;
        }

        public static UniformBuffer Create(uint bindingIndex, uint size)
        {
            UniformBuffer ubo = new UniformBuffer();
            ubo.Generate();
            ubo.Bind();

            byte[] data = new byte[(int)size];
            
            ubo.BufferData(data, BufferUsageARB.DynamicCopy);
            ubo.BindBufferBase(bindingIndex);
            ubo.Unbind();

            return ubo;
        }

        public unsafe void ObjectLabel(string label)
        {
            int major = 0;
            int minor = 0;
            
            GL.GetIntegerv(GetPName.MajorVersion, &major);
            GL.GetIntegerv(GetPName.MinorVersion, &minor);

            bool KHRDebugAvailable = (major == 4 && minor >= 3) || IsExtensionSupported("KHR_debug");
            if(KHRDebugAvailable)
                GL.ObjectLabel(ObjectIdentifier.Buffer, (uint)id, label.Length, label);
        }

        private bool IsExtensionSupported(string name)
        {
            int n = GL.GetInteger(GetPName.NumExtensions);
            for (uint i = 0; i < n; i++)
            {
                string extension = GL.GetStringi(StringName.Extensions, i);
                if (extension == name) 
                    return true;
            }

            return false;
        }

    }
}