#ifndef AABB_HLSL
#define AABB_HLSL

#include "./Common.hlsl"
#include "./Interval.hlsl"

struct AABB
{
    interval x, y, z;
    
};

AABB create_aabb(float3 a, float3 b)
{
    AABB aabb;
    aabb.x = (a[0] <= b[0]) ? create_interval(a[0], b[0]) : create_interval(b[0], a[0]);
    aabb.y = (a[1] <= b[1]) ? create_interval(a[1], b[1]) : create_interval(b[1], a[1]);
    aabb.z = (a[2] <= b[2]) ? create_interval(a[2], b[2]) : create_interval(b[2], a[2]);
    return aabb;
}

#endif