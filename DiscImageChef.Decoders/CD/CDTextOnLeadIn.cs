﻿// /***************************************************************************
// The Disc Image Chef
// ----------------------------------------------------------------------------
//
// Filename       : CDText.cs
// Version        : 1.0
// Author(s)      : Natalia Portillo
//
// Component      : Component
//
// Revision       : $Revision$
// Last change by : $Author$
// Date           : $Date$
//
// --[ Description ] ----------------------------------------------------------
//
// Description
//
// --[ License ] --------------------------------------------------------------
//
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as
//     published by the Free Software Foundation, either version 3 of the
//     License, or (at your option) any later version.
//
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
//
//     You should have received a copy of the GNU General Public License
//     along with this program.  If not, see <http://www.gnu.org/licenses/>.
//
// ----------------------------------------------------------------------------
// Copyright (C) 2011-2015 Claunia.com
// ****************************************************************************/
// //$Id$
using System;
using DiscImageChef.Console;
using System.Text;

namespace DiscImageChef.Decoders.CD
{
    /// <summary>
    /// Information from the following standards:
    /// ANSI X3.304-1997
    /// T10/1048-D revision 9.0
    /// T10/1048-D revision 10a
    /// T10/1228-D revision 7.0c
    /// T10/1228-D revision 11a
    /// T10/1363-D revision 10g
    /// T10/1545-D revision 1d
    /// T10/1545-D revision 5
    /// T10/1545-D revision 5a
    /// T10/1675-D revision 2c
    /// T10/1675-D revision 4
    /// T10/1836-D revision 2g
    /// </summary>
    public static class CDTextOnLeadIn
    {
        public enum PackTypeIndicator : byte
        {
            /// <summary>
            /// Title of the track (or album if track == 0)
            /// </summary>
            Title = 0x80,
            /// <summary>
            /// Performer
            /// </summary>
            Performer = 0x81,
            /// <summary>
            /// Songwriter
            /// </summary>
            Songwriter = 0x82,
            /// <summary>
            /// Composer
            /// </summary>
            Composer = 0x83,
            /// <summary>
            /// Arranger
            /// </summary>
            Arranger = 0x84,
            /// <summary>
            /// Message from the content provider or artist
            /// </summary>
            Message = 0x85,
            /// <summary>
            /// Disc identification information
            /// </summary>
            DiscIdentification = 0x86,
            /// <summary>
            /// Genre identification
            /// </summary>
            GenreIdentification = 0x87,
            /// <summary>
            /// Table of content information
            /// </summary>
            TOCInformation = 0x88,
            /// <summary>
            /// Second table of content information
            /// </summary>
            SecondTOCInformation = 0x89,
            /// <summary>
            /// Reserved
            /// </summary>
            Reserved1 = 0x8A,
            /// <summary>
            /// Reserved
            /// </summary>
            Reserved2 = 0x8B,
            /// <summary>
            /// Reserved
            /// </summary>
            Reserved3 = 0x8C,
            /// <summary>
            /// Reserved for content provider only
            /// </summary>
            ReservedForContentProvider = 0x8D,
            /// <summary>
            /// UPC of album or ISRC of track
            /// </summary>
            UPCorISRC = 0x8E,
            /// <summary>
            /// Size information of the block
            /// </summary>
            BlockSizeInformation = 0x8F
        }

        public struct CDTextLeadIn
        {
            /// <summary>
            /// Total size of returned CD-Text information minus this field
            /// </summary>
            public UInt16 DataLength;
            /// <summary>
            /// Reserved
            /// </summary>
            public byte Reserved1;
            /// <summary>
            /// Reserved
            /// </summary>
            public byte Reserved2;
            /// <summary>
            /// CD-Text data packs
            /// </summary>
            public CDTextPack[] DataPacks;
        }

