using UnityEngine;


namespace _BVHAccel
{
    public struct Interval
    {
        public float max, min;
        public Interval(float min, float max)
        {
            this.max = max;
            this.min = min;
        }
    }

    public struct AABB
    {
        public Interval x, y, z;
        public AABB(Vector3 a, Vector3 b)
        {
            // Treat the two points a and b as extrema for the bounding box, so we don't require a
            // particular minimum/maximum coordinate order.

            x = (a[0] <= b[0]) ? new Interval(a[0], b[0]) : new Interval(b[0], a[0]);
            y = (a[1] <= b[1]) ? new Interval(a[1], b[1]) : new Interval(b[1], a[1]);
            z = (a[2] <= b[2]) ? new Interval(a[2], b[2]) : new Interval(b[2], a[2]);
        }

        public AABB(Interval x, Interval y, Interval z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
    }
}

