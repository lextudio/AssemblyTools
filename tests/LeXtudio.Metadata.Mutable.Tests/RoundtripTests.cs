using Xunit;
using LeXtudio.Metadata.Mutable;

namespace LeXtudio.Metadata.Mutable.Tests
{
    public class RoundtripTests
    {
        [Fact]
        public void Import_System_String_ReturnsTypeReference()
        {
            var module = new MutableModuleDefinition("TestModule", MutableModuleKind.Dll);

            // Create a type system instance bound to this module and import a runtime type
            var ts = new MutableTypeSystem(module);
            var strRef = ts.Import(typeof(string));

            Assert.NotNull(strRef);
            Assert.Equal("System.String", strRef.FullName);
        }
    }
}
