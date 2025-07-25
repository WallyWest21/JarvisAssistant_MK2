using Microsoft.Extensions.Logging;
using Moq;
using JarvisAssistant.Services;
using JarvisAssistant.Core.Models;
using JarvisAssistant.Core.ErrorCodes;
using Xunit;
using JarvisAssistant.Core.Interfaces;

namespace JarvisAssistant.UnitTests.Services
{
    /// <summary>
    /// Comprehensive unit tests for the ErrorCodeRegistry.
    /// Validates error code structure, parsing, and utility methods.
    /// </summary>
    public class ErrorCodeRegistryTests
    {
        [Fact]
        public void IsValidErrorCode_ValidCodes_ReturnsTrue()
        {
            // Act & Assert
            Assert.True(ErrorCodeRegistry.IsValidErrorCode("LLM-CONN-001"));
            Assert.True(ErrorCodeRegistry.IsValidErrorCode("VCE-PROC-001"));
            Assert.True(ErrorCodeRegistry.IsValidErrorCode("NET-AUTH-001"));
            Assert.True(ErrorCodeRegistry.IsValidErrorCode("DB-CONN-001"));
        }

        [Theory]
        [InlineData("INVALID")]
        [InlineData("LLM-CONN")]
        [InlineData("LLM-CONN-ABC")]
        [InlineData("")]
        [InlineData(null)]
        public void IsValidErrorCode_InvalidCodes_ReturnsFalse(string? errorCode)
        {
            // Act
            var result = ErrorCodeRegistry.IsValidErrorCode(errorCode!);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void GetServiceFromErrorCode_ValidCode_ReturnsService()
        {
            // Act
            var service = ErrorCodeRegistry.GetServiceFromErrorCode("LLM-CONN-001");

            // Assert
            Assert.Equal("LLM", service);
        }

        [Fact]
        public void GetCategoryFromErrorCode_ValidCode_ReturnsCategory()
        {
            // Act
            var category = ErrorCodeRegistry.GetCategoryFromErrorCode("LLM-CONN-001");

            // Assert
            Assert.Equal("CONN", category);
        }

        [Fact]
        public void GetNumberFromErrorCode_ValidCode_ReturnsNumber()
        {
            // Act
            var number = ErrorCodeRegistry.GetNumberFromErrorCode("LLM-CONN-001");

            // Assert
            Assert.Equal("001", number);
        }

        [Fact]
        public void GetServiceFromErrorCode_InvalidCode_ReturnsNull()
        {
            // Act
            var service = ErrorCodeRegistry.GetServiceFromErrorCode("INVALID");

            // Assert
            Assert.Null(service);
        }

        [Fact]
        public void ErrorCodesByService_ContainsExpectedServices()
        {
            // Act & Assert
            Assert.True(ErrorCodeRegistry.ErrorCodesByService.ContainsKey("LLM"));
            Assert.True(ErrorCodeRegistry.ErrorCodesByService.ContainsKey("VCE"));
            Assert.True(ErrorCodeRegistry.ErrorCodesByService.ContainsKey("NET"));
            Assert.True(ErrorCodeRegistry.ErrorCodesByService.ContainsKey("DB"));
        }

        [Fact]
        public void ErrorCodesByCategory_ContainsExpectedCategories()
        {
            // Act & Assert
            Assert.True(ErrorCodeRegistry.ErrorCodesByCategory.ContainsKey("CONN"));
            Assert.True(ErrorCodeRegistry.ErrorCodesByCategory.ContainsKey("AUTH"));
            Assert.True(ErrorCodeRegistry.ErrorCodesByCategory.ContainsKey("PROC"));
        }

        [Fact]
        public void ErrorCodes_AllDefinedCodes_AreValid()
        {
            // Act & Assert - Test a sample of defined error codes
            Assert.True(ErrorCodeRegistry.IsValidErrorCode(ErrorCodeRegistry.LLM_CONN_001));
            Assert.True(ErrorCodeRegistry.IsValidErrorCode(ErrorCodeRegistry.VCE_PROC_001));
            Assert.True(ErrorCodeRegistry.IsValidErrorCode(ErrorCodeRegistry.NET_CONN_001));
            Assert.True(ErrorCodeRegistry.IsValidErrorCode(ErrorCodeRegistry.DB_CONN_001));
        }
    }

    /// <summary>
    /// Unit tests for the JarvisErrorMessages class.
    /// Validates message retrieval and fallback behavior.
    /// </summary>
    public class JarvisErrorMessagesTests
    {
        [Fact]
        public void GetErrorMessage_ValidErrorCode_ReturnsCorrectMessage()
        {
            // Act
            var message = JarvisErrorMessages.GetErrorMessage(ErrorCodeRegistry.LLM_CONN_001);

            // Assert
            Assert.NotEmpty(message);
            Assert.Contains("neural pathways", message.ToLower());
        }

        [Fact]
        public void GetErrorMessage_InvalidErrorCode_ReturnsFallbackMessage()
        {
            // Act
            var message = JarvisErrorMessages.GetErrorMessage("INVALID-CODE-999");

            // Assert
            Assert.NotEmpty(message);
            Assert.Contains("unexpected situation", message.ToLower());
        }

        [Fact]
        public void GetErrorMessage_NullOrEmptyCode_ReturnsFallbackMessage()
        {
            // Act
            var messageNull = JarvisErrorMessages.GetErrorMessage(null!);
            var messageEmpty = JarvisErrorMessages.GetErrorMessage("");

            // Assert
            Assert.NotEmpty(messageNull);
            Assert.NotEmpty(messageEmpty);
            Assert.Contains("unexpected situation", messageNull.ToLower());
            Assert.Contains("unexpected situation", messageEmpty.ToLower());
        }

        [Fact]
        public void AllDefinedErrorCodes_HaveMessages()
        {
            // Arrange - Get all error code constants via reflection
            var errorCodeFields = typeof(ErrorCodeRegistry)
                .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                .Where(f => f.IsLiteral && f.FieldType == typeof(string))
                .Where(f => f.Name.Contains("_") && f.Name.Length > 10) // Filter for actual error codes
                .Select(f => f.GetValue(null)?.ToString())
                .Where(code => !string.IsNullOrEmpty(code));

            // Act & Assert
            foreach (var errorCode in errorCodeFields)
            {
                var message = JarvisErrorMessages.GetErrorMessage(errorCode!);
                Assert.NotEmpty(message);
                Assert.NotEqual(JarvisErrorMessages.GetErrorMessage("INVALID-CODE"), message);
            }
        }
    }
}
