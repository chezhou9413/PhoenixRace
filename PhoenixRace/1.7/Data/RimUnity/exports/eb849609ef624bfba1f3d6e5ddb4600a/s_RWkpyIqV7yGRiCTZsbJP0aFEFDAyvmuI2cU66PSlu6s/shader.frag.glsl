#version 450
float RU_CompatPow6(float x) {
    float result = float(1.0);
    x *= x;
    result *= x;
    x *= x;
    result *= x;
    return result;
}
float RU_CompatSmoothstep(float low, float high, float x) {
    float t = clamp((x - low) / (high - low), float(0.0), float(1.0));
    return t * t * (3.0 - 2.0 * t);
}
vec2 RU_CompatPow6(vec2 x) {
    vec2 result = vec2(1.0);
    x *= x;
    result *= x;
    x *= x;
    result *= x;
    return result;
}
vec2 RU_CompatSmoothstep(vec2 low, vec2 high, vec2 x) {
    vec2 t = clamp((x - low) / (high - low), vec2(0.0), vec2(1.0));
    return t * t * (3.0 - 2.0 * t);
}
vec2 RU_CompatSmoothstep(float low, float high, vec2 x) { return RU_CompatSmoothstep(vec2(low), vec2(high), x); }
vec3 RU_CompatPow6(vec3 x) {
    vec3 result = vec3(1.0);
    x *= x;
    result *= x;
    x *= x;
    result *= x;
    return result;
}
vec3 RU_CompatSmoothstep(vec3 low, vec3 high, vec3 x) {
    vec3 t = clamp((x - low) / (high - low), vec3(0.0), vec3(1.0));
    return t * t * (3.0 - 2.0 * t);
}
vec3 RU_CompatSmoothstep(float low, float high, vec3 x) { return RU_CompatSmoothstep(vec3(low), vec3(high), x); }
vec4 RU_CompatPow6(vec4 x) {
    vec4 result = vec4(1.0);
    x *= x;
    result *= x;
    x *= x;
    result *= x;
    return result;
}
vec4 RU_CompatSmoothstep(vec4 low, vec4 high, vec4 x) {
    vec4 t = clamp((x - low) / (high - low), vec4(0.0), vec4(1.0));
    return t * t * (3.0 - 2.0 * t);
}
vec4 RU_CompatSmoothstep(float low, float high, vec4 x) { return RU_CompatSmoothstep(vec4(low), vec4(high), x); }


#line 56 0
struct RU_MaterialData_0
{
    float _BreathMax; // @default = 1
    float _BreathMin; // @default = 0
    float _BreathSpeed; // @default = 1.2
    vec4 _CoreColor; // @default = vec4(0.8, 1, 1, 1) @color
    float _CorePower; // @default = 2
    vec4 _EdgeColor1; // @default = vec4(0, 1, 0.8, 1) @color
    vec4 _EdgeColor2; // @default = vec4(0.4, 0, 1, 1) @color
    float _EdgeColorScale; // @default = 3
    float _EdgeColorSpeed; // @default = 1.8
    float _EdgeDistort; // @default = 0.7
    float _EdgeNoiseScale; // @default = 5
    float _EdgeNoiseSpeed; // @default = 2
    float _EdgeSharp; // @default = 4
    float _EdgeWidth; // @default = 0.18
    vec4 _FlashColor; // @default = vec4(1, 1, 1, 1) @color
    float _FlashSharp; // @default = 6
    float _Intensity; // @default = 3
    float _NoiseScale; // @default = 3
    float _NoiseSpeed; // @default = 1.5
    float _NoseSoft; // @default = 0.08
    vec4 _RimColor; // @default = vec4(0, 0.6, 1, 1) @color
    float _RimPower; // @default = 4
    mat4 _RimUnity_View;
    float _RippleAmp; // @default = 0.3
    float _RippleFreq; // @default = 8
    float _RippleSpeed; // @default = 4
    vec4 _TailColor; // @default = vec4(0, 0.8, 1, 1) @color
    float _TailCutoff; // @default = 0.05
    float _TailNoiseAmp; // @default = 0.18
    float _TailNoiseScale; // @default = 4
    float _TailNoiseSpeed; // @default = 2.5
    float _TailSoftBase; // @default = 0.15
    vec4 _Time;
};


