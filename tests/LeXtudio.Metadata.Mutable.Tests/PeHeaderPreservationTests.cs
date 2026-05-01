using System;
using System.IO;
using System.Text;
using System.Reflection.PortableExecutable;
using LeXtudio.Metadata.Mutable;
using Xunit;

namespace LeXtudio.Metadata.Mutable.Tests
{
    public class PeHeaderPreservationTests
    {
        [Fact]
        public void RoundTrip_PeAndCorHeader_Preserved()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "wxsg_pehdr_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            var srcPath = typeof(MutableAssemblyWriterRoundTripTests).Assembly.Location;
            var origPath = Path.Combine(tempDir, "orig.dll");
            var rewrittenPath = Path.Combine(tempDir, "rew.dll");
            File.Copy(srcPath, origPath);

            try
            {
                var reader = new MutableAssemblyReader();
                var assembly = reader.Read(origPath, new MutableReaderParameters { ReadMethodBodies = false });
                assembly.MainModule.FileName = rewrittenPath;

                var writer = new MutableAssemblyWriter(assembly);
                writer.Write(rewrittenPath);

                using var sOrig = File.OpenRead(origPath);
                using var peOrig = new PEReader(sOrig);
                using var sRew = File.OpenRead(rewrittenPath);
                using var peRew = new PEReader(sRew);

                var hdrOrig = peOrig.PEHeaders;
                var hdrRew = peRew.PEHeaders;

                var sb = new StringBuilder();
                void CheckEq<T>(string name, T a, T b)
                {
                    if (!Equals(a, b))
                        sb.AppendLine($"{name}: orig={a} rewritten={b}");
                }

                CheckEq("COFF.TimeDateStamp", hdrOrig.CoffHeader.TimeDateStamp, hdrRew.CoffHeader.TimeDateStamp);

                var ph1 = hdrOrig.PEHeader;
                var ph2 = hdrRew.PEHeader;

                CheckEq("PE.ImageBase", ph1.ImageBase, ph2.ImageBase);
                CheckEq("PE.SectionAlignment", ph1.SectionAlignment, ph2.SectionAlignment);
                CheckEq("PE.FileAlignment", ph1.FileAlignment, ph2.FileAlignment);

                var cor1 = hdrOrig.CorHeader;
                var cor2 = hdrRew.CorHeader;
                if (cor1 != null || cor2 != null)
                {
                    if (cor1 == null || cor2 == null)
                    {
                        sb.AppendLine($"CorHeader mismatch: orig is {(cor1==null?"null":"present")}, rewritten is {(cor2==null?"null":"present")}");
                    }
                    else
                    {
                        CheckEq("Cor.Flags", cor1.Flags, cor2.Flags);
                        CheckEq("Cor.EntryPoint", cor1.EntryPointTokenOrRelativeVirtualAddress, cor2.EntryPointTokenOrRelativeVirtualAddress);
                        CheckEq("Cor.Resources.RVA", cor1.ResourcesDirectory.RelativeVirtualAddress, cor2.ResourcesDirectory.RelativeVirtualAddress);
                        CheckEq("Cor.Resources.Size", cor1.ResourcesDirectory.Size, cor2.ResourcesDirectory.Size);
                        CheckEq("Cor.StrongName.RVA", cor1.StrongNameSignatureDirectory.RelativeVirtualAddress, cor2.StrongNameSignatureDirectory.RelativeVirtualAddress);
                        CheckEq("Cor.StrongName.Size", cor1.StrongNameSignatureDirectory.Size, cor2.StrongNameSignatureDirectory.Size);
                    }
                }

                if (sb.Length > 0)
                {
                    // Dump detailed mapping of optional header and COR header
                    try
                    {
                        var origBytesAll = File.ReadAllBytes(origPath);
                        var rewBytesAll = File.ReadAllBytes(rewrittenPath);
                        int origPe = BitConverter.ToInt32(origBytesAll, 0x3c);
                        int rewPe = BitConverter.ToInt32(rewBytesAll, 0x3c);
                        int origOptStart = origPe + 24;
                        int rewOptStart = rewPe + 24;

                        sb.AppendLine("-- Optional header offsets and values --");
                        void DumpOpt(string tag, byte[] bytes, int optStart)
                        {
                            ushort magic = BitConverter.ToUInt16(bytes, optStart);
                            sb.AppendLine($"{tag}: optStart=0x{optStart:X} magic=0x{magic:X}");
                            var sizeOfCode = BitConverter.ToUInt32(bytes, optStart + 4);
                            var sizeOfImage = BitConverter.ToUInt32(bytes, optStart + 0x38);
                            sb.AppendLine($"{tag}: SizeOfCode @ 0x{optStart + 4:X} = {sizeOfCode}");
                            sb.AppendLine($"{tag}: SizeOfImage @ 0x{optStart + 0x38:X} = {sizeOfImage}");
                        }

                        DumpOpt("orig", origBytesAll, origOptStart);
                        DumpOpt("rew", rewBytesAll, rewOptStart);

                        // Cor header mapping
                        if (hdrOrig.CorHeader != null && hdrRew.CorHeader != null)
                        {
                            sb.AppendLine("-- CorHeader mapping --");
                            // COM descriptor data directory is index 14. Data directories
                            // start at OptionalHeader + 0x60 for PE32, 0x70 for PE32+; each
                            // directory entry is 8 bytes (RVA, Size).
                            ushort origMagic = BitConverter.ToUInt16(origBytesAll, origOptStart);
                            ushort rewMagic = BitConverter.ToUInt16(rewBytesAll, rewOptStart);
                            int origDataDirStart = origOptStart + (origMagic == 0x20B ? 0x70 : 0x60);
                            int rewDataDirStart = rewOptStart + (rewMagic == 0x20B ? 0x70 : 0x60);
                            int origComDirOffset = origDataDirStart + (14 * 8);
                            int rewComDirOffset = rewDataDirStart + (14 * 8);
                            int origCorRva = BitConverter.ToInt32(origBytesAll, origComDirOffset);
                            int rewCorRva = BitConverter.ToInt32(rewBytesAll, rewComDirOffset);
                            sb.AppendLine($"orig Cor RVA=0x{origCorRva:X} rew Cor RVA=0x{rewCorRva:X}");

                            int OrigRvaToFile(int rva, PEHeaders hdr, byte[] bytes)
                            {
                                foreach (var sect in hdr.SectionHeaders)
                                {
                                    if (rva >= sect.VirtualAddress && rva < sect.VirtualAddress + Math.Max(sect.VirtualSize, sect.SizeOfRawData))
                                    {
                                        return rva - sect.VirtualAddress + sect.PointerToRawData;
                                    }
                                }
                                return -1;
                            }

                            int origCorFile = OrigRvaToFile(origCorRva, hdrOrig, origBytesAll);
                            int rewCorFile = OrigRvaToFile(rewCorRva, hdrRew, rewBytesAll);
                            sb.AppendLine($"orig Cor file offset=0x{origCorFile:X} rew Cor file offset=0x{rewCorFile:X}");

                            if (origCorFile > 0)
                            {
                                uint origEntry = BitConverter.ToUInt32(origBytesAll, origCorFile + 20);
                                uint origStrongRva = BitConverter.ToUInt32(origBytesAll, origCorFile + 32);
                                uint origStrongSize = BitConverter.ToUInt32(origBytesAll, origCorFile + 36);
                                sb.AppendLine($"orig Cor.EntryPoint @ 0x{origCorFile + 20:X} = 0x{origEntry:X}");
                                sb.AppendLine($"orig StrongName @ 0x{origCorFile + 32:X} = RVA=0x{origStrongRva:X} Size=0x{origStrongSize:X}");
                            }
                            if (rewCorFile > 0)
                            {
                                uint rewEntry = BitConverter.ToUInt32(rewBytesAll, rewCorFile + 20);
                                uint rewStrongRva = BitConverter.ToUInt32(rewBytesAll, rewCorFile + 32);
                                uint rewStrongSize = BitConverter.ToUInt32(rewBytesAll, rewCorFile + 36);
                                sb.AppendLine($"rew Cor.EntryPoint @ 0x{rewCorFile + 20:X} = 0x{rewEntry:X}");
                                sb.AppendLine($"rew StrongName @ 0x{rewCorFile + 32:X} = RVA=0x{rewStrongRva:X} Size=0x{rewStrongSize:X}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        sb.AppendLine("Diagnostics dump failed: " + ex);
                    }

                    Assert.Fail("PE/Cor header differences detected:\n" + sb.ToString());
                }
            }
            finally
            {
                try { Directory.Delete(tempDir, recursive: true); } catch { }
            }
        }
    }
}
