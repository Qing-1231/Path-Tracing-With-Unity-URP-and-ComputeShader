#ifndef MESH_HLSL
#define MESH_HLSL

struct MeshObject
{
    float4x4 localToWorldMatrix;
    int indices_offset;
    int indices_count;
    float3 albedo;
    float3 specular;
    float smoothness;
    float ior;
    float3 emission;
};

StructuredBuffer<MeshObject> _MeshObjects;
StructuredBuffer<float3> _Vertices;
StructuredBuffer<int> _Indices;


#endif