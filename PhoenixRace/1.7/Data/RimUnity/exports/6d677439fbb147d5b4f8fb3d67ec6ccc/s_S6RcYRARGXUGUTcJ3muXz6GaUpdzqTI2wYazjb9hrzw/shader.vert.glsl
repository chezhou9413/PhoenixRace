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
    vec4 _Color; // @default = vec4(0.05, 0.8, 1, 0.6) @color
};


#line 59
layout(binding = 0, set = 2, std140) uniform block_RU_MaterialData_0
{
    vec4 _Color; // @default = vec4(0.05, 0.8, 1, 0.6) @color
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


#line 23
layout(location = 0) in vec4 v_vertex_0; // @vertex Position required


#line 23
layout(location = 2) in vec4 v_color_0; // @vertex Color optional


#line 63
struct Output_0
{
    vec4 pos_0;
    vec4 color_0;
};


#line 63
void main()
{

#line 63
    Output_0 o_0;

#line 63
    o_0.pos_0 = UnityObjectToClipPos_0(v_vertex_0);

#line 63
    o_0.color_0 = v_color_0 * RU_Linear(RU_Material_0._Color);

#line 63
    Output_0 _S1 = o_0;

#line 63
    gl_Position = o_0.pos_0;

#line 63
    entryPointParam_vert_color_0 = _S1.color_0;

#line 63
    return;
}


// @rimunity-host-matrices 1

// @rimunity-linear-program 1
