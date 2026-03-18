namespace MiniEngine.GraphicsManagement.Shaders.PostProcessing
{
    public static class PostProcessingShader
    {
        public static string vertexSource = @"out vec2 TexCoords;

void main() {
    // Generates a triangle that covers the [-1, 1] range
    // Vertex 0: (-1, -1), UV (0, 0)
    // Vertex 1: ( 3, -1), UV (2, 0)
    // Vertex 2: (-1,  3), UV (0, 2)
    
    float x = -1.0 + float((gl_VertexID & 1) << 2);
    float y = -1.0 + float((gl_VertexID & 2) << 1);
    
    TexCoords.x = (x + 1.0) * 0.5;
    TexCoords.y = (y + 1.0) * 0.5;
    
    gl_Position = vec4(x, y, 0.0, 1.0);
}";
    }
}