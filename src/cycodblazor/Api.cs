using Microsoft.JSInterop;

namespace cycodblazor;

public static class Api
{
  // This method is callable from JS as DotNet.invokeMethodAsync('cycodheadless', 'Version')
  [JSInvokable(nameof(Version))]
  public static string Version() => "1.0.0 (Blazor WebAssembly)";
}
