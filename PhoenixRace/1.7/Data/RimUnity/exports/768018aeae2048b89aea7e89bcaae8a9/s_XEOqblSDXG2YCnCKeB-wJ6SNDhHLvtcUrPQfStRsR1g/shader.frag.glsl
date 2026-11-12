#version 450
vec3 RU_Decode(vec3 c) {
    return mix(pow(max((c + 0.055) / 1.055, vec3(0.0)), vec3(2.4)), c / 12.92, lessThanEqual(c, vec3(0.04045)));
}
vec3 RU_Encode(vec3 c) {
    return mix(1.055 * pow(max(c, vec3(0.0)), vec3(1.0 / 2.4)) - 0.055, c * 12.92, lessThanEqual(c, vec3(0.0031308)));
}
vec4 RU_Linear(vec4 c) { return vec4(RU_Decode(c.rgb), c.a); }
vec3 RU_Linear(vec3 c) { return RU_Decode(c); }
vec2 RU_Linear(vec2 c) { return RU_Decode(vec3(c, 0.0)).xy; }
float RU_Linear(float c) { return RU_Decode(vec3(c)).x; }
vec4 RU_SampleLinear(vec4 c, float srgb) { return srgb > 0.5 ? RU_Linear(c) : c; }


#line 43 0
struct RU_MaterialData_0
{
    vec4 _AtmosphereColor; // @default = vec4(0.24, 0.58, 1, 1) @color
    float _AtmosphereStrength; // @default = 0.38
    float _AtmosphereWidth; // @default = 0.018
    vec4 _ClipRect;
    float _EdgeBlur; // @default = 2.4
    float _Exposure; // @default = 1.3
    vec4 _MainTex_TexelSize;
    float _Midtone; // @default = 0.82
    vec4 _PlanetCenter;
    vec4 _ViewPlane;
};


#line 55
layout(binding = 0, set = 2, std140) uniform block_RU_MaterialData_0
{
    vec4 _AtmosphereColor; // @default = vec4(0.24, 0.58, 1, 1) @color
    float _AtmosphereStrength; // @default = 0.38
    float _AtmosphereWidth; // @default = 0.018
    float _EdgeBlur; // @default = 2.4
    float _Exposure; // @default = 1.3
    vec4 _MainTex_TexelSize;
    float _Midtone; // @default = 0.82
    vec4 _PlanetCenter;
    vec4 _ViewPlane;

    float _UiMaskCount; // @default = 0.0
    float _UiRectMaskCount; // @default = 0.0
    float _RimUnitySrgb_MainTex; // @default = 1
    float _RimUnitySrgb_UiMaskAtlas; // @default = 1
}RU_Material_0;

#line 67
layout(binding = 1, set = 2)
uniform sampler2D _MainTex; // @default = black


#line 69



#line 14961 1
vec3 saturate_0(vec3 x_0)
{

#line 14969
    return clamp(x_0, vec3(0.0), vec3(1.0));
}


#line 79 0
float AtmosphereBand_0(vec2 uv_0)
{

#line 79
    vec3 ray_0 = normalize(vec3((uv_0 * 2.0 - 1.0) * RU_Material_0._ViewPlane.xy, 1.0));

#line 79
    float edge_0 = (length(cross(RU_Material_0._PlanetCenter.xyz, ray_0)) - 1.0) / RU_Material_0._AtmosphereWidth;

#line 79
    float _S1;

#line 79
    if(edge_0 < 0.0)
    {

#line 79
        _S1 = 0.55000001192092896;

#line 79
    }
    else
    {

#line 79
        _S1 = 1.0;

#line 79
    }

#line 79
    float edge_1 = edge_0 * _S1;

#line 79
    return exp(- edge_1 * edge_1) * step(0.0, dot(RU_Material_0._PlanetCenter.xyz, ray_0));
}


#line 71
vec4 RU_tex2D_MainTex_0(vec2 uv_1)
{

#line 71
    return (RU_SampleLinear(texture(_MainTex, (vec2(uv_1.x, 1.0 - uv_1.y))), RU_Material_0._RimUnitySrgb_MainTex));
}


#line 79
vec3 SampleSoftEdge_0(vec2 uv_2, float band_0)
{

#line 79
    vec2 offset_0 = abs(RU_Material_0._MainTex_TexelSize.xy) * RU_Material_0._EdgeBlur;

#line 79
    vec3 center_0 = RU_tex2D_MainTex_0(uv_2).xyz;

#line 79
    vec2 _S2 = vec2(offset_0.x, 0.0);

#line 79
    vec2 _S3 = vec2(0.0, offset_0.y);

#line 79
    return mix(center_0, center_0 * 0.40000000596046448 + RU_tex2D_MainTex_0(uv_2 + _S2).xyz * 0.15000000596046448 + RU_tex2D_MainTex_0(uv_2 - _S2).xyz * 0.15000000596046448 + RU_tex2D_MainTex_0(uv_2 + _S3).xyz * 0.15000000596046448 + RU_tex2D_MainTex_0(uv_2 - _S3).xyz * 0.15000000596046448, vec3(band_0));
}


#line 79
layout(location = 0)
out vec4 entryPointParam_frag_0;


#line 79
layout(location = 1) in vec2 input_uv_0; // @vertex TexCoord0 optional


