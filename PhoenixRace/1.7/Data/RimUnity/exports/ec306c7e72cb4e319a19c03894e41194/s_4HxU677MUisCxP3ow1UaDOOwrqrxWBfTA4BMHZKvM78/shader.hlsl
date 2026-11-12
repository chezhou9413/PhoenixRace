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
float _BaseFresnelIntensity;
float _BaseFresnelPower;
float4 _BreakTint;
float4 _Color;
float _Dissolve;
float _DissolveSoftness;
float _HexIntensity;
float _HexScale;
float4 _ImpactColor;
float _ImpactFadePower;
float _ImpactHexShape;
float _ImpactIntensity;
float _ImpactMaxRadius;
float4 _ImpactPoint0;
float4 _ImpactPoint1;
float4 _ImpactPoint2;
float4 _ImpactPoint3;
float4 _ImpactPoint4;
float4 _ImpactPoint5;
float4 _ImpactPoint6;
float4 _ImpactPoint7;
float4 _ImpactRedColor;
float _ImpactRedDuration;
float _ImpactRedIntensity;
float _ImpactRedRadius;
float _ImpactRingWidth;
float _ImpactSpeed;
float _LineThickness;
float2 _NoiseFlowSpeed;
float _NoiseIntensity;
float _NoiseScale;
float4x4 _RimUnity_View;
float _ShatterGlow;
float _SpreadSpeed;
float _SpreadWidth;
float4 _Time;
float3 _WorldSpaceCameraPos;
float4 unity_OrthoParams;
};
[[vk::binding(0, 2)]] ConstantBuffer<RU_MaterialData> RU_Material;
// @rimunity-material _BaseFresnelIntensity,_BaseFresnelPower,_BreakTint,_Color,_Dissolve,_DissolveSoftness,_HexIntensity,_HexScale,_ImpactColor,_ImpactFadePower,_ImpactHexShape,_ImpactIntensity,_ImpactMaxRadius,_ImpactPoint0,_ImpactPoint1,_ImpactPoint2,_ImpactPoint3,_ImpactPoint4,_ImpactPoint5,_ImpactPoint6,_ImpactPoint7,_ImpactRedColor,_ImpactRedDuration,_ImpactRedIntensity,_ImpactRedRadius,_ImpactRingWidth,_ImpactSpeed,_LineThickness,_NoiseFlowSpeed,_NoiseIntensity,_NoiseScale,_RimUnity_View,_ShatterGlow,_SpreadSpeed,_SpreadWidth,_Time,_WorldSpaceCameraPos,unity_OrthoParams
#define _ImpactPoint7 (RU_Material._ImpactPoint7)
#define _ImpactPoint6 (RU_Material._ImpactPoint6)
#define _ImpactPoint5 (RU_Material._ImpactPoint5)
#define _ImpactPoint4 (RU_Material._ImpactPoint4)
#define _ImpactPoint3 (RU_Material._ImpactPoint3)
#define _ImpactPoint2 (RU_Material._ImpactPoint2)
#define _ImpactPoint1 (RU_Material._ImpactPoint1)
#define _ImpactPoint0 (RU_Material._ImpactPoint0)
#define _ImpactRedIntensity (RU_Material._ImpactRedIntensity)
#define _ImpactRedDuration (RU_Material._ImpactRedDuration)
#define _ImpactRedRadius (RU_Material._ImpactRedRadius)
#define _ImpactRedColor (RU_Material._ImpactRedColor)
#define _ImpactHexShape (RU_Material._ImpactHexShape)
#define _ImpactFadePower (RU_Material._ImpactFadePower)
#define _ImpactIntensity (RU_Material._ImpactIntensity)
#define _ImpactRingWidth (RU_Material._ImpactRingWidth)
#define _ImpactMaxRadius (RU_Material._ImpactMaxRadius)
#define _ImpactSpeed (RU_Material._ImpactSpeed)
#define _ImpactColor (RU_Material._ImpactColor)
#define _NoiseIntensity (RU_Material._NoiseIntensity)
#define _NoiseFlowSpeed (RU_Material._NoiseFlowSpeed)
#define _NoiseScale (RU_Material._NoiseScale)
#define _BreakTint (RU_Material._BreakTint)
#define _DissolveSoftness (RU_Material._DissolveSoftness)
#define _ShatterGlow (RU_Material._ShatterGlow)
#define _Dissolve (RU_Material._Dissolve)
#define _SpreadWidth (RU_Material._SpreadWidth)
#define _SpreadSpeed (RU_Material._SpreadSpeed)
#define _BaseFresnelIntensity (RU_Material._BaseFresnelIntensity)
#define _BaseFresnelPower (RU_Material._BaseFresnelPower)
#define _Color (RU_Material._Color)
#define _HexIntensity (RU_Material._HexIntensity)
#define _LineThickness (RU_Material._LineThickness)
#define _HexScale (RU_Material._HexScale)
#define _Time (RU_Material._Time)
#define _WorldSpaceCameraPos (RU_Material._WorldSpaceCameraPos)
#define unity_OrthoParams (RU_Material.unity_OrthoParams)
#define _RimUnity_View (RU_Material._RimUnity_View)
[[vk::binding(1, 2)]] Texture2D _NoiseTex;
// @rimunity-texture 1 _NoiseTex
[[vk::binding(2, 2)]] SamplerState RU_Sampler_NoiseTex;
// @rimunity-combined 1 2
float4 RU_tex2D_NoiseTex(float2 uv) { return _NoiseTex.Sample(RU_Sampler_NoiseTex, float2(uv.x,1-uv.y)); }
float4 RU_tex2D_NoiseTex(float3 uv) { return RU_tex2D_NoiseTex(uv.xy); }
float4 RU_tex2D_NoiseTex(float4 uv) { return RU_tex2D_NoiseTex(uv.xy); }
float4 RU_tex2Dlod_NoiseTex(float4 uv) { return _NoiseTex.SampleLevel(RU_Sampler_NoiseTex, float2(uv.x,1-uv.y),uv.w); }
float4 RU_tex2Dbias_NoiseTex(float4 uv) { return _NoiseTex.SampleBias(RU_Sampler_NoiseTex, float2(uv.x,1-uv.y),uv.w); }
float4 RU_tex2Dproj_NoiseTex(float4 uv) { return _NoiseTex.Sample(RU_Sampler_NoiseTex, float2(uv.x/uv.w,1-uv.y/uv.w)); }
float4 RU_tex2Dgrad_NoiseTex(float2 uv,float2 dx,float2 dy) { return _NoiseTex.SampleGrad(RU_Sampler_NoiseTex,float2(uv.x,1-uv.y),float2(dx.x,-dx.y),float2(dy.x,-dy.y)); }

