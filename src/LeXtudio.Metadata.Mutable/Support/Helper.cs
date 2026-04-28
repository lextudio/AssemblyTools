using LeXtudio.Metadata.Mutable;

namespace LeXtudio.Metadata
{
    static class Helper
    {
        public static string GetParameterTypeName(MutableParameterDefinition param)
        {
            return param?.ParameterType?.FullName ?? string.Empty;
        }
    }
}
