#ifndef INTERVAL_HLSL
#define INTERVAL_HLSL

#include "./Common.hlsl"

// interval
struct interval
{
    float max;
    float min;
    
    float size()
    {
        return max - min;
    }

    bool contains(double x)
    {
        return min <= x && x <= max;
    }

    bool surrounds( double x)
    {
        return min < x && x < max;
    }
    
    float clamp(float x)
    {
        if (x < min) return min;
        if (x > max) return max;
        return x;
    }
};

interval create_interval(float min, float max)
{
    interval i;
    i.min = min;
    i.max = max;
    return i;
}

interval expand(interval i, float delta)
{
    float padding = delta / 2.0;
    return create_interval(i.min - padding, i.max + padding);
}


#endif