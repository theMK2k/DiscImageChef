﻿// /***************************************************************************
// The Disc Image Chef
// ----------------------------------------------------------------------------
//
// Filename       : FAT16_MBR.cs
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
using System.IO;
using DiscImageChef.Filesystems;
using DiscImageChef.Filters;
using DiscImageChef.ImagePlugins;
using NUnit.Framework;
using DiscImageChef.DiscImages;
using DiscImageChef.PartPlugins;
using DiscImageChef.CommonTypes;
using System.Collections.Generic;

namespace DiscImageChef.Tests.Filesystems
{
    [TestFixture]
    public class FAT16_MBR
    {
        readonly string[] testfiles = {
            "drdos_3.40.vdi.lz", "drdos_3.41.vdi.lz", "drdos_5.00.vdi.lz", "drdos_6.00.vdi.lz",
            "drdos_7.02.vdi.lz", "drdos_7.03.vdi.lz", "drdos_8.00.vdi.lz", "msdos331.vdi.lz",
            "msdos401.vdi.lz", "msdos500.vdi.lz", "msdos600.vdi.lz", "msdos620rc1.vdi.lz",
            "msdos620.vdi.lz", "msdos621.vdi.lz", "msdos622.vdi.lz", "msdos710.vdi.lz",
            "novelldos_7.00.vdi.lz", "opendos_7.01.vdi.lz", "pcdos2000.vdi.lz", "pcdos400.vdi.lz",
            "pcdos500.vdi.lz", "pcdos502.vdi.lz", "pcdos610.vdi.lz", "pcdos630.vdi.lz",
            "msos2_1.21.vdi.lz", "msos2_1.30.1.vdi.lz", "multiuserdos_7.22r4.vdi.lz", "os2_1.20.vdi.lz",
            "os2_1.30.vdi.lz", "os2_6.307.vdi.lz", "os2_6.514.vdi.lz", "os2_6.617.vdi.lz",
            "os2_8.162.vdi.lz", "os2_9.023.vdi.lz", "ecs.vdi.lz",
        };

        readonly ulong[] sectors = {
            1024000, 1024000, 1024000, 1024000,
            1024000, 1024000, 1024000, 1024000,
            1024000, 1024000, 1024000, 1024000,
            1024000, 1024000, 1024000, 1024000,
            1024000, 1024000, 1024000, 1024000,
            1024000, 1024000, 1024000, 1024000,
            1024000, 1024000, 1024000, 1024000,
            1024000, 1024000, 1024000, 1024000,
            1024000, 1024000, 1024000,
        };

        readonly uint[] sectorsize = {
            512, 512, 512, 512,
            512, 512, 512, 512,
            512, 512, 512, 512,
            512, 512, 512, 512,
            512, 512, 512, 512,
            512, 512, 512, 512,
            512, 512, 512, 512,
            512, 512, 512, 512,
            512, 512, 512,
        };

        readonly long[] clusters = {
            63882, 63941, 63941, 63941,
            63941, 63941, 63941, 63941,
            63941, 63941, 63941, 63941,
            63941, 63941, 63941, 63941,
            63941, 63941, 63941, 63941,
            63941, 63941, 63941, 63941,
            63941, 63941, 63941, 63941,
            63941, 63941, 63941, 63941,
            63941, 63941, 63882,
        };

        readonly int[] clustersize = {
            8192, 8192, 8192, 8192,
            8192, 8192, 8192, 8192,
            8192, 8192, 8192, 8192,
            8192, 8192, 8192, 8192,
            8192, 8192, 8192, 8192,
            8192, 8192, 8192, 8192,
            8192, 8192, 8192, 8192,
            8192, 8192, 8192, 8192,
            8192, 8192, 8192,
        };

        readonly string[] volumename = {
            null,null,null,null,
            null,null,"VOLUMELABEL",null,
            "VOLUMELABEL","VOLUMELABEL","VOLUMELABEL","VOLUMELABEL",
            "VOLUMELABEL","VOLUMELABEL","VOLUMELABEL","VOLUMELABEL",
            null,null,"VOLUMELABEL","NO NAME    ",
            "VOLUMELABEL","VOLUMELABEL","VOLUMELABEL","VOLUMELABEL",
            "NO NAME    ","NO NAME    ",null,"NO NAME    ",
            "NO NAME    ","NO NAME    ","NO NAME    ","NO NAME    ",
            "NO NAME    ","NO NAME    ","NO NAME    ",
        };

        readonly string[] volumeserial = {
            null,null,null,null,
            null,null,"1BFB0748",null,
            "217B1909","0C6D18FC","382B18F4","3E2018E9",
            "0D2418EF","195A181B","27761816","356B1809",
            null,null,"2272100F","07280FE1",
            "1F630FF9","18340FFE","3F3F1003","273D1009",
            "9C162C15","9C1E2C15",null,"5BE66015",
            "5BE43015","5BEAC015","E6B18414","E6C63414",
            "1C069414","1C059414","1BE5B814",
        };

        readonly string[] oemid = {
            "IBM  3.2", "IBM  3.2", "IBM  3.3", "IBM  3.3",
            "IBM  3.3", "DRDOS  7", "IBM  5.0", "IBM  3.3",
            "MSDOS4.0", "MSDOS5.0", "MSDOS5.0", "MSDOS5.0",
            "MSDOS5.0", "MSDOS5.0", "MSDOS5.0", "MSWIN4.1",
            "IBM  3.3", "IBM  3.3", "IBM  7.0", "IBM  4.0",
            "IBM  5.0", "IBM  5.0", "IBM  6.0", "IBM  6.0",
            "IBM 10.2", "IBM 10.2", "IBM  3.2", "IBM 10.2",
            "IBM 10.2", "IBM 20.0", "IBM 20.0", "IBM 20.0",
            "IBM 20.0", "IBM 20.0", "IBM 4.50",
        };

        [Test]
        public void Test()
        {
            for(int i = 0; i < testfiles.Length; i++)
            {
                string location = Path.Combine(Consts.TestFilesRoot, "filesystems", "fat16_mbr", testfiles[i]);
                Filter filter = new LZip();
                filter.Open(location);
                ImagePlugin image = new VDI();
                Assert.AreEqual(true, image.OpenImage(filter), testfiles[i]);
                Assert.AreEqual(sectors[i], image.ImageInfo.sectors, testfiles[i]);
                Assert.AreEqual(sectorsize[i], image.ImageInfo.sectorSize, testfiles[i]);
                PartPlugin parts = new MBR();
                Assert.AreEqual(true, parts.GetInformation(image, out List<Partition> partitions), testfiles[i]);
                Filesystem fs = new FAT();
                Assert.AreEqual(true, fs.Identify(image, partitions[0].PartitionStartSector, partitions[0].PartitionStartSector + partitions[0].PartitionSectors - 1), testfiles[i]);
                fs.GetInformation(image, partitions[0].PartitionStartSector, partitions[0].PartitionStartSector + partitions[0].PartitionSectors - 1, out string information);
                Assert.AreEqual(clusters[i], fs.XmlFSType.Clusters, testfiles[i]);
                Assert.AreEqual(clustersize[i], fs.XmlFSType.ClusterSize, testfiles[i]);
                Assert.AreEqual("FAT16", fs.XmlFSType.Type, testfiles[i]);
                Assert.AreEqual(volumename[i], fs.XmlFSType.VolumeName, testfiles[i]);
                Assert.AreEqual(volumeserial[i], fs.XmlFSType.VolumeSerial, testfiles[i]);
                Assert.AreEqual(oemid[i], fs.XmlFSType.SystemIdentifier, testfiles[i]);
            }
        }
    }
}
