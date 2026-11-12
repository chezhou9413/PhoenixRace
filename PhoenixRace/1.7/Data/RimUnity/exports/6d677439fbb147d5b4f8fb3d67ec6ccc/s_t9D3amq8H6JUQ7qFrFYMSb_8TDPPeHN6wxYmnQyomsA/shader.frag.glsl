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
float RU_CompatPow3(float x) {
    float result = float(1.0);
    result *= x;
    x *= x;
    result *= x;
    return result;
}
vec2 RU_CompatPow3(vec2 x) {
    vec2 result = vec2(1.0);
    result *= x;
    x *= x;
    result *= x;
    return result;
}
vec3 RU_CompatPow3(vec3 x) {
    vec3 result = vec3(1.0);
    result *= x;
    x *= x;
    result *= x;
    return result;
}
vec4 RU_CompatPow3(vec4 x) {
    vec4 result = vec4(1.0);
    result *= x;
    x *= x;
    result *= x;
    return result;
}


#line 56 0
struct RU_MaterialData_0
{
    vec4 _Color; // @default = vec4(0.03, 0.3, 0.4, 1) @color
    float _Grid; // @default = 1
    vec4 _RimColor; // @default = vec4(0.1, 0.9, 1, 1) @color
    vec4 _Time;
    vec3 _WorldSpaceCameraPos;
};


#line 63
layout(binding = 0, set = 2, std140) uniform block_RU_MaterialData_0
{
    vec4 _Color; // @default = vec4(0.03, 0.3, 0.4, 1) @color
    float _Grid; // @default = 1
    vec4 _RimColor; // @default = vec4(0.1, 0.9, 1, 1) @color
    vec4 _Time;
    vec4 _WorldSpaceCameraPos;
}RU_Material_0;

#line 14946 1
float saturate_0(float x_0)
{

#line 14954
    return clamp(x_0, 0.0, 1.0);
}


#line 71 0
layout(location = 0)
out vec4 entryPointParam_frag_0;


#line 71
layout(location = 1) in vec3 i_local_0; // @vertex TexCoord0 optional


#line 71
layout(location = 5) in vec3 i_normal_0; // @vertex TexCoord1 optional


#line 71
layout(location = 6) in vec3 i_view_0; // @vertex TexCoord2 optional


#line 71
layout(location = 2) in vec4 i_color_0; // @vertex Color optional


#line 72
void RU_SourceMain()
{

#line 72
    vec3 n_0 = normalize(i_normal_0);

#line 72
    vec3 p_0 = normalize(i_local_0);

#line 72
    float _S1 = p_0.y;

#line 72
    vec2 uv_0 = vec2((atan((p_0.z),(p_0.x))) * 3.81972002983093262, asin(_S1) * 7.63944005966186523);

#line 72
    vec2 grid_0 = abs(fract(uv_0 - 0.5) - 0.5) / max((fwidth((uv_0))), vec2(0.00100000004749745));

#line 72
    entryPointParam_frag_0 = vec4(RU_Linear(RU_Material_0._Color).xyz * i_color_0.xyz * (0.2199999988079071 + 0.77999997138977051 * saturate_0(dot(n_0, normalize(vec3(-0.5, 0.69999998807907104, -0.80000001192092896))))) + RU_Linear(RU_Material_0._RimColor).xyz * (RU_CompatPow3(1.0 - saturate_0(dot(n_0, normalize(i_view_0)))) * 0.85000002384185791 + (1.0 - saturate_0(min(grid_0.x, grid_0.y))) * 0.14000000059604645 * RU_Material_0._Grid + RU_CompatPow3(saturate_0(1.0 - abs(fract(_S1 * 0.5 + 0.5 - RU_Material_0._Time.y * 0.11999999731779099) - 0.5) * 24.0)) * 0.15999999642372131), 1.0);

#line 72
    return;
}


// @rimunity-host-matrices 1

void main() { RU_SourceMain(); entryPointParam_frag_0.rgb = RU_Encode(entryPointParam_frag_0.rgb); }

// @rimunity-linear-program 1
