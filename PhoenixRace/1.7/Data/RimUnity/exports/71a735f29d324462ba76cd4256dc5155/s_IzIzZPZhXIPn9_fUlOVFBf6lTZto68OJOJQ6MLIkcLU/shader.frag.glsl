#version 450


#line 56 0
struct RU_MaterialData_0
{
    float _AlphaBoost; // @default = 1
    vec4 _Color; // @default = vec4(1, 1, 1, 1) @color
    vec4 _FlowSpeed; // @default = vec4(0.1, 0.1, 0, 0)
    vec4 _MainTex_ST; // @default = vec4(0, 0, 1, 1)
    float _NoiseStrength; // @default = 0.3
    vec4 _Time;
};


#line 64
layout(binding = 0, set = 2, std140) uniform block_RU_MaterialData_0
{
    float _AlphaBoost; // @default = 1
    vec4 _Color; // @default = vec4(1, 1, 1, 1) @color
    vec4 _FlowSpeed; // @default = vec4(0.1, 0.1, 0, 0)
    vec4 _MainTex_ST; // @default = vec4(0, 0, 1, 1)
    float _NoiseStrength; // @default = 0.3
    vec4 _Time;
}RU_Material_0;

#line 83
layout(binding = 3, set = 2)
uniform sampler2D _NoiseTex; // @default = white


#line 85



#line 72
layout(binding = 1, set = 2)
uniform sampler2D _MainTex; // @default = white


#line 74



#line 14946 1
float saturate_0(float x_0)
{

#line 14954
    return clamp(x_0, 0.0, 1.0);
}


#line 87 0
vec4 RU_tex2D_NoiseTex_0(vec2 uv_0)
{

#line 87
    return (texture(_NoiseTex, (vec2(uv_0.x, 1.0 - uv_0.y))));
}


#line 76
vec4 RU_tex2D_MainTex_0(vec2 uv_1)
{

#line 76
    return (texture(_MainTex, (vec2(uv_1.x, 1.0 - uv_1.y))));
}


#line 76
layout(location = 0)
out vec4 entryPointParam_frag_0;


#line 76
layout(location = 1) in vec2 i_uv_0; // @vertex TexCoord0 optional


#line 76
layout(location = 2) in vec4 i_vertexColor_0; // @vertex Color optional


#line 95
void main()
{

#line 95
    vec4 mainTex_0 = RU_tex2D_MainTex_0(i_uv_0);

#line 95
    float _S1 = RU_tex2D_NoiseTex_0(i_uv_0 + RU_Material_0._Time.y * RU_Material_0._FlowSpeed.xy).x - 0.5;

#line 95
    entryPointParam_frag_0 = vec4(mainTex_0.xyz * i_vertexColor_0.xyz + _S1 * RU_Material_0._NoiseStrength * i_vertexColor_0.xyz, saturate_0(mainTex_0.w * i_vertexColor_0.w * RU_Material_0._AlphaBoost * (1.0 + _S1 * RU_Material_0._NoiseStrength)));

#line 95
    return;
}


// @rimunity-host-matrices 1
