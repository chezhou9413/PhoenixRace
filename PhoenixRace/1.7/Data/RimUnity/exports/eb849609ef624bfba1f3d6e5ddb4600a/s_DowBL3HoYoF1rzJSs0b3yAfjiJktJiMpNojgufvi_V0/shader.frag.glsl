#version 450


#line 66 0
layout(binding = 1, set = 2)
uniform sampler2D _MainTex; // @default = white


#line 68



#line 56
struct RU_MaterialData_0
{
    vec4 _Color; // @default = vec4(1, 1, 1, 1) @color
    float _GlowMultiplier; // @default = 2
    vec4 _MainTex_ST; // @default = vec4(0, 0, 1, 1)
};


#line 61
layout(binding = 0, set = 2, std140) uniform block_RU_MaterialData_0
{
    vec4 _Color; // @default = vec4(1, 1, 1, 1) @color
    float _GlowMultiplier; // @default = 2
    vec4 _MainTex_ST; // @default = vec4(0, 0, 1, 1)
}RU_Material_0;


vec4 RU_tex2D_MainTex_0(vec2 uv_0)
{

#line 70
    return (texture(_MainTex, (vec2(uv_0.x, 1.0 - uv_0.y))));
}


#line 70
layout(location = 0)
out vec4 entryPointParam_frag_0;


#line 70
layout(location = 1) in vec2 i_uv_0; // @vertex TexCoord0 optional


#line 70
layout(location = 2) in vec4 i_color_0; // @vertex Color optional


#line 78
void main()
{

#line 78
    vec4 _S1 = RU_tex2D_MainTex_0(i_uv_0) * i_color_0;

#line 78
    vec4 col_0 = _S1;

#line 78
    col_0.xyz = _S1.xyz * RU_Material_0._GlowMultiplier;

#line 78
    entryPointParam_frag_0 = col_0;

#line 78
    return;
}


// @rimunity-host-matrices 1
