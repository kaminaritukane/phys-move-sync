using Unity.Mathematics;

namespace Improbable.Gdk.StandardTypes
{
    public static class TransformUtils
    {
        public static quaternion ToUnityQuaternion(this CompressedQuaternion compressedQuaternion)
        {
            // The raw uint representing a compressed quaternion.
            var compressedValue = compressedQuaternion.Data;

            // q[x, y, z, w]
            var q = new float4();

            // Mask of 23 0's and 9 1's.
            const uint mask = (1u << 9) - 1u;

            // Only need the two leftmost bits to find the index of the largest component.
            int largestIndex = (int)(compressedValue >> 30);
            float sumSquares = 0;
            for (var i = 3; i >= 0; --i)
            {
                if (i != largestIndex)
                {
                    // Apply mask to return the 9 bits representing a component's value.
                    uint magnitude = compressedValue & mask;

                    // Get the 10th bit from the right (the signbit of the component).
                    uint signBit = (compressedValue >> 9) & 0x1;

                    // Convert back from the range [0,1] to [0, 1/sqrt(2)].
                    q[i] = SqrtHalf * ((float)magnitude) / mask;

                    // If signbit is set, negate the value.
                    if (signBit == 1)
                    {
                        q[i] *= -1;
                    }

                    // Add to the rolling sum of each component's square value.
                    sumSquares += math.pow(q[i], 2);

                    // Shift right by 10 so that the next component's bits are evaluated in the next loop iteration.
                    compressedValue >>= 10;
                }
            }

            // The value of the largest component is 1 - the sum of the squares of the smallest three components.
            q[largestIndex] = math.sqrt(1f - sumSquares);

            return new quaternion(q[0], q[1], q[2], q[3]);
        }

        private const float SqrtHalf = 0.70710678118f;

        public static CompressedQuaternion ToCompressedQuaternion(this quaternion quaternion)
        {
            // Ensure we have a unit quaternion before compression.
            quaternion = math.normalizesafe(quaternion);

            // Stack allocate float array to ensure it's discarded when the method returns.
            var q = quaternion.value;

            // Iterate through quaternion to find the index of the largest component.
            uint largestIndex = 0;
            var largestValue = math.abs(q[(int)largestIndex]);
            for (uint i = 1; i < 4; ++i)
            {
                var componentAbsolute = math.abs(q[(int)i]);
                if (componentAbsolute > largestValue)
                {
                    largestIndex = i;
                    largestValue = componentAbsolute;
                }
            }

            // Since -q == q, transform the quaternion so that the largest component is positive. This means the sign
            // bit of the largest component does not need to be sent.
            uint negativeBit = quaternion.value[(int)largestIndex] < 0 ? 1u : 0u;

            // Initialise a uint with the index of the largest component. The variable is shifted left after each
            // section of the uint is populated. At the end of the loop, the uint has the following structure:
            // |largest index (2)|signbit (1) + component (9)|signbit (1) + component (9)|signbit (1) + component (9)|
            uint compressedQuaternion = largestIndex;
            for (uint i = 0; i < 4; i++)
            {
                if (i != largestIndex)
                {
                    // If quaternion needs to be transformed, flip the sign bit.
                    uint signBit = (q[(int)i] < 0 ? 1u : 0u) ^ negativeBit;

                    // The maximum possible value of the second largest component in a unit quaternion is 1/sqrt(2), so
                    // translate the value from the range [0, 1/sqrt(2)] to [0, 1] for higher precision. Add 0.5f for
                    // rounding up the value before casting to a uint.
                    uint magnitude = (uint)(((1u << 9) - 1u) * (math.abs(q[(int)i]) / SqrtHalf) + 0.5f);

                    // Shift uint by 10 bits then place the component's sign bit and 9-bit magnitude in the gap.
                    compressedQuaternion = (compressedQuaternion << 10) | (signBit << 9) | magnitude;
                }
            }

            return new CompressedQuaternion(compressedQuaternion);
        }
    }
}
