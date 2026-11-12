Shader "PhoenixRace/HologramPlanet"
{
    Properties
    {
        _Color ("表面颜色", Color) = (0.03,0.3,0.4,1)
        _RimColor ("轮廓颜色", Color) = (0.1,0.9,1,1)
        _Grid ("经纬网强度", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Cull Back ZWrite On
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct Input { float4 vertex:POSITION; float3 normal:NORMAL; float4 color:COLOR; };
            struct Output { float4 pos:SV_POSITION; float3 local:TEXCOORD0; float3 normal:TEXCOORD1; float3 view:TEXCOORD2; float4 color:COLOR; };
            float4 _Color, _RimColor;
            float _Grid;
            //传递球面坐标、世界法线和观察方向，供全息明暗与扫描计算。
            Output vert(Input v)
            {
                Output o;
                o.pos=UnityObjectToClipPos(v.vertex);
                o.local=v.vertex.xyz;
                o.normal=UnityObjectToWorldNormal(v.normal);
                o.view=UnityWorldSpaceViewDir(mul(unity_ObjectToWorld,v.vertex).xyz);
                o.color=v.color;
                return o;
            }
            //结合地貌颜色、定向明暗、菲涅尔边缘和动态扫描显示立体全息表面。
            fixed4 frag(Output i):SV_Target
            {
                float3 n=normalize(i.normal);
                float rim=pow(1-saturate(dot(n,normalize(i.view))),3);
                float light=0.22+0.78*saturate(dot(n,normalize(float3(-0.5,0.7,-0.8))));
                float3 p=normalize(i.local);
                float2 uv=float2(atan2(p.z,p.x)*3.81972,asin(p.y)*7.63944);
                float2 grid=abs(frac(uv-0.5)-0.5)/max(fwidth(uv),0.001);
                float lines=1-saturate(min(grid.x,grid.y));
                float scan=pow(saturate(1-abs(frac(p.y*0.5+0.5-_Time.y*0.12)-0.5)*24),3);
                float3 baseColor=_Color.rgb*i.color.rgb;
                float3 color=baseColor*light+_RimColor.rgb*(rim*0.85+lines*0.14*_Grid+scan*0.16);
                return float4(color,1);
            }
            ENDCG
        }
    }
}
