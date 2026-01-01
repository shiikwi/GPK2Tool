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

        public static byte[] lzssCompress(byte[] input)
        {
            if (input == null || input.Length == 0) return Array.Empty<byte>();

            MemoryStream output = new MemoryStream();
            byte[] window = new byte[4096];
            int winPos = 4078;
            int inPtr = 0;

            while (inPtr < input.Length)
            {
                long controlPos = output.Position;
                output.WriteByte(0);
                byte flagByte = 0;

                for (int i = 0; i < 8 && inPtr < input.Length; i++)
                {
                    int matchPos = 0;
                    int matchLen = 0;

                    int maxMatchLimit = Math.Min(18, input.Length - inPtr);

                    if (maxMatchLimit >= 3)
                    {
                        for (int j = 0; j < 4096; j++)
                        {
                            if (window[j] != input[inPtr]) continue;

                            int currLen = 1;
                            while (currLen < maxMatchLimit &&
                                   window[(j + currLen) & 0xFFF] == input[inPtr + currLen])
                            {
                                currLen++;
                            }

                            if (currLen > matchLen)
                            {
                                matchLen = currLen;
                                matchPos = j;
                                if (matchLen == 18) break;
                            }
                        }
                    }

                    if (matchLen >= 3)
                    {
                        output.WriteByte((byte)(matchPos & 0xFF));
                        output.WriteByte((byte)(((matchPos >> 4) & 0xF0) | ((matchLen - 3) & 0x0F)));

                        for (int k = 0; k < matchLen; k++)
                        {
                            window[winPos] = input[inPtr++];
                            winPos = (winPos + 1) & 0xFFF;
                        }
                    }
                    else
                    {
                        flagByte |= (byte)(1 << i);
                        byte b = input[inPtr++];
                        output.WriteByte(b);
                        window[winPos] = b;
                        winPos = (winPos + 1) & 0xFFF;
                    }
                }
                long currentEnd = output.Position;
                output.Position = controlPos;
                output.WriteByte(flagByte);
                output.Position = currentEnd;
            }
            return output.ToArray();
        }

    }
}