        public struct CDTextPack
        {
            /// <summary>
            /// Byte 0
            /// Pack ID1 (Pack Type)
            /// </summary>
            public byte HeaderID1;
            /// <summary>
            /// Byte 1
            /// Pack ID2 (Track number)
            /// </summary>
            public byte HeaderID2;
            /// <summary>
            /// Byte 2
            /// Pack ID3
            /// </summary>
            public byte HeaderID3;
            /// <summary>
            /// Byte 3, bit 7
            /// Double Byte Character Code
            /// </summary>
            public bool DBCC;
            /// <summary>
            /// Byte 3, bits 6 to 4
            /// Block number
            /// </summary>
            public byte BlockNumber;
            /// <summary>
            /// Byte 3, bits 3 to 0
            /// Character position
            /// </summary>
            public byte CharacterPosition;
            /// <summary>
            /// Bytes 4 to 15
            /// Text data
            /// </summary>
            public byte[] TextDataField;
            /// <summary>
            /// Bytes 16 to 17
            /// CRC16
            /// </summary>
            public UInt16 CRC;
        }

        public static CDTextLeadIn? DecodeCDTextLeadIn(byte[] CDTextResponse)
        {
            if (CDTextResponse == null)
                return null;

            CDTextLeadIn decoded = new CDTextLeadIn();

            BigEndianBitConverter.IsLittleEndian = BitConverter.IsLittleEndian;

            decoded.DataLength = BigEndianBitConverter.ToUInt16(CDTextResponse, 0);
            decoded.Reserved1 = CDTextResponse[2];
            decoded.Reserved2 = CDTextResponse[3];
            decoded.DataPacks = new CDTextPack[(decoded.DataLength - 2) / 18];

            if (decoded.DataLength + 2 != CDTextResponse.Length)
            {
                DicConsole.DebugWriteLine("CD-TEXT decoder", "Expected CD-TEXT size ({0} bytes) is not received size ({1} bytes), not decoding", decoded.DataLength + 2, CDTextResponse.Length);
                return null;
            }

            for (int i = 0; i < ((decoded.DataLength - 2) / 18); i++)
            {
                decoded.DataPacks[i].HeaderID1 = CDTextResponse[0 + i * 18 + 4];
                decoded.DataPacks[i].HeaderID2 = CDTextResponse[1 + i * 18 + 4];
                decoded.DataPacks[i].HeaderID3 = CDTextResponse[2 + i * 18 + 4];
                decoded.DataPacks[i].DBCC = Convert.ToBoolean(CDTextResponse[3 + i * 8 + 4] & 0x80);
                decoded.DataPacks[i].BlockNumber = (byte)((CDTextResponse[3 + i * 8 + 4] & 0x70) >> 4);
                decoded.DataPacks[i].CharacterPosition = (byte)(CDTextResponse[3 + i * 8 + 4] & 0x0F);
                decoded.DataPacks[i].TextDataField = new byte[12];
                Array.Copy(CDTextResponse, 4, decoded.DataPacks[i].TextDataField, 0, 12);
                decoded.DataPacks[i].CRC = BigEndianBitConverter.ToUInt16(CDTextResponse, 16 + i * 8 + 4);
            }

            return decoded;
        }

