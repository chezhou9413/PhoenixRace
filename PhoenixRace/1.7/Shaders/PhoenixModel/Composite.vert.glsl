#version 450
layout(location = 0) in vec3 inPosition; // @vertex Position required
layout(location = 1) in vec2 inUv; // @vertex TexCoord0 required
layout(location = 0) out vec2 uv;

//全屏三角形直接写裁剪坐标，附件 UV 的原点与 Vulkan 视口一致。
void main()
{
    gl_Position = vec4(inPosition.xy, 0.0, 1.0);
    uv = inUv;
}
