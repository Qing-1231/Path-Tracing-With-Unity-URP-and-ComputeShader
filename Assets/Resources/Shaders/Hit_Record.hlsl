#ifndef HIT_RECORD_HLSL
#define HIT_RECORD_HLSL

#include "./Material.hlsl"
#include "./Ray.hlsl"

struct hit_record
{
    float3 p;
    float3 normal;
    float t;
    bool front_face;
    material mat;
};

void set_face_normal(inout hit_record rec, inout ray r, inout float3 outward_normal)
{
    // Sets the hit record normal vector.
    // NOTE: the parameter `outward_normal` is assumed to have unit length.

    rec.front_face = dot(r.direction, outward_normal) < 0;
    rec.normal = rec.front_face ? outward_normal : -outward_normal;
}

#endif