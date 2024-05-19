#ifndef MATERIAL_HLSL
#define MATERIAL_HLSL

#include "./Common.hlsl"

struct Material
{
    float3 albedo;
    float fuzz;
    float refraction_index;
};

float reflectance(float cosine, float refraction_index)
{
    // Use Schlick's approximation for reflectance.
    float r0 = (1 - refraction_index) / (1 + refraction_index);
    r0 = r0 * r0;
    return r0 + (1 - r0) * pow((1 - cosine), 5);
}


#endif