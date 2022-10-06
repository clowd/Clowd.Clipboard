global using global::System;
global using global::System.Collections.Generic;
global using global::System.IO;
global using global::System.Linq;
global using global::System.Threading;
global using global::System.Threading.Tasks;
global using global::System.Runtime.Versioning;

#if !NET6_0_OR_GREATER
namespace System.Runtime.Versioning
{
    [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = true)]
    internal class SupportedOSPlatformGuardAttribute : Attribute
    {
        public SupportedOSPlatformGuardAttribute(string platformName) { }
    }
}
#endif

#if NETFRAMEWORK || NETSTANDARD2_0_OR_GREATER
namespace System.Runtime.Versioning
{
    [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = true)]
    internal class SupportedOSPlatformAttribute : Attribute
    {
        public SupportedOSPlatformAttribute(string platformName) { }
    }
}

namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}
#endif