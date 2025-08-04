using Xunit;
using FluentAssertions;

namespace WitchCityRope.Web.Tests.New
{
    public class SimpleTest
    {
        [Fact]
        public void BasicTest_Works()
        {
            // Arrange
            var expected = 42;
            
            // Act
            var actual = 42;
            
            // Assert
            actual.Should().Be(expected);
        }
    }
}