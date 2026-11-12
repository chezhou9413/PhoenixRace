#version 450


#line 68 0
layout(binding = 1, set = 2)
uniform sampler2D _MainTex; // @default = white


#line 70



#line 79
layout(binding = 3, set = 2)
uniform sampler2D _NoiseTex; // @default = white


#line 81



#line 56
struct RU_MaterialData_0
{
    vec4 _Color; // @default = vec4(1, 1, 1, 1) @color
    float _DissolveAmount; // @default = 0.5
    vec4 _EdgeColor; // @default = vec4(0, 1, 1, 1) @color
    float _EdgeWidth; // @default = 0.05
};


#line 62
layout(binding = 0, set = 2, std140) uniform block_RU_MaterialData_0
{
    vec4 _Color; // @default = vec4(1, 1, 1, 1) @color
    float _DissolveAmount; // @default = 0.5
    vec4 _EdgeColor; // @default = vec4(0, 1, 1, 1) @color
    float _EdgeWidth; // @default = 0.05
}RU_Material_0;

#line 9392 1
void clip_0(float x_0)
{

#line 9398
    if(x_0 < 0.0)
    {

#line 9398
        discard;

#line 9398
    }

    return;
}


#line 72 0
vec4 RU_tex2D_MainTex_0(vec2 uv_0)
{

#line 72
    return (texture(_MainTex, (vec2(uv_0.x, 1.0 - uv_0.y))));
}


#line 83
vec4 RU_tex2D_NoiseTex_0(vec2 uv_1)
{

#line 83
    return (texture(_NoiseTex, (vec2(uv_1.x, 1.0 - uv_1.y))));
}


#line 83
layout(location = 0)
out vec4 entryPointParam_frag_0;


#line 83
layout(location = 2) in vec4 IN_color_0; // @vertex Color optional


#line 83
layout(location = 1) in vec2 IN_texcoord_0; // @vertex TexCoord0 optional


#line 91
void main()
{

#line 91
    vec4 _S1 = RU_tex2D_MainTex_0(IN_texcoord_0) * IN_color_0;

#line 91
    vec4 c_0 = _S1;

#line 91
    float noise_0 = RU_tex2D_NoiseTex_0(IN_texcoord_0).x;

#line 91
    clip_0(noise_0 - RU_Material_0._DissolveAmount);

#line 91
    c_0.xyz = _S1.xyz + RU_Material_0._EdgeColor.xyz * step(noise_0, RU_Material_0._DissolveAmount + RU_Material_0._EdgeWidth) * _S1.w;

#line 91
    entryPointParam_frag_0 = c_0;

#line 91
    return;
}


// @rimunity-host-matrices 1
