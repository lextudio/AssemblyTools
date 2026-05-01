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
                CheckEq("PE.SizeOfImage", ph1.SizeOfImage, ph2.SizeOfImage);
                CheckEq("PE.SizeOfCode", ph1.SizeOfCode, ph2.SizeOfCode);

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
                    Assert.False(true, "PE/Cor header differences detected:\n" + sb.ToString());
            }
            finally
            {
                try { Directory.Delete(tempDir, recursive: true); } catch { }
            }
        }
    }
}
