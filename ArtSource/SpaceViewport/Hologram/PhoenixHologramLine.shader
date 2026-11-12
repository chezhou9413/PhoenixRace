Shader "PhoenixRace/HologramLine"
{
    Properties { _Color ("光线颜色", Color) = (0.05,0.8,1,0.6) }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha One
            ZWrite Off Cull Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct Input { float4 vertex:POSITION; float4 color:COLOR; };
            struct Output { float4 pos:SV_POSITION; float4 color:COLOR; };
            float4 _Color;
            //转换轨道和定位线顶点并保留各顶点亮度。
            Output vert(Input v) { Output o; o.pos=UnityObjectToClipPos(v.vertex); o.color=v.color*_Color; return o; }
            //输出叠加光线，保留不透明球面对背面轨道的遮挡。
            fixed4 frag(Output i):SV_Target { return i.color; }
            ENDCG
        }
    }
}
