using Microsoft.JSInterop;
using Moq;
using Xunit;
using Cycodblazor.Services;

namespace Cycodblazor.Tests.Services
{
    public class BrowserStorageProviderTests
    {
        private readonly Mock<IJSRuntime> _mockJSRuntime;
        private readonly BrowserStorageProvider _storageProvider;

        public BrowserStorageProviderTests()
        {
            _mockJSRuntime = new Mock<IJSRuntime>();
            _storageProvider = new BrowserStorageProvider(_mockJSRuntime.Object, localStorageMaxSize: 100);
        }

        [Fact]
        public async Task WriteTextAsync_UsesLocalStorage_ForSmallContent()
        {
            // Arrange
            const string path = "test.txt";
            const string content = "Small content";

            // Act
            await _storageProvider.WriteTextAsync(path, content);

            // Assert
            _mockJSRuntime.Verify(js => js.InvokeAsync<IJSVoidResult>(
                "localStorage.setItem",
                It.Is<object[]>(args => args[0].Equals(path) && args[1].Equals(content))
            ), Times.Once);
        }

        [Fact]
        public async Task WriteTextAsync_UsesIndexedDB_ForLargeContent()
        {
            // Arrange
            const string path = "large.txt";
            var largeContent = new string('x', 200); // Exceeds 100 byte limit

            // Setup IndexedDB initialization
            _mockJSRuntime.Setup(js => js.InvokeAsync<IJSVoidResult>("initializeIndexedDb", It.IsAny<object[]>()))
                         .Returns(ValueTask.FromResult<IJSVoidResult>(null!));

            // Act
            await _storageProvider.WriteTextAsync(path, largeContent);

            // Assert
            _mockJSRuntime.Verify(js => js.InvokeAsync<IJSVoidResult>(
                "indexedDbSet",
                It.Is<object[]>(args => 
                    args[0].Equals("CycodStorage") && 
                    args[1].Equals(path) && 
                    args[2].Equals(largeContent))
            ), Times.Once);

            // Should also remove from localStorage
            _mockJSRuntime.Verify(js => js.InvokeAsync<IJSVoidResult>(
                "localStorage.removeItem",
                It.Is<object[]>(args => args[0].Equals(path))
            ), Times.Once);
        }

        [Fact]
        public async Task ReadTextAsync_ChecksLocalStorageFirst()
        {
            // Arrange
            const string path = "test.txt";
            const string expectedContent = "Test content";
            
            _mockJSRuntime.Setup(js => js.InvokeAsync<string>("localStorage.getItem", It.IsAny<object[]>()))
                         .ReturnsAsync(expectedContent);

            // Act
            var result = await _storageProvider.ReadTextAsync(path);

            // Assert
            Assert.Equal(expectedContent, result);
            _mockJSRuntime.Verify(js => js.InvokeAsync<string>(
                "localStorage.getItem",
                It.Is<object[]>(args => args[0].Equals(path))
            ), Times.Once);
        }

        [Fact]
        public async Task ReadTextAsync_FallsBackToIndexedDB_WhenLocalStorageEmpty()
        {
            // Arrange
            const string path = "test.txt";
            const string expectedContent = "IndexedDB content";
            
            _mockJSRuntime.Setup(js => js.InvokeAsync<string>("localStorage.getItem", It.IsAny<object[]>()))
                         .ReturnsAsync((string?)null);
            
            _mockJSRuntime.Setup(js => js.InvokeAsync<IJSVoidResult>("initializeIndexedDb", It.IsAny<object[]>()))
                         .Returns(ValueTask.FromResult<IJSVoidResult>(null!));
            
            _mockJSRuntime.Setup(js => js.InvokeAsync<string>("indexedDbGet", It.IsAny<object[]>()))
                         .ReturnsAsync(expectedContent);

            // Act
            var result = await _storageProvider.ReadTextAsync(path);

            // Assert
            Assert.Equal(expectedContent, result);
            _mockJSRuntime.Verify(js => js.InvokeAsync<string>(
                "indexedDbGet",
                It.Is<object[]>(args => args[0].Equals("CycodStorage") && args[1].Equals(path))
            ), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_RemovesFromBothStorageTypes()
        {
            // Arrange
            const string path = "test.txt";
            
            _mockJSRuntime.Setup(js => js.InvokeAsync<IJSVoidResult>("initializeIndexedDb", It.IsAny<object[]>()))
                         .Returns(ValueTask.FromResult<IJSVoidResult>(null!));

            // Act
            await _storageProvider.DeleteAsync(path);

            // Assert
            _mockJSRuntime.Verify(js => js.InvokeAsync<IJSVoidResult>(
                "localStorage.removeItem",
                It.Is<object[]>(args => args[0].Equals(path))
            ), Times.Once);

            _mockJSRuntime.Verify(js => js.InvokeAsync<IJSVoidResult>(
                "indexedDbDelete",
                It.Is<object[]>(args => args[0].Equals("CycodStorage") && args[1].Equals(path))
            ), Times.Once);
        }

        [Fact]
        public async Task AppendTextAsync_ReadsExistingContent_ThenWrites()
        {
            // Arrange
            const string path = "test.txt";
            const string existingContent = "Existing ";
            const string newContent = "content";
            const string expectedFinalContent = "Existing content";

            _mockJSRuntime.Setup(js => js.InvokeAsync<string>("localStorage.getItem", It.IsAny<object[]>()))
                         .ReturnsAsync(existingContent);

            // Act
            await _storageProvider.AppendTextAsync(path, newContent);

            // Assert
            _mockJSRuntime.Verify(js => js.InvokeAsync<IJSVoidResult>(
                "localStorage.setItem",
                It.Is<object[]>(args => args[0].Equals(path) && args[1].Equals(expectedFinalContent))
            ), Times.Once);
        }
    }
}