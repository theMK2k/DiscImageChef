﻿// /***************************************************************************
// The Disc Image Chef
// ----------------------------------------------------------------------------
//
// Filename       : DecodeATARegisters.cs
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
using DiscImageChef.Decoders.ATA;
using System.Text;
namespace DiscImageChef.Tests.Devices
{
    partial class MainClass
    {
        public static string DecodeATAStatus(byte status)
        {
            string ret = "";

            if((status & 0x80) == 0x80)
                ret += "BSY ";
            if((status & 0x40) == 0x40)
                ret += "DRDY ";
            if((status & 0x20) == 0x20)
                ret += "DWF ";
            if((status & 0x10) == 0x10)
                ret += "DSC ";
            if((status & 0x8) == 0x8)
                ret += "DRQ ";
            if((status & 0x4) == 0x4)
                ret += "CORR ";
            if((status & 0x2) == 0x2)
                ret += "IDX ";
            if((status & 0x1) == 0x1)
                ret += "ERR ";

            return ret;
        }

        public static string DecodeATAError(byte status)
        {
            string ret = "";

            if((status & 0x80) == 0x80)
                ret += "BBK ";
            if((status & 0x40) == 0x40)
                ret += "UNC ";
            if((status & 0x20) == 0x20)
                ret += "MC ";
            if((status & 0x10) == 0x10)
                ret += "IDNF ";
            if((status & 0x8) == 0x8)
                ret += "MCR ";
            if((status & 0x4) == 0x4)
                ret += "ABRT ";
            if((status & 0x2) == 0x2)
                ret += "TK0NF ";
            if((status & 0x1) == 0x1)
                ret += "AMNF ";

            return ret;
        }

        public static string DecodeATARegisters(AtaErrorRegistersCHS registers)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("Status: {0}", DecodeATAStatus(registers.status)).AppendLine();
            sb.AppendFormat("Error: {0}", DecodeATAStatus(registers.error)).AppendLine();
            sb.AppendFormat("Device: {0}", (registers.deviceHead >> 4) & 0x01).AppendLine();
            sb.AppendFormat("Cylinder: {0}", (registers.cylinderHigh) << 8 + registers.cylinderLow).AppendLine();
            sb.AppendFormat("Head: {0}", registers.deviceHead & 0xF).AppendLine();
            sb.AppendFormat("Sector: {0}", registers.sector).AppendLine();
            sb.AppendFormat("Count: {0}", registers.sectorCount).AppendLine();
            sb.AppendFormat("LBA?: {0}", Convert.ToBoolean(registers.deviceHead & 0x40)).AppendLine();
            sb.AppendFormat("Bit 7 set?: {0}", Convert.ToBoolean(registers.deviceHead & 0x80)).AppendLine();
            sb.AppendFormat("Bit 5 set?: {0}", Convert.ToBoolean(registers.deviceHead & 0x20)).AppendLine();
            return sb.ToString();
        }

        public static string DecodeATARegisters(AtaErrorRegistersLBA28 registers)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("Status: {0}", DecodeATAStatus(registers.status)).AppendLine();
            sb.AppendFormat("Error: {0}", DecodeATAStatus(registers.error)).AppendLine();
            sb.AppendFormat("Device: {0}", (registers.deviceHead >> 4) & 0x01).AppendLine();
            sb.AppendFormat("LBA: {0}", ((registers.deviceHead & 0xF) << 24) + (registers.lbaHigh << 16) + (registers.lbaMid << 8) + registers.lbaLow);
            sb.AppendFormat("Count: {0}", registers.sectorCount).AppendLine();
            sb.AppendFormat("LBA?: {0}", Convert.ToBoolean(registers.deviceHead & 0x40)).AppendLine();
            sb.AppendFormat("Bit 7 set?: {0}", Convert.ToBoolean(registers.deviceHead & 0x80)).AppendLine();
            sb.AppendFormat("Bit 5 set?: {0}", Convert.ToBoolean(registers.deviceHead & 0x20)).AppendLine();
            return sb.ToString();
        }

        public static string DecodeATARegisters(AtaErrorRegistersLBA48 registers)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("Status: {0}", DecodeATAStatus(registers.status)).AppendLine();
            sb.AppendFormat("Error: {0}", DecodeATAStatus(registers.error)).AppendLine();
            sb.AppendFormat("Device: {0}", (registers.deviceHead >> 4) & 0x01).AppendLine();
            sb.AppendFormat("LBA: {0}", ((ulong)(registers.deviceHead & 0xF) * (ulong)0x100000000000) + (registers.lbaHigh * (ulong)0x100000000L) + (ulong)(registers.lbaMid << 16) + registers.lbaLow);
            sb.AppendFormat("Count: {0}", registers.sectorCount).AppendLine();
            sb.AppendFormat("LBA?: {0}", Convert.ToBoolean(registers.deviceHead & 0x40)).AppendLine();
            sb.AppendFormat("Bit 7 set?: {0}", Convert.ToBoolean(registers.deviceHead & 0x80)).AppendLine();
            sb.AppendFormat("Bit 5 set?: {0}", Convert.ToBoolean(registers.deviceHead & 0x20)).AppendLine();
            return sb.ToString();
        }
    }
}
