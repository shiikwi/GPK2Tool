using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
#pragma warning disable CS8605

namespace gfbTool
{
    public static class Util
    {
        public static T ByteArrayToStruct<T>(byte[] bytes) where T : struct
        {
            GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            try { return (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T)); }
            finally { handle.Free(); }
        }

        public static void CycleXor(byte[] data, uint key)
        {
            ulong key8 = ((ulong)key << 32) | key;
            int i = 0;
            for (; i <= data.Length - 8; i += 8)
            {
                ulong val = BitConverter.ToUInt64(data, i);
                BitConverter.GetBytes(val ^ key8).CopyTo(data, i);
            }
            byte[] keyBytes = BitConverter.GetBytes(key);
            for (int j = 0; i < data.Length; i++, j++)
            {
                data[i] ^= keyBytes[j % 4];
            }
        }
    }
}
