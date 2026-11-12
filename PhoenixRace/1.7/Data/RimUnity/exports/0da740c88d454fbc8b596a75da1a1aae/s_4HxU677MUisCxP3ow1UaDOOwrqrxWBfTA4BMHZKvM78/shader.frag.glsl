#version 450
float RU_CompatSmoothstep(float low, float high, float x) {
    float t = clamp((x - low) / (high - low), float(0.0), float(1.0));
    return t * t * (3.0 - 2.0 * t);
}
vec2 RU_CompatSmoothstep(vec2 low, vec2 high, vec2 x) {
    vec2 t = clamp((x - low) / (high - low), vec2(0.0), vec2(1.0));
    return t * t * (3.0 - 2.0 * t);
}
vec2 RU_CompatSmoothstep(float low, float high, vec2 x) { return RU_CompatSmoothstep(vec2(low), vec2(high), x); }
vec3 RU_CompatSmoothstep(vec3 low, vec3 high, vec3 x) {
    vec3 t = clamp((x - low) / (high - low), vec3(0.0), vec3(1.0));
    return t * t * (3.0 - 2.0 * t);
}
vec3 RU_CompatSmoothstep(float low, float high, vec3 x) { return RU_CompatSmoothstep(vec3(low), vec3(high), x); }
vec4 RU_CompatSmoothstep(vec4 low, vec4 high, vec4 x) {
    vec4 t = clamp((x - low) / (high - low), vec4(0.0), vec4(1.0));
    return t * t * (3.0 - 2.0 * t);
}
vec4 RU_CompatSmoothstep(float low, float high, vec4 x) { return RU_CompatSmoothstep(vec4(low), vec4(high), x); }


#line 56 0
struct RU_MaterialData_0
{
    float _BaseFresnelIntensity; // @default = 0.5
    float _BaseFresnelPower; // @default = 4
    vec4 _BreakTint; // @default = vec4(1, 1, 1, 1) @color
    vec4 _Color; // @default = vec4(1, 0.5, 0, 1) @color
    float _Dissolve; // @default = 0
    float _DissolveSoftness; // @default = 0.1
    float _HexIntensity; // @default = 5
    float _HexScale; // @default = 30
    vec4 _ImpactColor; // @default = vec4(1, 0.8, 0.3, 1) @color
    float _ImpactFadePower; // @default = 1.5
    float _ImpactHexShape; // @default = 1
    float _ImpactIntensity; // @default = 15
    float _ImpactMaxRadius; // @default = 1.8
    vec4 _ImpactPoint0; // @default = vec4(0, 0, 0, 0)
    vec4 _ImpactPoint1; // @default = vec4(0, 0, 0, 0)
    vec4 _ImpactPoint2; // @default = vec4(0, 0, 0, 0)
    vec4 _ImpactPoint3; // @default = vec4(0, 0, 0, 0)
    vec4 _ImpactPoint4; // @default = vec4(0, 0, 0, 0)
    vec4 _ImpactPoint5; // @default = vec4(0, 0, 0, 0)
    vec4 _ImpactPoint6; // @default = vec4(0, 0, 0, 0)
    vec4 _ImpactPoint7; // @default = vec4(0, 0, 0, 0)
    vec4 _ImpactRedColor; // @default = vec4(1.5, 0.1, 0.1, 1) @color
    float _ImpactRedDuration; // @default = 1.2
    float _ImpactRedIntensity; // @default = 1.5
    float _ImpactRedRadius; // @default = 0.35
    float _ImpactRingWidth; // @default = 0.35
    float _ImpactSpeed; // @default = 2
    float _LineThickness; // @default = 0.05
    vec2 _NoiseFlowSpeed; // @default = vec2(0.3, 0.2)
    float _NoiseIntensity; // @default = 1.2
    float _NoiseScale; // @default = 0.5
    mat4 _RimUnity_View;
    float _ShatterGlow; // @default = 10
    float _SpreadSpeed; // @default = 1
    float _SpreadWidth; // @default = 0.2
    vec4 _Time;
    vec3 _WorldSpaceCameraPos;
    vec4 unity_OrthoParams;
};


