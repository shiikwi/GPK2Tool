using System;
using System.Collections.Generic;
using System.Text;

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

                sceData = gfbTool.Util.LzssDecompress(sceData, header.UnpackSize);
                var outStrings = Encoding.GetEncoding("shift_jis").GetString(sceData);
                File.WriteAllText(outpath, outStrings, new UTF8Encoding(false));
            }
        }

        private void NOTDecrypt(byte[] data)
        {
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = (byte)(~data[i]);
            }
        }

    }
}
