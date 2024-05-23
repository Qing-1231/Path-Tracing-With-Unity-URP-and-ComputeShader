using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using _BVHAccel;
using _RayTracingMaterial;

namespace _Hittable
{
    public struct Sphere
    {
        public Vector3 position;
        public float radius;
        public RayTracingMaterial mat;
        public AABB bbox;

        public Sphere(Vector3 p, float r, RayTracingMaterial m)
        {
            position = p;
            radius = r;
            mat = m;
            Vector3 rvec = new(radius, radius, radius);
            bbox = new AABB(position - rvec, position + rvec);
        }

        public AABB bounding_box() { return bbox; }
    }
}