#line 96
layout(binding = 0, set = 2, std140) uniform block_RU_MaterialData_0
{
    float _BaseFresnelIntensity; // @default = 0.5
    float _BaseFresnelPower; // @default = 4
    vec4 _BreakTint; // @default = vec4(1, 1, 1, 1) @color
    vec4 _Color; // @default = vec4(1, 0.5, 0, 1) @color
    float _Dissolve; // @default = 0
    float _DissolveSoftness; // @default = 0.1
    float _HexIntensity; // @default = 5
    float _HexScale; // @default = 30
    vec4 _ImpactColor; // @default = vec4(1, 0.8, 0.3, 1) @color
    float _ImpactFadePower; // @default = 1.5
    float _ImpactHexShape; // @default = 1
    float _ImpactIntensity; // @default = 15
    float _ImpactMaxRadius; // @default = 1.8
    vec4 _ImpactPoint0; // @default = vec4(0, 0, 0, 0)
    vec4 _ImpactPoint1; // @default = vec4(0, 0, 0, 0)
    vec4 _ImpactPoint2; // @default = vec4(0, 0, 0, 0)
    vec4 _ImpactPoint3; // @default = vec4(0, 0, 0, 0)
    vec4 _ImpactPoint4; // @default = vec4(0, 0, 0, 0)
    vec4 _ImpactPoint5; // @default = vec4(0, 0, 0, 0)
    vec4 _ImpactPoint6; // @default = vec4(0, 0, 0, 0)
    vec4 _ImpactPoint7; // @default = vec4(0, 0, 0, 0)
    vec4 _ImpactRedColor; // @default = vec4(1.5, 0.1, 0.1, 1) @color
    float _ImpactRedDuration; // @default = 1.2
    float _ImpactRedIntensity; // @default = 1.5
    float _ImpactRedRadius; // @default = 0.35
    float _ImpactRingWidth; // @default = 0.35
    float _ImpactSpeed; // @default = 2
    float _LineThickness; // @default = 0.05
    vec2 _NoiseFlowSpeed; // @default = vec2(0.3, 0.2)
    float _NoiseIntensity; // @default = 1.2
    float _NoiseScale; // @default = 0.5
    mat4 _RimUnity_View;
    float _ShatterGlow; // @default = 10
    float _SpreadSpeed; // @default = 1
    float _SpreadWidth; // @default = 0.2
    vec4 _Time;
    vec4 _WorldSpaceCameraPos;
    vec4 unity_OrthoParams;
}RU_Material_0;

#line 136
layout(binding = 1, set = 2)
uniform sampler2D _NoiseTex; // @default = white


#line 138



#line 14946 1
float saturate_0(float x_0)
{

#line 14954
    return clamp(x_0, 0.0, 1.0);
}


#line 140 0
vec4 RU_tex2D_NoiseTex_0(vec2 uv_0)
{

#line 140
    return (texture(_NoiseTex, (vec2(uv_0.x, 1.0 - uv_0.y))));
}


#line 160
float HexDist_0(vec2 p_0, vec2 center_0)
{

#line 160
    vec2 d_0 = abs(p_0 - center_0);

#line 160
    return max(d_0.x, dot(d_0, vec2(1.0, 1.73204994201660156)) * 0.5) / 0.86599999666213989;
}


#line 160
float CalculateImpactWave_0(vec2 currentUV_0, vec4 impactData_0)
{

#line 160
    float _S1 = impactData_0.w;

#line 160
    if(_S1 <= 0.00100000004749745)
    {

#line 160
        return 0.0;
    }

#line 160
    vec2 impactUV_0 = impactData_0.xy;

#line 160
    float lifeTime_0 = RU_Material_0._Time.y - impactData_0.z;

#line 160
    if(lifeTime_0 < 0.0)
    {

#line 160
        return 0.0;
    }

#line 160
    float currentRadius_0 = lifeTime_0 * RU_Material_0._ImpactSpeed;

#line 160
    if(currentRadius_0 > (RU_Material_0._ImpactMaxRadius))
    {

#line 160
        return 0.0;
    }

#line 160
    float distToImpact_0;

#line 160
    if((RU_Material_0._ImpactHexShape) > 0.5)
    {

#line 160
        distToImpact_0 = HexDist_0(currentUV_0, impactUV_0);

#line 160
    }
    else
    {

#line 160
        distToImpact_0 = distance(currentUV_0, impactUV_0);

#line 160
    }

#line 160
    return RU_CompatSmoothstep(0.0, 1.0, 1.0 - saturate_0(abs(distToImpact_0 - currentRadius_0) / RU_Material_0._ImpactRingWidth)) * pow(1.0 - saturate_0(currentRadius_0 / RU_Material_0._ImpactMaxRadius), RU_Material_0._ImpactFadePower) * _S1;
}


