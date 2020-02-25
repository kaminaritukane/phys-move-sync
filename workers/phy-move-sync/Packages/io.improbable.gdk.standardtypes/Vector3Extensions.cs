using Improbable.Gdk.Standardtypes;
using UnityEngine;
using Unity.Mathematics;

namespace Improbable.Gdk.StandardTypes
{
    public static class Vector3Extensions
    {
        public static int ToInt100k(this float value)
        {
            return Mathf.RoundToInt(value * 100000);
        }

        public static float ToFloat100k(this int value)
        {
            return ((float)value) / 100000;
        }

        public static IntAbsolute ToIntAbsolute(this Vector3 value)
        {
            return new IntAbsolute
            {
                X = value.x.ToInt100k(),
                Y = value.y.ToInt100k(),
                Z = value.z.ToInt100k()
            };
        }

        public static Vector3 ToVector3(this IntAbsolute value)
        {
            return new Vector3(value.X.ToFloat100k(), value.Y.ToFloat100k(), value.Z.ToFloat100k());
        }

        public static IntDelta ToIntDelta(this Vector3 value)
        {
            return new IntDelta
            {
                X = value.x.ToInt100k(),
                Y = value.y.ToInt100k(),
                Z = value.z.ToInt100k()
            };
        }

        public static Vector3 ToVector3(this IntDelta value)
        {
            return new Vector3(value.X.ToFloat100k(), value.Y.ToFloat100k(), value.Z.ToFloat100k());
        }

        public static IntAbsolute ToIntAbsolute(this float3 value)
        {
            return new IntAbsolute
            {
                X = value.x.ToInt100k(),
                Y = value.y.ToInt100k(),
                Z = value.z.ToInt100k()
            };
        }

        public static float3 ToFloat3(this IntAbsolute value)
        {
            return new float3(value.X.ToFloat100k(), value.Y.ToFloat100k(), value.Z.ToFloat100k());
        }

        public static Coordinates ToSpatialCoordinates(this Vector3 unityVector3)
        {
            return new Coordinates
            {
                X = unityVector3.x,
                Y = unityVector3.y,
                Z = unityVector3.z
            };
        }
    }
}

