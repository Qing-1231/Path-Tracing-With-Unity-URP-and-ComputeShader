#ifndef HIT_RECORD_HLSL
#define HIT_RECORD_HLSL

#include "./Material.hlsl"
#include "./Ray.hlsl"

struct HitRecord
{
    float3 p;
    float3 normal;
    float t;
    bool front_face;
    Material mat;
    
    void set_face_normal(Ray r, float3 outward_normal)
    {
        // Sets the hit record normal vector.
        // NOTE: the parameter `outward_normal` is assumed to have unit length.

        front_face = dot(r.direction, outward_normal) < 0;
        normal = front_face ? outward_normal : -outward_normal;
    }
};



#endif