#line 79
void ruUiOriginalMain()
{

#line 79
    float band_1 = AtmosphereBand_0(input_uv_0);

#line 79
    entryPointParam_frag_0 = vec4(1.0 - (1.0 - saturate_0(pow(max(SampleSoftEdge_0(input_uv_0, band_1), vec3(0.0)), vec3(RU_Material_0._Midtone)) * RU_Material_0._Exposure)) * (1.0 - RU_Linear(RU_Material_0._AtmosphereColor).xyz * band_1 * RU_Material_0._AtmosphereStrength), 1.0);

#line 79
    return;
}


// @rimunity-host-matrices 1

layout(std430, set = 2, binding = 2) readonly buffer _UiMasks { vec4 rows[]; } masks;
layout(set = 2, binding = 3) uniform sampler2D _UiMaskAtlas;
layout(std430, set = 2, binding = 4) readonly buffer _UiMaskRegions { vec4 rows[]; } regions;
//图集内按原图的环绕和过滤规则取样，避免相邻区域串色。
ivec2 maskTexel(ivec2 p, ivec2 size, int wrap)
{
    if (wrap == 0) return (p % size + size) % size;
    if (wrap == 2) { p = (p % (size * 2) + size * 2) % (size * 2); return min(p, size * 2 - 1 - p); }
    if (wrap == 3) p = max(p, -p - 1);
    return clamp(p, ivec2(0), size - 1);
}
float maskAlpha(int i, vec2 uv)
{
    ivec4 r = ivec4(regions.rows[i * 2]);
    vec4 options = regions.rows[i * 2 + 1];
    int wrap = int(options.y);
    vec2 p = uv * vec2(r.zw) - 0.5;
    if (options.x < 0.5) return texelFetch(_UiMaskAtlas, r.xy + maskTexel(ivec2(floor(p + 0.5)), r.zw, wrap), 0).a;
    ivec2 b = ivec2(floor(p)); vec2 f = fract(p);
    float a = texelFetch(_UiMaskAtlas, r.xy + maskTexel(b, r.zw, wrap), 0).a;
    float c = texelFetch(_UiMaskAtlas, r.xy + maskTexel(b + ivec2(1, 0), r.zw, wrap), 0).a;
    float d = texelFetch(_UiMaskAtlas, r.xy + maskTexel(b + ivec2(0, 1), r.zw, wrap), 0).a;
    float e = texelFetch(_UiMaskAtlas, r.xy + maskTexel(b + ivec2(1, 1), r.zw, wrap), 0).a;
    return mix(mix(a, c, f.x), mix(d, e, f.x), f.y);
}
//GUI 没有 Stencil 附件，按每层 Mask 的字形透明覆盖率裁切。
float slicedCoordinate(float p, float first, float last, float uvFirst, float uvLast)
{
    if (p < first) return p / max(first, 0.000001) * uvFirst;
    if (p > 1.0 - last) return 1.0 - (1.0 - p) / max(last, 0.000001) * uvLast;
    return uvFirst + (p - first) / max(1.0 - first - last, 0.000001) * (1.0 - uvFirst - uvLast);
}
void applyUiMasks()
{
    for (int i = 0; i < int(RU_Material_0._UiMaskCount); ++i)
    {
        vec3 p = vec3(gl_FragCoord.xy, 1.0);
        vec2 q = vec2(dot(masks.rows[i * 7].xyz, p), dot(masks.rows[i * 7 + 1].xyz, p)) / dot(masks.rows[i * 7 + 2].xyz, p);
        if (q.x < 0.0 || q.y < 0.0 || q.x > 1.0 || q.y > 1.0) discard;
        vec4 d = masks.rows[i * 7 + 4], b = masks.rows[i * 7 + 5];
        vec4 options = masks.rows[i * 7 + 6];
        if (options.x > 0.5)
        {
            if (options.y < 0.5 && q.x > d.x && q.x < 1.0 - d.z && q.y > d.y && q.y < 1.0 - d.w) discard;
            q.x = slicedCoordinate(q.x, d.x, d.z, b.x, b.z);
            q.y = slicedCoordinate(q.y, d.y, d.w, b.y, b.w);
        }
        vec4 r = masks.rows[i * 7 + 3];
        vec2 t = q * r.zw + r.xy; t.y = 1.0 - t.y;
        float a = maskAlpha(i, t);
        if (a < 0.001) discard;
    }
}
//矩形裁切和羽化使用与 Mask 相同的屏幕到局部投影约定。
layout(std430, set = 2, binding = 5) readonly buffer _UiRectMasks { vec4 rows[]; } rectangles;
float rectCoverage()
{
    float coverage = 1.0;
    for (int i = 0; i < int(RU_Material_0._UiRectMaskCount); ++i)
    {
        vec3 p = vec3(gl_FragCoord.xy, 1.0);
        vec2 q = vec2(dot(rectangles.rows[i * 4].xyz, p), dot(rectangles.rows[i * 4 + 1].xyz, p)) / dot(rectangles.rows[i * 4 + 2].xyz, p);
        if (q.x < 0.0 || q.y < 0.0 || q.x > 1.0 || q.y > 1.0) discard;
        vec2 edge = min(q, 1.0 - q);
        vec2 softness = rectangles.rows[i * 4 + 3].xy;
        vec2 alpha = clamp(edge / max(softness, vec2(0.000001)), 0.0, 1.0);
        coverage *= alpha.x * alpha.y;
    }
    return coverage;
}

void RU_SourceMain() { applyUiMasks(); ruUiOriginalMain(); entryPointParam_frag_0.a *= rectCoverage(); }

void main() { RU_SourceMain(); entryPointParam_frag_0.rgb = RU_Encode(entryPointParam_frag_0.rgb); }

// @rimunity-linear-program 1
