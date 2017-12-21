﻿// /***************************************************************************
// The Disc Image Chef
// ----------------------------------------------------------------------------
//
// Filename       : SecureDigital.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Core algorithms.
//
// --[ Description ] ----------------------------------------------------------
//
//     Dumps SecureDigital and MultiMediaCard flash cards.
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
// Copyright © 2011-2018 Natalia Portillo
// ****************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using DiscImageChef.CommonTypes;
using DiscImageChef.Console;
using DiscImageChef.Core.Logging;
using DiscImageChef.Decoders.MMC;
using DiscImageChef.Devices;
using DiscImageChef.Filesystems;
using DiscImageChef.Filters;
using DiscImageChef.DiscImages;
using Extents;
using Schemas;

namespace DiscImageChef.Core.Devices.Dumping
{
    public class SecureDigital
    {
        public static void Dump(Device dev, string devicePath, string outputPrefix, ushort retryPasses, bool force,
                                bool dumpRaw, bool persistent, bool stopOnError, ref Metadata.Resume resume,
                                ref DumpLog dumpLog, Encoding encoding)
        {
            bool aborted;
            MhddLog mhddLog;
            IbgLog ibgLog;

            if(dumpRaw)
            {
                DicConsole.ErrorWriteLine("Raw dumping is not supported in MultiMediaCard or SecureDigital devices.");

                if(force) DicConsole.ErrorWriteLine("Continuing...");
                else
                {
                    DicConsole.ErrorWriteLine("Aborting...");
                    return;
                }
            }

            bool sense;
            ushort currentProfile = 0x0001;
            uint timeout = 5;
            double duration;

            CICMMetadataType sidecar =
                new CICMMetadataType {BlockMedia = new BlockMediaType[] {new BlockMediaType()}};

            uint blocksToRead = 128;
            uint blockSize = 512;
            ulong blocks = 0;
            byte[] cid = null;
            byte[] csd = null;
            byte[] ocr = null;
            byte[] ecsd = null;
            byte[] scr = null;
            uint[] response;
            int physicalBlockSize = 0;
            bool byteAddressed = true;

            switch(dev.Type) {
                case DeviceType.MMC:
                {
                    ExtendedCSD ecsdDecoded = new ExtendedCSD();
                    CSD csdDecoded = new CSD();

                    dumpLog.WriteLine("Reading Extended CSD");
                    sense = dev.ReadExtendedCsd(out ecsd, out response, timeout, out duration);
                    if(!sense)
                    {
                        ecsdDecoded = Decoders.MMC.Decoders.DecodeExtendedCSD(ecsd);
                        blocksToRead = ecsdDecoded.OptimalReadSize;
                        blocks = ecsdDecoded.SectorCount;
                        blockSize = (uint)(ecsdDecoded.SectorSize == 1 ? 4096 : 512);
                        if(ecsdDecoded.NativeSectorSize == 0) physicalBlockSize = 512;
                        else if(ecsdDecoded.NativeSectorSize == 1) physicalBlockSize = 4096;
                        // Supposing it's high-capacity MMC if it has Extended CSD...
                        byteAddressed = false;
                    }
                    else ecsd = null;

                    dumpLog.WriteLine("Reading CSD");
                    sense = dev.ReadCsd(out csd, out response, timeout, out duration);
                    if(!sense)
                    {
                        if(blocks == 0)
                        {
                            csdDecoded = Decoders.MMC.Decoders.DecodeCSD(csd);
                            blocks = (ulong)((csdDecoded.Size + 1) * Math.Pow(2, csdDecoded.SizeMultiplier + 2));
                            blockSize = (uint)Math.Pow(2, csdDecoded.ReadBlockLength);
                        }
                    }
                    else csd = null;

                    dumpLog.WriteLine("Reading OCR");
                    sense = dev.ReadOcr(out ocr, out response, timeout, out duration);
                    if(sense) ocr = null;

                    sidecar.BlockMedia[0].MultiMediaCard = new MultiMediaCardType();
                    break;
                }
                case DeviceType.SecureDigital:
                {
                    Decoders.SecureDigital.CSD csdDecoded = new Decoders.SecureDigital.CSD();

                    dumpLog.WriteLine("Reading CSD");
                    sense = dev.ReadCsd(out csd, out response, timeout, out duration);
                    if(!sense)
                    {
                        csdDecoded = Decoders.SecureDigital.Decoders.DecodeCSD(csd);
                        blocks = (ulong)(csdDecoded.Structure == 0
                                             ? (csdDecoded.Size + 1) * Math.Pow(2, csdDecoded.SizeMultiplier + 2)
                                             : (csdDecoded.Size + 1) * 1024);
                        blockSize = (uint)Math.Pow(2, csdDecoded.ReadBlockLength);
                        // Structure >=1 for SDHC/SDXC, so that's block addressed
                        byteAddressed = csdDecoded.Structure == 0;
                    }
                    else csd = null;

                    dumpLog.WriteLine("Reading OCR");
                    sense = dev.ReadSdocr(out ocr, out response, timeout, out duration);
                    if(sense) ocr = null;

                    dumpLog.WriteLine("Reading SCR");
                    sense = dev.ReadScr(out scr, out response, timeout, out duration);
                    if(sense) scr = null;

                    sidecar.BlockMedia[0].SecureDigital = new SecureDigitalType();
                    break;
                }
            }

            dumpLog.WriteLine("Reading CID");
            sense = dev.ReadCid(out cid, out response, timeout, out duration);
            if(sense) cid = null;

            DumpType cidDump = null;
            DumpType csdDump = null;
            DumpType ocrDump = null;

            if(cid != null)
            {
                cidDump = new DumpType
                {
                    Image = outputPrefix + ".cid.bin",
                    Size = cid.Length,
                    Checksums = Checksum.GetChecksums(cid).ToArray()
                };
                DataFile.WriteTo("MMC/SecureDigital Dump", cidDump.Image, cid);
            }
            ;
            if(csd != null)
            {
                csdDump = new DumpType
                {
                    Image = outputPrefix + ".csd.bin",
                    Size = csd.Length,
                    Checksums = Checksum.GetChecksums(csd).ToArray()
                };
                DataFile.WriteTo("MMC/SecureDigital Dump", csdDump.Image, csd);
            }
            ;
            if(ecsd != null)
            {
                sidecar.BlockMedia[0].MultiMediaCard.ExtendedCSD = new DumpType
                {
                    Image = outputPrefix + ".ecsd.bin",
                    Size = ecsd.Length,
                    Checksums = Checksum.GetChecksums(ecsd).ToArray()
                };
                DataFile.WriteTo("MMC/SecureDigital Dump", sidecar.BlockMedia[0].MultiMediaCard.ExtendedCSD.Image,
                                 ecsd);
            }
            ;
            if(ocr != null)
            {
                ocrDump = new DumpType
                {
                    Image = outputPrefix + ".ocr.bin",
                    Size = ocr.Length,
                    Checksums = Checksum.GetChecksums(ocr).ToArray()
                };
                DataFile.WriteTo("MMC/SecureDigital Dump", ocrDump.Image, ocr);
            }
            ;
            if(scr != null)
            {
                sidecar.BlockMedia[0].SecureDigital.SCR = new DumpType
                {
                    Image = outputPrefix + ".scr.bin",
                    Size = scr.Length,
                    Checksums = Checksum.GetChecksums(scr).ToArray()
                };
                DataFile.WriteTo("MMC/SecureDigital Dump", sidecar.BlockMedia[0].SecureDigital.SCR.Image, scr);
            }
            ;

            switch(dev.Type) {
                case DeviceType.MMC:
                    sidecar.BlockMedia[0].MultiMediaCard.CID = cidDump;
                    sidecar.BlockMedia[0].MultiMediaCard.CSD = csdDump;
                    sidecar.BlockMedia[0].MultiMediaCard.OCR = ocrDump;
                    break;
                case DeviceType.SecureDigital:
                    sidecar.BlockMedia[0].SecureDigital.CID = cidDump;
                    sidecar.BlockMedia[0].SecureDigital.CSD = csdDump;
                    sidecar.BlockMedia[0].SecureDigital.OCR = ocrDump;
                    break;
            }

            DateTime start;
            DateTime end;
            double totalDuration = 0;
            double totalChkDuration = 0;
            double currentSpeed = 0;
            double maxSpeed = double.MinValue;
            double minSpeed = double.MaxValue;
            Checksum dataChk;

            aborted = false;
            System.Console.CancelKeyPress += (sender, e) => { e.Cancel = aborted = true; };

            DataFile dumpFile;

            if(blocks == 0)
            {
                dumpLog.WriteLine("Cannot get device size.");
                DicConsole.ErrorWriteLine("Unable to get device size.");
                return;
            }

            dumpLog.WriteLine("Device reports {0} blocks.", blocks);

            byte[] cmdBuf;
            bool error = true;

            while(true)
            {
                error = dev.Read(out cmdBuf, out response, 0, blockSize, blocksToRead, byteAddressed, timeout,
                                 out duration);

                if(error) blocksToRead /= 2;

                if(!error || blocksToRead == 1) break;
            }

            if(error)
            {
                blocksToRead = 1;
                dumpLog.WriteLine("ERROR: Cannot get blocks to read, device error {0}.", dev.LastError);
                DicConsole.ErrorWriteLine("Device error {0} trying to guess ideal transfer length.", dev.LastError);
                return;
            }

            dumpLog.WriteLine("Device can read {0} blocks at a time.", blocksToRead);

            DumpHardwareType currentTry = null;
            ExtentsULong extents = null;
            ResumeSupport.Process(true, false, blocks, dev.Manufacturer, dev.Model, dev.Serial, dev.PlatformId,
                                  ref resume, ref currentTry, ref extents);
            if(currentTry == null || extents == null)
                throw new Exception("Could not process resume file, not continuing...");

            DicConsole.WriteLine("Reading {0} sectors at a time.", blocksToRead);

            mhddLog = new MhddLog(outputPrefix + ".mhddlog.bin", dev, blocks, blockSize, blocksToRead);
            ibgLog = new IbgLog(outputPrefix + ".ibg", currentProfile);
            dumpFile = new DataFile(outputPrefix + ".bin");
            dumpFile.Seek(resume.NextBlock, blockSize);
            if(resume.NextBlock > 0) dumpLog.WriteLine("Resuming from block {0}.", resume.NextBlock);

            start = DateTime.UtcNow;
            for(ulong i = resume.NextBlock; i < blocks; i += blocksToRead)
            {
                if(aborted)
                {
                    currentTry.Extents = Metadata.ExtentsConverter.ToMetadata(extents);
                    dumpLog.WriteLine("Aborted!");
                    break;
                }

                if(blocks - i < blocksToRead) blocksToRead = (byte)(blocks - i);

#pragma warning disable RECS0018 // Comparison of floating point numbers with equality operator
                if(currentSpeed > maxSpeed && currentSpeed != 0) maxSpeed = currentSpeed;
                if(currentSpeed < minSpeed && currentSpeed != 0) minSpeed = currentSpeed;
#pragma warning restore RECS0018 // Comparison of floating point numbers with equality operator

                DicConsole.Write("\rReading sector {0} of {1} ({2:F3} MiB/sec.)", i, blocks, currentSpeed);

                error = dev.Read(out cmdBuf, out response, (uint)i, blockSize, blocksToRead, byteAddressed, timeout,
                                 out duration);

                if(!error)
                {
                    mhddLog.Write(i, duration);
                    ibgLog.Write(i, currentSpeed * 1024);
                    dumpFile.Write(cmdBuf);
                    extents.Add(i, blocksToRead, true);
                }
                else
                {
                    for(ulong b = i; b < i + blocksToRead; b++) resume.BadBlocks.Add(b);

                    if(duration < 500) mhddLog.Write(i, 65535);
                    else mhddLog.Write(i, duration);

                    ibgLog.Write(i, 0);
                    dumpFile.Write(new byte[blockSize * blocksToRead]);
                    dumpLog.WriteLine("Error reading {0} blocks from block {1}.", blocksToRead, i);
                }

#pragma warning disable IDE0004 // Cast is necessary, otherwise incorrect value is created
                currentSpeed = (double)blockSize * blocksToRead / (double)1048576 / (duration / (double)1000);
#pragma warning restore IDE0004 // Cast is necessary, otherwise incorrect value is created
                GC.Collect();
                resume.NextBlock = i + blocksToRead;
            }

            end = DateTime.Now;
            DicConsole.WriteLine();
            mhddLog.Close();
#pragma warning disable IDE0004 // Cast is necessary, otherwise incorrect value is created
            ibgLog.Close(dev, blocks, blockSize, (end - start).TotalSeconds, currentSpeed * 1024,
                         (double)blockSize * (double)(blocks + 1) / 1024 / (totalDuration / 1000), devicePath);
#pragma warning restore IDE0004 // Cast is necessary, otherwise incorrect value is created
            dumpLog.WriteLine("Dump finished in {0} seconds.", (end - start).TotalSeconds);
            dumpLog.WriteLine("Average dump speed {0:F3} KiB/sec.",
                              (double)blockSize * (double)(blocks + 1) / 1024 / (totalDuration / 1000));

            #region Error handling
            if(resume.BadBlocks.Count > 0 && !aborted)
            {
                int pass = 0;
                bool forward = true;
                bool runningPersistent = false;

                repeatRetryLba:
                ulong[] tmpArray = resume.BadBlocks.ToArray();
                foreach(ulong badSector in tmpArray)
                {
                    if(aborted)
                    {
                        currentTry.Extents = Metadata.ExtentsConverter.ToMetadata(extents);
                        dumpLog.WriteLine("Aborted!");
                        break;
                    }

                    DicConsole.Write("\rRetrying sector {0}, pass {1}, {3}{2}", badSector, pass + 1,
                                     forward ? "forward" : "reverse",
                                     runningPersistent ? "recovering partial data, " : "");

                    error = dev.Read(out cmdBuf, out response, (uint)badSector, blockSize, 1, byteAddressed, timeout,
                                     out duration);

                    totalDuration += duration;

                    if(!error)
                    {
                        resume.BadBlocks.Remove(badSector);
                        extents.Add(badSector);
                        dumpFile.WriteAt(cmdBuf, badSector, blockSize);
                        dumpLog.WriteLine("Correctly retried block {0} in pass {1}.", badSector, pass);
                    }
                    else if(runningPersistent) dumpFile.WriteAt(cmdBuf, badSector, blockSize);
                }

                if(pass < retryPasses && !aborted && resume.BadBlocks.Count > 0)
                {
                    pass++;
                    forward = !forward;
                    resume.BadBlocks.Sort();
                    resume.BadBlocks.Reverse();
                    goto repeatRetryLba;
                }

                DicConsole.WriteLine();
            }
            #endregion Error handling

            currentTry.Extents = Metadata.ExtentsConverter.ToMetadata(extents);

            dataChk = new Checksum();
            dumpFile.Seek(0, SeekOrigin.Begin);
            blocksToRead = 500;

            dumpLog.WriteLine("Checksum starts.");
            for(ulong i = 0; i < blocks; i += blocksToRead)
            {
                if(aborted)
                {
                    dumpLog.WriteLine("Aborted!");
                    break;
                }

                if(blocks - i < blocksToRead) blocksToRead = (byte)(blocks - i);

                DicConsole.Write("\rChecksumming sector {0} of {1} ({2:F3} MiB/sec.)", i, blocks, currentSpeed);

                DateTime chkStart = DateTime.UtcNow;
                byte[] dataToCheck = new byte[blockSize * blocksToRead];
                dumpFile.Read(dataToCheck, 0, (int)(blockSize * blocksToRead));
                dataChk.Update(dataToCheck);
                DateTime chkEnd = DateTime.UtcNow;

                double chkDuration = (chkEnd - chkStart).TotalMilliseconds;
                totalChkDuration += chkDuration;

                currentSpeed = (double)blockSize * blocksToRead / (double)1048576 / (chkDuration / (double)1000);
            }

            DicConsole.WriteLine();
            dumpFile.Close();
            end = DateTime.UtcNow;
            dumpLog.WriteLine("Checksum finished in {0} seconds.", (end - start).TotalSeconds);
            dumpLog.WriteLine("Average checksum speed {0:F3} KiB/sec.",
                              (double)blockSize * (double)(blocks + 1) / 1024 / (totalChkDuration / 1000));

            PluginBase plugins = new PluginBase();
            plugins.RegisterAllPlugins(encoding);
            ImagePlugin imageFormat;

            FiltersList filtersList = new FiltersList();
            Filter inputFilter = filtersList.GetFilter(outputPrefix + ".bin");

            if(inputFilter == null)
            {
                DicConsole.ErrorWriteLine("Cannot open file just created, this should not happen.");
                return;
            }

            imageFormat = ImageFormat.Detect(inputFilter);
            PartitionType[] xmlFileSysInfo = null;

            try { if(!imageFormat.OpenImage(inputFilter)) imageFormat = null; }
            catch { imageFormat = null; }

            if(imageFormat != null)
            {
                dumpLog.WriteLine("Getting partitions.");
                List<Partition> partitions = Partitions.GetAll(imageFormat);
                Partitions.AddSchemesToStats(partitions);
                dumpLog.WriteLine("Found {0} partitions.", partitions.Count);

                if(partitions.Count > 0)
                {
                    xmlFileSysInfo = new PartitionType[partitions.Count];
                    for(int i = 0; i < partitions.Count; i++)
                    {
                        xmlFileSysInfo[i] = new PartitionType
                        {
                            Description = partitions[i].Description,
                            EndSector = (int)(partitions[i].Start + partitions[i].Length - 1),
                            Name = partitions[i].Name,
                            Sequence = (int)partitions[i].Sequence,
                            StartSector = (int)partitions[i].Start,
                            Type = partitions[i].Type
                        };
                        List<FileSystemType> lstFs = new List<FileSystemType>();
                        dumpLog.WriteLine("Getting filesystems on partition {0}, starting at {1}, ending at {2}, with type {3}, under scheme {4}.",
                                          i, partitions[i].Start, partitions[i].End, partitions[i].Type,
                                          partitions[i].Scheme);

                        foreach(Filesystem plugin in plugins.PluginsList.Values)
                            try
                            {
                                if(plugin.Identify(imageFormat, partitions[i]))
                                {
                                    plugin.GetInformation(imageFormat, partitions[i], out string foo);
                                    lstFs.Add(plugin.XmlFSType);
                                    Statistics.AddFilesystem(plugin.XmlFSType.Type);
                                    dumpLog.WriteLine("Filesystem {0} found.", plugin.XmlFSType.Type);
                                }
                            }
#pragma warning disable RECS0022 // A catch clause that catches System.Exception and has an empty body
                            catch
#pragma warning restore RECS0022 // A catch clause that catches System.Exception and has an empty body
                            {
                                //DicConsole.DebugWriteLine("Dump-media command", "Plugin {0} crashed", _plugin.Name);
                            }

                        if(lstFs.Count > 0) xmlFileSysInfo[i].FileSystems = lstFs.ToArray();
                    }
                }
                else
                {
                    dumpLog.WriteLine("Getting filesystem for whole device.");

                    xmlFileSysInfo = new PartitionType[1];
                    xmlFileSysInfo[0] = new PartitionType {EndSector = (int)(blocks - 1), StartSector = 0};
                    List<FileSystemType> lstFs = new List<FileSystemType>();

                    Partition wholePart =
                        new Partition {Name = "Whole device", Length = blocks, Size = blocks * blockSize};

                    foreach(Filesystem plugin in plugins.PluginsList.Values)
                        try
                        {
                            if(plugin.Identify(imageFormat, wholePart))
                            {
                                plugin.GetInformation(imageFormat, wholePart, out string foo);
                                lstFs.Add(plugin.XmlFSType);
                                Statistics.AddFilesystem(plugin.XmlFSType.Type);
                                dumpLog.WriteLine("Filesystem {0} found.", plugin.XmlFSType.Type);
                            }
                        }
#pragma warning disable RECS0022 // A catch clause that catches System.Exception and has an empty body
                        catch
#pragma warning restore RECS0022 // A catch clause that catches System.Exception and has an empty body
                        {
                            //DicConsole.DebugWriteLine("Create-sidecar command", "Plugin {0} crashed", _plugin.Name);
                        }

                    if(lstFs.Count > 0) xmlFileSysInfo[0].FileSystems = lstFs.ToArray();
                }
            }

            sidecar.BlockMedia[0].Checksums = dataChk.End().ToArray();
            string xmlDskTyp = null, xmlDskSubTyp = null;
            switch(dev.Type) {
                case DeviceType.MMC:
                    Metadata.MediaType.MediaTypeToString(MediaType.MMC, out xmlDskTyp, out xmlDskSubTyp);
                    sidecar.BlockMedia[0].Dimensions = Metadata.Dimensions.DimensionsFromMediaType(MediaType.MMC);
                    break;
                case DeviceType.SecureDigital:
                    Metadata.MediaType.MediaTypeToString(MediaType.SecureDigital, out xmlDskTyp, out xmlDskSubTyp);
                    sidecar.BlockMedia[0].Dimensions = Metadata.Dimensions.DimensionsFromMediaType(MediaType.SecureDigital);
                    break;
            }
            sidecar.BlockMedia[0].DiskType = xmlDskTyp;
            sidecar.BlockMedia[0].DiskSubType = xmlDskSubTyp;
            // TODO: Implement device firmware revision
            sidecar.BlockMedia[0].Image = new ImageType
            {
                format = "Raw disk image (sector by sector copy)",
                Value = outputPrefix + ".bin"
            };
            switch(dev.Type) {
                case DeviceType.MMC: sidecar.BlockMedia[0].Interface = "MultiMediaCard";
                    break;
                case DeviceType.SecureDigital: sidecar.BlockMedia[0].Interface = "SecureDigital";
                    break;
            }
            sidecar.BlockMedia[0].LogicalBlocks = (long)blocks;
            sidecar.BlockMedia[0].PhysicalBlockSize = physicalBlockSize > 0 ? physicalBlockSize : (int)blockSize;
            sidecar.BlockMedia[0].LogicalBlockSize = (int)blockSize;
            sidecar.BlockMedia[0].Manufacturer = dev.Manufacturer;
            sidecar.BlockMedia[0].Model = dev.Model;
            sidecar.BlockMedia[0].Serial = dev.Serial;
            sidecar.BlockMedia[0].Size = (long)(blocks * blockSize);
            if(xmlFileSysInfo != null) sidecar.BlockMedia[0].FileSystemInformation = xmlFileSysInfo;

            DicConsole.WriteLine();

            DicConsole.WriteLine("Took a total of {0:F3} seconds ({1:F3} processing commands, {2:F3} checksumming).",
                                 (end - start).TotalSeconds, totalDuration / 1000, totalChkDuration / 1000);
            DicConsole.WriteLine("Avegare speed: {0:F3} MiB/sec.",
                                 (double)blockSize * (double)(blocks + 1) / 1048576 / (totalDuration / 1000));
            DicConsole.WriteLine("Fastest speed burst: {0:F3} MiB/sec.", maxSpeed);
            DicConsole.WriteLine("Slowest speed burst: {0:F3} MiB/sec.", minSpeed);
            DicConsole.WriteLine("{0} sectors could not be read.", resume.BadBlocks.Count);
            if(resume.BadBlocks.Count > 0) resume.BadBlocks.Sort();
            DicConsole.WriteLine();

            if(!aborted)
            {
                DicConsole.WriteLine("Writing metadata sidecar");

                FileStream xmlFs = new FileStream(outputPrefix + ".cicm.xml", FileMode.Create);

                System.Xml.Serialization.XmlSerializer xmlSer =
                    new System.Xml.Serialization.XmlSerializer(typeof(CICMMetadataType));
                xmlSer.Serialize(xmlFs, sidecar);
                xmlFs.Close();
            }

            switch(dev.Type) {
                case DeviceType.MMC: Statistics.AddMedia(MediaType.MMC, true);
                    break;
                case DeviceType.SecureDigital: Statistics.AddMedia(MediaType.SecureDigital, true);
                    break;
            }
        }
    }
}