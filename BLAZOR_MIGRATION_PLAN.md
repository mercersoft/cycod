# Cycod to Cycodlib Migration Plan for Blazor Support

## Overview
This document outlines the migration plan for moving shared code from the `cycod` console application to the `cycodlib` shared library, enabling code reuse in the `cycodblazor` Blazor application.

## Migration Strategy

### Phase 1: Create Core Abstractions in Cycodlib

#### 1.1 Logging Abstraction
```csharp
namespace Cycodlib.Abstractions
{
    public interface ILogger
    {
        void WriteDebug(string message);
        void WriteWarning(string message);
        void WriteLine(string message, bool overrideQuiet = false);
        void WriteError(string message);
        bool IsDebugEnabled { get; }
    }
}
```

#### 1.2 Configuration Abstraction
```csharp
namespace Cycodlib.Abstractions
{
    public interface IConfigurationProvider
    {
        string? GetEnvironmentVariable(string name);
        string? GetConfigValue(string key);
        int GetConfigValueAsInt(string key, int defaultValue = 0);
        Task SaveConfigValueAsync(string key, string value);
    }
}
```

#### 1.3 Storage Abstraction
```csharp
namespace Cycodlib.Abstractions
{
    public interface IStorageProvider
    {
        Task<string?> ReadTextAsync(string path);
        Task WriteTextAsync(string path, string content);
        Task<bool> ExistsAsync(string path);
        Task<IEnumerable<string>> ListFilesAsync(string directory, string pattern);
        Task DeleteAsync(string path);
    }
}
```

#### 1.4 Shell/Process Execution Abstraction
```csharp
namespace Cycodlib.Abstractions
{
    public interface IShellExecutor
    {
        Task<ShellResult> ExecuteAsync(string command, ShellType shell, int timeoutMs);
        bool IsAvailable { get; }
    }
}
```

### Phase 2: Components to Move Immediately (Pure Logic)

These components can be moved to `cycodlib` without modification:

1. **Models and Data Structures**
   - `CycoDevProgramInfo.cs` → `Cycodlib.Core/ProgramInfo.cs`
   - `ChatHistoryDefaults.cs` → `Cycodlib.Core/Chat/ChatHistoryDefaults.cs`
   - `TrajectoryFormatter.cs` → `Cycodlib.Core/Formatting/TrajectoryFormatter.cs`

2. **MCP Configuration Classes**
   - `IMcpServerConfigItem.cs` → `Cycodlib.Mcp/IMcpServerConfigItem.cs`
   - `McpServerConfig.cs` → `Cycodlib.Mcp/McpServerConfig.cs`
   - `StdioServerConfig.cs` → `Cycodlib.Mcp/StdioServerConfig.cs`
   - `SseServerConfig.cs` → `Cycodlib.Mcp/SseServerConfig.cs`

3. **Function Calling Pure Logic**
   - `DateAndTimeHelperFunctions.cs` → `Cycodlib.Functions/DateAndTimeHelperFunctions.cs`

### Phase 3: Components Requiring Abstraction

These components need to be refactored to use abstractions:

#### 3.1 Chat Client Components
**Current Location:** `src/cycod/ChatClient/`
**Target Location:** `Cycodlib.Chat/`

**Refactoring Steps:**
1. Extract `ChatClientFactory` core logic
2. Replace `ConsoleHelpers` calls with `ILogger`
3. Replace `EnvironmentHelpers` and `ConfigStore` with `IConfigurationProvider`
4. Move HTTP pipeline configuration as-is

#### 3.2 Function Calling Framework
**Current Location:** `src/cycod/FunctionCalling/`
**Target Location:** `Cycodlib.Functions/`

**Refactoring Steps:**
1. Extract `FunctionFactory` and `FunctionCallDetector`
2. Replace console output with `ILogger`
3. Keep reflection-based function discovery

#### 3.3 Chat Message Helpers
**Current Location:** `src/cycod/Helpers/ChatMessageHelpers.cs`
**Target Location:** `Cycodlib.Chat/ChatMessageHelpers.cs`

