using UnityEngine;
using Unity.Mathematics;

namespace Improbable.Gdk.StandardTypes
{
    public static class Vector3Extensions
    {
        private const float BIT_OFFSET_10K = 10000f;

        public static int ToInt10k(this float value)
        {
            return Mathf.RoundToInt(value * BIT_OFFSET_10K);
        }

        public static float ToFloat10k(this int value)
        {
            return ((float)value) / BIT_OFFSET_10K;
        }

        public static IntAbsolute ToIntAbsolute(this Vector3 value)
        {
            return new IntAbsolute
            {
                X = value.x.ToInt10k(),
                Y = value.y.ToInt10k(),
                Z = value.z.ToInt10k()
            };
        }

        public static Vector3 ToVector3(this IntAbsolute value)
        {
            return new Vector3(value.X.ToFloat10k(), value.Y.ToFloat10k(), value.Z.ToFloat10k());
        }

        public static IntDelta ToIntDelta(this Vector3 value)
        {
            return new IntDelta
            {
                X = value.x.ToInt10k(),
                Y = value.y.ToInt10k(),
                Z = value.z.ToInt10k()
            };
        }

        public static Vector3 ToVector3(this IntDelta value)
        {
            return new Vector3(value.X.ToFloat10k(), value.Y.ToFloat10k(), value.Z.ToFloat10k());
        }

        public static IntAbsolute ToIntAbsolute(this float3 value)
        {
            return new IntAbsolute
            {
                X = value.x.ToInt10k(),
                Y = value.y.ToInt10k(),
                Z = value.z.ToInt10k()
            };
        }

        public static float3 ToFloat3(this IntAbsolute value)
        {
            return new float3(value.X.ToFloat10k(), value.Y.ToFloat10k(), value.Z.ToFloat10k());
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

        public static float3 ToFloat3(this Coordinates coord)
        {
            return new float3((float)coord.X, (float)coord.Y, (float)coord.Z);
        }

        public static float Radians(float3 from, float3 to)
        {
            var dot = math.dot(math.normalizesafe(from), math.normalizesafe(to));
            return math.acos(dot);
        }
    }
}
