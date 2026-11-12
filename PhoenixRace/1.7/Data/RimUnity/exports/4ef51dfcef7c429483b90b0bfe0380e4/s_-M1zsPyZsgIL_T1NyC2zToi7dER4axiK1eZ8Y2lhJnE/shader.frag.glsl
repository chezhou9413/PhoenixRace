#version 450


#line 56 0
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

#line 74
layout(binding = 1, set = 2)
uniform sampler2D _MainTex; // @default = white


#line 76



#line 86
float random2d_0(vec2 n_0)
{

#line 86
    return fract(sin(dot(n_0, vec2(12.98980045318603516, 4.14139986038208008))) * 43758.546875);
}


#line 78
vec4 RU_tex2D_MainTex_0(vec2 uv_0)
{

#line 78
    return (texture(_MainTex, (vec2(uv_0.x, 1.0 - uv_0.y))));
}


#line 78
layout(location = 0)
out vec4 entryPointParam_frag_0;


#line 78
layout(location = 2) in vec4 IN_color_0; // @vertex Color optional


#line 78
layout(location = 1) in vec2 IN_texcoord_0; // @vertex TexCoord0 optional


#line 86
void main()
{

#line 86
    float time_0 = RU_Material_0._Time.y * RU_Material_0._Speed;

#line 86
    vec2 blockPos_0 = floor(IN_texcoord_0 * RU_Material_0._BlockSize);

#line 86
    float randNoise_0 = random2d_0(blockPos_0 + vec2(time_0, time_0));

#line 86
    float isGlitching_0 = step(0.85000002384185791, randNoise_0);

#line 86
    vec2 displacedUV_0 = IN_texcoord_0 + (vec2(randNoise_0, random2d_0(blockPos_0 + time_0 * 1.5)) - 0.5) * 2.0 * (RU_Material_0._GlitchIntensity * isGlitching_0);

#line 86
    vec2 _S1 = vec2(RU_Material_0._ColorSplit * isGlitching_0, 0.0);

#line 86
    vec4 colG_0 = RU_tex2D_MainTex_0(displacedUV_0);

#line 86
    entryPointParam_frag_0 = vec4(RU_tex2D_MainTex_0(displacedUV_0 + _S1).x, colG_0.y, RU_tex2D_MainTex_0(displacedUV_0 - _S1).z, colG_0.w) * IN_color_0;

#line 86
    return;
}


// @rimunity-host-matrices 1
