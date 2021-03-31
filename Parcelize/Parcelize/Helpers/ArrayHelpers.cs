using System;
using System.Runtime.CompilerServices;

namespace Zintom.Parcelize.Helpers
{
    /// <summary>
    /// 
    /// </summary>
    public static class ArrayHelpers
    {
        /// <summary>
        /// Combines the given <paramref name="arrays"/> into one large array.
        /// </summary>
        /// <returns>The <paramref name="arrays"/> combined as one.</returns>
        public static byte[] CombineArrays(params byte[][] arrays)
        {
            int totalSize = 0;
            for (int i = 0; i < arrays.Length; i++)
            {
                totalSize += arrays[i].Length;
            }

            byte[] combined = new byte[totalSize];

            int bytesCopied = 0;
            for (int i = 0; i < arrays.Length; i++)
            {
                Array.Copy(arrays[i], 0, combined, bytesCopied, arrays[i].Length);
                bytesCopied += arrays[i].Length;
            }

            return combined;
        }
    }
}
