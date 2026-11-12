#version 450
layout(location = 0) in vec3 inPosition; // @vertex Position required
layout(location = 1) in vec2 inUv; // @vertex TexCoord0 optional @default = vec2(0.0, 0.0)
layout(location = 2) in vec3 inNormal; // @vertex Normal optional @default = vec3(0.0, 1.0, 0.0)
layout(push_constant) uniform Push { mat4 modelMatrix; } push;
layout(set = 0, binding = 0) uniform VertexUniforms { mat4 ViewProj; } view;
layout(set = 2, binding = 0, std140) uniform MaterialUniforms
{
    vec4 _Color; // @color @default = vec4(1.0, 1.0, 1.0, 1.0)
    vec4 _MainTex_ST; // @default = vec4(0.0, 0.0, 1.0, 1.0)
    vec4 _CaptureOrigin; // @default = vec4(0.0, 0.0, 0.0, 0.0)
    vec4 _CaptureForward; // @default = vec4(0.0, 0.0, 1.0, 0.0)
} material;
layout(location = 0) out vec2 uv;
layout(location = 1) out vec3 normal;
layout(location = 2) out float distanceToCamera;

//几何和颜色使用相同模型矩阵，法线用逆转置处理非均匀缩放。
void main()
{
    vec4 world = push.modelMatrix * vec4(inPosition, 1.0);
    gl_Position = view.ViewProj * world;
    normal = transpose(inverse(mat3(push.modelMatrix))) * inNormal;
    distanceToCamera = dot(world.xyz - material._CaptureOrigin.xyz, material._CaptureForward.xyz);
    uv = inUv;
}
