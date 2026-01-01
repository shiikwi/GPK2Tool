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
            for(int j = 0; i < data.Length; i++, j++)
            {
                data[i] ^= keyBytes[j % 4];
            }
        }

        public static byte[] LzssDecompress(byte[] input, uint outLen)
        {
            byte[] output = new byte[outLen];
            byte[] window = new byte[4096];
            int winPos = 4078;
            int inPtr = 0, outPtr = 0;
            uint flags = 0;

            while (outPtr < outLen && inPtr < input.Length)
            {
                flags >>= 1;
                if ((flags & 0x100) == 0)
                {
                    flags = (uint)input[inPtr++] | 0xFF00;
                }

                if ((flags & 1) != 0)
                {
                    byte b = input[inPtr++];
                    output[outPtr++] = b;
                    window[winPos] = b;
                    winPos = (winPos + 1) & 0xFFF;
                }
                else
                {
                    if (inPtr + 1 >= input.Length) break;
                    int b1 = input[inPtr++];
                    int b2 = input[inPtr++];

                    int offset = ((b2 & 0xF0) << 4) | b1;
                    int count = (b2 & 0x0F) + 2;

                    for (int i = 0; i <= count; i++)
                    {
                        byte b = window[(offset + i) & 0xFFF];
                        if (outPtr < outLen)
                        {
                            output[outPtr++] = b;
                            window[winPos] = b;
                            winPos = (winPos + 1) & 0xFFF;
                        }
                    }
                }
            }
            return output;
        }

    }
}