**Refactoring Steps:**
1. Extract pure serialization/deserialization logic
2. Replace file I/O with `IStorageProvider`
3. Replace console output with `ILogger`

### Phase 4: Platform-Specific Implementations

Create platform-specific implementations in respective projects:

#### 4.1 Console Implementations (in cycod)
```csharp
// ConsoleLogger.cs
public class ConsoleLogger : ILogger
{
    public void WriteDebug(string message) => ConsoleHelpers.WriteDebugLine(message);
    // ... other implementations
}

// FileStorageProvider.cs
public class FileStorageProvider : IStorageProvider
{
    public async Task<string?> ReadTextAsync(string path) => File.ReadAllText(path);
    // ... other implementations
}
```

#### 4.2 Blazor Implementations (in cycodblazor)
```csharp
// BlazorLogger.cs
public class BlazorLogger : ILogger
{
    private readonly IJSRuntime _jsRuntime;
    public void WriteDebug(string message) => _jsRuntime.InvokeVoidAsync("console.log", message);
    // ... other implementations
}

// BrowserStorageProvider.cs
public class BrowserStorageProvider : IStorageProvider
{
    private readonly IJSRuntime _jsRuntime;
    public async Task<string?> ReadTextAsync(string path) 
        => await _jsRuntime.InvokeAsync<string>("localStorage.getItem", path);
    // ... other implementations
}
```

### Phase 5: Components That Stay in Cycod

These components are console-specific and cannot be used in Blazor:

1. **Shell/Process Execution Tools**
   - `ShellCommandToolHelperFunctions.cs`
   - `CycoDmdCliWrapper.cs`
   - `CodeExplorationHelperFunctions.cs` (current implementation)

2. **File System Dependent Helpers**
   - `ChatHistoryFileHelpers.cs`
   - `PromptFileHelpers.cs`
   - `McpFileHelpers.cs` (file operations)

3. **Console-Specific Infrastructure**
   - `ConsoleHelpers.cs`
   - `ProgramRunner.cs`
   - All command-line command implementations

### Phase 6: Alternative Implementations for Blazor

For components that can't work in Blazor, create alternatives:

1. **Code Exploration Service**
   - Create REST API endpoint that runs cycodmd
   - Implement `ApiCodeExplorationService` in Blazor

2. **MCP Support**
   - Focus on SSE-based MCP servers (web-compatible)
   - Create simplified `BlazorMcpClientManager` supporting only SSE

3. **Configuration Storage**
   - Use browser LocalStorage for simple configs
   - Use IndexedDB for complex data structures

## Project Structure After Migration

```
cycod/
├── src/
│   ├── cycodlib/                    # Shared library
│   │   ├── Abstractions/            # Core interfaces
│   │   ├── Chat/                    # Chat client logic
│   │   ├── Functions/               # Function calling framework
│   │   ├── Mcp/                     # MCP protocol support
│   │   └── Core/                    # Core models and utilities
│   ├── cycod/                       # Console application
│   │   ├── Implementation/          # Console-specific implementations
│   │   ├── Commands/                # CLI commands
│   │   └── Tools/                   # Console-only tools
│   └── cycodblazor/                 # Blazor application
│       ├── Implementation/          # Blazor-specific implementations
│       ├── Services/                # Blazor services
│       └── Components/              # UI components
```

## Implementation Order

1. **Week 1:** Create abstractions and basic project structure
2. **Week 2:** Move pure logic components
3. **Week 3:** Refactor and move chat client components
4. **Week 4:** Refactor and move function calling framework
5. **Week 5:** Create platform-specific implementations
6. **Week 6:** Integration testing and refinement

## Success Criteria

- [ ] Shared chat client creation works in both console and Blazor
- [ ] Function calling framework operates in both environments
- [ ] No direct console or file system dependencies in cycodlib
- [ ] All tests pass for both cycod and cycodblazor
- [ ] Clear separation of concerns between shared and platform-specific code

## Notes

- Prioritize SSE-based MCP servers for initial Blazor support
- Consider using SignalR for real-time updates in Blazor
- Implement proper async/await patterns throughout
- Use dependency injection for all abstractions
- Consider feature flags for platform-specific functionality