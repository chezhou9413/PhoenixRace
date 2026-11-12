#version 450
layout(binding = 1, set = 2)
uniform sampler2D _MainTex; // @default = white


#line 15 0
struct RU_CameraData_0
{
    mat4 ViewProj_0;
};


#line 16
layout(binding = 0, std140) uniform block_RU_CameraData_0
{
    mat4 ViewProj_0;
}RU_Camera_0;

#line 17
struct RU_ObjectData_0
{
    mat4 model_0;
};


#line 18
layout(push_constant, std430) uniform block_RU_ObjectData_0
{
    mat4 model_0;
}RU_Object_0;

#line 56
struct RU_MaterialData_0
{
    float _BlockSize; // @default = 10
    vec4 _Color; // @default = vec4(1, 1, 1, 1) @color
    float _ColorSplit; // @default = 0.02
    float _GlitchIntensity; // @default = 0.05
    vec4 _MainTex_ST; // @default = vec4(0, 0, 1, 1)
    float _Speed; // @default = 5
    vec4 _Time;
};


#line 65
layout(binding = 0, set = 2, std140) uniform block_RU_MaterialData_0
{
    float _BlockSize; // @default = 10
    vec4 _Color; // @default = vec4(1, 1, 1, 1) @color
    float _ColorSplit; // @default = 0.02
    float _GlitchIntensity; // @default = 0.05
    vec4 _MainTex_ST; // @default = vec4(0, 0, 1, 1)
    float _Speed; // @default = 5
    vec4 _Time;
}RU_Material_0;

#line 23
vec4 UnityObjectToClipPos_0(vec4 p_0)
{

#line 23
    return ((((((p_0) * (transpose(RU_Object_0.model_0))))) * (transpose(RU_Camera_0.ViewProj_0))));
}


#line 23
layout(location = 2)
out vec4 entryPointParam_vert_color_0;


#line 3346 1
layout(location = 1)
out vec2 entryPointParam_vert_texcoord_0;


#line 3346
layout(location = 5)
out vec4 entryPointParam_vert_worldPosition_0;


#line 3346
layout(location = 0) in vec4 IN_vertex_0; // @vertex Position required


#line 3346
layout(location = 2) in vec4 IN_color_0; // @vertex Color optional


#line 3346
layout(location = 1) in vec2 IN_texcoord_0; // @vertex TexCoord0 optional


#line 86 0
struct v2f_0
{
    vec4 vertex_0;
    vec4 color_0;
    vec2 texcoord_0;
    vec4 worldPosition_0;
};


#line 86
void main()
{

#line 86
    v2f_0 OUT_0;

#line 86
    OUT_0.worldPosition_0 = IN_vertex_0;

#line 86
    OUT_0.vertex_0 = UnityObjectToClipPos_0(IN_vertex_0);

#line 86
    OUT_0.texcoord_0 = IN_texcoord_0.xy * RU_Material_0._MainTex_ST.zwxy.xy + RU_Material_0._MainTex_ST.zwxy.zw;

#line 86
    OUT_0.color_0 = IN_color_0 * RU_Material_0._Color;

#line 86
    v2f_0 _S1 = OUT_0;

#line 86
    gl_Position = OUT_0.vertex_0;

#line 86
    entryPointParam_vert_color_0 = _S1.color_0;

#line 86
    entryPointParam_vert_texcoord_0 = _S1.texcoord_0;

#line 86
    entryPointParam_vert_worldPosition_0 = _S1.worldPosition_0;

#line 86
    return;
}


// @rimunity-host-matrices 1
