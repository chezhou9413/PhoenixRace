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
layout(set = 2, binding = 3) uniform sampler2D _UiMaskAtlas;
layout(binding = 1, set = 2)
uniform sampler2D _MainTex; // @default = black
layout(std430, set = 2, binding = 5) readonly buffer _UiRectMasks { vec4 rows[]; } rectangles;
layout(std430, set = 2, binding = 4) readonly buffer _UiMaskRegions { vec4 rows[]; } regions;
layout(std430, set = 2, binding = 2) readonly buffer _UiMasks { vec4 rows[]; } masks;
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


#line 15 0
struct RU_FrameData_0
{
    vec4 sunDirection_0;
    mat4 worldToCamera_0;
    mat4 viewProj_0;
};


#line 16
layout(binding = 1, std140) uniform block_RU_FrameData_0
{
    vec4 sunDirection_0;
    mat4 worldToCamera_0;
    mat4 viewProj_0;
}RU_Frame_0;

#line 19
vec4 UnityObjectToClipPos_0(vec4 p_0)
{

#line 19
    return (((p_0) * (transpose(RU_Frame_0.viewProj_0))));
}


#line 79
layout(location = 1)
out vec2 entryPointParam_vert_uv_0;


#line 79
layout(location = 5)
out vec2 entryPointParam_vert_position_0;


#line 79
layout(location = 0) in vec4 input_vertex_0; // @vertex Position required


#line 79
layout(location = 1) in vec2 input_uv_0; // @vertex TexCoord0 optional


#line 79
struct VertexOutput_0
{
    vec4 vertex_0;
    vec2 uv_0;
    vec2 position_0;
};


#line 79
void main()
{

#line 79
    VertexOutput_0 output_0;

#line 79
    output_0.vertex_0 = UnityObjectToClipPos_0(input_vertex_0);

#line 79
    output_0.uv_0 = input_uv_0;

#line 79
    output_0.position_0 = input_vertex_0.xy;

#line 79
    VertexOutput_0 _S1 = output_0;

#line 79
    gl_Position = output_0.vertex_0;

#line 79
    entryPointParam_vert_uv_0 = _S1.uv_0;

#line 79
    entryPointParam_vert_position_0 = _S1.position_0;

#line 79
    return;
}


// @rimunity-host-matrices 1

// @rimunity-linear-program 1
