using System;
using Xunit;
using FluentAssertions;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using WitchCityRope.Web.Tests.Helpers;

namespace WitchCityRope.Web.Tests
{
    public class SampleWorkingTest : ComponentTestBase
    {
        [Fact]
        public void TestInfrastructure_Works()
        {
            // Arrange
            var expectedValue = 42;
            
            // Act
            var actualValue = 42;
            
            // Assert
            actualValue.Should().Be(expectedValue);
        }
        
        [Fact]
        public void ComponentTestBase_ServicesAvailable()
        {
            // Assert that the base class properly set up services
            NavigationManagerMock.Should().NotBeNull();
            JSRuntimeMock.Should().NotBeNull();
            AuthServiceMock.Should().NotBeNull();
            LocalStorageMock.Should().NotBeNull();
            NotificationServiceMock.Should().NotBeNull();
            ToastServiceMock.Should().NotBeNull();
        }
    }
}