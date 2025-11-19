using FluentAssertions;
using VettedMemberImport.Services;

namespace VettedMemberImport.Tests.Services;

/// <summary>
/// Tests for DateParser service - handles various CSV date formats
/// </summary>
public class DateParserTests
{
    private readonly DateParser _sut;

    public DateParserTests()
    {
        _sut = new DateParser();
    }

    [Fact]
    public void ParseDate_WithMonthDayFormat_InfersCurrentYear()
    {
        // Arrange
        var dateStr = "7/11";

        // Act
        var result = _sut.ParseDate(dateStr);

        // Assert
        result.Should().NotBeNull();
        // DateTime.TryParse infers current year when year is not provided
        result!.Value.Year.Should().BeGreaterThanOrEqualTo(2022);
        result.Value.Month.Should().Be(7);
        result.Value.Day.Should().Be(11);
        result.Value.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void ParseDate_WithPaddedMonthDayFormat_InfersCurrentYear()
    {
        // Arrange
        var dateStr = "07/11";

        // Act
        var result = _sut.ParseDate(dateStr);

        // Assert
        result.Should().NotBeNull();
        // DateTime.TryParse infers current year when year is not provided
        result!.Value.Year.Should().BeGreaterThanOrEqualTo(2022);
        result.Value.Month.Should().Be(7);
        result.Value.Day.Should().Be(11);
        result.Value.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void ParseDate_WithTwoDigitYear_Converts2022()
    {
        // Arrange
        var dateStr = "7/11/22";

        // Act
        var result = _sut.ParseDate(dateStr);

        // Assert
        result.Should().NotBeNull();
        result!.Value.Year.Should().Be(2022);
        result.Value.Month.Should().Be(7);
        result.Value.Day.Should().Be(11);
        result.Value.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void ParseDate_WithFourDigitYear_ParsesCorrectly()
    {
        // Arrange
        var dateStr = "7/11/2022";

        // Act
        var result = _sut.ParseDate(dateStr);

        // Assert
        result.Should().NotBeNull();
        result!.Value.Year.Should().Be(2022);
        result.Value.Month.Should().Be(7);
        result.Value.Day.Should().Be(11);
        result.Value.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void ParseDate_WithDashSeparator_ParsesCorrectly()
    {
        // Arrange
        var dateStr = "7-11-22";

        // Act
        var result = _sut.ParseDate(dateStr);

        // Assert
        result.Should().NotBeNull();
        result!.Value.Year.Should().Be(2022);
        result.Value.Month.Should().Be(7);
        result.Value.Day.Should().Be(11);
        result.Value.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void ParseDate_WithISOFormat_ParsesCorrectly()
    {
        // Arrange
        var dateStr = "2022-07-11";

        // Act
        var result = _sut.ParseDate(dateStr);

        // Assert
        result.Should().NotBeNull();
        result!.Value.Year.Should().Be(2022);
        result.Value.Month.Should().Be(7);
        result.Value.Day.Should().Be(11);
        result.Value.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ParseDate_WithNullOrWhitespace_ReturnsNull(string? dateStr)
    {
        // Act
        var result = _sut.ParseDate(dateStr);

        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("not a date")]
    [InlineData("13/45/2022")]
    [InlineData("abc/def/ghi")]
    public void ParseDate_WithInvalidFormat_ReturnsNull(string dateStr)
    {
        // Act
        var result = _sut.ParseDate(dateStr);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ParseDate_WithWhitespace_TrimsAndParses()
    {
        // Arrange
        var dateStr = "  7/11/2022  ";

        // Act
        var result = _sut.ParseDate(dateStr);

        // Assert
        result.Should().NotBeNull();
        result!.Value.Year.Should().Be(2022);
        result.Value.Month.Should().Be(7);
        result.Value.Day.Should().Be(11);
    }

    [Fact]
    public void InferDecisionDate_WithNullNotes_ReturnsSubmittedPlusThirtyDays()
    {
        // Arrange
        var submittedAt = new DateTime(2022, 6, 1, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var result = _sut.InferDecisionDate(null, submittedAt);

        // Assert
        result.Should().NotBeNull();
        result!.Value.Should().Be(submittedAt.AddDays(30));
    }

    [Fact]
    public void InferDecisionDate_WithEmptyNotes_ReturnsSubmittedPlusThirtyDays()
    {
        // Arrange
        var submittedAt = new DateTime(2022, 6, 1, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var result = _sut.InferDecisionDate("", submittedAt);

        // Assert
        result.Should().NotBeNull();
        result!.Value.Should().Be(submittedAt.AddDays(30));
    }

    [Fact]
    public void InferDecisionDate_WithDateInNotes_ExtractsLastDate()
    {
        // Arrange
        var submittedAt = new DateTime(2022, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var notes = "Application submitted 6/1/22. Interview held & accepted 7/21/22";

        // Act
        var result = _sut.InferDecisionDate(notes, submittedAt);

        // Assert
        result.Should().NotBeNull();
        result!.Value.Year.Should().Be(2022);
        result.Value.Month.Should().Be(7);
        result.Value.Day.Should().Be(21);
    }

    [Fact]
    public void InferDecisionDate_WithMultipleDates_UsesLastDate()
    {
        // Arrange
        var submittedAt = new DateTime(2022, 5, 15, 0, 0, 0, DateTimeKind.Utc);
        var notes = "5/15/22 submitted, 6/1/22 contacted, 7/21/22 approved";

        // Act
        var result = _sut.InferDecisionDate(notes, submittedAt);

        // Assert
        result.Should().NotBeNull();
        result!.Value.Month.Should().Be(7);
        result.Value.Day.Should().Be(21);
    }

    [Fact]
    public void InferDecisionDate_WithNoValidDates_ReturnsSubmittedPlusThirtyDays()
    {
        // Arrange
        var submittedAt = new DateTime(2022, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var notes = "Applicant looks great, no concerns";

        // Act
        var result = _sut.InferDecisionDate(notes, submittedAt);

        // Assert
        result.Should().NotBeNull();
        result!.Value.Should().Be(submittedAt.AddDays(30));
    }
}
