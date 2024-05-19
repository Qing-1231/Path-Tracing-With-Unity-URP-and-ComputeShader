#ifndef INTERVAL_HLSL
#define INTERVAL_HLSL

#include "./Common.hlsl"

// interval
struct interval
{
    float max;
    float min;
};

interval create_interval(float min, float max)
{
    interval i;
    i.min = min;
    i.max = max;
    return i;
}

float size(interval i)
{
    return i.max - i.min;
}

bool contains(interval i, double x)
{
    return i.min <= x && x <= i.max;
}

bool surrounds(interval i, double x)
{
    return i.min < x && x < i.max;
}


#endif