#line 160
float CalculateImpactRed_0(vec2 currentUV_1, vec4 impactData_1)
{

#line 160
    float _S2 = impactData_1.w;

#line 160
    if(_S2 <= 0.00100000004749745)
    {

#line 160
        return 0.0;
    }

#line 160
    float lifeTime_1 = RU_Material_0._Time.y - impactData_1.z;

#line 160
    bool _S3;

#line 160
    if(lifeTime_1 < 0.0)
    {

#line 160
        _S3 = true;

#line 160
    }
    else
    {

#line 160
        _S3 = lifeTime_1 > (RU_Material_0._ImpactRedDuration);

#line 160
    }

#line 160
    if(_S3)
    {

#line 160
        return 0.0;
    }

#line 160
    return RU_CompatSmoothstep(0.0, 1.0, 1.0 - saturate_0(distance(currentUV_1, impactData_1.xy) / RU_Material_0._ImpactRedRadius)) * pow(1.0 - saturate_0(lifeTime_1 / RU_Material_0._ImpactRedDuration), 1.5) * _S2;
}


#line 160
layout(location = 0)
out vec4 entryPointParam_frag_0;


#line 160
layout(location = 3) in vec3 i_worldNormal_0; // @vertex Normal optional


#line 160
layout(location = 1) in vec3 i_worldPos_0; // @vertex TexCoord0 optional


#line 160
layout(location = 5) in vec2 i_viewUV_0; // @vertex TexCoord1 optional


