using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using MiniEngine.GraphicsManagement.Shaders.Includes;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace MiniEngine.GraphicsManagement
{
    public enum UniformName
    {
		AmbientOcclusion,
		BrightnessThreshold,
		Color,
		Emissive,
		EmissionFactor,
        LightSpaceMatrix,
		Metallic,
		Model,
		ModelInverted,
		MVP,
		Roughness,
		Texture,
		TextureOffset,
        TextureShadow,
		TextureTiling,
		COUNT
    }

    public sealed class Shader
    {
        private int id;
        private int[] uniforms;
        private static Dictionary<string, string> includes = new Dictionary<string, string>();
        private static int currentBoundShader = 0;

        public int Id => id;

        public Shader()
        {
            id = 0;
            uniforms = new int[(int)UniformName.COUNT];
            LoadIncludes();
        }

        public Shader(string vertexSource, string fragmentSource)
        {
            id = 0;
            uniforms = new int[(int)UniformName.COUNT];
            LoadIncludes();
            Generate(vertexSource, fragmentSource);
        }

        public void Generate(string vertexSource, string fragmentSource)
        {
            if(!PreProcess(vertexSource, out string vertexCode))
                throw new Exception("Failed to compile vertex shader");
            
            if(!PreProcess(fragmentSource, out string fragmentCode))
                throw new Exception("Failed to compile fragment shader");

            int vertexShader = Compile(vertexCode, ShaderType.VertexShader);
            int fragmentShader = Compile(fragmentCode, ShaderType.FragmentShader);

            int[] shaders = {
                vertexShader,
                fragmentShader
            };

            id = CreateAndLinkProgram(shaders);

            GetUniforms();
        }

        public void Destroy()
        {
            if(id > 0)
                GL.DeleteShader(id);
        }

        public void Use()
        {
            if(id != currentBoundShader)
            {
                currentBoundShader = id;
                GL.UseProgram(id);
            }
        }

        public void SetInt(string name, int value)
        {
            GL.Uniform1i(GL.GetUniformLocation(id, name), value);
        }

        public void SetFloat(string name, float value)
        {
            GL.Uniform1f(GL.GetUniformLocation(id, name), value);
        }

        public unsafe void SetFloat2(string name, Vector2 value)
        {
            float *pValue = (float*)&value;
            GL.Uniform2fv(GL.GetUniformLocation(id, name), 1, pValue);
        }

        public unsafe void SetFloat3(string name, Vector3 value)
        {
            float *pValue = (float*)&value;
            GL.Uniform3fv(GL.GetUniformLocation(id, name), 1, pValue);
        }

        public unsafe void SetFloat4(string name, Vector4 value)
        {
            float *pValue = (float*)&value;
            GL.Uniform4fv(GL.GetUniformLocation(id, name), 1, pValue);
        }

        public unsafe void SetFloat4(string name, Color value)
        {
            float *pValue = (float*)&value;
            GL.Uniform4fv(GL.GetUniformLocation(id, name), 1, pValue);
        }

        public unsafe void SetMat2(string name, Matrix2 value)
        {
            float *pValue = (float*)&value;
            GL.UniformMatrix2fv(GL.GetUniformLocation(id, name), 1, false, pValue);
        }

        public unsafe void SetMat3(string name, Matrix3 value)
        {
            float *pValue = (float*)&value;
            GL.UniformMatrix3fv(GL.GetUniformLocation(id, name), 1, false, pValue);
        }

        public unsafe void SetMat4(string name, Matrix4 value)
        {
            float *pValue = (float*)&value;
            GL.UniformMatrix4fv(GL.GetUniformLocation(id, name), 1, false, pValue);
        }

        public void SetInt(UniformName name, int value)
        {
            GL.Uniform1i(uniforms[(int)name], value);
        }

        public void SetFloat(UniformName name, float value)
        {
            GL.Uniform1f(uniforms[(int)name], value);
        }

        public unsafe void SetFloat2(UniformName name, Vector2 value)
        {
            float *pValue = (float*)&value;
            GL.Uniform2fv(uniforms[(int)name], 1, pValue);
        }

        public unsafe void SetFloat3(UniformName name, Vector3 value)
        {
            float *pValue = (float*)&value;
            GL.Uniform3fv(uniforms[(int)name], 1, pValue);
        }

        public unsafe void SetFloat4(UniformName name, Vector4 value)
        {
            float *pValue = (float*)&value;
            GL.Uniform4fv(uniforms[(int)name], 1, pValue);
        }

        public unsafe void SetFloat4(UniformName name, Color value)
        {
            float *pValue = (float*)&value;
            GL.Uniform4fv(uniforms[(int)name], 1, pValue);
        }

        public unsafe void SetMat2(UniformName name, Matrix2 value)
        {
            float *pValue = (float*)&value;
            GL.UniformMatrix2fv(uniforms[(int)name], 1, false, pValue);
        }

        public unsafe void SetMat3(UniformName name, Matrix3 value)
        {
            float *pValue = (float*)&value;
            GL.UniformMatrix3fv(uniforms[(int)name], 1, false, pValue);
        }

        public unsafe void SetMat4(UniformName name, Matrix4 value)
        {
            float *pValue = (float*)&value;
            GL.UniformMatrix4fv(uniforms[(int)name], 1, false, pValue);
        }

        public void SetIntEx(int location, int value)
        {
            GL.Uniform1i(location, value);
        }

        public void SetUIntEx(int location, uint value)
        {
            GL.Uniform1ui(location, value);
        }

        public void SetFloatEx(int location, float value)
        {
            GL.Uniform1f(location, value);
        }

        public unsafe void SetFloat2Ex(int location, Vector2 value)
        {
            float *pValue = (float*)&value;
            GL.Uniform2fv(location, 1, pValue);
        }

        public unsafe void SetFloat3Ex(int location, Vector3 value)
        {
            float *pValue = (float*)&value;
            GL.Uniform3fv(location, 1, pValue);
        }

        public unsafe void SetFloat4Ex(int location, Vector4 value)
        {
            float *pValue = (float*)&value;
            GL.Uniform4fv(location, 1, pValue);
        }

        public unsafe void SetFloat4Ex(int location, Color value)
        {
            float *pValue = (float*)&value;
            GL.Uniform4fv(location, 1, pValue);
        }

        public unsafe void SetMat2Ex(int location, Matrix2 value)
        {
            float *pValue = (float*)&value;
            GL.UniformMatrix2fv(location, 1, false, pValue);
        }

        public unsafe void SetMat3Ex(int location, Matrix3 value)
        {
            float *pValue = (float*)&value;
            GL.UniformMatrix3fv(location, 1, false, pValue);
        }

        public unsafe void SetMat4Ex(int location, Matrix4 value)
        {
            float *pValue = (float*)&value;
            GL.UniformMatrix4fv(location, 1, false, pValue);
        }

        public bool PreProcess(string source, out string processedCode)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("#version 300 es");
            sb.AppendLine("precision highp float;");
            sb.AppendLine("precision highp sampler2D;");
            sb.AppendLine("precision highp sampler2DArray;");
            sb.AppendLine();
            sb.Append(source);

            string currentCode = sb.ToString();
            
            // Regex pattern for #include <filename>
            string pattern = @"#include\s*<([^>]+)>";
            
            while (true)
            {
                Match match = Regex.Match(currentCode, pattern);
                if (!match.Success) 
                    break;

                string includeFile = match.Groups[1].Value;

                if (includes.TryGetValue(includeFile, out string includeContent))
                {
                    string replacement = includeContent + "\n\n";
                    currentCode = currentCode.Remove(match.Index, match.Length)
                                            .Insert(match.Index, replacement);
                }
                else
                {
                    Console.WriteLine($"Failed to compile shader, include file not found: {includeFile}");
                    processedCode = currentCode;
                    return false;
                }
            }

            processedCode = currentCode;
            return true;
        }

        private static int Compile(string source, ShaderType type)
        {
            int shader = GL.CreateShader(type);
            GL.ShaderSource(shader, source);
            GL.CompileShader(shader);

            int success = 0;

            unsafe
            {
                int *pSucces = &success;
                GL.GetShaderiv(shader, ShaderParameterName.CompileStatus, &success);
            }

            if (success == 0)
            {
                GL.GetShaderInfoLog(shader, out string infoLog);
                switch(type)
                {
                case ShaderType.GeometryShader:
                    throw new Exception("Geometry shader compilation failed: " + infoLog);
                case ShaderType.FragmentShader:
                    throw new Exception("Fragment shader compilation failed: " + infoLog);
                case ShaderType.VertexShader:
                    throw new Exception("Vertex shader compilation failed: " + infoLog);
                default:
                    throw new Exception("Unknown shader compilation failed: " + infoLog);
                }
            }

            return shader;
        }

        private static unsafe int CreateAndLinkProgram(int[] shaders, List<string> varyings = null, TransformFeedbackBufferMode mode = TransformFeedbackBufferMode.InterleavedAttribs)
        {
            if(shaders == null)
                return 0;

            if(shaders.Length == 0)
                return 0;
            
            int id = GL.CreateProgram();

            for(int i = 0; i < shaders.Length; i++)
            {
                GL.AttachShader(id, shaders[i]);
            }

            if(varyings != null)
            {
                byte** pVaryings = (byte**)Marshal.AllocHGlobal(sizeof(byte*) * varyings.Count);

                for (int i = 0; i < varyings.Count; i++)
                {
                    byte[] bytes = Encoding.UTF8.GetBytes(varyings[i] + '\0'); // null-terminated

                    IntPtr strPtr = Marshal.AllocHGlobal(bytes.Length);
                    Marshal.Copy(bytes, 0, strPtr, bytes.Length);

                    pVaryings[i] = (byte*)strPtr;
                }

                GL.TransformFeedbackVaryings(id, varyings.Count, pVaryings, mode);

                for (int i = 0; i < varyings.Count; i++)
                {
                    IntPtr ptr = new IntPtr(pVaryings[i]);
                    Marshal.FreeHGlobal(ptr);
                }
                
                Marshal.FreeHGlobal(new IntPtr(pVaryings));
            }

            GL.LinkProgram(id);

            int success = 0;

            GL.GetProgramiv(id, ProgramPropertyARB.LinkStatus, &success);

            if (success == 0)
            {
                GL.GetProgramInfoLog(id, out string infoLog);
                GL.DeleteProgram(id);
                throw new Exception("Shader program linking failed: " + infoLog);
            }

            for(int i = 0; i < shaders.Length; i++)
            {
                GL.DeleteShader(shaders[i]);
            }

            return id;
        }
        
        private static void LoadIncludes()
        {
            if(includes.Count == 0)
            {
                includes["camera"] = CameraShader.Source;
                includes["light"] = LightShader.Source;
                includes["pbr"] = PBRShader.Source;
                includes["world"] = WorldShader.Source;
                includes["shadow"] = ShadowShader.Source;
            }
        }

        private void GetUniforms()
        {
            var getUniformLocation = (UniformName uName, string name) => {
                uniforms[(int)uName] = GL.GetUniformLocation(id, name);
            };

            getUniformLocation(UniformName.AmbientOcclusion, "uAmbientOcclusion");
            getUniformLocation(UniformName.BrightnessThreshold, "uBrightnessThreshold");
            getUniformLocation(UniformName.Color, "uColor");
            getUniformLocation(UniformName.Emissive, "uEmissive");
            getUniformLocation(UniformName.EmissionFactor, "uEmissionFactor");
            getUniformLocation(UniformName.Metallic, "uMetallic");
            getUniformLocation(UniformName.LightSpaceMatrix, "uLightSpaceMatrix");
            getUniformLocation(UniformName.Model, "uModel");
            getUniformLocation(UniformName.ModelInverted, "uModelInverted");
            getUniformLocation(UniformName.MVP, "uMVP");
            getUniformLocation(UniformName.Roughness, "uRoughness");
            getUniformLocation(UniformName.Texture, "uTexture");
            getUniformLocation(UniformName.TextureOffset, "uTextureOffset");
            getUniformLocation(UniformName.TextureShadow, "uTextureShadow");
            getUniformLocation(UniformName.TextureTiling, "uTextureTiling");
        }
    }
}