# Blazor Functionality Testing Plan

## Overview

This document outlines a comprehensive testing strategy for validating the Blazor implementation of the cycodlib shared library. The plan covers unit testing, integration testing, end-to-end testing, and manual testing scenarios to ensure the Blazor application works reliably across different browsers and maintains compatibility with the console application.

## Table of Contents

1. [Test Project Setup](#test-project-setup)
2. [Unit Testing](#unit-testing)
3. [Integration Testing](#integration-testing)
4. [End-to-End Testing](#end-to-end-testing)
5. [Manual Testing Scenarios](#manual-testing-scenarios)
6. [Execution Instructions](#execution-instructions)
7. [Success Criteria](#success-criteria)

## Test Project Setup

### Prerequisites

Install required packages:

```bash
cd src/cycodblazor.Tests
dotnet add package Microsoft.NET.Test.Sdk --version 17.8.0
dotnet add package xunit --version 2.6.1
dotnet add package xunit.runner.visualstudio --version 2.5.3
dotnet add package coverlet.collector --version 6.0.0
dotnet add package bunit --version 1.24.10
dotnet add package bunit.web --version 1.24.10
dotnet add package Moq --version 4.20.69
dotnet add package Microsoft.Playwright --version 1.40.0
dotnet add package Microsoft.JSInterop --version 9.0.0
```

### Project Structure

```
cycodblazor.Tests/
├── Services/
│   ├── BlazorLoggerTests.cs
│   ├── BrowserStorageProviderTests.cs
│   └── BlazorConfigurationProviderTests.cs
├── Components/
│   ├── LogViewerIntegrationTests.cs
│   └── LogViewerComponentTests.cs
├── E2E/
│   ├── BlazorFunctionalityTests.cs
│   └── CrossBrowserTests.cs
├── ManualTestingGuide.md
└── cycodblazor.Tests.csproj
```

## Unit Testing

### 1. BlazorLogger Service Tests

**File**: `Services/BlazorLoggerTests.cs`

**Test Cases**:
- ✅ WriteDebug adds log entry when debug enabled
- ✅ WriteError calls console.error and adds log entry
- ✅ WriteLine respects quiet mode setting
- ✅ WriteWarning calls console.warn and adds log entry
- ✅ GetRecentLogEntries returns correct count
- ✅ ClearLogs removes all entries
- ✅ LogEntryAdded event fires correctly
- ✅ Log entry limit prevents memory leaks (1000 max)
- ✅ Thread safety for concurrent log operations

**Key Validations**:
- JavaScript interop calls with correct parameters
- Event system for real-time UI updates
- Memory management and entry limits
- Error handling when JSRuntime fails

### 2. BrowserStorageProvider Service Tests

**File**: `Services/BrowserStorageProviderTests.cs`

**Test Cases**:
- ✅ WriteTextAsync uses localStorage for small content
- ✅ WriteTextAsync uses IndexedDB for large content (>1MB)
- ✅ ReadTextAsync checks localStorage first
- ✅ ReadTextAsync falls back to IndexedDB when localStorage empty
- ✅ DeleteAsync removes from both storage types
- ✅ AppendTextAsync reads existing content then writes
- ✅ ExistsAsync checks both storage types
- ✅ ListFilesAsync enumerates localStorage keys
- ✅ GetItemInfoAsync returns correct metadata
- ✅ CreateDirectoryAsync handles no-op gracefully

**Key Validations**:
- Automatic size-based storage selection
- Fallback mechanisms between storage types
- Error handling for storage failures
- IndexedDB initialization and management

### 3. BlazorConfigurationProvider Service Tests

**File**: `Services/BlazorConfigurationProviderTests.cs`

**Test Cases**:
- ✅ GetConfigValue retrieves from localStorage
- ✅ SaveConfigValueAsync stores in localStorage
- ✅ RemoveConfigValueAsync removes from localStorage
- ✅ GetEnvironmentVariable accesses browser environment
- ✅ GetConfigValueAsInt parses integer values
- ✅ GetConfigValueAsBool parses boolean values
- ✅ GetAllKeysAsync enumerates all configuration keys

## Integration Testing

### 4. LogViewer Component Integration

**File**: `Components/LogViewerIntegrationTests.cs`

**Test Cases**:
- ✅ LogViewer displays log entries correctly
- ✅ Debug filter works (show/hide debug messages)
- ✅ Clear button removes all logs
- ✅ Real-time updates when new logs added
- ✅ Log entry limit enforcement (1000 max)
- ✅ Auto-scroll functionality
- ✅ CSS classes applied correctly for log levels
- ✅ Timestamp formatting
- ✅ Component disposal and event cleanup

### 5. Full Stack Integration

**File**: `Integration/FullStackIntegrationTests.cs`

**Test Cases**:
- ✅ FunctionCallingChat with BrowserStorageProvider
- ✅ FunctionFactory with BlazorLogger dependency injection
- ✅ Chat history persistence across sessions
- ✅ Large chat history storage (IndexedDB transition)
- ✅ Function execution with logging
- ✅ Configuration access from chat components
- ✅ Error propagation through the stack

## End-to-End Testing

### 6. Browser Automation Tests

**File**: `E2E/BlazorFunctionalityTests.cs`

**Test Scenarios**:
- ✅ BrowserStorage can store and retrieve data
- ✅ IndexedDB handles large data correctly
- ✅ Logger writes to browser console
- ✅ LogViewer displays and updates logs in real browser
- ✅ FunctionCallingChat works with browser storage
- ✅ Configuration provider accesses browser settings

**Setup Requirements**:
```bash
# Install Playwright browsers
npx playwright install
```

### 7. Cross-Browser Compatibility

**File**: `E2E/CrossBrowserTests.cs`

**Browsers to Test**:
- Chrome/Chromium
- Firefox
- Safari (if on macOS)
- Edge

**Test Cases**:
- ✅ LocalStorage API compatibility
- ✅ IndexedDB API differences
- ✅ Console logging methods
- ✅ JavaScript interop functionality
- ✅ Component rendering consistency

## Manual Testing Scenarios

### 8. Browser Storage Testing

**Test 1: LocalStorage Functionality**
1. Open browser developer tools → Application → Storage → LocalStorage
2. Navigate to Blazor app test page
3. Execute: `storageProvider.WriteTextAsync("small-key", "small content")`
4. Verify data appears in LocalStorage
5. Execute: `storageProvider.ReadTextAsync("small-key")`
6. Verify returned content matches

**Test 2: IndexedDB Large Data**
1. Open developer tools → Application → Storage → IndexedDB
2. Execute: `storageProvider.WriteTextAsync("large-key", largeContent)` (>1MB)
3. Verify "CycodStorage" database appears in IndexedDB
4. Verify data is NOT in LocalStorage
5. Retrieve large data and verify integrity

**Test 3: Storage Fallback**
1. Disable IndexedDB in browser settings
2. Try storing large data
3. Verify fallback to LocalStorage
4. Check error handling and logging

### 9. Logger Testing

**Test 4: Console Output**
1. Open browser console
2. Execute logger methods:
   ```javascript
   logger.WriteLine("Info message")
   logger.WriteError("Error message")
   logger.WriteDebug("Debug message")
   logger.WriteWarning("Warning message")
   ```
3. Verify appropriate console methods called
4. Verify message formatting and timestamps

**Test 5: LogViewer Component**
1. Navigate to page with LogViewer component
2. Generate various log levels
3. Test filtering (show/hide debug)
4. Test auto-scroll functionality
5. Test clear logs button
6. Verify real-time updates
7. Test log entry limit (1000 max)

### 10. Configuration Testing

**Test 6: Browser Configuration**
1. Test localStorage-based configuration storage
2. Set values and verify persistence across reloads
3. Test configuration scopes
4. Verify environment variable access

### 11. Function System Integration

**Test 7: Function Factory with DI**
1. Create FunctionFactory with BlazorLogger
2. Add test functions
3. Verify function discovery
4. Test function execution with logging

**Test 8: Chat History Persistence**
1. Create FunctionCallingChat with BrowserStorageProvider
2. Conduct chat conversation
3. Save chat history
4. Reload page/component
5. Verify chat history restored
6. Test large chat histories (>1MB)

### 12. Performance Testing

**Test 9: Storage Performance**
1. Measure time for various data sizes
2. Test 1000+ storage operations
3. Monitor memory usage
4. Compare IndexedDB vs LocalStorage performance

**Test 10: Logging Performance**
1. Generate high-frequency logs (1000+/second)
2. Monitor browser performance
3. Verify entry limit prevents memory leaks
4. Test LogViewer rendering performance

### 13. Error Scenarios

**Test 11: Storage Errors**
1. Fill browser storage to capacity
2. Test error handling and fallbacks
3. Simulate storage access denied
4. Test corrupted data recovery

**Test 12: JavaScript Interop Errors**
1. Test missing JavaScript functions
2. Simulate network issues affecting JSRuntime
3. Test error propagation JS→C#
4. Verify graceful degradation

### 14. Real-World Scenarios

**Test 13: Data Migration**
1. Simulate console app creating data
2. Test Blazor app accessing same data
3. Verify format compatibility
4. Test migration between storage backends

**Test 14: Concurrent Usage**
1. Open multiple browser tabs
2. Test concurrent storage access
3. Verify data consistency
4. Test real-time updates across instances

## Execution Instructions

### Running Unit Tests

```bash
cd src/cycodblazor.Tests
dotnet test --logger trx --collect:"XPlat Code Coverage"
```

### Running Integration Tests

```bash
# Start Blazor app in test mode
cd src/cycodblazor
dotnet run --environment Testing

# Run integration tests
cd ../cycodblazor.Tests
dotnet test --filter Category=Integration
```

### Running E2E Tests

```bash
# Install Playwright (first time only)
npx playwright install

# Start Blazor app
cd src/cycodblazor
dotnet run

# Run E2E tests
cd ../cycodblazor.Tests
dotnet test --filter Category=E2E
```

### Running Cross-Browser Tests

```bash
# Set browser environment variables
export BROWSER=chrome
dotnet test --filter Category=CrossBrowser

export BROWSER=firefox
dotnet test --filter Category=CrossBrowser

export BROWSER=safari  # macOS only
dotnet test --filter Category=CrossBrowser
```

### Manual Testing

1. Start the Blazor application:
   ```bash
   cd src/cycodblazor
   dotnet run
   ```

2. Navigate to test pages:
   - `http://localhost:5000/test` - Basic functionality
   - `http://localhost:5000/log-viewer-test` - LogViewer component
   - `http://localhost:5000/chat-test` - Chat functionality
   - `http://localhost:5000/config-test` - Configuration testing

3. Follow the manual testing scenarios outlined above

### Generating Test Reports

```bash
# Generate coverage report
dotnet test --collect:"XPlat Code Coverage"
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"TestResults/coverage" -reporttypes:Html

# Generate test results
dotnet test --logger:html --results-directory:TestResults
```

## Success Criteria

### Unit Tests
- [ ] 95%+ code coverage for all Blazor services
- [ ] All service methods tested with success and error cases
- [ ] Mock JSRuntime interactions verified
- [ ] Memory leak prevention validated

### Integration Tests
- [ ] LogViewer component renders and updates correctly
- [ ] Real-time event system works reliably
- [ ] Full dependency injection chain functional
- [ ] Cross-component communication validated

### E2E Tests
- [ ] All storage operations work in real browsers
- [ ] JavaScript interop reliable across scenarios
- [ ] Performance acceptable under load
- [ ] Error handling graceful in browser environment

### Cross-Browser Compatibility
- [ ] Core functionality works in Chrome, Firefox, Safari, Edge
- [ ] Storage APIs consistent across browsers
- [ ] No browser-specific JavaScript errors
- [ ] UI components render correctly everywhere

### Manual Testing
- [ ] All manual test scenarios pass
- [ ] Real-world usage patterns validated
- [ ] Performance acceptable for production use
- [ ] Error scenarios handled gracefully

### Performance Benchmarks
- [ ] Storage operations < 100ms for data < 1MB
- [ ] Log entry rendering < 16ms (60fps)
- [ ] Memory usage stable over long sessions
- [ ] No memory leaks detected

### Security & Reliability
- [ ] No sensitive data leaked to console
- [ ] XSS prevention in log display
- [ ] Data integrity maintained across storage types
- [ ] Graceful degradation when features unavailable

## Test Data and Fixtures

Create test data files in `TestData/`:
- `small-text.txt` - Small text file (< 1KB)
- `medium-text.txt` - Medium text file (100KB)
- `large-text.txt` - Large text file (2MB)
- `sample-chat-history.json` - Sample chat conversation
- `sample-config.json` - Sample configuration data

## Continuous Integration

Add to CI/CD pipeline:

```yaml
# .github/workflows/blazor-tests.yml
name: Blazor Tests

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v3
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: 9.0.x
    
    - name: Install Playwright
      run: npx playwright install
    
    - name: Run Unit Tests
      run: |
        cd src/cycodblazor.Tests
        dotnet test --logger trx --collect:"XPlat Code Coverage"
    
    - name: Run E2E Tests
      run: |
        cd src/cycodblazor
        dotnet run &
        sleep 10
        cd ../cycodblazor.Tests
        dotnet test --filter Category=E2E
```

## Notes and Considerations

1. **Browser Storage Limits**: Test with browser storage quotas in mind
2. **Mobile Testing**: Consider testing on mobile browsers for responsive design
3. **Accessibility**: Ensure LogViewer component meets accessibility standards
4. **Internationalization**: Test with different locales and character sets
5. **Offline Scenarios**: Test behavior when network is unavailable
6. **Version Compatibility**: Test across different browser versions

This testing plan ensures comprehensive validation of the Blazor functionality and provides confidence in the production readiness of the cycodlib shared library implementation.