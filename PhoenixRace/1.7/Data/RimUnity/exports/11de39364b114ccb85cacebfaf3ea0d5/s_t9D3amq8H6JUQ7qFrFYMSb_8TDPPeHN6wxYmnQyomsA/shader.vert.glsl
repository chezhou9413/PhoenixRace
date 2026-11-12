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
layout(location = 1)
out vec3 entryPointParam_vert_local_0;


#line 31
layout(location = 5)
out vec3 entryPointParam_vert_normal_0;


#line 31
layout(location = 6)
out vec3 entryPointParam_vert_view_0;


#line 31
layout(location = 2)
out vec4 entryPointParam_vert_color_0;


#line 31
layout(location = 0) in vec4 v_vertex_0; // @vertex Position required


#line 31
layout(location = 3) in vec3 v_normal_0; // @vertex Normal optional


#line 31
layout(location = 2) in vec4 v_color_0; // @vertex Color optional


#line 71
struct Output_0
{
    vec4 pos_0;
    vec3 local_0;
    vec3 normal_1;
    vec3 view_0;
    vec4 color_0;
};


#line 72
void main()
{

#line 72
    Output_0 o_0;

#line 72
    o_0.pos_0 = UnityObjectToClipPos_0(v_vertex_0);

#line 72
    o_0.local_0 = v_vertex_0.xyz;

#line 72
    o_0.normal_1 = UnityObjectToWorldNormal_0(v_normal_0);

#line 72
    o_0.view_0 = (RU_Material_0._WorldSpaceCameraPos.xyz) - (((v_vertex_0) * (transpose(RU_Object_0.model_0)))).xyz.xyz;

#line 72
    o_0.color_0 = v_color_0;

#line 72
    Output_0 _S2 = o_0;

#line 72
    gl_Position = o_0.pos_0;

#line 72
    entryPointParam_vert_local_0 = _S2.local_0;

#line 72
    entryPointParam_vert_normal_0 = _S2.normal_1;

#line 72
    entryPointParam_vert_view_0 = _S2.view_0;

#line 72
    entryPointParam_vert_color_0 = _S2.color_0;

#line 72
    return;
}


// @rimunity-host-matrices 1

// @rimunity-linear-program 1
