#ifndef INTERSECT_HLSL
#define INTERSECT_HLSL

#include "./CPUInput.hlsl"
#include "./Common.hlsl"
#include "./Ray.hlsl"
#include "./Hit_Record.hlsl"
#include "./Interval.hlsl"

bool IntersectTriangle_MT97(Ray r, Interval ray_t, float3 v0, float3 v1, float3 v2, inout float t, inout float u, inout float v)
{
    float3 edge1 = v1 - v0;
    float3 edge2 = v2 - v0;

    float3 pvec = cross(r.direction, edge2);

    float det = dot(edge1, pvec);

    if (abs(det) < 0.0)
        return false;
    float inv_det = 1.0f / det;

    float3 tvec = r.origin - v0;

    u = dot(tvec, pvec) * inv_det;
    if (u < 0.0f || u > 1.0f)
        return false;

    float3 qvec = cross(tvec, edge1);

    v = dot(r.direction, qvec) * inv_det;
    if (v < 0.0f || (u + v) > 1.0f)
        return false;

    t = dot(edge2, qvec) * inv_det;
    
    return ray_t.surrounds(t);
}

#endif