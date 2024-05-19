#ifndef RAY_HLSL
#define RAY_HLSL

#include "./Common.hlsl"

float4x4 _CameraToWorld;
float4x4 _CameraInverseProjection;

struct Ray
{
    float3 origin;
    float3 direction;
    
    float3 at(float t)
    {
        return origin + t * direction;
    }
};



Ray CreateRay(float3 origin, float3 direction)
{
    Ray r;
    r.origin = origin;
    r.direction = direction;
    return r;
}

Ray CreateCameraRay(float2 uv)
{
    float3 origin = mul(_CameraToWorld, float4(0.0f, 0.0f, 0.0f, 1.0f)).xyz;
    float3 direction = mul(_CameraInverseProjection, float4(uv, 0.0f, 1.0f)).xyz;
    direction = mul(_CameraToWorld, float4(direction, 0.0f)).xyz;
    direction = normalize(direction);

    return CreateRay(origin, direction);
}

#endif