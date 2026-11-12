#version 450
layout(location = 0) in vec2 uv;
layout(location = 0) out vec4 outColor;
layout(set = 2, binding = 0, std140) uniform MaterialUniforms
{
    float _Opacity; // @default = 1.0
} material;
layout(set = 2, binding = 1) uniform sampler2D _MainTex;

//预乘颜色与 Alpha 同时淡出，完全透明处不写地图深度。
void main()
{
    outColor = texture(_MainTex, uv) * material._Opacity;
    if (outColor.a < 0.0039) discard;
}
