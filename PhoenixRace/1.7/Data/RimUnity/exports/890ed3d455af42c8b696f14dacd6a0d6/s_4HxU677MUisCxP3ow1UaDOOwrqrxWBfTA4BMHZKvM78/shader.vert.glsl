#version 450
layout(binding = 1, set = 2)
uniform sampler2D _NoiseTex; // @default = white


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
    float _BaseFresnelIntensity; // @default = 0.5
    float _BaseFresnelPower; // @default = 4
    vec4 _BreakTint; // @default = vec4(1, 1, 1, 1) @color
    vec4 _Color; // @default = vec4(1, 0.5, 0, 1) @color
    float _Dissolve; // @default = 0
    float _DissolveSoftness; // @default = 0.1
    float _HexIntensity; // @default = 5
    float _HexScale; // @default = 30
    vec4 _ImpactColor; // @default = vec4(1, 0.8, 0.3, 1) @color
    float _ImpactFadePower; // @default = 1.5
    float _ImpactHexShape; // @default = 1
    float _ImpactIntensity; // @default = 15
    float _ImpactMaxRadius; // @default = 1.8
    vec4 _ImpactPoint0; // @default = vec4(0, 0, 0, 0)
    vec4 _ImpactPoint1; // @default = vec4(0, 0, 0, 0)
    vec4 _ImpactPoint2; // @default = vec4(0, 0, 0, 0)
    vec4 _ImpactPoint3; // @default = vec4(0, 0, 0, 0)
    vec4 _ImpactPoint4; // @default = vec4(0, 0, 0, 0)
    vec4 _ImpactPoint5; // @default = vec4(0, 0, 0, 0)
    vec4 _ImpactPoint6; // @default = vec4(0, 0, 0, 0)
    vec4 _ImpactPoint7; // @default = vec4(0, 0, 0, 0)
    vec4 _ImpactRedColor; // @default = vec4(1.5, 0.1, 0.1, 1) @color
    float _ImpactRedDuration; // @default = 1.2
    float _ImpactRedIntensity; // @default = 1.5
    float _ImpactRedRadius; // @default = 0.35
    float _ImpactRingWidth; // @default = 0.35
    float _ImpactSpeed; // @default = 2
    float _LineThickness; // @default = 0.05
    vec2 _NoiseFlowSpeed; // @default = vec2(0.3, 0.2)
    float _NoiseIntensity; // @default = 1.2
    float _NoiseScale; // @default = 0.5
    mat4 _RimUnity_View;
    float _ShatterGlow; // @default = 10
    float _SpreadSpeed; // @default = 1
    float _SpreadWidth; // @default = 0.2
    vec4 _Time;
    vec3 _WorldSpaceCameraPos;
    vec4 unity_OrthoParams;
};


#line 96
layout(binding = 0, set = 2, std140) uniform block_RU_MaterialData_0
{
    float _BaseFresnelIntensity; // @default = 0.5
    float _BaseFresnelPower; // @default = 4
    vec4 _BreakTint; // @default = vec4(1, 1, 1, 1) @color
    vec4 _Color; // @default = vec4(1, 0.5, 0, 1) @color
    float _Dissolve; // @default = 0
    float _DissolveSoftness; // @default = 0.1
    float _HexIntensity; // @default = 5
    float _HexScale; // @default = 30
    vec4 _ImpactColor; // @default = vec4(1, 0.8, 0.3, 1) @color
    float _ImpactFadePower; // @default = 1.5
    float _ImpactHexShape; // @default = 1
    float _ImpactIntensity; // @default = 15
    float _ImpactMaxRadius; // @default = 1.8
    vec4 _ImpactPoint0; // @default = vec4(0, 0, 0, 0)
    vec4 _ImpactPoint1; // @default = vec4(0, 0, 0, 0)
    vec4 _ImpactPoint2; // @default = vec4(0, 0, 0, 0)
    vec4 _ImpactPoint3; // @default = vec4(0, 0, 0, 0)
    vec4 _ImpactPoint4; // @default = vec4(0, 0, 0, 0)
    vec4 _ImpactPoint5; // @default = vec4(0, 0, 0, 0)
    vec4 _ImpactPoint6; // @default = vec4(0, 0, 0, 0)
    vec4 _ImpactPoint7; // @default = vec4(0, 0, 0, 0)
    vec4 _ImpactRedColor; // @default = vec4(1.5, 0.1, 0.1, 1) @color
    float _ImpactRedDuration; // @default = 1.2
    float _ImpactRedIntensity; // @default = 1.5
    float _ImpactRedRadius; // @default = 0.35
    float _ImpactRingWidth; // @default = 0.35
    float _ImpactSpeed; // @default = 2
    float _LineThickness; // @default = 0.05
    vec2 _NoiseFlowSpeed; // @default = vec2(0.3, 0.2)
    float _NoiseIntensity; // @default = 1.2
    float _NoiseScale; // @default = 0.5
    mat4 _RimUnity_View;
    float _ShatterGlow; // @default = 10
    float _SpreadSpeed; // @default = 1
    float _SpreadWidth; // @default = 0.2
    vec4 _Time;
    vec4 _WorldSpaceCameraPos;
    vec4 unity_OrthoParams;
}RU_Material_0;

#line 23
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
layout(location = 3)
out vec3 entryPointParam_vert_worldNormal_0;


#line 31
layout(location = 1)
out vec3 entryPointParam_vert_worldPos_0;


#line 31
layout(location = 5)
out vec2 entryPointParam_vert_viewUV_0;


#line 31
layout(location = 0) in vec4 v_vertex_0; // @vertex Position required


#line 31
layout(location = 3) in vec3 v_normal_0; // @vertex Normal optional


#line 148
struct v2f_0
{
    vec4 vertex_0;
    vec3 worldNormal_0;
    vec3 worldPos_0;
    vec2 viewUV_0;
};


#line 160
void main()
{

#line 160
    v2f_0 o_0;

#line 160
    o_0.vertex_0 = UnityObjectToClipPos_0(v_vertex_0);

#line 160
    vec3 _S2 = UnityObjectToWorldNormal_0(v_normal_0);

#line 160
    o_0.worldNormal_0 = _S2;

#line 160
    o_0.worldPos_0 = (((v_vertex_0) * (transpose(RU_Object_0.model_0)))).xyz;

#line 160
    o_0.viewUV_0 = (((_S2) * (mat3(transpose(RU_Material_0._RimUnity_View)[0].xyz, transpose(RU_Material_0._RimUnity_View)[1].xyz, transpose(RU_Material_0._RimUnity_View)[2].xyz)))).xy * 0.5 + 0.5;

#line 160
    v2f_0 _S3 = o_0;

#line 160
    gl_Position = o_0.vertex_0;

#line 160
    entryPointParam_vert_worldNormal_0 = _S3.worldNormal_0;

#line 160
    entryPointParam_vert_worldPos_0 = _S3.worldPos_0;

#line 160
    entryPointParam_vert_viewUV_0 = _S3.viewUV_0;

#line 160
    return;
}


// @rimunity-host-matrices 1
