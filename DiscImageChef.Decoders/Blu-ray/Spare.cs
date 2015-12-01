﻿// /***************************************************************************
// The Disc Image Chef
// ----------------------------------------------------------------------------
//
// Filename       : Spare.cs
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

namespace DiscImageChef.Decoders.Bluray
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
    public static class Spare
    {
        #region Public methods
        public static SpareAreaInformation? Decode(byte[] SAIResponse)
        {
            if (SAIResponse == null)
                return null;

            if (SAIResponse.Length != 16)
            {
                DicConsole.DebugWriteLine("BD Spare Area Information decoder", "Found incorrect Blu-ray Spare Area Information size ({0} bytes)", SAIResponse.Length);
                return null;
            }

            SpareAreaInformation decoded = new SpareAreaInformation();

            BigEndianBitConverter.IsLittleEndian = BitConverter.IsLittleEndian;

            decoded.DataLength = BigEndianBitConverter.ToUInt16(SAIResponse, 0);
            decoded.Reserved1 = SAIResponse[2];
            decoded.Reserved2 = SAIResponse[3];
            decoded.Reserved3 = BigEndianBitConverter.ToUInt32(SAIResponse, 4);
            decoded.FreeSpareBlocks = BigEndianBitConverter.ToUInt32(SAIResponse, 8);
            decoded.AllocatedSpareBlocks = BigEndianBitConverter.ToUInt32(SAIResponse, 12);

            return decoded;
        }

        public static string Prettify(SpareAreaInformation? SAIResponse)
        {
            if (SAIResponse == null)
                return null;

            SpareAreaInformation response = SAIResponse.Value;

            StringBuilder sb = new StringBuilder();

            #if DEBUG
            if(response.Reserved1 != 0)
                sb.AppendFormat("Reserved1 = 0x{0:X2}", response.Reserved1).AppendLine();
            if(response.Reserved2 != 0)
                sb.AppendFormat("Reserved2 = 0x{0:X2}", response.Reserved2).AppendLine();
            if(response.Reserved3 != 0)
                sb.AppendFormat("Reserved3 = 0x{0:X8}", response.Reserved3).AppendLine();
            #endif
            sb.AppendFormat("{0} free spare blocks", response.FreeSpareBlocks).AppendLine();
            sb.AppendFormat("{0} allocated spare blocks", response.AllocatedSpareBlocks).AppendLine();

            return sb.ToString();
        }

        public static string Prettify(byte[] SAIResponse)
        {
            return Prettify(Decode(SAIResponse));
        }
        #endregion Public methods

        #region Public structures
        public struct SpareAreaInformation
        {
            /// <summary>
            /// Bytes 0 to 1
            /// Always 14
            /// </summary>
            public UInt16 DataLength;
            /// <summary>
            /// Byte 2
            /// Reserved
            /// </summary>
            public byte Reserved1;
            /// <summary>
            /// Byte 3
            /// Reserved
            /// </summary>
            public byte Reserved2;
            /// <summary>
            /// Bytes 4 to 7
            /// Reserved
            /// </summary>
            public UInt32 Reserved3;
            /// <summary>
            /// Bytes 8 to 11
            /// Free spare blocks
            /// </summary>
            public UInt32 FreeSpareBlocks;
            /// <summary>
            /// Bytes 12 to 15
            /// Allocated spare blocks
            /// </summary>
            public UInt32 AllocatedSpareBlocks;
        }
        #endregion Public structures
    }
}