#line 91
layout(binding = 0, set = 2, std140) uniform block_RU_MaterialData_0
{    float _BreathSpeed; // @default = 1.2
    vec4 _CoreColor; // @default = vec4(0.8, 1, 1, 1) @color
    float _CorePower; // @default = 2
    vec4 _EdgeColor1; // @default = vec4(0, 1, 0.8, 1) @color
    vec4 _EdgeColor2; // @default = vec4(0.4, 0, 1, 1) @color
    float _EdgeColorScale; // @default = 3
    float _EdgeColorSpeed; // @default = 1.8
    float _EdgeDistort; // @default = 0.7
    float _EdgeNoiseScale; // @default = 5
    float _EdgeNoiseSpeed; // @default = 2
    float _EdgeSharp; // @default = 4
    float _EdgeWidth; // @default = 0.18
    vec4 _FlashColor; // @default = vec4(1, 1, 1, 1) @color
    float _FlashSharp; // @default = 6
    float _Intensity; // @default = 3
    float _NoiseScale; // @default = 3
    float _NoiseSpeed; // @default = 1.5
    float _NoseSoft; // @default = 0.08
    vec4 _RimColor; // @default = vec4(0, 0.6, 1, 1) @color
    float _RimPower; // @default = 4
    mat4 _RimUnity_View;
    float _RippleAmp; // @default = 0.3
    float _RippleFreq; // @default = 8
    float _RippleSpeed; // @default = 4
    vec4 _TailColor; // @default = vec4(0, 0.8, 1, 1) @color
    float _TailCutoff; // @default = 0.05
    float _TailNoiseAmp; // @default = 0.18
    float _TailNoiseScale; // @default = 4
    float _TailNoiseSpeed; // @default = 2.5
    float _TailSoftBase; // @default = 0.15
    vec4 _Time;
}RU_Material_0;

#line 14946 1
float saturate_0(float x_0)
{

#line 14954
    return clamp(x_0, 0.0, 1.0);
}


#line 127 0
float hash2_0(vec2 p_0)
{

#line 127
    return fract(sin(dot(p_0, vec2(127.09999847412109375, 311.70001220703125))) * 43758.546875);
}


#line 127
float vnoise2_0(vec2 p_1)
{

#line 127
    vec2 i_0 = floor(p_1);

#line 127
    vec2 f_0 = fract(p_1);

#line 127
    vec2 u_0 = f_0 * f_0 * (3.0 - 2.0 * f_0);

#line 127
    float _S1 = u_0.x;

#line 127
    return mix(mix(hash2_0(i_0), hash2_0(i_0 + vec2(1.0, 0.0)), _S1), mix(hash2_0(i_0 + vec2(0.0, 1.0)), hash2_0(i_0 + vec2(1.0, 1.0)), _S1), u_0.y);
}


#line 127
float fbm2_0(vec2 p_2)
{

#line 127
    return vnoise2_0(p_2) * 0.60000002384185791 + vnoise2_0(p_2 * 2.0 + 5.30000019073486328) * 0.25 + vnoise2_0(p_2 * 4.09999990463256836 + 11.69999980926513672) * 0.15000000596046448;
}


#line 127
float hash3_0(vec3 p_3)
{

#line 127
    return fract(sin(dot(p_3, vec3(127.09999847412109375, 311.70001220703125, 74.6999969482421875))) * 43758.546875);
}


#line 127
float vnoise3_0(vec3 p_4)
{

#line 127
    vec3 i_1 = floor(p_4);

#line 127
    vec3 f_1 = fract(p_4);

#line 127
    vec3 u_1 = f_1 * f_1 * (3.0 - 2.0 * f_1);

#line 127
    float _S2 = u_1.x;

#line 127
    float _S3 = u_1.y;

#line 127
    return mix(mix(mix(hash3_0(i_1), hash3_0(i_1 + vec3(1.0, 0.0, 0.0)), _S2), mix(hash3_0(i_1 + vec3(0.0, 1.0, 0.0)), hash3_0(i_1 + vec3(1.0, 1.0, 0.0)), _S2), _S3), mix(mix(hash3_0(i_1 + vec3(0.0, 0.0, 1.0)), hash3_0(i_1 + vec3(1.0, 0.0, 1.0)), _S2), mix(hash3_0(i_1 + vec3(0.0, 1.0, 1.0)), hash3_0(i_1 + vec3(1.0, 1.0, 1.0)), _S2), _S3), u_1.z);
}


#line 127
float fbm3_0(vec3 p_5)
{

#line 127
    return vnoise3_0(p_5) * 0.55000001192092896 + vnoise3_0(p_5 * 2.09999990463256836 + 1.70000004768371582) * 0.30000001192092896 + vnoise3_0(p_5 * 4.30000019073486328 + 3.20000004768371582) * 0.15000000596046448;
}


#line 127
layout(location = 0)
out vec4 entryPointParam_frag_0;


#line 127
layout(location = 5) in vec3 i_worldPos_0; // @vertex TexCoord1 optional


#line 127
layout(location = 6) in float i_axisT_0; // @vertex TexCoord2 optional


#line 127
layout(location = 7) in float i_fresnel_0; // @vertex TexCoord3 optional


