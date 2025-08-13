# Manual Testing Guide for Blazor Functionality

## Browser Storage Testing

### Test 1: LocalStorage Functionality
1. Open browser developer tools → Application → Storage → LocalStorage
2. Navigate to Blazor app test page
3. Store small data (< 1MB): `storageProvider.WriteTextAsync("small-key", "small content")`
4. Verify data appears in LocalStorage
5. Retrieve data: `storageProvider.ReadTextAsync("small-key")`
6. Verify returned content matches

### Test 2: IndexedDB Large Data
1. Open developer tools → Application → Storage → IndexedDB
2. Store large data (> 1MB): `storageProvider.WriteTextAsync("large-key", largeContent)`
3. Verify "CycodStorage" database appears in IndexedDB
4. Verify data is NOT in LocalStorage
5. Retrieve large data and verify content integrity
6. Test automatic cleanup when storage is full

### Test 3: Storage Fallback
1. Disable IndexedDB in browser (dev tools → Application → Storage → clear)
2. Try storing large data
3. Verify fallback to LocalStorage occurs
4. Check error handling and logging

## Logger Testing

### Test 4: Console Output
1. Open browser console
2. Execute logger methods:
   - `logger.WriteLine("Info message")`
   - `logger.WriteError("Error message")`
   - `logger.WriteDebug("Debug message")`
   - `logger.WriteWarning("Warning message")`
3. Verify appropriate console methods are called
4. Verify message formatting and timestamps

### Test 5: LogViewer Component
1. Navigate to page with LogViewer component
2. Generate various log levels
3. Test filtering (show/hide debug)
4. Test auto-scroll functionality
5. Test clear logs button
6. Verify real-time updates when new logs are added
7. Test log entry limit (1000 entries max)

## Configuration Testing

### Test 6: Browser Configuration
1. Test localStorage-based configuration storage
2. Set configuration values and verify persistence across page reloads
3. Test configuration scope (user vs local)
4. Verify environment variable access (what's available in browser)

## Function System Integration

### Test 7: Function Factory with DI
1. Create FunctionFactory with BlazorLogger
2. Add test functions
3. Verify functions are discovered correctly
4. Test function execution with logging

### Test 8: Chat History Persistence
1. Create FunctionCallingChat with BrowserStorageProvider
2. Conduct chat conversation
3. Save chat history
4. Reload page/component
5. Verify chat history is restored correctly
6. Test with large chat histories (>1MB) to trigger IndexedDB

## Cross-Platform Compatibility

### Test 9: Browser Compatibility
Test in multiple browsers:
- Chrome/Chromium
- Firefox
- Safari
- Edge

Verify:
- LocalStorage works consistently
- IndexedDB support and API differences
- Console logging methods
- JavaScript interop functionality

### Test 10: Mobile Browser Testing
Test on mobile devices:
- iOS Safari
- Android Chrome
- Verify touch interactions with LogViewer
- Test storage limitations on mobile
- Verify responsive design of components

## Performance Testing

### Test 11: Storage Performance
1. Measure time to store/retrieve various data sizes
2. Test with 1000+ storage operations
3. Monitor memory usage during large operations
4. Test IndexedDB vs LocalStorage performance differences

### Test 12: Logging Performance
1. Generate high-frequency log messages (1000+ per second)
2. Monitor browser performance and responsiveness
3. Verify log entry limit prevents memory leaks
4. Test LogViewer rendering performance with many entries

## Error Scenarios

### Test 13: Storage Errors
1. Fill browser storage to capacity
2. Test error handling and fallback mechanisms
3. Simulate storage access denied scenarios
4. Test corrupted storage data recovery

### Test 14: JavaScript Interop Errors
1. Test scenarios where JavaScript functions are not available
2. Simulate network issues affecting JSRuntime
3. Test error propagation from JavaScript to C#
4. Verify graceful degradation when features are unavailable

## Real-World Scenarios

### Test 15: Data Migration
1. Simulate console app creating configuration/data
2. Test Blazor app accessing same logical data
3. Verify data format compatibility
4. Test migration between storage backends

### Test 16: Concurrent Usage
1. Open multiple browser tabs/windows
2. Test concurrent storage access
3. Verify data consistency
4. Test real-time log updates across instances

### Test 17: Production Scenarios
1. Test with production-sized data sets
2. Simulate long-running browser sessions
3. Test with realistic user interaction patterns
4. Verify cleanup and garbage collection

## Accessibility Testing

### Test 18: LogViewer Accessibility
1. Test keyboard navigation
2. Verify screen reader compatibility
3. Test color contrast for different log levels
4. Verify ARIA labels and semantic markup

## Security Testing

### Test 19: Data Security
1. Verify sensitive data is not logged to console
2. Test cross-origin restrictions for storage
3. Verify no XSS vulnerabilities in log display
4. Test input sanitization for configuration values