#line 160
void main()
{

#line 160
    vec2 p_1 = i_viewUV_0 * RU_Material_0._HexScale;

#line 160
    const vec2 r_0 = vec2(1.0, 1.73204994201660156);

#line 160
    const vec2 h_0 = r_0 * 0.5;

#line 160
    vec2 p1_0 = floor(p_1 / r_0) * r_0 + h_0;

#line 160
    vec2 p2_0 = floor((p_1 - h_0) / r_0) * r_0 + r_0;

#line 160
    vec2 _S4 = p_1 - p1_0;

#line 160
    vec2 _S5 = p_1 - p2_0;

#line 160
    bool isP1_0 = (length(_S4)) < (length(_S5));

#line 160
    vec2 gv_0;

#line 160
    if(isP1_0)
    {

#line 160
        gv_0 = _S4;

#line 160
    }
    else
    {

#line 160
        gv_0 = _S5;

#line 160
    }

#line 160
    vec2 centerUV_0;

#line 160
    if(isP1_0)
    {

#line 160
        centerUV_0 = p1_0;

#line 160
    }
    else
    {

#line 160
        centerUV_0 = p2_0;

#line 160
    }

#line 160
    vec2 p_abs_0 = abs(gv_0);

#line 160
    float lineMask_0 = RU_CompatSmoothstep(0.5 - RU_Material_0._LineThickness, 0.5, max(p_abs_0.x, dot(p_abs_0, r_0) * 0.5));

#line 160
    vec2 normCenter_0 = centerUV_0 / RU_Material_0._HexScale;

#line 160
    float distFromCenter_0 = distance(normCenter_0, vec2(0.5, 0.5)) * 2.0;

#line 160
    float isAlive_0 = step(RU_Material_0._Dissolve, distFromCenter_0);

#line 160
    float edgeGlow_0 = RU_CompatSmoothstep(RU_Material_0._Dissolve + RU_Material_0._DissolveSoftness, RU_Material_0._Dissolve, distFromCenter_0) * isAlive_0 * RU_Material_0._ShatterGlow * step(0.00999999977648258, RU_Material_0._Dissolve);

#line 160
    float energyFlow_0 = RU_tex2D_NoiseTex_0(i_worldPos_0.xz * RU_Material_0._NoiseScale + RU_Material_0._Time.y * RU_Material_0._NoiseFlowSpeed).x * RU_Material_0._NoiseIntensity;

#line 160
    vec3 normal_0 = normalize(i_worldNormal_0);

#line 160
    vec3 viewDir_0;

#line 160
    if((RU_Material_0.unity_OrthoParams.w) > 0.5)
    {

#line 160
        viewDir_0 = normalize(vec3(transpose(RU_Material_0._RimUnity_View)[0].z, transpose(RU_Material_0._RimUnity_View)[1].z, transpose(RU_Material_0._RimUnity_View)[2].z));

#line 160
    }
    else
    {

#line 160
        viewDir_0 = normalize((RU_Material_0._WorldSpaceCameraPos.xyz).xyz - i_worldPos_0);

#line 160
    }

#line 160
    float outwardDist_0 = 1.0 - saturate_0(dot(normal_0, viewDir_0));

#line 160
    float fresnel_0 = pow(outwardDist_0, RU_Material_0._BaseFresnelPower) * RU_Material_0._BaseFresnelIntensity;

#line 160
    float scan_0 = fract(outwardDist_0 - RU_Material_0._Time.y * RU_Material_0._SpreadSpeed);

#line 160
    float waveSum_0 = CalculateImpactWave_0(normCenter_0, RU_Material_0._ImpactPoint0) + CalculateImpactWave_0(normCenter_0, RU_Material_0._ImpactPoint1) + CalculateImpactWave_0(normCenter_0, RU_Material_0._ImpactPoint2) + CalculateImpactWave_0(normCenter_0, RU_Material_0._ImpactPoint3) + CalculateImpactWave_0(normCenter_0, RU_Material_0._ImpactPoint4) + CalculateImpactWave_0(normCenter_0, RU_Material_0._ImpactPoint5) + CalculateImpactWave_0(normCenter_0, RU_Material_0._ImpactPoint6) + CalculateImpactWave_0(normCenter_0, RU_Material_0._ImpactPoint7);

#line 160
    float redSum_0 = saturate_0(CalculateImpactRed_0(i_viewUV_0, RU_Material_0._ImpactPoint0) + CalculateImpactRed_0(i_viewUV_0, RU_Material_0._ImpactPoint1) + CalculateImpactRed_0(i_viewUV_0, RU_Material_0._ImpactPoint2) + CalculateImpactRed_0(i_viewUV_0, RU_Material_0._ImpactPoint3) + CalculateImpactRed_0(i_viewUV_0, RU_Material_0._ImpactPoint4) + CalculateImpactRed_0(i_viewUV_0, RU_Material_0._ImpactPoint5) + CalculateImpactRed_0(i_viewUV_0, RU_Material_0._ImpactPoint6) + CalculateImpactRed_0(i_viewUV_0, RU_Material_0._ImpactPoint7));

#line 160
    vec4 _S6 = RU_Material_0._Color * fresnel_0 + RU_Material_0._Color * (lineMask_0 * (RU_CompatSmoothstep(0.0, 0.10000000149011612, scan_0) * RU_CompatSmoothstep(RU_Material_0._SpreadWidth, RU_Material_0._SpreadWidth - 0.10000000149011612, scan_0)) * RU_Material_0._HexIntensity + lineMask_0 * edgeGlow_0) + RU_Material_0._Color * energyFlow_0 * (fresnel_0 + 0.10000000149011612);

#line 160
    vec4 col_0 = _S6;

#line 160
    col_0.xyz = _S6.xyz + RU_Material_0._ImpactColor.xyz * lineMask_0 * waveSum_0 * RU_Material_0._ImpactIntensity;

#line 160
    col_0[3] = col_0[3] + waveSum_0 * RU_Material_0._ImpactIntensity * 0.15000000596046448;

#line 160
    col_0.xyz = col_0.xyz + RU_Material_0._ImpactRedColor.xyz * (redSum_0 * RU_Material_0._ImpactRedIntensity) * mix(0.30000001192092896, 1.0, lineMask_0);

#line 160
    col_0[3] = col_0[3] + redSum_0 * 0.25;

#line 160
    vec4 _S7 = col_0 * isAlive_0;

#line 160
    col_0 = _S7;

#line 160
    col_0[3] = max(_S7.w * isAlive_0, 0.0);

#line 160
    col_0.xyz = col_0.xyz * RU_Material_0._BreakTint.xyz;

#line 160
    entryPointParam_frag_0 = col_0;

#line 160
    return;
}


// @rimunity-host-matrices 1
