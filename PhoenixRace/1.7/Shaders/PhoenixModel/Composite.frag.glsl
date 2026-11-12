#version 450
layout(location = 0) in vec2 uv;
layout(location = 0) out vec4 outColor;
layout(set = 2, binding = 0, std140) uniform MaterialUniforms
{
    vec4 _MainTex_TexelSize; // @default = vec4(0.001, 0.001, 1000.0, 1000.0)
    vec4 _GeometryTex_TexelSize; // @default = vec4(0.001, 0.001, 1000.0, 1000.0)
    vec4 _CaptureRight; // @default = vec4(1.0, 0.0, 0.0, 0.0)
    vec4 _CaptureUp; // @default = vec4(0.0, 1.0, 0.0, 0.0)
    vec4 _CaptureForward; // @default = vec4(0.0, 0.0, 1.0, 0.0)
    vec4 _Capture; // @default = vec4(1.0, 0.01, 1.0, 0.0)
    vec4 _OutlineColor; // @default = vec4(0.0, 0.0, 0.0, 1.0)
} material;
layout(set = 2, binding = 1) uniform sampler2D _MainTex;
layout(set = 2, binding = 2) uniform sampler2D _GeometryTex;
const vec2 directions[8] = vec2[8](vec2(1, 0), vec2(-1, 0), vec2(0, 1), vec2(0, -1),
    vec2(0.7071, 0.7071), vec2(-0.7071, 0.7071), vec2(0.7071, -0.7071), vec2(-0.7071, -0.7071));

//越过附件边缘的采样为空白，不重复采样边缘像素形成粗黑框。
vec4 colorAt(vec2 p)
{
    if (any(lessThan(p, vec2(0))) || any(greaterThan(p, vec2(1)))) return vec4(0);
    return texture(_MainTex, p);
}

//点采样保证法线和距离来自同一个真实表面。
vec4 geometryAt(vec2 p)
{
    if (any(lessThan(p, vec2(0))) || any(greaterThan(p, vec2(1)))) return vec4(0);
    return texture(_GeometryTex, p);
}

//由正交相机距离重建相对世界位置，原点在相邻表面求差时抵消。
vec3 positionAt(vec2 p, float depth)
{
    return material._CaptureRight.xyz * ((p.x * 2.0 - 1.0) * material._Capture.x)
        + material._CaptureUp.xyz * ((1.0 - p.y * 2.0) * material._Capture.x)
        + material._CaptureForward.xyz * depth;
}

//轻量局部遮蔽只处理前方邻近表面，不把共面接缝变成内部黑线。
float contactShade(vec2 p, vec4 center)
{
    vec2 pixel = (floor(p * material._GeometryTex_TexelSize.zw) + 0.5) * material._GeometryTex_TexelSize.xy;
    vec3 origin = positionAt(pixel, center.a);
    float radius = material._Capture.x * 0.08;
    float occlusion = 0.0;
    for (int i = 0; i < 8; i++)
    {
        vec2 q = pixel + directions[i] * (0.018 + 0.003 * float(i));
        vec4 neighbor = geometryAt(q);
        if (neighbor.a <= 0.0) continue;
        q = (floor(q * material._GeometryTex_TexelSize.zw) + 0.5) * material._GeometryTex_TexelSize.xy;
        vec3 delta = positionAt(q, neighbor.a) - origin;
        float distance = length(delta);
        float above = max(0.0, dot(center.xyz, delta) - radius * 0.08);
        occlusion += clamp(above / max(distance, radius * 0.05), 0.0, 1.0)
            * (1.0 - smoothstep(radius * 0.25, radius, distance));
    }
    return 1.0 - occlusion * 0.045;
}

//表面光影与外轮廓分别计算，最终输出预乘颜色供地图透明合成。
vec4 shadeAt(vec2 p)
{
    vec4 source = colorAt(p);
    vec4 geometry = geometryAt(p);
    float shade = 1.0;
    if (geometry.a > 0.0)
    {
        vec3 n = normalize(geometry.xyz);
        float hemisphere = mix(0.35, 0.66, clamp(n.y * 0.5 + 0.5, 0.0, 1.0));
        float key = max(dot(n, normalize(vec3(-0.5, 1.0, -0.6))), 0.0) * 0.36;
        float fill = max(dot(n, normalize(vec3(0.8, 0.5, 0.4))), 0.0) * 0.10;
        shade = (hemisphere + key + fill) * contactShade(p, geometry);
    }
    float coverage = source.a;
    for (int i = 0; i < 8; i++) coverage = max(coverage, colorAt(p + directions[i] * material._Capture.y).a);
    float border = max(0.0, coverage - source.a);
    vec3 color = source.rgb * source.a * shade + material._OutlineColor.rgb * border;
    return vec4(color, source.a + border);
}

//二倍采集时平均四个结果，先计算轮廓再缩小，避免近景锯齿和透明黑边。
void main()
{
    if (material._Capture.z > 1.5)
    {
        vec2 d = material._MainTex_TexelSize.xy * 0.5;
        outColor = (shadeAt(uv + d) + shadeAt(uv - d)
            + shadeAt(uv + vec2(d.x, -d.y)) + shadeAt(uv + vec2(-d.x, d.y))) * 0.25;
    }
    else outColor = shadeAt(uv);
}
