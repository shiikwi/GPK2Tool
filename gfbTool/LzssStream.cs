using System;
using System.Collections.Generic;
using System.Text;

namespace gfbTool
{
    public class LzssStream
    {
        private const int N = 4096;
        private const int F = 18;
        private const int THRESHOLD = 2;
        private const int NIL = N;

        private int[] lson = new int[N + 1];
        private int[] rson = new int[N + 257];
        private int[] dad = new int[N + 1];

        private byte[] text_buf = new byte[N + F - 1];

        private int match_position;
        private int match_length;

        public LzssStream()
        {
            for (int i = 0; i < lson.Length; i++) lson[i] = 0;
            for (int i = 0; i < rson.Length; i++) rson[i] = 0;
            for (int i = 0; i < dad.Length; i++) dad[i] = 0;
        }

        private void InitTree()
        {
            for (int i = N + 1; i <= N + 256; i++) rson[i] = NIL;
            for (int i = 0; i < N; i++) dad[i] = NIL;
        }

        private void InsertNode(int r)
        {
            int i, p, cmp;

            cmp = 1;
            p = N + 1 + text_buf[r];
            rson[r] = lson[r] = NIL;
            match_length = 0;

            for (; ; )
            {
                if (cmp >= 0)
                {
                    if (rson[p] != NIL) p = rson[p];
                    else { rson[p] = r; dad[r] = p; return; }
                }
                else
                {
                    if (lson[p] != NIL) p = lson[p];
                    else { lson[p] = r; dad[r] = p; return; }
                }

                for (i = 1; i < F; i++)
                {
                    if ((cmp = text_buf[r + i] - text_buf[p + i]) != 0) break;
                }

                if (i > match_length)
                {
                    match_position = p;
                    match_length = i;
                    if (match_length >= F) break;
                }
            }

            dad[r] = dad[p];
            lson[r] = lson[p];
            rson[r] = rson[p];
            dad[lson[p]] = r;
            dad[rson[p]] = r;

            if (rson[dad[p]] == p) rson[dad[p]] = r;
            else lson[dad[p]] = r;

            dad[p] = NIL;
        }

        private void DeleteNode(int p)
        {
            int q;

            if (dad[p] == NIL) return;
            if (rson[p] == NIL) q = lson[p];
            else if (lson[p] == NIL) q = rson[p];
            else
            {
                q = lson[p];
                if (rson[q] != NIL)
                {
                    do { q = rson[q]; } while (rson[q] != NIL);
                    rson[dad[q]] = lson[q];
                    dad[lson[q]] = dad[q];
                    lson[q] = lson[p];
                    dad[lson[p]] = q;
                }
                rson[q] = rson[p];
                dad[rson[p]] = q;
            }
            dad[q] = dad[p];
            if (rson[dad[p]] == p) rson[dad[p]] = q;
            else lson[dad[p]] = q;
            dad[p] = NIL;
        }

        public byte[] Compress(byte[] input)
        {
            if (input == null || input.Length == 0) return Array.Empty<byte>();

            using (MemoryStream ms = new MemoryStream())
            {
                InitTree();

                byte[] code_buf = new byte[17];
                code_buf[0] = 0;
                int code_ptr = 1;
                int mask = 1;

                int s = 0;
                int r = N - F;
                int len = 0;
                int in_ptr = 0;

                Array.Clear(text_buf, 0, text_buf.Length);

                for (len = 0; len < F && in_ptr < input.Length; len++)
                {
                    text_buf[r + len] = input[in_ptr++];
                }

                if (len == 0) return Array.Empty<byte>();

                for (int i = 1; i <= F; i++) InsertNode(r - i);
                InsertNode(r);

                do
                {
                    if (match_length > len) match_length = len;

                    if (match_length > THRESHOLD)
                    {
                        code_buf[code_ptr++] = (byte)(match_position & 0xFF);
                        code_buf[code_ptr++] = (byte)(((match_position >> 4) & 0xF0) | (match_length - (THRESHOLD + 1)));
                    }
                    else
                    {
                        match_length = 1;
                        code_buf[0] |= (byte)mask;
                        code_buf[code_ptr++] = text_buf[r];
                    }

                    mask <<= 1;
                    if (mask == 0x100)
                    {
                        ms.Write(code_buf, 0, code_ptr);
                        code_buf[0] = 0;
                        code_ptr = 1;
                        mask = 1;
                    }

                    int last_match_length = match_length;
                    int i = 0;
                    for (; i < last_match_length && in_ptr < input.Length; i++)
                    {
                        DeleteNode(s);
                        byte b = input[in_ptr++];
                        text_buf[s] = b;
                        if (s < F - 1) text_buf[s + N] = b;
                        s = (s + 1) & (N - 1);
                        r = (r + 1) & (N - 1);
                        InsertNode(r);
                    }

                    while (i++ < last_match_length)
                    {
                        DeleteNode(s);
                        s = (s + 1) & (N - 1);
                        r = (r + 1) & (N - 1);
                        if (--len != 0) InsertNode(r);
                    }
                } while (len > 0);

                if (code_ptr > 1)
                {
                    ms.Write(code_buf, 0, code_ptr);
                }

                return ms.ToArray();
            }
        }
        public byte[] Decompress(byte[] input, uint unpackSize)
        {
            if (input == null || input.Length == 0) return Array.Empty<byte>();

            byte[] output = new byte[unpackSize];
            int outPtr = 0;
            int inPtr = 0;

            byte[] window = new byte[N];
            Array.Clear(window, 0, window.Length);

            int winPtr = N - F;

            uint flags = 0;

            while (outPtr < unpackSize)
            {
                flags >>= 1;
                if ((flags & 0x100) == 0)
                {
                    if (inPtr >= input.Length) break;
                    flags = (uint)(input[inPtr++] | 0xFF00);
                }

                if ((flags & 1) != 0)
                {
                    if (inPtr >= input.Length) break;
                    byte b = input[inPtr++];

                    if (outPtr < unpackSize)
                    {
                        output[outPtr++] = b;
                        window[winPtr] = b;
                        winPtr = (winPtr + 1) & (N - 1);
                    }
                }
                else
                {
                    if (inPtr + 1 >= input.Length) break;

                    int pLo = input[inPtr++];
                    int pHiLen = input[inPtr++];

                    int matchPos = pLo | ((pHiLen & 0xF0) << 4);
                    int matchLen = (pHiLen & 0x0F) + (THRESHOLD + 1);

                    for (int k = 0; k < matchLen; k++)
                    {
                        if (outPtr < unpackSize)
                        {
                            byte b = window[(matchPos + k) & (N - 1)];
                            output[outPtr++] = b;

                            window[winPtr] = b;
                            winPtr = (winPtr + 1) & (N - 1);
                        }
                    }
                }
            }

            return output;
        }

    }
}