        public static string PrettifyCDTextLeadIn(CDTextLeadIn? CDTextResponse)
        {
            if (CDTextResponse == null)
                return null;

            CDTextLeadIn response = CDTextResponse.Value;

            StringBuilder sb = new StringBuilder();

            #if DEBUG
            if(response.Reserved1 != 0)
                sb.AppendFormat("Reserved1 = 0x{0:X2}", response.Reserved1).AppendLine();
            if(response.Reserved2 != 0)
                sb.AppendFormat("Reserved2 = 0x{0:X2}", response.Reserved2).AppendLine();
            #endif
            foreach (CDTextPack descriptor in response.DataPacks)
            {
                if ((descriptor.HeaderID1 & 0x80) != 0x80)
                    sb.AppendFormat("Incorrect CD-Text pack type {0}, not decoding", descriptor.HeaderID1).AppendLine();
                else
                {
                    switch (descriptor.HeaderID1)
                    {
                        case 0x80:
                            {
                                sb.Append("CD-Text pack contains title for ");
                                if (descriptor.HeaderID2 == 0x00)
                                    sb.AppendLine("album");
                                else
                                    sb.AppendFormat("track {0}", descriptor.HeaderID2).AppendLine();
                                break;
                            }
                        case 0x81:
                            {
                                sb.Append("CD-Text pack contains performer for ");
                                if (descriptor.HeaderID2 == 0x00)
                                    sb.AppendLine("album");
                                else
                                    sb.AppendFormat("track {0}", descriptor.HeaderID2).AppendLine();
                                break;
                            }
                        case 0x82:
                            {
                                sb.Append("CD-Text pack contains songwriter for ");
                                if (descriptor.HeaderID2 == 0x00)
                                    sb.AppendLine("album");
                                else
                                    sb.AppendFormat("track {0}", descriptor.HeaderID2).AppendLine();
                                break;
                            }
                        case 0x83:
                            {
                                sb.Append("CD-Text pack contains composer for ");
                                if (descriptor.HeaderID2 == 0x00)
                                    sb.AppendLine("album");
                                else
                                    sb.AppendFormat("track {0}", descriptor.HeaderID2).AppendLine();
                                break;
                            }
                        case 0x84:
                            {
                                sb.Append("CD-Text pack contains arranger for ");
                                if (descriptor.HeaderID2 == 0x00)
                                    sb.AppendLine("album");
                                else
                                    sb.AppendFormat("track {0}", descriptor.HeaderID2).AppendLine();
                                break;
                            }
                        case 0x85:
                            {
                                sb.Append("CD-Text pack contains content provider's message for ");
                                if (descriptor.HeaderID2 == 0x00)
                                    sb.AppendLine("album");
                                else
                                    sb.AppendFormat("track {0}", descriptor.HeaderID2).AppendLine();
                                break;
                            }
                        case 0x86:
                            {
                                sb.AppendLine("CD-Text pack contains disc identification information");
                                break;
                            }
                        case 0x87:
                            {
                                sb.AppendLine("CD-Text pack contains genre identification information");
                                break;
                            }
                        case 0x88:
                            {
                                sb.AppendLine("CD-Text pack contains table of contents information");
                                break;
                            }
                        case 0x89:
                            {
                                sb.AppendLine("CD-Text pack contains second table of contents information");
                                break;
                            }
                        case 0x8A:
                        case 0x8B:
                        case 0x8C:
                            {
                                sb.AppendLine("CD-Text pack contains reserved data");
                                break;
                            }
                        case 0x8D:
                            {
                                sb.AppendLine("CD-Text pack contains data reserved for content provider only");
                                break;
                            }
                        case 0x8E:
                            {
                                if (descriptor.HeaderID2 == 0x00)
                                    sb.AppendLine("CD-Text pack contains UPC");
                                else
                                    sb.AppendFormat("CD-Text pack contains ISRC for track {0}", descriptor.HeaderID2).AppendLine();
                                break;
                            }
                        case 0x8F:
                            {
                                sb.AppendLine("CD-Text pack contains size block information");
                                break;
                            }
                    }

                    switch (descriptor.HeaderID1)
                    {
                        case 0x80:
                        case 0x81:
                        case 0x82:
                        case 0x83:
                        case 0x84:
                        case 0x85:
                        case 0x86:
                        case 0x87:
                        case 0x8E:
                            {
                                if (descriptor.DBCC)
                                    sb.AppendLine("Double Byte Character Code is used");
                                sb.AppendFormat("Block number {0}", descriptor.BlockNumber).AppendLine();
                                sb.AppendFormat("Character position {0}", descriptor.CharacterPosition).AppendLine();
                                sb.AppendFormat("Text field: \"{0}\"", StringHandlers.CToString(descriptor.TextDataField)).AppendLine();
                                break;
                            }
                        default:
                            {
                                sb.AppendFormat("Binary contents: {0}", PrintHex.ByteArrayToHexArrayString(descriptor.TextDataField, 28)).AppendLine();
                                break;
                            }
                    }

                    sb.AppendFormat("CRC: 0x{0:X4}", descriptor.CRC).AppendLine();
                }
            }

            return sb.ToString();
        }

        public static string PrettifyCDTextLeadIn(byte[] CDTextResponse)
        {
            CDTextLeadIn? decoded = DecodeCDTextLeadIn(CDTextResponse);
            return PrettifyCDTextLeadIn(decoded);
        }
    }
}
