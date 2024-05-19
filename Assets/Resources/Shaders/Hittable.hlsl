#ifndef SPHERE_HLSL
#define SPHERE_HLSL

#include "./Material.hlsl"
#include "./Interval.hlsl"
#include "./Ray.hlsl"
#include "./Hit_Record.hlsl"


struct Sphere
{
    float3 center;
    float radius;
    Material mat;
    
    bool hit(inout Ray r, interval ray_t, inout HitRecord rec)
    {
        float3 oc = center - r.origin;
        float a = dot(r.direction, r.direction);
        float h = dot(r.direction, oc);
        float c = dot(oc, oc) - radius * radius;
        
        float discriminant = h * h - a * c;
        if (discriminant < 0)
            return false;

        float sqrtd = sqrt(discriminant);

        // Find the nearest root that lies in the acceptable range.
        float root = (h - sqrtd) / a;
        if (!ray_t.surrounds(root))
        {
            root = (h + sqrtd) / a;
            if (!ray_t.surrounds(root))
                return false;
        }

        rec.t = root;
        rec.p = r.at(rec.t);
        rec.mat = mat;
        float3 outward_normal = (rec.p - center) / radius;
        rec.set_face_normal(r, outward_normal);

        return true;
    }
};

StructuredBuffer<Sphere> _Spheres;

struct HittableList
{
    StructuredBuffer<Sphere> _Spheres;
    
    bool hit(inout Ray r, interval ray_t, inout HitRecord rec)
    {
        HitRecord temp_rec;
        bool hit_anything = false;
        float closest_so_far = ray_t.max;
    
        uint count, stride, i;
        
        // hit spheres
        _Spheres.GetDimensions(count, stride);
        for (i = 0; i < count; i++)
        {
            if (_Spheres[i].hit(r, create_interval(ray_t.min, closest_so_far), temp_rec))
            {
                closest_so_far = temp_rec.t;
                hit_anything = true;
                rec = temp_rec;
            }
        }
        return hit_anything;
    }
};




#endif