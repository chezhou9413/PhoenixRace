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
float4 _Color;
float _Grid;
float4 _RimColor;
float4 _Time;
float3 _WorldSpaceCameraPos;
};
[[vk::binding(0, 2)]] ConstantBuffer<RU_MaterialData> RU_Material;
// @rimunity-material _Color,_Grid,_RimColor,_Time,_WorldSpaceCameraPos
#define _Grid (RU_Material._Grid)
#define _RimColor (RU_Material._RimColor)
#define _Color (RU_Material._Color)
#define _Time (RU_Material._Time)
#define _WorldSpaceCameraPos (RU_Material._WorldSpaceCameraPos)

struct Input { [[vk::location(0)]] float4 vertex : POSITION ; [[vk::location(3)]] float3 normal : NORMAL ; [[vk::location(2)]] float4 color : COLOR ; } ; struct Output { float4 pos : SV_POSITION ; [[vk::location(1)]] float3 local : TEXCOORD0 ; [[vk::location(5)]] float3 normal : TEXCOORD1 ; [[vk::location(6)]] float3 view : TEXCOORD2 ; [[vk::location(2)]] float4 color : COLOR ; } ; 
  Output vert ( Input v ) { Output o ; o . pos = UnityObjectToClipPos ( v . vertex ) ; o . local = v . vertex . xyz ; o . normal = UnityObjectToWorldNormal ( v . normal ) ; o . view = ( _WorldSpaceCameraPos - ( mul ( unity_ObjectToWorld , v . vertex ) . xyz ) . xyz ) ; o . color = v . color ; return o ; } float4 frag ( Output i ) : SV_Target { float3 n = normalize ( i . normal ) ; float rim = pow ( 1 - saturate ( dot ( n , normalize ( i . view ) ) ) , 3 ) ; float light = 0.22 + 0.78 * saturate ( dot ( n , normalize ( float3 ( - 0.5 , 0.7 , - 0.8 ) ) ) ) ; float3 p = normalize ( i . local ) ; float2 uv = float2 ( atan2 ( p . z , p . x ) * 3.81972 , asin ( p . y ) * 7.63944 ) ; float2 grid = abs ( frac ( uv - 0.5 ) - 0.5 ) / max ( fwidth ( uv ) , 0.001 ) ; float lines = 1 - saturate ( min ( grid . x , grid . y ) ) ; float scan = pow ( saturate ( 1 - abs ( frac ( p . y * 0.5 + 0.5 - _Time . y * 0.12 ) - 0.5 ) * 24 ) , 3 ) ; float3 baseColor = _Color . rgb * i . color . rgb ; float3 color = baseColor * light + _RimColor . rgb * ( rim * 0.85 + lines * 0.14 * _Grid + scan * 0.16 ) ; return float4 ( color , 1 ) ; } 
