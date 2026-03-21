namespace MiniEngine.GraphicsManagement.Shaders
{
    public static class ScreenShader
    {
        public static readonly string vertexSource = @"out vec2 TexCoords;

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

        public static readonly string fragmentSource = @"uniform sampler2D uTexture;

in vec2 TexCoords;
out vec4 FragColor;

vec3 aces_tonemapping(vec3 color) {
    float a = 2.51;
    float b = 0.03;
    float c = 2.43;
    float d = 0.59;
    float e = 0.14;
    return clamp((color * (a * color + b)) / (color * (c * color + d) + e), 0.0, 1.0);
}

float random(vec2 uv) {
    return fract(sin(dot(uv.xy, vec2(12.9898, 78.233))) * 43758.5453);
}

void main() {
    vec3 hdrColor = texture(uTexture, TexCoords).rgb;

    vec3 mapped = aces_tonemapping(hdrColor);

    mapped.rgb = pow(mapped.rgb, vec3(0.454545455));

    // Dithering: Apply a tiny bit of noise to break up banding
    // (1.0 / 255.0) represents the size of one 8-bit color step
    float dither = (random(TexCoords) - 0.5) * (1.0 / 255.0);
    mapped.rgb += dither;

    FragColor = vec4(mapped, 1.0);
}";
    }
}