// @rimunity-vertex 0 Position required
// @rimunity-vertex 1 TexCoord0 optional
// @rimunity-vertex 2 Color optional
// @rimunity-vertex 3 Normal optional
// @rimunity-vertex 4 Tangent optional
// @rimunity-vertex 5 TexCoord1 optional
// @rimunity-vertex 6 TexCoord2 optional
// @rimunity-vertex 7 TexCoord3 optional
// @rimunity-vertex 8 TexCoord4 optional
// @rimunity-vertex 9 TexCoord5 optional
// @rimunity-vertex 10 TexCoord6 optional
// @rimunity-vertex 11 TexCoord7 optional
// @rimunity-vertex 12 BlendWeight optional
// @rimunity-vertex 13 BlendIndices optional
struct RU_FrameData { float4 sunDirection; float4x4 worldToCamera; float4x4 viewProj; };
[[vk::binding(1, 0)]] ConstantBuffer<RU_FrameData> RU_Frame;
#define UNITY_MATRIX_VP RU_Frame.viewProj
#define UNITY_MATRIX_MVP RU_Frame.viewProj
float4 UnityObjectToClipPos(float4 p) { return mul(RU_Frame.viewProj, p); }
float4 UnityObjectToClipPos(float3 p) { return UnityObjectToClipPos(float4(p, 1)); }
float4 TransformObjectToHClip(float3 p) { return UnityObjectToClipPos(p); }
float4 ComputeScreenPos(float4 p) { return float4(p.xy * 0.5 + p.w * 0.5, p.zw); }
#define TRANSFORM_TEX(uv,name) ((uv).xy * name##_ST.xy + name##_ST.zw)
#define UNITY_INITIALIZE_OUTPUT(type,name) name = (type)0
//非雾、非实例化、单眼变体在 Unity 中也不产生这些字段和调用。
#define UNITY_FOG_COORDS(index)
#define UNITY_TRANSFER_FOG(output,position)
#define UNITY_APPLY_FOG(coord,color)
#define UNITY_VERTEX_INPUT_INSTANCE_ID
#define UNITY_SETUP_INSTANCE_ID(input)
#define UNITY_TRANSFER_INSTANCE_ID(input,output)
#define UNITY_VERTEX_OUTPUT_STEREO
#define UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output)
#define SAMPLE_TEXTURE2D(tex,samp,uv) tex.Sample(samp,float2((uv).x,1-(uv).y))
#define SAMPLE_TEXTURE2D_LOD(tex,samp,uv,lod) tex.SampleLevel(samp,float2((uv).x,1-(uv).y),lod)
#define SAMPLE_TEXTURE2D_BIAS(tex,samp,uv,bias) tex.SampleBias(samp,float2((uv).x,1-(uv).y),bias)
float4 ComputeGrabScreenPos(float4 p) { float4 r = p * 0.5; r.xy = float2(r.x, -r.y) + r.w; r.zw = p.zw; return r; }
float3 UnpackNormal(float4 n) { n.x *= n.w; float2 xy = n.xy * 2 - 1; return float3(xy, sqrt(1 - saturate(dot(xy, xy)))); }
float UnityGet2DClipping(float2 p, float4 r) { float2 v = step(r.xy,p)*step(p,r.zw); return v.x*v.y; }
struct appdata_base { [[vk::location(0)]] float4 vertex : POSITION; [[vk::location(3)]] float3 normal : NORMAL; [[vk::location(1)]] float4 texcoord : TEXCOORD0; };
struct appdata_tan { [[vk::location(0)]] float4 vertex : POSITION; [[vk::location(4)]] float4 tangent : TANGENT; [[vk::location(3)]] float3 normal : NORMAL; [[vk::location(1)]] float4 texcoord : TEXCOORD0; };
struct appdata_full { [[vk::location(0)]] float4 vertex : POSITION; [[vk::location(4)]] float4 tangent : TANGENT; [[vk::location(3)]] float3 normal : NORMAL; [[vk::location(1)]] float4 texcoord : TEXCOORD0; [[vk::location(5)]] float4 texcoord1 : TEXCOORD1; [[vk::location(6)]] float4 texcoord2 : TEXCOORD2; [[vk::location(7)]] float4 texcoord3 : TEXCOORD3; [[vk::location(2)]] float4 color : COLOR; };
struct RU_MaterialData {
float4 _AtmosphereColor;
float _AtmosphereStrength;
float _AtmosphereWidth;
float4 _ClipRect;
float _EdgeBlur;
float _Exposure;
float4 _MainTex_TexelSize;
float _Midtone;
float4 _PlanetCenter;
float4 _ViewPlane;
};
[[vk::binding(0, 2)]] ConstantBuffer<RU_MaterialData> RU_Material;
// @rimunity-material _AtmosphereColor,_AtmosphereStrength,_AtmosphereWidth,_ClipRect,_EdgeBlur,_Exposure,_MainTex_TexelSize,_Midtone,_PlanetCenter,_ViewPlane
#define _ClipRect (RU_Material._ClipRect)
#define _EdgeBlur (RU_Material._EdgeBlur)
#define _AtmosphereStrength (RU_Material._AtmosphereStrength)
#define _AtmosphereWidth (RU_Material._AtmosphereWidth)
#define _AtmosphereColor (RU_Material._AtmosphereColor)
#define _ViewPlane (RU_Material._ViewPlane)
#define _PlanetCenter (RU_Material._PlanetCenter)
#define _MainTex_TexelSize (RU_Material._MainTex_TexelSize)
#define _Midtone (RU_Material._Midtone)
#define _Exposure (RU_Material._Exposure)
[[vk::binding(1, 2)]] Texture2D _MainTex;
// @rimunity-texture 1 _MainTex
[[vk::binding(2, 2)]] SamplerState RU_Sampler_MainTex;
// @rimunity-combined 1 2
float4 RU_tex2D_MainTex(float2 uv) { return _MainTex.Sample(RU_Sampler_MainTex, float2(uv.x,1-uv.y)); }
float4 RU_tex2D_MainTex(float3 uv) { return RU_tex2D_MainTex(uv.xy); }
float4 RU_tex2D_MainTex(float4 uv) { return RU_tex2D_MainTex(uv.xy); }
float4 RU_tex2Dlod_MainTex(float4 uv) { return _MainTex.SampleLevel(RU_Sampler_MainTex, float2(uv.x,1-uv.y),uv.w); }
float4 RU_tex2Dbias_MainTex(float4 uv) { return _MainTex.SampleBias(RU_Sampler_MainTex, float2(uv.x,1-uv.y),uv.w); }
float4 RU_tex2Dproj_MainTex(float4 uv) { return _MainTex.Sample(RU_Sampler_MainTex, float2(uv.x/uv.w,1-uv.y/uv.w)); }
float4 RU_tex2Dgrad_MainTex(float2 uv,float2 dx,float2 dy) { return _MainTex.SampleGrad(RU_Sampler_MainTex,float2(uv.x,1-uv.y),float2(dx.x,-dx.y),float2(dy.x,-dy.y)); }

           struct VertexInput { [[vk::location(0)]] float4 vertex : POSITION ; [[vk::location(1)]] float2 uv : TEXCOORD0 ; } ; struct VertexOutput { float4 vertex : SV_POSITION ; [[vk::location(1)]] float2 uv : TEXCOORD0 ; [[vk::location(5)]] float2 position : TEXCOORD1 ; } ; VertexOutput vert ( VertexInput input ) { VertexOutput output ; output . vertex = UnityObjectToClipPos ( input . vertex ) ; output . uv = input . uv ; output . position = input . vertex . xy ; return output ; } float AtmosphereBand ( float2 uv ) { float3 ray = normalize ( float3 ( ( uv * 2.0 - 1.0 ) * _ViewPlane . xy , 1.0 ) ) ; float distanceToAxis = length ( cross ( _PlanetCenter . xyz , ray ) ) ; float edge = ( distanceToAxis - 1.0 ) / _AtmosphereWidth ; edge *= edge < 0.0 ? 0.55 : 1.0 ; return exp ( - edge * edge ) * step ( 0.0 , dot ( _PlanetCenter . xyz , ray ) ) ; } float3 SampleSoftEdge ( float2 uv , float band ) { float2 offset = abs ( _MainTex_TexelSize . xy ) * _EdgeBlur ; float3 center = RU_tex2D_MainTex( uv ) . rgb ; float3 blurred = center * 0.4 ; blurred += RU_tex2D_MainTex( uv + float2 ( offset . x , 0 ) ) . rgb * 0.15 ; blurred += RU_tex2D_MainTex( uv - float2 ( offset . x , 0 ) ) . rgb * 0.15 ; blurred += RU_tex2D_MainTex( uv + float2 ( 0 , offset . y ) ) . rgb * 0.15 ; blurred += RU_tex2D_MainTex( uv - float2 ( 0 , offset . y ) ) . rgb * 0.15 ; return lerp ( center , blurred , band ) ; } float4 frag ( VertexOutput input ) : SV_Target { float band = AtmosphereBand ( input . uv ) ; float3 color = SampleSoftEdge ( input . uv , band ) ; color = saturate ( pow ( max ( color , 0.0 ) , _Midtone ) * _Exposure ) ; color = 1.0 - ( 1.0 - color ) * ( 1.0 - _AtmosphereColor . rgb * band * _AtmosphereStrength ) ; float alpha = 1.0 ; return float4 ( color , alpha ) ; } 
