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
struct RU_CameraData { float4x4 ViewProj; };
[[vk::binding(0, 0)]] ConstantBuffer<RU_CameraData> RU_Camera;
struct RU_ObjectData { float4x4 model; };
[[vk::push_constant]] ConstantBuffer<RU_ObjectData> RU_Object;
#define unity_ObjectToWorld RU_Object.model
#define UNITY_MATRIX_M RU_Object.model
#define UNITY_MATRIX_VP RU_Camera.ViewProj
#define UNITY_MATRIX_MVP mul(RU_Camera.ViewProj, RU_Object.model)
float4 UnityObjectToClipPos(float4 p) { return mul(RU_Camera.ViewProj, mul(RU_Object.model, p)); }
float4 UnityObjectToClipPos(float3 p) { return UnityObjectToClipPos(float4(p, 1)); }
float3 TransformObjectToWorld(float3 p) { return mul(RU_Object.model, float4(p, 1)).xyz; }
float4 TransformWorldToHClip(float3 p) { return mul(RU_Camera.ViewProj, float4(p, 1)); }
float4 UnityWorldToClipPos(float3 p) { return TransformWorldToHClip(p); }
float3 UnityObjectToWorldNormal(float3 normal) {
    float3x3 m = (float3x3)RU_Object.model;
    float3x3 cofactors = float3x3(cross(m[1],m[2]), cross(m[2],m[0]), cross(m[0],m[1]));
    return normalize(mul(cofactors, normal) * sign(dot(m[0],cofactors[0])));
}
float3 UnityObjectToWorldDir(float3 direction) { return normalize(mul((float3x3)RU_Object.model, direction)); }
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
float _BreathMax;
float _BreathMin;
float _BreathSpeed;
float4 _CoreColor;
float _CorePower;
float4 _EdgeColor1;
float4 _EdgeColor2;
float _EdgeColorScale;
float _EdgeColorSpeed;
float _EdgeDistort;
float _EdgeNoiseScale;
float _EdgeNoiseSpeed;
float _EdgeSharp;
float _EdgeWidth;
float4 _FlashColor;
float _FlashSharp;
float _Intensity;
float _NoiseScale;
float _NoiseSpeed;
float _NoseSoft;
float4 _RimColor;
float _RimPower;
float4x4 _RimUnity_View;
float _RippleAmp;
float _RippleFreq;
float _RippleSpeed;
float4 _TailColor;
float _TailCutoff;
float _TailNoiseAmp;
float _TailNoiseScale;
float _TailNoiseSpeed;
float _TailSoftBase;
float4 _Time;
};
[[vk::binding(0, 2)]] ConstantBuffer<RU_MaterialData> RU_Material;
// @rimunity-material _BreathMax,_BreathMin,_BreathSpeed,_CoreColor,_CorePower,_EdgeColor1,_EdgeColor2,_EdgeColorScale,_EdgeColorSpeed,_EdgeDistort,_EdgeNoiseScale,_EdgeNoiseSpeed,_EdgeSharp,_EdgeWidth,_FlashColor,_FlashSharp,_Intensity,_NoiseScale,_NoiseSpeed,_NoseSoft,_RimColor,_RimPower,_RimUnity_View,_RippleAmp,_RippleFreq,_RippleSpeed,_TailColor,_TailCutoff,_TailNoiseAmp,_TailNoiseScale,_TailNoiseSpeed,_TailSoftBase,_Time
#define _NoiseSpeed (RU_Material._NoiseSpeed)
#define _NoiseScale (RU_Material._NoiseScale)
#define _TailNoiseAmp (RU_Material._TailNoiseAmp)
#define _TailNoiseSpeed (RU_Material._TailNoiseSpeed)
#define _TailNoiseScale (RU_Material._TailNoiseScale)
#define _TailSoftBase (RU_Material._TailSoftBase)
#define _TailCutoff (RU_Material._TailCutoff)
#define _EdgeColorScale (RU_Material._EdgeColorScale)
#define _EdgeColorSpeed (RU_Material._EdgeColorSpeed)
#define _EdgeSharp (RU_Material._EdgeSharp)
#define _EdgeWidth (RU_Material._EdgeWidth)
#define _EdgeDistort (RU_Material._EdgeDistort)
#define _EdgeNoiseSpeed (RU_Material._EdgeNoiseSpeed)
#define _EdgeNoiseScale (RU_Material._EdgeNoiseScale)
#define _RippleAmp (RU_Material._RippleAmp)
#define _RippleFreq (RU_Material._RippleFreq)
#define _RippleSpeed (RU_Material._RippleSpeed)
#define _FlashSharp (RU_Material._FlashSharp)
#define _BreathMax (RU_Material._BreathMax)
#define _BreathMin (RU_Material._BreathMin)
#define _BreathSpeed (RU_Material._BreathSpeed)
#define _Intensity (RU_Material._Intensity)
#define _NoseSoft (RU_Material._NoseSoft)
#define _RimPower (RU_Material._RimPower)
#define _CorePower (RU_Material._CorePower)
#define _EdgeColor2 (RU_Material._EdgeColor2)
#define _EdgeColor1 (RU_Material._EdgeColor1)
#define _TailColor (RU_Material._TailColor)
#define _FlashColor (RU_Material._FlashColor)
#define _RimColor (RU_Material._RimColor)
#define _CoreColor (RU_Material._CoreColor)
#define _Time (RU_Material._Time)
#define _RimUnity_View (RU_Material._RimUnity_View)

                               struct appdata { [[vk::location(0)]] float4 vertex : POSITION ; [[vk::location(3)]] float3 normal : NORMAL ; [[vk::location(1)]] float2 uv : TEXCOORD0 ; } ; struct v2f { float4 pos : SV_POSITION ; [[vk::location(1)]] float3 worldNormal : TEXCOORD0 ; [[vk::location(5)]] float3 worldPos : TEXCOORD1 ; [[vk::location(6)]] float axisT : TEXCOORD2 ; [[vk::location(7)]] float fresnel : TEXCOORD3 ; } ; float hash2 ( float2 p ) { return frac ( sin ( dot ( p , float2 ( 127.1 , 311.7 ) ) ) * 43758.5453 ) ; } float hash3 ( float3 p ) { return frac ( sin ( dot ( p , float3 ( 127.1 , 311.7 , 74.7 ) ) ) * 43758.5453 ) ; } float vnoise2 ( float2 p ) { float2 i = floor ( p ) ; float2 f = frac ( p ) ; float2 u = f * f * ( 3.0 - 2.0 * f ) ; return lerp ( lerp ( hash2 ( i ) , hash2 ( i + float2 ( 1 , 0 ) ) , u . x ) , lerp ( hash2 ( i + float2 ( 0 , 1 ) ) , hash2 ( i + float2 ( 1 , 1 ) ) , u . x ) , u . y ) ; } float vnoise3 ( float3 p ) { float3 i = floor ( p ) ; float3 f = frac ( p ) ; float3 u = f * f * ( 3.0 - 2.0 * f ) ; return lerp ( lerp ( lerp ( hash3 ( i ) , hash3 ( i + float3 ( 1 , 0 , 0 ) ) , u . x ) , lerp ( hash3 ( i + float3 ( 0 , 1 , 0 ) ) , hash3 ( i + float3 ( 1 , 1 , 0 ) ) , u . x ) , u . y ) , lerp ( lerp ( hash3 ( i + float3 ( 0 , 0 , 1 ) ) , hash3 ( i + float3 ( 1 , 0 , 1 ) ) , u . x ) , lerp ( hash3 ( i + float3 ( 0 , 1 , 1 ) ) , hash3 ( i + float3 ( 1 , 1 , 1 ) ) , u . x ) , u . y ) , u . z ) ; } float fbm3 ( float3 p ) { return vnoise3 ( p ) * 0.55 + vnoise3 ( p * 2.1 + 1.7 ) * 0.30 + vnoise3 ( p * 4.3 + 3.2 ) * 0.15 ; } float fbm2 ( float2 p ) { return vnoise2 ( p ) * 0.60 + vnoise2 ( p * 2.0 + 5.3 ) * 0.25 + vnoise2 ( p * 4.1 + 11.7 ) * 0.15 ; } v2f vert ( appdata v ) { v2f o ; o . pos = UnityObjectToClipPos ( v . vertex ) ; o . worldPos = mul ( unity_ObjectToWorld , v . vertex ) . xyz ; float3 wn = UnityObjectToWorldNormal ( v . normal ) ; o . worldNormal = wn ; o . axisT = v . uv . y ; float3 camFwd = - normalize ( float3 ( _RimUnity_View [ 2 ] [ 0 ] , _RimUnity_View [ 2 ] [ 1 ] , _RimUnity_View [ 2 ] [ 2 ] ) ) ; o . fresnel = 1.0 - saturate ( dot ( normalize ( wn ) , camFwd ) ) ; return o ; } float4 frag ( v2f i ) : SV_Target { float fresnel = i . fresnel ; float axisT = i . axisT ; float3 wpos = i . worldPos ; float t = _Time . y ; float2 tailCircleCoord = float2 ( wpos . y , wpos . z ) * _TailNoiseScale ; float ang = t * _TailNoiseSpeed ; float ca = cos ( ang ) ; float sa = sin ( ang ) ; float2 rotatedCoord = float2 ( tailCircleCoord . x * ca - tailCircleCoord . y * sa , tailCircleCoord . x * sa + tailCircleCoord . y * ca ) ; float tailNoise = fbm2 ( rotatedCoord ) ; float cutoffOffset = ( tailNoise - 0.5 ) * _TailNoiseAmp ; float dynamicCutoff = _TailCutoff + cutoffOffset ; float dynamicSoft = _TailSoftBase + ( tailNoise - 0.5 ) * _TailSoftBase * 0.6 ; dynamicSoft = max ( 0.01 , dynamicSoft ) ; float tailMask = smoothstep ( dynamicCutoff - dynamicSoft , dynamicCutoff + dynamicSoft , axisT ) ; float tailEdgeW = dynamicSoft * 1.2 ; float tailEdge = smoothstep ( dynamicCutoff - tailEdgeW , dynamicCutoff , axisT ) * ( 1.0 - smoothstep ( dynamicCutoff , dynamicCutoff + tailEdgeW * 0.4 , axisT ) ) ; tailEdge = saturate ( tailEdge ) ; float noseMask = smoothstep ( 1.0 , 1.0 - _NoseSoft , axisT ) ; float alphaMask = tailMask * noseMask ; float3 edgeNoiseCoord = wpos * _EdgeNoiseScale + float3 ( t * _EdgeNoiseSpeed , t * _EdgeNoiseSpeed * 0.7 , - t * _EdgeNoiseSpeed * 1.3 ) ; float edgeNoise = fbm3 ( edgeNoiseCoord ) ; float distortedFresnel = saturate ( fresnel - ( edgeNoise - 0.5 ) * _EdgeDistort ) ; float edgeLow = 1.0 - _EdgeWidth ; float edgeBand = smoothstep ( edgeLow - 0.05 , edgeLow , distortedFresnel ) - smoothstep ( 0.95 , 1.0 , distortedFresnel ) ; edgeBand = saturate ( pow ( edgeBand , 1.0 / _EdgeSharp ) ) ; float3 colorNoiseCoord = wpos * _EdgeColorScale + float3 ( - t * _EdgeColorSpeed , t * _EdgeColorSpeed * 1.1 , - t * _EdgeColorSpeed * 0.6 ) ; float colorNoise = fbm3 ( colorNoiseCoord ) ; float s = sin ( t * _BreathSpeed * 3.14159 ) * 0.5 + 0.5 ; float breath = pow ( s , _FlashSharp ) ; float breathM = lerp ( _BreathMin , _BreathMax , breath ) ; float3 edgeColor = lerp ( _EdgeColor1 . rgb , _EdgeColor2 . rgb , saturate ( colorNoise + breath * 0.2 ) ) ; float extremeEdge = pow ( saturate ( distortedFresnel ) , 6.0 ) ; edgeColor = lerp ( edgeColor , _FlashColor . rgb , extremeEdge * breath * 0.6 ) ; float rippleVal = sin ( axisT * _RippleFreq - t * _RippleSpeed ) * 0.5 + 0.5 ; rippleVal *= pow ( fresnel , 1.5 ) * _RippleAmp ; float2 surfUV = float2 ( axisT * 2.0 , wpos . y + wpos . z ) * _NoiseScale + float2 ( t * _NoiseSpeed , t * _NoiseSpeed * 0.8 ) ; float surfNoise = fbm2 ( surfUV ) * 0.4 ; float core = pow ( 1.0 - distortedFresnel , _CorePower ) ; float rim = pow ( distortedFresnel , _RimPower ) ; float3 baseColor = _CoreColor . rgb * core + _RimColor . rgb * rim ; float3 rippleColor = lerp ( baseColor , _FlashColor . rgb , rippleVal + surfNoise ) ; float tailBlend = saturate ( ( 1.0 - axisT ) * ( 1.0 - tailMask + 0.3 ) ) ; float3 bodyColor = lerp ( rippleColor , _TailColor . rgb , tailBlend * 0.5 ) ; float3 tailGlowColor = lerp ( _TailColor . rgb , _FlashColor . rgb , breath * 0.4 ) ; bodyColor += tailGlowColor * tailEdge * 1.8 ; float3 finalColor = bodyColor + edgeColor * edgeBand * 1.5 ; finalColor += _FlashColor . rgb * breath * 0.3 * rim ; finalColor *= _Intensity ; float alpha = ( core * 0.6 + rim * 0.4 + rippleVal * 0.2 + edgeBand * 0.3 + tailEdge * 0.25 ) * alphaMask ; return float4 ( finalColor * alpha , alpha ) ; } 
