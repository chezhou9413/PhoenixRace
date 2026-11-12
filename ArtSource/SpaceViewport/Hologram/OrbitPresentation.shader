//将星球离屏画面作为不透明舷窗背景显示，并提升曝光和中间调。
Shader "PhoenixRace/OrbitPresentation"
{
    Properties
    {
        [PerRendererData] _MainTex ("星球画面", 2D) = "black" {}
        _Exposure ("曝光倍率", Float) = 1.3
        _Midtone ("中间调指数", Float) = 0.82
        _AtmosphereColor ("大气散射颜色", Color) = (0.24, 0.58, 1, 1)
        _AtmosphereWidth ("大气边缘厚度", Float) = 0.018
        _AtmosphereStrength ("大气辉光强度", Float) = 0.38
        _EdgeBlur ("边缘模糊像素", Float) = 2.4
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            sampler2D _MainTex;
            float _Exposure;
            float _Midtone;
            float4 _MainTex_TexelSize;
            float4 _PlanetCenter;
            float4 _ViewPlane;
            float4 _AtmosphereColor;
            float _AtmosphereWidth;
            float _AtmosphereStrength;
            float _EdgeBlur;
            float4 _ClipRect;
            //传递画布顶点、纹理坐标和局部裁切位置。
            struct VertexInput { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct VertexOutput { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; float2 position : TEXCOORD1; };
            //将画布顶点投影到屏幕，保留裁切所需的位置。
            VertexOutput vert(VertexInput input)
            {
                VertexOutput output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                output.position = input.vertex.xy;
                return output;
            }
            //依据镜头射线到球心的最近距离，计算随真实地平线移动的大气带。
            float AtmosphereBand(float2 uv)
            {
                float3 ray = normalize(float3((uv * 2.0 - 1.0) * _ViewPlane.xy, 1.0));
                float distanceToAxis = length(cross(_PlanetCenter.xyz, ray));
                float edge = (distanceToAxis - 1.0) / _AtmosphereWidth;
                //地表一侧的薄雾更宽，外侧快速衰减到透明太空。
                edge *= edge < 0.0 ? 0.55 : 1.0;
                return exp(-edge * edge) * step(0.0, dot(_PlanetCenter.xyz, ray));
            }
            //只柔化地平线附近的纹理，避免将大陆内部和整幅舷窗一起模糊。
            float3 SampleSoftEdge(float2 uv, float band)
            {
                float2 offset = abs(_MainTex_TexelSize.xy) * _EdgeBlur;
                float3 center = tex2D(_MainTex, uv).rgb;
                float3 blurred = center * 0.4;
                blurred += tex2D(_MainTex, uv + float2(offset.x, 0)).rgb * 0.15;
                blurred += tex2D(_MainTex, uv - float2(offset.x, 0)).rgb * 0.15;
                blurred += tex2D(_MainTex, uv + float2(0, offset.y)).rgb * 0.15;
                blurred += tex2D(_MainTex, uv - float2(0, offset.y)).rgb * 0.15;
                return lerp(center, blurred, band);
            }
            //提亮地表并叠加柔和蓝色散射，保持背景不透明和太空暗部。
            float4 frag(VertexOutput input) : SV_Target
            {
                float band = AtmosphereBand(input.uv);
                float3 color = SampleSoftEdge(input.uv, band);
                color = saturate(pow(max(color, 0.0), _Midtone) * _Exposure);
                color = 1.0 - (1.0 - color) * (1.0 - _AtmosphereColor.rgb * band * _AtmosphereStrength);
                float alpha = 1.0;
                #ifdef UNITY_UI_CLIP_RECT
                alpha *= UnityGet2DClipping(input.position, _ClipRect);
                #endif
                return float4(color, alpha);
            }
            ENDCG
        }
    }
}
