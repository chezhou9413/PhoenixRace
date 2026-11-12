#version 450
layout(location = 0) in vec3 inPosition; // @vertex Position required
layout(location = 1) in vec2 inUv; // @vertex TexCoord0 required
layout(push_constant) uniform Push { mat4 modelMatrix; } push;
layout(set = 0, binding = 0) uniform VertexUniforms { mat4 ViewProj; } view;
layout(location = 0) out vec2 uv;

//面片矩阵分别承载飞机的相机朝向和炮塔的地面投影。
void main()
{
    gl_Position = view.ViewProj * push.modelMatrix * vec4(inPosition, 1.0);
    uv = inUv;
}
