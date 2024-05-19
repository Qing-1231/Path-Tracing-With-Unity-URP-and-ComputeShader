#ifndef RTWEEKEND_HLSL
#define RTWEEKEND_HLSL

static const float PI = 3.14159265f;
static const float EPSILON = 1e-8;
static const float INFINITY = 1e+8;

float sdot(float3 x, float3 y, float f = 1.0f)
{
    return saturate(dot(x, y) * f);
}

float SmoothnessToPhongAlpha(float s)
{
    return pow(1000.0f, s * s);
}

#endif