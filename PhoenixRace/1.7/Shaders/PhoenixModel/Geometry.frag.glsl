#version 450
layout(location = 0) in vec2 uv;
layout(location = 1) in vec3 normal;
layout(location = 2) in float distanceToCamera;
layout(location = 0) out vec4 outGeometry;
layout(set = 2, binding = 0, std140) uniform MaterialUniforms
{
    vec4 _Color; // @color @default = vec4(1.0, 1.0, 1.0, 1.0)
    vec4 _MainTex_ST; // @default = vec4(0.0, 0.0, 1.0, 1.0)
    vec4 _CaptureOrigin; // @default = vec4(0.0, 0.0, 0.0, 0.0)
    vec4 _CaptureForward; // @default = vec4(0.0, 0.0, 1.0, 0.0)
} material;
layout(set = 2, binding = 1) uniform sampler2D _MainTex;

//源贴图使用 RimUnity 的一次 UV 翻转；浮点 Alpha 保存正距离，零留给背景。
void main()
{
    vec2 sampleUv = uv * material._MainTex_ST.zw + material._MainTex_ST.xy;
    if (texture(_MainTex, vec2(sampleUv.x, 1.0 - sampleUv.y)).a * material._Color.a < 0.01) discard;
    outGeometry = vec4(normalize(normal), distanceToCamera);
}
