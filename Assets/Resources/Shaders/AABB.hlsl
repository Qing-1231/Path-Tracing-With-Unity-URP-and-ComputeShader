#ifndef AABB_HLSL
#define AABB_HLSL

#include "./Common.hlsl"
#include "./Interval.hlsl"
#include "./Ray.hlsl"
#include "./CPUInput.hlsl"

struct AABB
{
    Interval x, y, z;
    
    Interval axis_interval(int n)
    {
        if (n == 1) return y;
        if (n == 2) return z;
        return x;
    }
    
    bool hit(Ray r, Interval ray_t) 
    {
        float3 ray_orig = r.origin;
        float3 ray_dir = r.direction;

        for (int axis = 0; axis < 3; axis++) {
            Interval ax = axis_interval(axis);
            float adinv = 1.0 / ray_dir[axis];

            float t0 = (ax.min - ray_orig[axis]) * adinv;
            float t1 = (ax.max - ray_orig[axis]) * adinv;

            if (t0 < t1) {
                if (t0 > ray_t.min) ray_t.min = t0;
                if (t1 < ray_t.max) ray_t.max = t1;
            } else {
                if (t1 > ray_t.min) ray_t.min = t1;
                if (t0 < ray_t.max) ray_t.max = t0;
            }

            if (ray_t.max <= ray_t.min)
                return false;
        }
        return true;
    }
};



#endif