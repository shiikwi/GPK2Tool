using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using gfbTool;

namespace sceTool
{
    public class scbFile
    {
        private const uint SCB_MAGIC = 0X4B504C47;  //GLPK

        private struct scbHeader
        {
            public uint magic;
            public uint UnpackSize;
            public uint EncryptFlag;
        }

        public void ExportScb(string filepath, string outpath)
        {
            using (BinaryReader br = new BinaryReader(File.OpenRead(filepath)))
            {
                var header = new scbHeader
                {
                    magic = br.ReadUInt32(),
                    UnpackSize = br.ReadUInt32(),
                    EncryptFlag = br.ReadUInt32(),
                };

                if (header.magic != SCB_MAGIC)
                    throw new Exception("Not valid scb file");

                var sceData = br.ReadBytes((int)br.BaseStream.Length - 0xC);
                if (header.EncryptFlag != 0) NOTDecrypt(sceData);

                var lzss = new LzssStream();
                sceData = lzss.Decompress(sceData, header.UnpackSize);
                var outStrings = Encoding.GetEncoding("shift_jis").GetString(sceData);
                File.WriteAllText(outpath, outStrings);
                //File.WriteAllText(outpath, outStrings, Encoding.GetEncoding("shift_jis"));
            }
        }

        public void ImportScb(string filepath, string scbpath, string sf0path)
        {
            var encoding = Encoding.GetEncoding("gbk", new EncoderExceptionFallback(), new DecoderExceptionFallback());
            string rawText = File.ReadAllText(filepath);
            //rawText = CleanText(rawText);
            List<uint> POffset = new List<uint>();
            byte[] buffer = encoding.GetBytes(rawText);

            string encodeStrings = encoding.GetString(buffer);
            var match = Regex.Matches(encodeStrings, @"\b(?i)Print\b(?=\s*\()");
            foreach (Match mc in match)
            {
                uint byteOff = (uint)encoding.GetByteCount(encodeStrings.Substring(0, mc.Index));
                POffset.Add(byteOff);
            }

            var header = new scbHeader
            {
                magic = SCB_MAGIC,
                EncryptFlag = 1
            };

            var lzss = new LzssStream();
            byte[] CompressBuffer = lzss.Compress(buffer);
            header.UnpackSize = (uint)buffer.Length;
            NOTDecrypt(CompressBuffer);

            using (FileStream fs = new FileStream(scbpath, FileMode.Create))
            using (BinaryWriter bw = new BinaryWriter(fs))
            {
                bw.Write(header.magic);
                bw.Write(header.UnpackSize);
                bw.Write(header.EncryptFlag);
                bw.Write(CompressBuffer);
            }

            if (POffset.Count == 0) return;
            using (FileStream fs = new FileStream(sf0path, FileMode.Create))
            using (BinaryWriter bw = new BinaryWriter(fs))
            {
                bw.Write((uint)POffset.Count);
                foreach (uint offset in POffset)
                {
                    bw.Write(offset);
                }
            }
        }

        private void NOTDecrypt(byte[] data)
        {
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = (byte)(~data[i]);
            }
        }

        private string CleanText(string text)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in text)
            {
                if (c == '\u301c') sb.Append('~');
                else if (c == '\u30fb') sb.Append('·');
                else sb.Append(c);
            }
            return sb.ToString();
        }
    }
}
