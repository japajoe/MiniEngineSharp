namespace MiniEngine.GraphicsManagement.Shaders
{
    public static class ShadowDepthShader
    {
        public static string vertexSource = @"layout(location = 0) in vec3 aPos;
layout(location = 1) in vec3 aNormal;
layout(location = 2) in vec2 aTexCoords;

uniform mat4 uModel;
uniform mat4 uLightSpaceMatrix;

out vec2 TexCoords;

void main() {
	TexCoords = aTexCoords;
	gl_Position = uLightSpaceMatrix * uModel * vec4(aPos, 1.0);
}";

        public static string fragmentSource = @"uniform sampler2D uTexture;

in vec2 TexCoords;

void main() {
	if (texture(uTexture, TexCoords).a < 0.5) {
        discard;
    }
}";
    }
}