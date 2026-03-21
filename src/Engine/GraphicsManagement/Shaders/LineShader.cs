namespace MiniEngine.GraphicsManagement.Shaders
{
    public static class LineShader
    {
        public static readonly string vertexSource = @"layout (location = 0) in vec3 aPos;
    layout (location = 1) in vec4 aColor;

    uniform mat4 uMVP;

    out vec4 Color;

    void main() {
        Color = aColor;
        gl_Position = uMVP * vec4(aPos, 1.0);
    }";

        public static readonly string fragmentSource = @"in vec4 Color;
    out vec4 FragColor;

    void main() {
        FragColor = Color;
    }";
    }
}