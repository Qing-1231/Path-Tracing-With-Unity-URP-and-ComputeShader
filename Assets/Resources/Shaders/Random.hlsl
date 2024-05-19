#ifndef RANDOM_HLSL
#define RANDOM_HLSL

#include "./Common.hlsl"

float2 _Pixel;
float _Seed;

float rand()
{
    float result = frac(sin(_Seed / 100.0f * dot(_Pixel, float2(12.9898f, 78.233f))) * 43758.5453f);
    _Seed += 1.0f;
    return result;
}
float3 random_in_unit_sphere()
{
    float phi = 2.0 * PI * rand(); // Random azimuth angle between 0 and 2*PI
    float cosTheta = 2.0 * rand() - 1.0; // Random elevation angle, cos(theta) between -1 and 1
    float sinTheta = sqrt(1.0 - cosTheta * cosTheta); // sin(theta) derived from cos(theta)

    float x = cos(phi) * sinTheta;
    float y = sin(phi) * sinTheta;
    float z = cosTheta;

    return float3(x, y, z);
}
float3 random_on_hemisphere(inout float3 normal)
{
    float3 on_unit_sphere = random_in_unit_sphere();
    if (dot(on_unit_sphere, normal) > 0.0) // In the same hemisphere as the normal
        return on_unit_sphere;
    else
        return -on_unit_sphere;
}

bool near_zero(float3 e)
{
    // Return true if the vector is close to zero in all dimensions.
    float s = 1e-8;
    return (abs(e.x) < s) && (abs(e.y) < s) && (abs(e.z) < s);
}

#endif