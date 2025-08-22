using FluentAssertions;
using WitchCityRope.Core;
using WitchCityRope.Core.ValueObjects;
using Xunit;

namespace WitchCityRope.Core.Tests.ValueObjects
{
    public class SceneNameTests
    {
        [Theory]
        [InlineData("AB")]
        [InlineData("SceneName")]
        [InlineData("Scene Name")]
        [InlineData("Scene-Name")]
        [InlineData("Scene_Name_123")]
        [InlineData("Very Long Scene Name That Is Still Valid")]
        [InlineData("12345")]
        public void Create_ValidSceneName_CreatesSuccessfully(string name)
        {
            // Act
            var sceneName = SceneName.Create(name);

            // Assert
            sceneName.Should().NotBeNull();
            sceneName.Value.Should().Be(name);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void Create_EmptyOrNull_ThrowsArgumentException(string name)
        {
            // Act
            var action = () => SceneName.Create(name);

            // Assert
            action.Should().Throw<ArgumentException>()
                .WithMessage("Scene name cannot be empty*");
        }

        [Fact]
        public void Create_SingleCharacter_ThrowsDomainException()
        {
            // Act
            var action = () => SceneName.Create("A");

            // Assert
            action.Should().Throw<DomainException>()
                .WithMessage("*Scene name must be at least 2 characters long*");
        }

        [Fact]
        public void Create_ExceedsMaxLength_ThrowsDomainException()
        {
            // Arrange
            var longName = new string('A', 51);

            // Act
            var action = () => SceneName.Create(longName);

            // Assert
            action.Should().Throw<DomainException>()
                .WithMessage("*Scene name cannot exceed 50 characters*");
        }

        [Fact]
        public void Create_ExactlyMinLength_CreatesSuccessfully()
        {
            // Act
            var sceneName = SceneName.Create("AB");

            // Assert
            sceneName.Value.Should().Be("AB");
        }

        [Fact]
        public void Create_ExactlyMaxLength_CreatesSuccessfully()
        {
            // Arrange
            var maxLengthName = new string('A', 50);

            // Act
            var sceneName = SceneName.Create(maxLengthName);

            // Assert
            sceneName.Value.Should().Be(maxLengthName);
            sceneName.Value.Length.Should().Be(50);
        }

        [Fact]
        public void Create_WithWhitespace_TrimsSuccessfully()
        {
            // Arrange
            var nameWithSpaces = "  SceneName  ";

            // Act
            var sceneName = SceneName.Create(nameWithSpaces);

            // Assert
            sceneName.Value.Should().Be("SceneName");
        }

        [Fact]
        public void Equals_SameNameDifferentCase_ReturnsTrue()
        {
            // Arrange
            var sceneName1 = SceneName.Create("SceneName");
            var sceneName2 = SceneName.Create("scenename");

            // Act & Assert
            sceneName1.Should().Be(sceneName2);
            (sceneName1 == sceneName2).Should().BeTrue();
            (sceneName1 != sceneName2).Should().BeFalse();
        }

        [Fact]
        public void Equals_DifferentNames_ReturnsFalse()
        {
            // Arrange
            var sceneName1 = SceneName.Create("SceneName1");
            var sceneName2 = SceneName.Create("SceneName2");

            // Act & Assert
            sceneName1.Should().NotBe(sceneName2);
            (sceneName1 == sceneName2).Should().BeFalse();
            (sceneName1 != sceneName2).Should().BeTrue();
        }

        [Fact]
        public void Equals_NullSceneName_ReturnsFalse()
        {
            // Arrange
            var sceneName = SceneName.Create("SceneName");

            // Act & Assert
            sceneName.Equals(null).Should().BeFalse();
            (sceneName == null).Should().BeFalse();
            (null == sceneName).Should().BeFalse();
        }

        [Fact]
        public void GetHashCode_SameNameDifferentCase_ReturnsSameHash()
        {
            // Arrange
            var sceneName1 = SceneName.Create("SceneName");
            var sceneName2 = SceneName.Create("SCENENAME");

            // Act & Assert
            sceneName1.GetHashCode().Should().Be(sceneName2.GetHashCode());
        }

        [Fact]
        public void ToString_ReturnsValue()
        {
            // Arrange
            var sceneName = SceneName.Create("MySceneName");

            // Act
            var result = sceneName.ToString();

            // Assert
            result.Should().Be("MySceneName");
        }

        [Fact]
        public void ImplicitStringConversion_ReturnsValue()
        {
            // Arrange
            var sceneName = SceneName.Create("MySceneName");

            // Act
            string result = sceneName;

            // Assert
            result.Should().Be("MySceneName");
        }

        [Theory]
        [InlineData("SceneName", "SceneName")]
        [InlineData("SCENENAME", "SCENENAME")]
        [InlineData("Scene Name", "Scene Name")]
        public void Value_PreservesOriginalCase(string input, string expected)
        {
            // Act
            var sceneName = SceneName.Create(input);

            // Assert
            sceneName.Value.Should().Be(expected);
        }

        [Theory]
        [InlineData("  Leading Space")]
        [InlineData("Trailing Space  ")]
        [InlineData("  Both Spaces  ")]
        public void Create_TrimsWhitespace_ButPreservesInternalSpaces(string input)
        {
            // Act
            var sceneName = SceneName.Create(input);

            // Assert
            sceneName.Value.Should().Be(input.Trim());
            sceneName.Value.Should().Contain(" ");
        }

        [Fact]
        public void Equals_WithObject_WorksCorrectly()
        {
            // Arrange
            var sceneName = SceneName.Create("SceneName");
            var sameSceneName = SceneName.Create("SceneName");
            var differentSceneName = SceneName.Create("DifferentName");
            object objSame = sameSceneName;
            object objDifferent = differentSceneName;
            object objNull = null;
            object objWrongType = "SceneName";

            // Act & Assert
            sceneName.Equals(objSame).Should().BeTrue();
            sceneName.Equals(objDifferent).Should().BeFalse();
            sceneName.Equals(objNull).Should().BeFalse();
            sceneName.Equals(objWrongType).Should().BeFalse();
        }

        [Theory]
        [InlineData("Scene Name", "scene name")]
        [InlineData("SCENE NAME", "Scene Name")]
        [InlineData("scene name", "SCENE NAME")]
        public void CaseInsensitiveComparison_WorksCorrectly(string name1, string name2)
        {
            // Arrange
            var sceneName1 = SceneName.Create(name1);
            var sceneName2 = SceneName.Create(name2);

            // Act & Assert
            sceneName1.Should().Be(sceneName2);
            sceneName1.GetHashCode().Should().Be(sceneName2.GetHashCode());
        }
    }
}