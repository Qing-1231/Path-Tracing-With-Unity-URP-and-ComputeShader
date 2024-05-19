#ifndef SPHERE_HLSL
#define SPHERE_HLSL

#include "./Material.hlsl"
#include "./Interval.hlsl"
#include "./Ray.hlsl"
#include "./Hit_Record.hlsl"


struct sphere
{
    float3 center;
    float radius;
    material mat;
};

StructuredBuffer<sphere> _spheres;

bool hit(sphere s, inout ray r, interval ray_t, inout hit_record rec)
{
    float3 oc = s.center - r.origin;
    float a = dot(r.direction, r.direction);
    float h = dot(r.direction, oc);
    float c = dot(oc, oc) - s.radius * s.radius;

    float discriminant = h * h - a * c;
    if (discriminant < 0)
        return false;

    float sqrtd = sqrt(discriminant);

    // Find the nearest root that lies in the acceptable range.
    float root = (h - sqrtd) / a;
    if (!surrounds(ray_t, root))
    {
        root = (h + sqrtd) / a;
        if (!surrounds(ray_t, root))
            return false;
    }

    rec.t = root;
    rec.p = at(r, rec.t);
    rec.mat = s.mat;
    float3 outward_normal = (rec.p - s.center) / s.radius;
    set_face_normal(rec, r, outward_normal);

    return true;
}




struct hittable_list
{
    StructuredBuffer<sphere> _spheres;
};

bool hit(hittable_list world, inout ray r, interval ray_t, inout hit_record rec)
{
    hit_record temp_rec;
    bool hit_anything = false;
    float closest_so_far = ray_t.max;
    
    uint count, stride, i;
    
    // hit spheres
    world._spheres.GetDimensions(count, stride);
    for (i = 0; i < count; i++)
    {
        if (hit(world._spheres[i], r, create_interval(ray_t.min, closest_so_far), temp_rec))
        {
            closest_so_far = temp_rec.t;
            hit_anything = true;
            rec = temp_rec;
        }
    }
    
    return hit_anything;
}


#endif