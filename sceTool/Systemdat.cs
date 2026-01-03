using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace sceTool
{
    public class Systemdat
    {
        private const ulong SYSTEMDAT_MAGIC = 0X2020202053475241;  //ARGS    
        private List<SystemEntry> DatEntries = new List<SystemEntry>();
        private List<SystemDatSymbol> DatSymbols = new List<SystemDatSymbol>();
        private Encoding encoding = Encoding.ASCII;

        public struct SystemEntry
        {
            public uint NameLen;
            public int Value;
            public int Type;
            public byte[] Name;
        }

        public class SystemDatSymbol
        {
            public int Value;
            public int Type;
            public string LabelName;
            public string LabelValue;
            public uint? LabelOffset;

        }

        public void DumpDat(string filepath, string outpath)
        {
            using (BinaryReader br = new BinaryReader(File.OpenRead(filepath)))
            {
                ulong magic = br.ReadUInt64();
                if (magic != SYSTEMDAT_MAGIC)
                    throw new Exception("Not valid system.dat file");
                int EntryCount = br.ReadInt32();

                for (int i = 0; i < EntryCount; i++)
                {
                    var entry = new SystemEntry();
                    entry.NameLen = br.ReadUInt32();
                    entry.Value = br.ReadInt32();
                    entry.Type = br.ReadInt32();
                    entry.Name = br.ReadBytes((int)entry.NameLen);
                    DatEntries.Add(entry);
                }
            }
            ReadSymbols();
            var json = JsonConvert.SerializeObject(DatSymbols, Formatting.Indented);
            File.WriteAllText(outpath, json);
        }

        private void ReadSymbols()
        {
            foreach(var entry in DatEntries)
            {
                var parts = StrUtil.SplitByNull(entry.Name);
                if (parts.Count > 2) throw new Exception($"Check Symbol: {parts[0]}");
                var offset = StrUtil.GetSymbolOffset(parts[1]);
                var symbol = new SystemDatSymbol
                {
                    Value = entry.Value,
                    Type = entry.Type,
                    LabelName = parts[0],
                    LabelValue = parts[1],
                    LabelOffset = offset == null ? null : offset
                };
                DatSymbols.Add(symbol);
            }
        }

        

    }
}
