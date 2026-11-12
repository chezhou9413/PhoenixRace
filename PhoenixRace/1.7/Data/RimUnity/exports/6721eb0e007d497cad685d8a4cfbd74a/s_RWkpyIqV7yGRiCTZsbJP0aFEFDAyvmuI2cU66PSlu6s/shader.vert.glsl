#version 450


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
    float _BreathMax; // @default = 1
    float _BreathMin; // @default = 0
    float _BreathSpeed; // @default = 1.2
    vec4 _CoreColor; // @default = vec4(0.8, 1, 1, 1) @color
    float _CorePower; // @default = 2
    vec4 _EdgeColor1; // @default = vec4(0, 1, 0.8, 1) @color
    vec4 _EdgeColor2; // @default = vec4(0.4, 0, 1, 1) @color
    float _EdgeColorScale; // @default = 3
    float _EdgeColorSpeed; // @default = 1.8
    float _EdgeDistort; // @default = 0.7
    float _EdgeNoiseScale; // @default = 5
    float _EdgeNoiseSpeed; // @default = 2
    float _EdgeSharp; // @default = 4
    float _EdgeWidth; // @default = 0.18
    vec4 _FlashColor; // @default = vec4(1, 1, 1, 1) @color
    float _FlashSharp; // @default = 6
    float _Intensity; // @default = 3
    float _NoiseScale; // @default = 3
    float _NoiseSpeed; // @default = 1.5
    float _NoseSoft; // @default = 0.08
    vec4 _RimColor; // @default = vec4(0, 0.6, 1, 1) @color
    float _RimPower; // @default = 4
    mat4 _RimUnity_View;
    float _RippleAmp; // @default = 0.3
    float _RippleFreq; // @default = 8
    float _RippleSpeed; // @default = 4
    vec4 _TailColor; // @default = vec4(0, 0.8, 1, 1) @color
    float _TailCutoff; // @default = 0.05
    float _TailNoiseAmp; // @default = 0.18
    float _TailNoiseScale; // @default = 4
    float _TailNoiseSpeed; // @default = 2.5
    float _TailSoftBase; // @default = 0.15
    vec4 _Time;
};


#line 91
layout(binding = 0, set = 2, std140) uniform block_RU_MaterialData_0
{    float _BreathSpeed; // @default = 1.2
    vec4 _CoreColor; // @default = vec4(0.8, 1, 1, 1) @color
    float _CorePower; // @default = 2
    vec4 _EdgeColor1; // @default = vec4(0, 1, 0.8, 1) @color
    vec4 _EdgeColor2; // @default = vec4(0.4, 0, 1, 1) @color
    float _EdgeColorScale; // @default = 3
    float _EdgeColorSpeed; // @default = 1.8
    float _EdgeDistort; // @default = 0.7
    float _EdgeNoiseScale; // @default = 5
    float _EdgeNoiseSpeed; // @default = 2
    float _EdgeSharp; // @default = 4
    float _EdgeWidth; // @default = 0.18
    vec4 _FlashColor; // @default = vec4(1, 1, 1, 1) @color
    float _FlashSharp; // @default = 6
    float _Intensity; // @default = 3
    float _NoiseScale; // @default = 3
    float _NoiseSpeed; // @default = 1.5
    float _NoseSoft; // @default = 0.08
    vec4 _RimColor; // @default = vec4(0, 0.6, 1, 1) @color
    float _RimPower; // @default = 4
    mat4 _RimUnity_View;
    float _RippleAmp; // @default = 0.3
    float _RippleFreq; // @default = 8
    float _RippleSpeed; // @default = 4
    vec4 _TailColor; // @default = vec4(0, 0.8, 1, 1) @color
    float _TailCutoff; // @default = 0.05
    float _TailNoiseAmp; // @default = 0.18
    float _TailNoiseScale; // @default = 4
    float _TailNoiseSpeed; // @default = 2.5
    float _TailSoftBase; // @default = 0.15
    vec4 _Time;
}RU_Material_0;

#line 14946 1
float saturate_0(float x_0)
{

#line 14954
    return clamp(x_0, 0.0, 1.0);
}


#line 23 0
vec4 UnityObjectToClipPos_0(vec4 p_0)
{

#line 23
    return ((((((p_0) * (transpose(RU_Object_0.model_0))))) * (transpose(RU_Camera_0.ViewProj_0))));
}



vec3 UnityObjectToWorldNormal_0(vec3 normal_0)
{

#line 29
    mat3 _S1 = mat3(transpose(RU_Object_0.model_0)[0].xyz, transpose(RU_Object_0.model_0)[1].xyz, transpose(RU_Object_0.model_0)[2].xyz);
    mat3 cofactors_0 = mat3(cross(_S1[1], _S1[2]), cross(_S1[2], _S1[0]), cross(_S1[0], _S1[1]));
    return normalize((((normal_0) * (cofactors_0))) * float((int(sign((dot(_S1[0], cofactors_0[0])))))));
}


#line 31
layout(location = 1)
out vec3 entryPointParam_vert_worldNormal_0;


#line 31
layout(location = 5)
out vec3 entryPointParam_vert_worldPos_0;


#line 31
layout(location = 6)
out float entryPointParam_vert_axisT_0;


#line 31
layout(location = 7)
out float entryPointParam_vert_fresnel_0;


#line 31
layout(location = 0) in vec4 v_vertex_0; // @vertex Position required


#line 31
layout(location = 3) in vec3 v_normal_0; // @vertex Normal optional


#line 127
layout(location = 1) in vec2 v_uv_0; // @vertex TexCoord0 optional


#line 127
struct v2f_0
{
    vec4 pos_0;
    vec3 worldNormal_0;
    vec3 worldPos_0;
    float axisT_0;
    float fresnel_0;
};


#line 127
void main()
{

#line 127
    v2f_0 o_0;

#line 127
    o_0.pos_0 = UnityObjectToClipPos_0(v_vertex_0);

#line 127
    o_0.worldPos_0 = (((v_vertex_0) * (transpose(RU_Object_0.model_0)))).xyz;

#line 127
    vec3 wn_0 = UnityObjectToWorldNormal_0(v_normal_0);

#line 127
    o_0.worldNormal_0 = wn_0;

#line 127
    o_0.axisT_0 = v_uv_0.y;

#line 127
    o_0.fresnel_0 = 1.0 - saturate_0(dot(normalize(wn_0), - normalize(vec3(transpose(RU_Material_0._RimUnity_View)[2][0], transpose(RU_Material_0._RimUnity_View)[2][1], transpose(RU_Material_0._RimUnity_View)[2][2]))));

#line 127
    v2f_0 _S2 = o_0;

#line 127
    gl_Position = o_0.pos_0;

#line 127
    entryPointParam_vert_worldNormal_0 = _S2.worldNormal_0;

#line 127
    entryPointParam_vert_worldPos_0 = _S2.worldPos_0;

#line 127
    entryPointParam_vert_axisT_0 = _S2.axisT_0;

#line 127
    entryPointParam_vert_fresnel_0 = _S2.fresnel_0;

#line 127
    return;
}


// @rimunity-host-matrices 1