#line 127
void main()
{

#line 127
    float t_0 = RU_Material_0._Time.y;

#line 127
    float _S4 = i_worldPos_0.y;

#line 127
    float _S5 = i_worldPos_0.z;

#line 127
    vec2 tailCircleCoord_0 = vec2(_S4, _S5) * RU_Material_0._TailNoiseScale;

#line 127
    float ang_0 = t_0 * RU_Material_0._TailNoiseSpeed;

#line 127
    float ca_0 = cos(ang_0);

#line 127
    float sa_0 = sin(ang_0);

#line 127
    float _S6 = tailCircleCoord_0.x;

#line 127
    float _S7 = tailCircleCoord_0.y;

#line 127
    float _S8 = fbm2_0(vec2(_S6 * ca_0 - _S7 * sa_0, _S6 * sa_0 + _S7 * ca_0)) - 0.5;

#line 127
    float dynamicCutoff_0 = RU_Material_0._TailCutoff + _S8 * RU_Material_0._TailNoiseAmp;

#line 127
    float _S9 = max(0.00999999977648258, RU_Material_0._TailSoftBase + _S8 * RU_Material_0._TailSoftBase * 0.60000002384185791);

#line 127
    float tailMask_0 = RU_CompatSmoothstep(dynamicCutoff_0 - _S9, dynamicCutoff_0 + _S9, i_axisT_0);

#line 127
    float tailEdgeW_0 = _S9 * 1.20000004768371582;

#line 127
    float tailEdge_0 = saturate_0(RU_CompatSmoothstep(dynamicCutoff_0 - tailEdgeW_0, dynamicCutoff_0, i_axisT_0) * (1.0 - RU_CompatSmoothstep(dynamicCutoff_0, dynamicCutoff_0 + tailEdgeW_0 * 0.40000000596046448, i_axisT_0)));

#line 127
    float _S10 = - t_0;

#line 127
    float distortedFresnel_0 = saturate_0(i_fresnel_0 - (fbm3_0(i_worldPos_0 * RU_Material_0._EdgeNoiseScale + vec3(t_0 * RU_Material_0._EdgeNoiseSpeed, t_0 * RU_Material_0._EdgeNoiseSpeed * 0.69999998807907104, _S10 * RU_Material_0._EdgeNoiseSpeed * 1.29999995231628418)) - 0.5) * RU_Material_0._EdgeDistort);

#line 127
    float edgeLow_0 = 1.0 - RU_Material_0._EdgeWidth;

#line 127
    float edgeBand_0 = saturate_0(pow(RU_CompatSmoothstep(edgeLow_0 - 0.05000000074505806, edgeLow_0, distortedFresnel_0) - RU_CompatSmoothstep(0.94999998807907104, 1.0, distortedFresnel_0), 1.0 / RU_Material_0._EdgeSharp));

#line 127
    float breath_0 = pow(sin(t_0 * RU_Material_0._BreathSpeed * 3.14159011840820312) * 0.5 + 0.5, RU_Material_0._FlashSharp);

#line 127
    float rippleVal_0 = (sin(i_axisT_0 * RU_Material_0._RippleFreq - t_0 * RU_Material_0._RippleSpeed) * 0.5 + 0.5) * (pow(i_fresnel_0, 1.5) * RU_Material_0._RippleAmp);

#line 127
    float core_0 = pow(1.0 - distortedFresnel_0, RU_Material_0._CorePower);

#line 127
    float rim_0 = pow(distortedFresnel_0, RU_Material_0._RimPower);

#line 127
    float alpha_0 = (core_0 * 0.60000002384185791 + rim_0 * 0.40000000596046448 + rippleVal_0 * 0.20000000298023224 + edgeBand_0 * 0.30000001192092896 + tailEdge_0 * 0.25) * (tailMask_0 * RU_CompatSmoothstep(1.0, 1.0 - RU_Material_0._NoseSoft, i_axisT_0));

#line 127
    entryPointParam_frag_0 = vec4((mix(mix(RU_Material_0._CoreColor.xyz * core_0 + RU_Material_0._RimColor.xyz * rim_0, RU_Material_0._FlashColor.xyz, vec3(rippleVal_0 + fbm2_0(vec2(i_axisT_0 * 2.0, _S4 + _S5) * RU_Material_0._NoiseScale + vec2(t_0 * RU_Material_0._NoiseSpeed, t_0 * RU_Material_0._NoiseSpeed * 0.80000001192092896)) * 0.40000000596046448)), RU_Material_0._TailColor.xyz, vec3(saturate_0((1.0 - i_axisT_0) * (1.0 - tailMask_0 + 0.30000001192092896)) * 0.5)) + mix(RU_Material_0._TailColor.xyz, RU_Material_0._FlashColor.xyz, vec3(breath_0 * 0.40000000596046448)) * tailEdge_0 * 1.79999995231628418 + mix(mix(RU_Material_0._EdgeColor1.xyz, RU_Material_0._EdgeColor2.xyz, vec3(saturate_0(fbm3_0(i_worldPos_0 * RU_Material_0._EdgeColorScale + vec3(_S10 * RU_Material_0._EdgeColorSpeed, t_0 * RU_Material_0._EdgeColorSpeed * 1.10000002384185791, _S10 * RU_Material_0._EdgeColorSpeed * 0.60000002384185791)) + breath_0 * 0.20000000298023224))), RU_Material_0._FlashColor.xyz, vec3(RU_CompatPow6(saturate_0(distortedFresnel_0)) * breath_0 * 0.60000002384185791)) * edgeBand_0 * 1.5 + RU_Material_0._FlashColor.xyz * breath_0 * 0.30000001192092896 * rim_0) * RU_Material_0._Intensity * alpha_0, alpha_0);

#line 127
    return;
}


// @rimunity-host-matrices 1
