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
uniform int uToneMappingStyle;
uniform int uDithering;
const float exposure = 1.0;

in vec2 TexCoords;
out vec4 FragColor;

#define TONEMAPPINGSTYLE_REINHARD 0
#define TONEMAPPINGSTYLE_ACES 1
#define TONEMAPPINGSTYLE_UNCHARTED 2
#define TONEMAPPINGSTYLE_EXPONENTIAL 3
#define TONEMAPPINGSTYLE_REINHARD_LUMA 4
#define TONEMAPPINGSTYLE_FILMIC 5
#define TONEMAPPINGSTYLE_LOTTES 6
#define TONEMAPPINGSTYLE_UCHIMURA 7

vec3 reinhard_tonemapping(vec3 color) {
	return color / (color + vec3(1.0));
}

vec3 aces_tonemapping(vec3 color) {
    float a = 2.51;
    float b = 0.03;
    float c = 2.43;
    float d = 0.59;
    float e = 0.14;
    return clamp((color * (a * color + b)) / (color * (c * color + d) + e), 0.0, 1.0);
}

vec3 uncharted_tonemapping(vec3 color) {
    float A = 0.15;
    float B = 0.50;
    float C = 0.10;
    float D = 0.20;
    float E = 0.02;
    float F = 0.30;
    return ((color *(A *color + C * B) + D * E) / (color * (A * color + B) + D *F)) -E / F;
}

vec3 exponential_tonemapping(vec3 color) {
	return vec3(1.0) - exp(-color * exposure);
}

// Luma-based Reinhard (Preserves color saturation better than standard)
vec3 reinhard_luma_tonemapping(vec3 color) {
    float luma = dot(color, vec3(0.2126, 0.7152, 0.0722));
    float toneMappedLuma = luma / (1.0 + luma);
    return color * (toneMappedLuma / luma);
}

// Filmic (Jim Hejl / Richard Burgess-Dawson version)
vec3 filmic_tonemapping(vec3 color) {
    vec3 x = max(vec3(0.0), color - 0.004);
    return (x * (6.2 * x + 0.5)) / (x * (6.2 * x + 1.7) + 0.06);
}

// Lottes (AMD / Doom 2016 style, very tunable)
vec3 lottes_tonemapping(vec3 color) {
    // These constants can be moved to uniforms for real-time tweaking
    float a = 1.6;  // Contrast
    float d = 0.977; // Shoulder
    float midIn = 0.18;
    float midOut = 0.18;
    
    float b = (-pow(midIn, a) + midOut * pow(d, a)) / (midOut * pow(d, a) - midOut * pow(midIn, a));
    float c = (pow(d, a) * pow(midIn, a) * (1.0 - midOut)) / (midOut * pow(d, a) - midOut * pow(midIn, a));
    
    return pow(color, vec3(a)) / (pow(color, vec3(a)) * b + c);
}

// Uchimura (Used in Gran Turismo, great for 'toe' control in shadows)
vec3 uchimura_tonemapping(vec3 color) {
    float P = 1.0;  // max brightness
    float a = 1.0;  // contrast
    float m = 0.22; // linear section start
    float l = 0.4;  // linear section length
    float c = 1.33; // black tightness
    float b = 0.0;  // pedestal
    
    float cp = P * pow(m, c);
    float l0 = ((P - cp) * l) / P;
    float S0 = m + l0;
    float S1 = m + a * l0;
    float C2 = (a * P) / (P - S1);
    float CP = -C2 * pow(P - S1, 2.0);

    vec3 w0 = vec3(1.0 - smoothstep(0.0, m, color));
    vec3 w2 = vec3(step(m + l0, color));
    vec3 w1 = vec3(1.0) - w0 - w2;

    vec3 T = vec3(m * pow(color / m, vec3(c)) + b);
    vec3 L = vec3(m + a * (color - m) + b);
    vec3 S = vec3(P - (P - S1) * exp(-C2 * (color - S1) / P) + b);

    return T * w0 + L * w1 + S * w2;
}

float random(vec2 uv) {
    return fract(sin(dot(uv.xy, vec2(12.9898, 78.233))) * 43758.5453);
}

void main() {
    vec3 hdrColor = texture(uTexture, TexCoords).rgb;

    vec3 mapped = vec3(0.0, 0.0, 0.0);

	if(uToneMappingStyle == TONEMAPPINGSTYLE_REINHARD)
		mapped = reinhard_tonemapping(hdrColor);
	else if(uToneMappingStyle == TONEMAPPINGSTYLE_ACES)
		mapped = aces_tonemapping(hdrColor);
	else if(uToneMappingStyle == TONEMAPPINGSTYLE_UNCHARTED)
		mapped = uncharted_tonemapping(hdrColor);
	else if(uToneMappingStyle == TONEMAPPINGSTYLE_EXPONENTIAL)
		mapped = exponential_tonemapping(hdrColor);
	else if(uToneMappingStyle == TONEMAPPINGSTYLE_REINHARD_LUMA)
		mapped = reinhard_luma_tonemapping(hdrColor);
	else if(uToneMappingStyle == TONEMAPPINGSTYLE_FILMIC)
		mapped = filmic_tonemapping(hdrColor);
	else if(uToneMappingStyle == TONEMAPPINGSTYLE_LOTTES)
		mapped = lottes_tonemapping(hdrColor);
	else if(uToneMappingStyle == TONEMAPPINGSTYLE_UCHIMURA)
		mapped = uchimura_tonemapping(hdrColor);
	else
		mapped = hdrColor;

    //mapped.rgb = pow(mapped.rgb, vec3(1.0 / 2.2));
    mapped.rgb = pow(mapped.rgb, vec3(0.454545455));

    // Dithering: Apply a tiny bit of noise to break up banding
    // (1.0 / 255.0) represents the size of one 8-bit color step
    if(uDithering > 0) {
        float dither = (random(TexCoords) - 0.5) * (1.0 / 255.0);
        mapped.rgb += dither;
    }

    FragColor = vec4(mapped, 1.0);
}";
    }
}