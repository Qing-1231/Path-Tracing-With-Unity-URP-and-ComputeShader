using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace _RayTracingMaterial
{
    public struct RayTracingMaterial
    {
        public Vector3 albedo;
        public float fuzz;
        public float refraction_index;
        public RayTracingMaterial(Vector3 a, float f, float ior)
        {
            albedo = a;
            fuzz = f;
            refraction_index = ior;
        }
        public RayTracingMaterial(Vector3 a)
        {
            albedo = a;
            fuzz = 1;
            refraction_index = 0;
        }
        public RayTracingMaterial(float ior)
        {
            albedo = new Vector3(1, 1, 1);
            fuzz = 0;
            refraction_index = ior;
        }
    }

}

