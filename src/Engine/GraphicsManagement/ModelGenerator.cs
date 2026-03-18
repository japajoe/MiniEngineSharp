using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using MiniEngine.GraphicsManagement.Renderers;
using MiniEngine.Utilities;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace MiniEngine.GraphicsManagement
{
    public enum ModelName : int
    {
        Capsule,
        Cube,
        Cylinder,
        Disk,
        Plane,
        Ring,
        Sphere,
        COUNT
    }

    public static class ModelGenerator
    {
        private static List<ModelProtoType> models = new List<ModelProtoType>();

        private static void Initialize()
        {
            for(int i = 0; i < (int)ModelName.COUNT; i++)
            {
                models.Add(null);
            }
            
            models[(int)ModelName.Cube] = CreateCube(new Vector3(1, 1, 1));
            models[(int)ModelName.Plane] = CreatePlane(new Vector3(1, 1, 1));
            models[(int)ModelName.Sphere] = CreateSphere(new Vector3(1, 1, 1));
        }

        public static Model Get(ModelName name)
        {
            if(models.Count == 0)
            {
                Initialize();
            }

            if(name == ModelName.COUNT)
                return null;
            Model model = new Model();
            model.Set(models[(int)name]);
            model.GetChild(0).transform.SetParent(model.transform);
            return model;            
        }

        private static void PrepareModelPrototype(ModelProtoType model, Vector3 scale, string name, bool generateNormals)
        {
            model.textureNames.Add("Diffuse");
            model.textures.Add(Texture2D.GetDiffuseTexture());

            model.meshes.Resize(1, new ModelMeshInfo());
            model.meshes[0].baseIndex = 0;
            model.meshes[0].baseVertex = 0;
            model.meshes[0].materialIndex = 0;
            model.meshes[0].name = name;
            model.meshes[0].numIndices = model.indices.Count;
            model.meshes[0].transformation = Matrix4.Identity;

            SetScale(model, scale);
            GenerateBounds(model);
            if(generateNormals)
                GenerateNormals(model);
            GenerateBuffers(model);
        }

        private static void SetScale(ModelProtoType model, Vector3 scale)
        {
            for(int i = 0; i < model.vertices.Count; i++)
                model.vertices[i] *= scale;
        }

        private static void GenerateBounds(ModelProtoType model)
        {
            model.meshes[0].bounds.Clear();

            for(int i = 0; i < model.vertices.Count; i++)
            {
                model.meshes[0].bounds.Grow(model.vertices[i]);
            }
        }

        private static void GenerateNormals(ModelProtoType model)
        {
            for (int i = 0; i < model.normals.Count; i++)
            {
                model.normals[i] = new Vector3(0.0f, 0.0f, 0.0f);
            }

            Func<int, int, int, Vector3> surfaceNormalsFromIndices = (indexA, indexB, indexC) => {
                Vector3 pA = model.vertices[indexA];
                Vector3 pB = model.vertices[indexB];
                Vector3 pC = model.vertices[indexC];

                Vector3 sideAB = pB - pA;
                Vector3 sideAC = pC - pA;

                return Vector3.Cross(sideAB, sideAC);
            };

            int triangleCount = model.indices.Count / 3;

            for (int i = 0; i < triangleCount; i++)
            {
                int normalTriangleIndex = i * 3;

                int vertexIndexA = model.indices[normalTriangleIndex];
                int vertexIndexB = model.indices[normalTriangleIndex + 1];
                int vertexIndexC = model.indices[normalTriangleIndex + 2];
                
                Vector3 triangleNormal = surfaceNormalsFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);

                model.normals[vertexIndexA] += triangleNormal;
                model.normals[vertexIndexB] += triangleNormal;
                model.normals[vertexIndexC] += triangleNormal;
            }

            for(int i = 0; i < model.normals.Count; i++)
            {
                if(model.normals[i].Length > 0)
                    model.normals[i].Normalize();
            }
        }

        private static void GenerateBuffers(ModelProtoType model)
        {
            var pVertices = CollectionsMarshal.AsSpan(model.vertices);
            var pNormals = CollectionsMarshal.AsSpan(model.normals);
            var pUVs = CollectionsMarshal.AsSpan(model.uvs);
            var pIndices = CollectionsMarshal.AsSpan(model.indices);

            GL.GenVertexArrays(1, ref model.vao);
            GL.BindVertexArray(model.vao);
            GL.GenBuffers(model.buffers);

            GL.BindBuffer(BufferTargetARB.ArrayBuffer, model.buffers[(int)ModelBufferType.Vertices]);
            GL.BufferData(BufferTargetARB.ArrayBuffer, pVertices, BufferUsageARB.StaticDraw);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 0, 0);

            GL.BindBuffer(BufferTargetARB.ArrayBuffer, model.buffers[(int)ModelBufferType.Normals]);
            GL.BufferData(BufferTargetARB.ArrayBuffer, pNormals, BufferUsageARB.StaticDraw);
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 0, 0);

            GL.BindBuffer(BufferTargetARB.ArrayBuffer, model.buffers[(int)ModelBufferType.UVs]);
            GL.BufferData(BufferTargetARB.ArrayBuffer, pUVs, BufferUsageARB.StaticDraw);
            GL.EnableVertexAttribArray(2);
            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, 0, 0);

            GL.BindBuffer(BufferTargetARB.ElementArrayBuffer, model.buffers[(int)ModelBufferType.Indices]);
            GL.BufferData(BufferTargetARB.ElementArrayBuffer, pIndices, BufferUsageARB.StaticDraw);

            GL.BindVertexArray(0);
        }

        private static ModelProtoType CreateCube(Vector3 scale)
        {
            ModelProtoType model = new ModelProtoType();

            model.vertices = new List<Vector3>();
            model.uvs = new List<Vector2>();
            model.normals = new List<Vector3>();

            model.vertices.Resize(24);
            model.uvs.Resize(24);
            model.normals.Resize(24);

            model.vertices[0] = new Vector3(0.5f, -0.5f, 0.5f);
            model.vertices[1] = new Vector3(-0.5f, -0.5f, 0.5f);
            model.vertices[2] = new Vector3(0.5f, 0.5f, 0.5f);
            model.vertices[3] = new Vector3(-0.5f, 0.5f, 0.5f);

            model.vertices[4] = new Vector3(0.5f, 0.5f, -0.5f);
            model.vertices[5] = new Vector3(-0.5f, 0.5f, -0.5f);
            model.vertices[6] = new Vector3(0.5f, -0.5f, -0.5f);
            model.vertices[7] = new Vector3(-0.5f, -0.5f, -0.5f);

            model.vertices[8] = new Vector3(0.5f, 0.5f, 0.5f);
            model.vertices[9] = new Vector3(-0.5f, 0.5f, 0.5f);
            model.vertices[10] = new Vector3(0.5f, 0.5f, -0.5f);
            model.vertices[11] = new Vector3(-0.5f, 0.5f, -0.5f);

            model.vertices[12] = new Vector3(0.5f, -0.5f, -0.5f);
            model.vertices[13] = new Vector3(0.5f, -0.5f, 0.5f);
            model.vertices[14] = new Vector3(-0.5f, -0.5f, 0.5f);
            model.vertices[15] = new Vector3(-0.5f, -0.5f, -0.5f);

            model.vertices[16] = new Vector3(-0.5f, -0.5f, 0.5f);
            model.vertices[17] = new Vector3(-0.5f, 0.5f, 0.5f);
            model.vertices[18] = new Vector3(-0.5f, 0.5f, -0.5f);
            model.vertices[19] = new Vector3(-0.5f, -0.5f, -0.5f);

            model.vertices[20] = new Vector3(0.5f, -0.5f, -0.5f);
            model.vertices[21] = new Vector3(0.5f, 0.5f, -0.5f);
            model.vertices[22] = new Vector3(0.5f, 0.5f, 0.5f);
            model.vertices[23] = new Vector3(0.5f, -0.5f, 0.5f);

            model.uvs[0] = new Vector2(0.0f, 0.0f);
            model.uvs[1] = new Vector2(1.0f, 0.0f);
            model.uvs[2] = new Vector2(0.0f, 1.0f);
            model.uvs[3] = new Vector2(1.0f, 1.0f);

            model.uvs[4] = new Vector2(0.0f, 1.0f);
            model.uvs[5] = new Vector2(1.0f, 1.0f);
            model.uvs[6] = new Vector2(0.0f, 1.0f);
            model.uvs[7] = new Vector2(1.0f, 1.0f);
            
            model.uvs[8] = new Vector2(0.0f, 0.0f);
            model.uvs[9] = new Vector2(1.0f, 0.0f);
            model.uvs[10] = new Vector2(0.0f, 0.0f);
            model.uvs[11] = new Vector2(1.0f, 0.0f);
            
            model.uvs[12] = new Vector2(0.0f, 0.0f);
            model.uvs[13] = new Vector2(0.0f, 1.0f);
            model.uvs[14] = new Vector2(1.0f, 1.0f);
            model.uvs[15] = new Vector2(1.0f, 0.0f);
            
            model.uvs[16] = new Vector2(0.0f, 0.0f);
            model.uvs[17] = new Vector2(0.0f, 1.0f);
            model.uvs[18] = new Vector2(1.0f, 1.0f);
            model.uvs[19] = new Vector2(1.0f, 0.0f);
            
            model.uvs[20] = new Vector2(0.0f, 0.0f);
            model.uvs[21] = new Vector2(0.0f, 1.0f);
            model.uvs[22] = new Vector2(1.0f, 1.0f);
            model.uvs[23] = new Vector2(1.0f, 0.0f);

            model.indices = new List<int>() {
                0, 2, 3,
                0, 3, 1,

                8, 4, 5,
                8, 5, 9,

                10, 6, 7,
                10, 7, 11,

                12, 13, 14,
                12, 14, 15,

                16, 17, 18,
                16, 18, 19,

                20, 21, 22,
                20, 22, 23
            };

            PrepareModelPrototype(model, scale, "Cube", true);
            
            return model;
        }

        private static ModelProtoType CreatePlane(Vector3 scale)
        {
            ModelProtoType model = new ModelProtoType();

            model.vertices = new List<Vector3>();
            model.uvs = new List<Vector2>();
            model.normals = new List<Vector3>();
            model.indices = new List<int>();

            model.vertices.Resize(4);
            model.uvs.Resize(4);
            model.normals.Resize(4);

            model.vertices[0] = new Vector3(-0.5f, 0.0f, -0.5f);
            model.vertices[1] = new Vector3(-0.5f, 0.0f,  0.5f);
            model.vertices[2] = new Vector3( 0.5f, 0.0f, -0.5f);
            model.vertices[3] = new Vector3( 0.5f, 0.0f,  0.5f);

            model.uvs[0] = new Vector2(0.0f, 0.0f);
            model.uvs[1] = new Vector2(0.0f, 1.0f);
            model.uvs[2] = new Vector2(1.0f, 0.0f);
            model.uvs[3] = new Vector2(1.0f, 1.0f);

            model.indices = new List<int>{
                0, 1, 2,
                2, 1, 3,
            };

            PrepareModelPrototype(model, scale, "Plane", true);
            
            return model;
        }

        private static ModelProtoType CreateSphere(Vector3 scale)
        {
            ModelProtoType model = new ModelProtoType();

            const int sectorCount = 72;
            const int stackCount = 24;
            const int vertexCount = (sectorCount + 1) * (stackCount + 1);
            const float radius = 0.5f;
            const float PI = (float)Math.PI;
            float x, y, z, xy;					// vertex position
            const float lengthInv = 1.0f / radius;    // vertex normal
            float s, t;                         // vertex texCoord
            const float sectorStep = 2 * PI / sectorCount;
            const float stackStep = PI / stackCount;
            float sectorAngle, stackAngle;
            int vertexIndex = 0;

            model.vertices = new List<Vector3>();
            model.uvs = new List<Vector2>();
            model.normals = new List<Vector3>();
            model.indices = new List<int>();

            model.vertices.Resize(vertexCount);
            model.uvs.Resize(vertexCount);
            model.normals.Resize(vertexCount);

            for(int i = 0; i <= stackCount; ++i)
            {
                stackAngle = PI / 2 - i * stackStep;        // starting from pi/2 to -pi/2
                xy = radius* (float)Math.Cos(stackAngle);             // r * cos(u)
                z = radius* (float)Math.Sin(stackAngle);              // r * sin(u)

                // add (sectorCount+1) vertices per stack
                // the first and last vertices have same position and normal, but different tex coords
                for(int j = 0; j <= sectorCount; ++j)
                {
                    Vector3 vPosition;
                    Vector3 vNormal;
                    Vector2 vUV;

                    sectorAngle = j * sectorStep;           // starting from 0 to 2pi

                    // vertex position (x, y, z)
                    x = xy * (float)Math.Cos(sectorAngle);             // r * cos(u) * cos(v)
                    y = xy * (float)Math.Sin(sectorAngle);             // r * cos(u) * sin(v)          
                    vPosition = new Vector3(x, y, z);

                    vNormal.X = x * lengthInv;
                    vNormal.Y = y * lengthInv;
                    vNormal.Z = z * lengthInv;

                    // vertex tex coord (s, t) range between [0, 1]
                    s = (float) j / sectorCount;
                    t = (float) i / stackCount;          
                    vUV = new Vector2(s, t);
                    
                    model.vertices[vertexIndex] = vPosition;
                    model.normals[vertexIndex] = vNormal;
                    model.uvs[vertexIndex] = vUV;
                    vertexIndex++;
                }
            }

            int k1, k2;

            for(int i = 0; i < stackCount; ++i)
            {
                k1 = i * (sectorCount + 1);     // beginning of current stack
                k2 = k1 + sectorCount + 1;      // beginning of next stack

                for(int j = 0; j < sectorCount; ++j, ++k1, ++k2)
                {
                    // 2 triangles per sector excluding first and last stacks
                    // k1 => k2 => k1+1
                    if(i != 0)
                    {
                        model.indices.Add(k1);
                        model.indices.Add(k2);
                        model.indices.Add(k1 + 1);
                    }

                    // k1+1 => k2 => k2+1
                    if(i != (stackCount-1))
                    {
                        model.indices.Add(k1 + 1);
                        model.indices.Add(k2);
                        model.indices.Add(k2 + 1);
                    }
                }
            }

            PrepareModelPrototype(model, scale, "Sphere", false);
            
            return model;
        }
    }
}