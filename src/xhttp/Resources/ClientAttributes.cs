using System;
using Microsoft.CodeAnalysis;

namespace Xhttp;

/// <summary>
/// Indicates that the marked type should be processed by xhttp as a HTTP client
/// to be generated.
/// </summary>
[Embedded,
 AttributeUsage(
     AttributeTargets.Class | AttributeTargets.Struct,
     Inherited = false)]
internal sealed class ClientAttribute : Attribute;
