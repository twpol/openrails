// COPYRIGHT 2009, 2010, 2011, 2012, 2013, 2014, 2015 by the Open Rails project.
// 
// This file is part of Open Rails.
// 
// Open Rails is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Open Rails is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Open Rails.  If not, see <http://www.gnu.org/licenses/>.

using Orts.Parsers.Msts;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Orts.Formats.Msts
{
    // This relates to TrPathFlags, which is not always present in .pat file
    // It is unclear what the other flags (bits) mean
    [Flags]
    public enum PathFlags
    {
        NotPlayerPath = 0x20,
    }

    /// <summary>A path is a route through a series of waypoints.</summary>
    /// <remarks>
    /// <para>
    /// The MSTS path files (.PAT) contain a series of waypoints (in world coordinates) through which it runs. They also contain instructions about which fork to take at switches and reverse points.
    /// </para>
    /// <para>
    /// They are organised are two collections of data: nodes (<see cref="TrPathNode"/>) and locations (<see cref="TrackPDP"/>).
    /// </para>
    /// <para>
    /// The nodes (<see cref="TrPathNode"/>) contain flags (indicating whether it is a waiting point, etc.), links to the subsequent nodes and a link to a location.
    /// </para>
    /// <para>
    /// The locations (<see cref="TrackPDP"/>) contain world coordinates and flags (indicating whether this location is a switch, etc.).
    /// </para>
    /// </remarks>
    public class PathFile
    {
        public string PathID { get; private set; }
        public string Name { get; private set; }
        public string Start { get; private set; }
        public string End { get; private set; }
        public PathFlags Flags { get; private set; }
        public bool IsPlayerPath { get { return (Flags & PathFlags.NotPlayerPath) == 0; } }

        public List<TrackPDP> TrackPDPs { get; private set; }
        public List<TrPathNode> TrPathNodes { get; private set; }

        /// <summary>
        /// Open a PAT file, parse it and store it
        /// </summary>
        /// <param name="filePath">path to the PAT file, including full path and extension</param>
        public PathFile(string filePath)
        {
            TrackPDPs = new List<TrackPDP>();
            TrPathNodes = new List<TrPathNode>();
            try
            {
                using (STFReader stf = new STFReader(filePath, false))
                    stf.ParseFile(new STFReader.TokenProcessor[] {
                    new STFReader.TokenProcessor("trackpdps", ()=>{ stf.MustMatch("("); stf.ParseBlock(new STFReader.TokenProcessor[] {
                        new STFReader.TokenProcessor("trackpdp", ()=>{ TrackPDPs.Add(new TrackPDP(stf)); }),
                    });}),
                    new STFReader.TokenProcessor("trackpath", ()=>{ stf.MustMatch("("); stf.ParseBlock(new STFReader.TokenProcessor[] {
                        new STFReader.TokenProcessor("trpathname", ()=>{ PathID = stf.ReadStringBlock(null); }),
                        new STFReader.TokenProcessor("name", ()=>{ Name = stf.ReadStringBlock(null); }),
                        new STFReader.TokenProcessor("trpathflags", ()=>{ Flags = (PathFlags)stf.ReadHexBlock(null); }),
                        new STFReader.TokenProcessor("trpathstart", ()=>{ Start = stf.ReadStringBlock(null); }),
                        new STFReader.TokenProcessor("trpathend", ()=>{ End = stf.ReadStringBlock(null); }),
                        new STFReader.TokenProcessor("trpathnodes", ()=>{
                            stf.MustMatch("(");
                            var count = stf.ReadInt(null);
                            stf.ParseBlock(new STFReader.TokenProcessor[] {
                                new STFReader.TokenProcessor("trpathnode", ()=>{
                                    if (--count < 0)
                                        STFException.TraceWarning(stf, "Skipped extra TrPathNodes");
                                    else
                                        TrPathNodes.Add(new TrPathNode(stf));
                                }),
                            });
                            if (count > 0)
                                STFException.TraceWarning(stf, count + " missing TrPathNodes(s)");
                        }),
                    });}),
                });
            }
            catch (Exception error)
            {
                Trace.TraceWarning(error.Message);
            }
        }

        public override string ToString()
        {
            return Name;
        }
    }

    /// <summary>
    /// Specifies the type of the track node/location.
    /// </summary>
    public enum TrackPDPType
    {
        Straight = 1,
        Junction = 2,
    }

    /// <summary>
    /// Specifies some additional flags for the track node/location.
    /// </summary>
    /// <remarks>
    /// The most common values are <c>0</c> and <c>1</c>, followed by <c>9</c>. Less common values are <c>2</c>, <c>12</c>, <c>3</c>, <c>13</c> and <c>11</c>.
    /// </remarks>
    /// Assuming in decimal:
    ///    0  0000
    ///    1  0001
    ///    9  1001
    ///    2  0010
    ///   12  1100
    ///    3  0011
    ///   13  1101
    ///   11  1011
    /// Assuming in hexadecimal:
    ///    0  00000000
    ///    1  00000001
    ///    9  00001001
    ///    2  00000010
    ///   12  00010010
    ///    3  00000011
    ///   13  00010011
    ///   11  00010001
    [Flags]
    public enum TrackPDPFlags
    {
        None = 0,
        Unk1 = 1,
        Broken = 2,
        Unk3 = 4,
        ChangedInEditor = 8,
    }

    // PDP might mean "Predetermined Position" according to http://acronyms.thefreedictionary.com/PDP.
    public class TrackPDP
    {
        // We are not using WorldLocation to keep MSTS file parsing independent of other parts of the code.
        public readonly int TileX;
        public readonly int TileZ;
        public readonly float X, Y, Z;
        public readonly TrackPDPType Type;
        public readonly TrackPDPFlags Flags;

        public bool IsJunction { get { return Type == TrackPDPType.Junction; } }
        public bool IsInvalid { get { return false; } }

        public TrackPDP(STFReader stf)
        {
            stf.MustMatch("(");
            TileX = stf.ReadInt(null);
            TileZ = stf.ReadInt(null);
            X = stf.ReadFloat(STFReader.UNITS.None, null);
            Y = stf.ReadFloat(STFReader.UNITS.None, null);
            Z = stf.ReadFloat(STFReader.UNITS.None, null);
            Type = (TrackPDPType)stf.ReadInt(0);
            Flags = (TrackPDPFlags)stf.ReadInt(0);
            stf.SkipRestOfBlock();
        }
    }

    /// <summary>
    /// Specifies what type of node within the path it is.
    /// </summary>
    /// <remarks>
    /// The most common values are <c>0000</c>, <c>0004</c>, <c>0001</c>, <c>0008</c> and <c>0002</c>. Less common values are <c>0010</c> and <c>0014</c>. Rare values are <c>0009</c>, <c>000C</c>, <c>0018</c>, <c>000A</c>, <c>0003</c> and <c>0011</c>.
    /// </remarks>
    [Flags]
    public enum TrPathNodeFlags
    {
        None = 0,
        ReversePoint = 1,
        WaitPoint = 2,
        IntermediatePoint = 4,
        UnkOtherExit = 8,
        Optional = 16,
    }

    /// <para>
    /// Most common combinations:
    ///   Track  Node  Interpretation
    ///   2   0  0000  Junction
    ///   1   1  0000  Start/end node (unk1)
    ///   1   1  0004  Intermediate node (unk1)
    ///   1   1  0001  Reverse point (unk1)
    ///   1   9  0000  Start/end node (unk1, changed in editor)
    ///   1   1  0002  Waiting point (unk1)
    ///   2   0  0008  Junction (other exit?)
    /// Less common combinations:
    ///   2   2  0008  Junction (broken, other exit?)
    ///   1   9  0001  Reverse point (unk1, changed in editor)
    ///   1   9  0002  Waiting point (unk1, changed in editor)
    ///   2  12  0000  Junction (unk3, changed in editor)
    ///   1   0  0000  ???
    ///   2   0  0010  Junction (optional)
    ///   1   3  0004  Intermediate node (unk1, broken)
    /// Rare combinations:
    ///   1   1  0014  Intermediate node (unk1, optional)
    ///   1  13  0000  ???
    ///   1   1  0008  ???
    ///   2  12  0008  Junction (unk3, changed in editor, other exit?)
    ///   1   1  000c  ???
    ///   1  11  0000  ???
    ///   1   3  0000  ???
    ///   1  13  0009  ???
    ///   1   1  0010  Start/end node (unk1, optional)
    ///   1   1  0009  ???
    ///   2   0  0001  Junction
    ///   1  13  0008  ???
    ///   1  13  0001  Reverse point (unk1, unk3, changed in editor)
    ///   1   1  000a  ???
    ///   2   2  0018  Junction (broken, other exit?, optional)
    ///   2   0  0018  Junction (other exit?, optional)
    ///   1   9  0003  ???
    ///   1  13  0004  Intermediate node (unk1, unk3, changed in editor)
    ///   1   1  0011  Reverse point (unk1, optional)
    ///   2   2  0000  Junction (broken)
    ///   1   9  0004  Intermediate node (unk1, changed in editor)
    ///   1   9  0008  ???
    /// 
    ///  2  0    0  node is at junction
    ///  1  1    0  node is start or end point
    ///  1  1    4  node is some intermediate point, not at junction
    ///  1  1    1  Reversal point
    ///  1 12    x  Seems to be indicating a path that is broken (or perhaps simply unfinished)
    ///  1  9    x  Sometimes seen this as a end or beginning of route, but not always
    ///  2  0    8  (e.g. Shiatsu, not clear why). It does not seem to be 'other exit'
    /// </para>
    /// <remarks>
    /// <code>
    /// TrPathNode ( Flags NextMainNode NextSidingNode PDPIndex )
    /// </code>
    /// <para>
    /// The flags included in a <see cref="TrPathNode"/> are broken down in to two components. The low 16 bits are as follows:
    /// </para>
    /// <list type="bullet">
    ///   <item>
    ///     <term>0x0001</term>
    ///     <description>Reverse point, where the path changes direction.</description>
    ///   </item>
    ///   <item>
    ///     <term>0x0002</term>
    ///     <description>Waiting point, where the path continues in the same direction but trains will stop here for a designated period of time</description>
    ///   </item>
    ///   <item>
    ///     <term>0x0004</term>
    ///     <description>Intermediate point, used to distinguish between otherwise ambigious paths.</description>
    ///   </item>
    ///   <item>
    ///     <term>0x0008</term>
    ///     <description>Other exit, ???</description>
    ///   </item>
    ///   <item>
    ///     <term>0x0010</term>
    ///     <description>Optional section, through which the train does not have to pass?</description>
    ///   </item>
    /// </list>
    /// <para>
    /// The high 16 bits of the flags are the waiting time (for waiting points) in seconds.
    /// </para>
    /// <para>
    /// NextMainNode, NextSidingNode:
    /// Indexes in to list of TrPathNode.
    /// -1/4294967295/0xFFFFFFFF indicates no subsequent node.
    /// </para>
    /// </remarks>
    public class TrPathNode
    {
        public TrPathNodeFlags Flags;
        public uint WaitingTime;

        public uint pathFlags, nextMainNode, nextSidingNode, fromPDP;

        // Note, pathFlags is a complicated beast, which is not fully understood, see AIPath.cs

        public bool HasNextMainNode { get { return (nextMainNode != 0xffffffff); } }
        public bool HasNextSidingNode { get { return (nextSidingNode != 0xffffffff); } }

        public TrPathNode(STFReader stf)
        {
            stf.MustMatch("(");
            pathFlags = stf.ReadHex(0);
            Flags = (TrPathNodeFlags)(pathFlags & 0xFFFF);
            WaitingTime = pathFlags >> 16;
            nextMainNode = stf.ReadUInt(null);
            nextSidingNode = stf.ReadUInt(null);
            fromPDP = stf.ReadUInt(null);
            stf.SkipRestOfBlock();
        }

        public TrPathNode(uint flags, uint nextNode, uint nextSiding, uint pdp)
        {
            pathFlags = flags;
            nextMainNode = nextNode;
            nextSidingNode = nextSiding;
            fromPDP = pdp;
        }
    }
}
