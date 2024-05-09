using Microsoft.AspNetCore.Mvc.ModelBinding;
using SoulsFormats;
using System;
using System.Collections.Generic;
using System.IO;
using static AquaModelLibrary.Data.BillyHatcher.StageDef;

namespace StudioCore.MsbEditor.MSBTypes.MSBillyData;
public partial class MSBilly : IMsb
{
    public ModelParam Models { get; set; }
    IMsbParam<IMsbModel> IMsb.Models => Models;

    public EventParam Events { get; set; }
    IMsbParam<IMsbEvent> IMsb.Events => Events;

    public PointParam Regions { get; set; }
    IMsbParam<IMsbRegion> IMsb.Regions => Regions;

    public PartsParam Parts { get; set; }
    IMsbParam<IMsbPart> IMsb.Parts => Parts;

    public DCX.Type Compression { get => DCX.Type.None; set => Compression = value; }

    public static MSBilly Read(StageDefinition stageDef)
    {
        var msb = new MSBilly();


        msb.Models = new ModelParam();
        msb.Events = new EventParam();
        msb.Regions = new PointParam();
        msb.Parts = new PartsParam();

        return msb;
    }

    public byte[] Write()
    {
        throw new NotImplementedException();
    }

    public byte[] Write(DCX.Type compression)
    {
        throw new NotImplementedException();
    }

    public void Write(string path)
    {
        throw new NotImplementedException();
    }

    public void Write(string path, DCX.Type compression)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// A generic group of entries in an MSB.
    /// </summary>
    public abstract class Param<T> where T : Entry
    {
        /// <summary>
        /// A string identifying the type of entries in the param.
        /// </summary>
        internal abstract string Name { get; }

        internal List<T> Read(BinaryReaderEx br)
        {
            br.AssertInt32(0);
            int nameOffset = br.ReadInt32();
            int offsetCount = br.ReadInt32();
            int[] entryOffsets = br.ReadInt32s(offsetCount - 1);
            int nextParamOffset = br.ReadInt32();

            string name = br.GetASCII(nameOffset);
            if (name != Name)
                throw new InvalidDataException($"Expected param \"{Name}\", got param \"{name}\"");

            var entries = new List<T>(offsetCount - 1);
            foreach (int offset in entryOffsets)
            {
                br.Position = offset;
                entries.Add(ReadEntry(br));
            }
            br.Position = nextParamOffset;
            return entries;
        }

        internal abstract T ReadEntry(BinaryReaderEx br);

        internal virtual void Write(BinaryWriterEx bw, List<T> entries)
        {
            bw.WriteInt32(0);
            bw.ReserveInt32("ParamNameOffset");
            bw.WriteInt32(entries.Count + 1);
            for (int i = 0; i < entries.Count; i++)
                bw.ReserveInt32($"EntryOffset{i}");
            bw.ReserveInt32("NextParamOffset");

            bw.FillInt32("ParamNameOffset", (int)bw.Position);
            bw.WriteASCII(Name, true);
            bw.Pad(4);

            int id = 0;
            Type type = null;
            for (int i = 0; i < entries.Count; i++)
            {
                if (type != entries[i].GetType())
                {
                    type = entries[i].GetType();
                    id = 0;
                }

                bw.FillInt32($"EntryOffset{i}", (int)bw.Position);
                entries[i].Write(bw, id);
                id++;
            }
        }

        /// <summary>
        /// Returns all of the entries in this param, in the order they will be written to the file.
        /// </summary>
        public abstract List<T> GetEntries();

        /// <summary>
        /// Returns the name of the param as a string.
        /// </summary>
        public override string ToString()
        {
            return $"{Name}";
        }
    }

    /// <summary>
    /// A generic entry in an MSB param.
    /// </summary>
    public abstract class Entry : IMsbEntry
    {
        /// <summary>
        /// The name of this entry.
        /// </summary>
        public string Name { get; set; }

        internal abstract void Write(BinaryWriterEx bw, int id);
    }
}
