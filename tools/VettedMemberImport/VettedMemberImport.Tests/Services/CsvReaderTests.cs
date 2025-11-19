using FluentAssertions;
using VettedMemberImport.Services;

namespace VettedMemberImport.Tests.Services;

/// <summary>
/// Tests for CsvReader service - parses Google Sheet CSV exports
/// </summary>
public class CsvReaderTests : IDisposable
{
    private readonly CsvReader _sut;
    private readonly List<string> _tempFiles = new();

    public CsvReaderTests()
    {
        _sut = new CsvReader();
    }

    public void Dispose()
    {
        // Cleanup temp files
        foreach (var file in _tempFiles)
        {
            if (File.Exists(file))
            {
                File.Delete(file);
            }
        }
    }

    [Fact]
    public void ReadCsvFile_WithValidData_ParsesAllRows()
    {
        // Arrange - Use alternative column names that CsvHelper can parse more easily
        var csvContent = @"Email,Nickname,Pronouns,FetLife,Description,Date Submitted,Notes
test@example.com,TestUser,they/them,fetlife123,Very motivated applicant,7/11/22,Interview held & accepted 07/21/22
user2@example.com,User2,she/her,fetlife456,Great references,7/15/22,Approved 07/25/22";

        var tempFile = CreateTempCsvFile(csvContent);

        // Act
        var result = _sut.ReadCsvFile(tempFile);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);

        var firstRow = result[0];
        firstRow.VetteesEmail.Should().Be("test@example.com");
        firstRow.VetteesNickname.Should().Be("TestUser");
        firstRow.VetteesPronouns.Should().Be("they/them");
        firstRow.FlHandles.Should().Be("fetlife123");
        firstRow.DescriptionOfTheAplicantAndMotivationToJoin.Should().Be("Very motivated applicant");
        firstRow.AppSubmitted.Should().Be("7/11/22");
        firstRow.RelevantNotes.Should().Be("Interview held & accepted 07/21/22");

        var secondRow = result[1];
        secondRow.VetteesEmail.Should().Be("user2@example.com");
        secondRow.VetteesNickname.Should().Be("User2");
    }

    [Fact]
    public void ReadCsvFile_WithMissingOptionalFields_ParsesSuccessfully()
    {
        // Arrange - Use alternative column names
        var csvContent = @"Email,Nickname,Pronouns,FetLife,Description,Date Submitted,Notes
test@example.com,TestUser,,,Good applicant,7/11/22,";

        var tempFile = CreateTempCsvFile(csvContent);

        // Act
        var result = _sut.ReadCsvFile(tempFile);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].VetteesEmail.Should().Be("test@example.com");
        result[0].VetteesNickname.Should().Be("TestUser");
        result[0].VetteesPronouns.Should().BeEmpty();
        result[0].FlHandles.Should().BeEmpty();
        result[0].RelevantNotes.Should().BeEmpty();
    }

    [Fact]
    public void ReadCsvFile_WithWhitespace_TrimsValues()
    {
        // Arrange - Use alternative column names
        var csvContent = @"Email,Nickname,Pronouns,FetLife,Description,Date Submitted,Notes
  test@example.com  ,  TestUser  ,  they/them  ,  fetlife123  ,  Great applicant  ,  7/11/22  ,  Approved  ";

        var tempFile = CreateTempCsvFile(csvContent);

        // Act
        var result = _sut.ReadCsvFile(tempFile);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].VetteesEmail.Should().Be("test@example.com");
        result[0].VetteesNickname.Should().Be("TestUser");
        result[0].VetteesPronouns.Should().Be("they/them");
        result[0].FlHandles.Should().Be("fetlife123");
    }

    [Fact]
    public void ReadCsvFile_WithAlternativeColumnNames_ParsesCorrectly()
    {
        // Arrange - Using alternative column name mapping
        var csvContent = @"Email,Nickname,Pronouns,FetLife,Motivation,Date Submitted,Notes
test@example.com,TestUser,they/them,fetlife123,Great applicant,7/11/22,Approved";

        var tempFile = CreateTempCsvFile(csvContent);

        // Act
        var result = _sut.ReadCsvFile(tempFile);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].VetteesEmail.Should().Be("test@example.com");
        result[0].VetteesNickname.Should().Be("TestUser");
        result[0].VetteesPronouns.Should().Be("they/them");
    }

    [Fact]
    public void ReadCsvFile_WithEmptyFile_ReturnsEmptyList()
    {
        // Arrange - Use alternative column names
        var csvContent = @"Email,Nickname,Pronouns,FetLife,Description,Date Submitted,Notes";

        var tempFile = CreateTempCsvFile(csvContent);

        // Act
        var result = _sut.ReadCsvFile(tempFile);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public void ReadCsvFile_WithQuotedFields_ParsesCorrectly()
    {
        // Arrange - CSV with quoted fields containing commas, use alternative column names
        var csvContent = @"Email,Nickname,Pronouns,FetLife,Description,Date Submitted,Notes
test@example.com,TestUser,they/them,fetlife123,""Very motivated, experienced applicant"",7/11/22,""Interview held, accepted 07/21/22""";

        var tempFile = CreateTempCsvFile(csvContent);

        // Act
        var result = _sut.ReadCsvFile(tempFile);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].DescriptionOfTheAplicantAndMotivationToJoin.Should().Be("Very motivated, experienced applicant");
        result[0].RelevantNotes.Should().Be("Interview held, accepted 07/21/22");
    }

    [Fact]
    public void ReadCsvFile_WithNonExistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var nonExistentFile = "/tmp/non_existent_file_12345.csv";

        // Act
        Action act = () => _sut.ReadCsvFile(nonExistentFile);

        // Assert
        act.Should().Throw<FileNotFoundException>();
    }

    [Fact]
    public void ReadCsvFile_WithMultilineNotes_ParsesCorrectly()
    {
        // Arrange - CSV with multiline quoted fields, use alternative column names
        var csvContent = @"Email,Nickname,Pronouns,FetLife,Description,Date Submitted,Notes
test@example.com,TestUser,they/them,fetlife123,Good applicant,7/11/22,""Line 1
Line 2
Line 3""";

        var tempFile = CreateTempCsvFile(csvContent);

        // Act
        var result = _sut.ReadCsvFile(tempFile);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].RelevantNotes.Should().Contain("Line 1");
        result[0].RelevantNotes.Should().Contain("Line 2");
        result[0].RelevantNotes.Should().Contain("Line 3");
    }

    [Fact]
    public void ReadCsvFile_WithSpecialCharacters_ParsesCorrectly()
    {
        // Arrange
        var csvContent = @"Vettee's email,Vettee's nickname,Vettee's pronouns,FL handles (specify if current, alternative or previous),Description of the aplicant and motivation to join,App Submitted,Relevant notes
test+tag@example.com,Test_User-123,they/them & xe/xir,fetlife_user-123,Applicant with résumé,7/11/22,Notes with special chars: @#$%";

        var tempFile = CreateTempCsvFile(csvContent);

        // Act
        var result = _sut.ReadCsvFile(tempFile);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].VetteesEmail.Should().Be("test+tag@example.com");
        result[0].VetteesNickname.Should().Be("Test_User-123");
        result[0].VetteesPronouns.Should().Be("they/them & xe/xir");
    }

    private string CreateTempCsvFile(string content)
    {
        var tempFile = Path.GetTempFileName();
        _tempFiles.Add(tempFile);
        File.WriteAllText(tempFile, content);
        return tempFile;
    }
}
