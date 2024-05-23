using System;
using System.Collections.Generic;
using UnityEngine;
using _Hittable;
using System.Drawing;
using UnityEditor.Experimental.GraphView;

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
        public Interval(Interval a, Interval b)
        {
            // Create the interval tightly enclosing the two input intervals.
            min = a.min <= b.min ? a.min : b.min;
            max = a.max >= b.max ? a.max : b.max;
        }

        public float size()
        {
            return max - min;
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

        public AABB(AABB box0, AABB box1)
        {
            x = new Interval(box0.x, box1.x);
            y = new Interval(box0.y, box1.y);
            z = new Interval(box0.z, box1.z);
        }

        public Interval axis_interval(int n)
        {
            if (n == 1) return y;
            if (n == 2) return z;
            return x;
        }

        public int longest_axis()
        {
            // Returns the index of the longest axis of the bounding box.
            if (x.size() > y.size())
                return x.size() > z.size() ? 0 : 2;
            else
                return y.size() > z.size() ? 1 : 2;
        }
    }

    public struct TempSphereData
    {
        public AABB bbox;
        public int sphereIndex;

        public TempSphereData(AABB bbox, int sphereIndex)
        {
            this.bbox = bbox;
            this.sphereIndex = sphereIndex;
        }
    }

    public class BVHNode
    {
        public AABB bbox;
        public BVHNode left;
        public BVHNode right;
        public int objectIndex = -1;

        BVHNode(List<TempSphereData> TempSpheres, int objIndex)
        {
            bbox = TempSpheres[objIndex].bbox;
            left = right = null;
            objectIndex = TempSpheres[objIndex].sphereIndex;
        }

        public BVHNode(List<TempSphereData> TempSpheres, int start, int end)
        {
            bbox = TempSpheres[start].bbox;
            for(int i = start + 1; i < end; i++)
            {
                bbox = new AABB(bbox, TempSpheres[i].bbox);
            }

            int axis = bbox.longest_axis();

            int object_span = end - start;

            if (object_span == 1)
            {
                left = right = null;
                objectIndex = TempSpheres[start].sphereIndex;
                bbox = TempSpheres[start].bbox;
            }
            else if (object_span == 2)
            {
                left = new BVHNode(TempSpheres, start);
                right = new BVHNode(TempSpheres, start + 1);
                bbox = new AABB(left.bbox, right.bbox);
            }
            else
            {
                int mid = start + object_span / 2;
                TempSpheres.Sort(start, object_span, Comparer<TempSphereData>.Create((a, b) =>
                {
                    var a_axis_interval = a.bbox.axis_interval(axis);
                    var b_axis_interval = b.bbox.axis_interval(axis);
                    return a_axis_interval.min.CompareTo(b_axis_interval.min);
                }));

                left = new BVHNode(TempSpheres, start, mid);
                right = new BVHNode(TempSpheres, mid, end);
            }
        }

        static public void TraverseBVHNode(List<BVHNodeFlat> flatNodes, BVHNode root)
        {
            // BFS
            int currentIndex = 0;
            Queue<BVHNode> queue = new Queue<BVHNode>();
            queue.Enqueue(root);

            while(queue.Count > 0)
            {
                BVHNode node = queue.Dequeue();

                if (node.objectIndex == -1) // Not Leaf Node
                {
                    BVHNodeFlat nodeFlat = new BVHNodeFlat
                    {
                        left = ++currentIndex,
                        right = ++currentIndex,
                        objectIndex = -1,
                        bbox = node.bbox,
                    };
                    flatNodes.Add(nodeFlat);
                    //TraverseBVHNode(flatNodes, node.left, ref currentIndex);
                    queue.Enqueue(node.left);
                    queue.Enqueue(node.right);
                }
                else // leaf node
                {
                    BVHNodeFlat nodeFlat = new BVHNodeFlat
                    {
                        left = -1,
                        right = -1,
                        objectIndex = node.objectIndex,
                        bbox = node.bbox,
                    };
                    flatNodes.Add(nodeFlat);
                }
            }
            
        }
            

        public void Flatten(List<BVHNodeFlat> flatNodes, ref int currentIndex)
        {
            int myIndex = currentIndex;
            BVHNodeFlat nodeFlat = new BVHNodeFlat
            {
                left = left != null ? ++currentIndex : -1,
                right = right != null ? ++currentIndex : -1,
                objectIndex = objectIndex,
                bbox = bbox,
            };

            flatNodes[myIndex] = nodeFlat;

            left?.Flatten(flatNodes, ref currentIndex);
            right?.Flatten(flatNodes, ref currentIndex);
        }

        //public void flatten(List<BVHNodeFlat> flatNodes, ref int currentIndex)
        //{
        //    int myIndex = currentIndex;
        //    if (objectIndex == -1) // Not leaf Node
        //    {
        //        flatNodes[myIndex] = new BVHNodeFlat
        //        {
        //            objectIndex = -1,
        //            bbox = bbox,
        //            left = ++currentIndex,
        //            right = ++currentIndex
        //        };
        //    }
        //    else // Leaf node
        //    {
        //        flatNodes[myIndex] = new BVHNodeFlat
        //        {
        //            objectIndex = objectIndex,
        //            bbox = bbox,
        //            left = -1,
        //            right = -1
        //        };
        //    }
        //}

        public AABB bounding_box() { return bbox; }

    }

    public struct BVHNodeFlat
    {
        public int left;
        public int right;
        public int objectIndex;
        public AABB bbox;
    }
}

