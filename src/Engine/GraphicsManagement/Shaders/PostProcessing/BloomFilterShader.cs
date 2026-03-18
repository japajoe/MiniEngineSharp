namespace MiniEngine.GraphicsManagement.Shaders.PostProcessing
{
    public static class BloomFilterShader
    {
        public static string fragmentSource = @"in vec2 TexCoords;
out vec4 FragColor;

#define SAMPLEMODE_DOWNSAMPLE 0
#define SAMPLEMODE_UPSAMPLE 1

uniform sampler2D uTexture;
uniform vec2 uResolution;
uniform float uFilterRadius;
uniform float uThreshold;
uniform int uSampleMode; // 0 for Downsample, 1 for Upsample, 2 for Blur

// Karis average stabilizes HDR flickering in the downsample pass
vec3 karis_average(vec3 col) {
    float luma = dot(col, vec3(0.2126, 0.7152, 0.0722));
    return col / (1.0 + luma);
}

void downsample() {
    vec2 texelSize = 1.0 / uResolution;
    float x = texelSize.x;
    float y = texelSize.y;

    // 13-tap filter pattern (Unity-style)
    // a - b - c
    // - d - e -
    // f - g - h
    // - i - j -
    // k - l - m
    vec3 a = texture(uTexture, vec2(TexCoords.x - 2.0 * x, TexCoords.y + 2.0 * y)).rgb;
    vec3 b = texture(uTexture, vec2(TexCoords.x,         TexCoords.y + 2.0 * y)).rgb;
    vec3 c = texture(uTexture, vec2(TexCoords.x + 2.0 * x, TexCoords.y + 2.0 * y)).rgb;
    vec3 d = texture(uTexture, vec2(TexCoords.x - x,     TexCoords.y + y)).rgb;
    vec3 e = texture(uTexture, vec2(TexCoords.x + x,     TexCoords.y + y)).rgb;
    vec3 f = texture(uTexture, vec2(TexCoords.x - 2.0 * x, TexCoords.y)).rgb;
    vec3 g = texture(uTexture, vec2(TexCoords.x,         TexCoords.y)).rgb;
    vec3 h = texture(uTexture, vec2(TexCoords.x + 2.0 * x, TexCoords.y)).rgb;
    vec3 i = texture(uTexture, vec2(TexCoords.x - x,     TexCoords.y - y)).rgb;
    vec3 j = texture(uTexture, vec2(TexCoords.x + x,     TexCoords.y - y)).rgb;
    vec3 k = texture(uTexture, vec2(TexCoords.x - 2.0 * x, TexCoords.y - 2.0 * y)).rgb;
    vec3 l = texture(uTexture, vec2(TexCoords.x,         TexCoords.y - 2.0 * y)).rgb;
    vec3 m = texture(uTexture, vec2(TexCoords.x + 2.0 * x, TexCoords.y - 2.0 * y)).rgb;

    vec3 group1 = karis_average((a + b + g + f) * 0.25);
    vec3 group2 = karis_average((b + c + h + g) * 0.25);
    vec3 group3 = karis_average((f + g + l + k) * 0.25);
    vec3 group4 = karis_average((g + h + m + l) * 0.25);
    vec3 group5 = karis_average((d + e + j + i) * 0.25);

    vec3 result = (group1 * 0.125) + (group2 * 0.125) + (group3 * 0.125) + (group4 * 0.125) + (group5 * 0.5);

    // Thresholding: Only keep colors brighter than the uThreshold
    // This only applies when uThreshold > 0 (the first pass)
    if (uThreshold > 0.0) {
        float brightness = dot(result, vec3(0.2126, 0.7152, 0.0722));
        if (brightness < uThreshold) {
            result = vec3(0.0);
        }
    }

    FragColor = vec4(result, 1.0);
}

void upsample() {
    float x = uFilterRadius;
    float y = uFilterRadius;

    // 9-tap tent filter
    // a - b - c
    // d - e - f
    // g - h - i
    vec3 a = texture(uTexture, vec2(TexCoords.x - x, TexCoords.y + y)).rgb;
    vec3 b = texture(uTexture, vec2(TexCoords.x,     TexCoords.y + y)).rgb;
    vec3 c = texture(uTexture, vec2(TexCoords.x + x, TexCoords.y + y)).rgb;
    vec3 d = texture(uTexture, vec2(TexCoords.x - x, TexCoords.y)).rgb;
    vec3 e = texture(uTexture, vec2(TexCoords.x,     TexCoords.y)).rgb;
    vec3 f = texture(uTexture, vec2(TexCoords.x + x, TexCoords.y)).rgb;
    vec3 g = texture(uTexture, vec2(TexCoords.x - x, TexCoords.y - y)).rgb;
    vec3 h = texture(uTexture, vec2(TexCoords.x,     TexCoords.y - y)).rgb;
    vec3 i = texture(uTexture, vec2(TexCoords.x + x, TexCoords.y - y)).rgb;

    vec3 result = e * 4.0;
    result += (b + d + f + h) * 2.0;
    result += (a + c + g + i);
    FragColor = vec4(result * (1.0 / 16.0), 1.0);
}

void main() {
    if (uSampleMode == SAMPLEMODE_DOWNSAMPLE) {
        downsample();
    } else {
        upsample();
    }
}";
    }
}