struct appdata { [[vk::location(0)]] float4 vertex : POSITION ; [[vk::location(3)]] float3 normal : NORMAL ; } ; struct v2f { float4 vertex : SV_POSITION ; [[vk::location(3)]] float3 worldNormal : NORMAL ; [[vk::location(1)]] float3 worldPos : TEXCOORD0 ; [[vk::location(5)]] float2 viewUV : TEXCOORD1 ; } ; 

  
 
 

                 


 


 v2f vert ( appdata v ) { v2f o ; o . vertex = UnityObjectToClipPos ( v . vertex ) ; o . worldNormal = UnityObjectToWorldNormal ( v . normal ) ; o . worldPos = mul ( unity_ObjectToWorld , v . vertex ) . xyz ; float3 viewNormal = mul ( ( float3x3 ) _RimUnity_View , o . worldNormal ) ; o . viewUV = viewNormal . xy * 0.5 + 0.5 ; return o ; } float HexDist ( float2 p , float2 center ) { float2 d = abs ( p - center ) ; return max ( d . x , dot ( d , float2 ( 1.0 , 1.73205 ) ) * 0.5 ) / 0.866 ; } float CalculateImpactWave ( float2 currentUV , float4 impactData ) { if ( impactData . w <= 0.001 ) return 0.0 ; float2 impactUV = impactData . xy ; float startTime = impactData . z ; float intensity = impactData . w ; float lifeTime = _Time . y - startTime ; if ( lifeTime < 0.0 ) return 0.0 ; float currentRadius = lifeTime * _ImpactSpeed ; if ( currentRadius > _ImpactMaxRadius ) return 0.0 ; float distToImpact ; if ( _ImpactHexShape > 0.5 ) distToImpact = HexDist ( currentUV , impactUV ) ; else distToImpact = distance ( currentUV , impactUV ) ; float ring = 1.0 - saturate ( abs ( distToImpact - currentRadius ) / _ImpactRingWidth ) ; ring = smoothstep ( 0.0 , 1.0 , ring ) ; float lifeFade = 1.0 - saturate ( currentRadius / _ImpactMaxRadius ) ; lifeFade = pow ( lifeFade , _ImpactFadePower ) ; return ring * lifeFade * intensity ; } float CalculateImpactRed ( float2 currentUV , float4 impactData ) { if ( impactData . w <= 0.001 ) return 0.0 ; float lifeTime = _Time . y - impactData . z ; if ( lifeTime < 0.0 || lifeTime > _ImpactRedDuration ) return 0.0 ; float d = distance ( currentUV , impactData . xy ) ; float spatial = 1.0 - saturate ( d / _ImpactRedRadius ) ; spatial = smoothstep ( 0.0 , 1.0 , spatial ) ; float timeFade = 1.0 - saturate ( lifeTime / _ImpactRedDuration ) ; timeFade = pow ( timeFade , 1.5 ) ; return spatial * timeFade * impactData . w ; } float4 frag ( v2f i ) : SV_Target { float2 p = i . viewUV * _HexScale ; float2 r = float2 ( 1.0 , 1.73205 ) ; float2 h = r * 0.5 ; float2 p1 = floor ( p / r ) * r + h ; float2 p2 = floor ( ( p - h ) / r ) * r + r ; bool isP1 = length ( p - p1 ) < length ( p - p2 ) ; float2 gv = isP1 ? p - p1 : p - p2 ; float2 centerUV = isP1 ? p1 : p2 ; float2 p_abs = abs ( gv ) ; float hexDist = max ( p_abs . x , dot ( p_abs , float2 ( 1.0 , 1.73205 ) ) * 0.5 ) ; float lineMask = smoothstep ( 0.5 - _LineThickness , 0.5 , hexDist ) ; float2 normCenter = centerUV / _HexScale ; float distFromCenter = distance ( normCenter , float2 ( 0.5 , 0.5 ) ) * 2.0 ; float isAlive = step ( _Dissolve , distFromCenter ) ; float dissolveActive = step ( 0.01 , _Dissolve ) ; float glowEdge = smoothstep ( _Dissolve + _DissolveSoftness , _Dissolve , distFromCenter ) ; float edgeGlow = glowEdge * isAlive * _ShatterGlow * dissolveActive ; float2 worldUV = i . worldPos . xz * _NoiseScale ; float2 flowUV = worldUV + _Time . y * _NoiseFlowSpeed ; float noise = RU_tex2D_NoiseTex( flowUV ) . r ; float energyFlow = noise * _NoiseIntensity ; float3 normal = normalize ( i . worldNormal ) ; float3 viewDir ; if ( unity_OrthoParams . w > 0.5 ) { viewDir = normalize ( float3 ( _RimUnity_View [ 0 ] . z , _RimUnity_View [ 1 ] . z , _RimUnity_View [ 2 ] . z ) ) ; } else { viewDir = normalize ( _WorldSpaceCameraPos . xyz - i . worldPos ) ; } float dotNV = saturate ( dot ( normal , viewDir ) ) ; float outwardDist = 1.0 - dotNV ; float fresnel = pow ( outwardDist , _BaseFresnelPower ) * _BaseFresnelIntensity ; float scan = frac ( outwardDist - _Time . y * _SpreadSpeed ) ; float scanMask = smoothstep ( 0.0 , 0.1 , scan ) * smoothstep ( _SpreadWidth , _SpreadWidth - 0.1 , scan ) ; float waveSum = 0.0 ; waveSum += CalculateImpactWave ( normCenter , _ImpactPoint0 ) ; waveSum += CalculateImpactWave ( normCenter , _ImpactPoint1 ) ; waveSum += CalculateImpactWave ( normCenter , _ImpactPoint2 ) ; waveSum += CalculateImpactWave ( normCenter , _ImpactPoint3 ) ; waveSum += CalculateImpactWave ( normCenter , _ImpactPoint4 ) ; waveSum += CalculateImpactWave ( normCenter , _ImpactPoint5 ) ; waveSum += CalculateImpactWave ( normCenter , _ImpactPoint6 ) ; waveSum += CalculateImpactWave ( normCenter , _ImpactPoint7 ) ; float redSum = 0.0 ; redSum += CalculateImpactRed ( i . viewUV , _ImpactPoint0 ) ; redSum += CalculateImpactRed ( i . viewUV , _ImpactPoint1 ) ; redSum += CalculateImpactRed ( i . viewUV , _ImpactPoint2 ) ; redSum += CalculateImpactRed ( i . viewUV , _ImpactPoint3 ) ; redSum += CalculateImpactRed ( i . viewUV , _ImpactPoint4 ) ; redSum += CalculateImpactRed ( i . viewUV , _ImpactPoint5 ) ; redSum += CalculateImpactRed ( i . viewUV , _ImpactPoint6 ) ; redSum += CalculateImpactRed ( i . viewUV , _ImpactPoint7 ) ; redSum = saturate ( redSum ) ; float4 col = _Color * fresnel ; float hexLines = ( lineMask * scanMask * _HexIntensity ) + ( lineMask * edgeGlow ) ; col += _Color * hexLines ; col += _Color * energyFlow * ( fresnel + 0.1 ) ; col . rgb += _ImpactColor . rgb * lineMask * waveSum * _ImpactIntensity ; col . a += waveSum * _ImpactIntensity * 0.15 ; float redStrength = redSum * _ImpactRedIntensity ; float redMask = lerp ( 0.3 , 1.0 , lineMask ) ; col . rgb += _ImpactRedColor . rgb * redStrength * redMask ; col . a += redSum * 0.25 ; col *= isAlive ; col . a = max ( col . a * isAlive , 0.0 ) ; col . rgb *= _BreakTint . rgb ; return col ; } 
