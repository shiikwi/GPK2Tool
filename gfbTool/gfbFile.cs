using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;

namespace gfbTool
{
    public class gfbFile
    {
        private const uint GFB_MAGIC = 0x20424647;  //GFB 
        private const int HEADER_SZIE = 0X40;

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct BITMAP
        {
            public uint biSize;
            public int biWidth;
            public int biHeight;
            public ushort biPlanes;
            public ushort biBitCount;
            public uint biCompression;
            public uint biSizeImage;
            public int biXPelsPerMeter;
            public int biYPelsPerMeter;
            public uint biClrUsed;
            public uint biClrImportant;
        };
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct gfbHeader
        {
            public uint magic;
            public uint EncryptFlag;
            public uint FormatFlag;
            public uint PackSize;
            public uint UnPackSize;
            public uint DataOffset;
            public BITMAP bitmap;
        };

        public byte[] gfb2bmp(string filepath)
        {
            using (BinaryReader reader = new BinaryReader(File.OpenRead(filepath)))
            {
                var headerbytes = reader.ReadBytes(HEADER_SZIE);
                var header = Util.ByteArrayToStruct<gfbHeader>(headerbytes);
                if (header.magic != GFB_MAGIC)
                    throw new Exception("Not valid GFB file");

                byte[] rawdata = reader.ReadBytes((int)reader.BaseStream.Length - HEADER_SZIE);
                if (header.EncryptFlag != 0)
                {
                    uint xorKey = header.UnPackSize | (~header.PackSize);
                    Util.CycleXor(rawdata, xorKey);
                }

                byte[] pixelData;
                if (header.FormatFlag != 0)
                {
                    var lzss = new LzssStream();
                    pixelData = lzss.Decompress(rawdata, header.UnPackSize);
                }
                else
                {
                    pixelData = rawdata;
                }

                return BuildBmp(header.bitmap, pixelData);
            }
        }

        private byte[] BuildBmp(BITMAP header, byte[] data)
        {
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter bw = new BinaryWriter(ms))
            {
                bw.Write((ushort)0x4D42);  //BM
                bw.Write((uint)(0x36 + data.Length));
                bw.Write((uint)0);
                bw.Write((uint)0x36);

                bw.Write(header.biSize);
                bw.Write(header.biWidth);
                bw.Write(header.biHeight);
                bw.Write(header.biPlanes);
                bw.Write(header.biBitCount);
                bw.Write(header.biCompression);
                bw.Write(header.biSizeImage);
                bw.Write(header.biXPelsPerMeter);
                bw.Write(header.biYPelsPerMeter);
                bw.Write(header.biClrUsed);
                bw.Write(header.biClrImportant);

                bw.Write(data);
                return ms.ToArray();
            }
        }

        public void bmp2gfb(string filepath, string gfbpath)
        {
            bool Ifencrypt = false;
            bool IfCompress = true;   //Also can create no Compress gfb file
            using (BinaryReader br = new BinaryReader(File.OpenRead(filepath)))
            {
                if (br.ReadUInt16() != 0x4D42)
                    throw new Exception("Not valid BMP file");

                br.BaseStream.Seek(8, SeekOrigin.Current);
                uint offset = br.ReadUInt32();
                var BITMAPbytes = br.ReadBytes(0x28);
                if (br.BaseStream.Position != offset)
                    throw new Exception("Not Support 8 bit bmp");

                var pixelData = br.ReadBytes((int)(br.BaseStream.Length - offset));
                var header = new gfbHeader
                {
                    magic = GFB_MAGIC,
                    EncryptFlag = (uint)(Ifencrypt ? 1 : 0),
                    FormatFlag = (uint)(IfCompress ? 1 : 0),
                    PackSize = 0,
                    UnPackSize = (uint)pixelData.Length,
                    DataOffset = HEADER_SZIE
                };

                byte[] CompressPixel = pixelData;
                if (IfCompress)
                {
                    var lzss = new LzssStream();
                    CompressPixel = lzss.Compress(pixelData);
                    header.PackSize = (uint)CompressPixel.Length;
                }

                if(Ifencrypt)
                {
                    uint xorKey = header.PackSize == 0 ? 0xFFFFFFFF : header.UnPackSize | (~header.PackSize);
                    Util.CycleXor(CompressPixel, xorKey);
                }

                using (FileStream fs = new FileStream(gfbpath, FileMode.Create))
                using (BinaryWriter bw = new BinaryWriter(fs))
                {
                    bw.Write(header.magic);
                    bw.Write(header.EncryptFlag);
                    bw.Write(header.FormatFlag);
                    bw.Write(header.PackSize);
                    bw.Write(header.UnPackSize);
                    bw.Write(header.DataOffset);

                    bw.Write(BITMAPbytes);
                    bw.Write(CompressPixel);
                }
            }
        }

    }
}
