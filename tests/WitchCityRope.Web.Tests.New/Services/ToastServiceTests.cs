using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Linq;
using System.Threading.Tasks;
using WitchCityRope.Web.Services;
using Xunit;

namespace WitchCityRope.Web.Tests.New.Services;

/// <summary>
/// Tests for ToastService implementation
/// </summary>
public class ToastServiceTests
{
    private readonly Mock<ILogger<ToastService>> _loggerMock;
    private readonly ToastService _toastService;

    public ToastServiceTests()
    {
        _loggerMock = new Mock<ILogger<ToastService>>();
        _toastService = new ToastService(_loggerMock.Object);
    }

    [Fact]
    public void ShowSuccess_AddsSuccessMessage()
    {
        // Arrange
        var message = "Operation completed successfully";
        var eventRaised = false;
        ToastMessage? raisedMessage = null;
        
        _toastService.OnShow += (msg) =>
        {
            eventRaised = true;
            raisedMessage = msg;
        };

        // Act
        _toastService.ShowSuccess(message);

        // Assert
        eventRaised.Should().BeTrue();
        raisedMessage.Should().NotBeNull();
        raisedMessage!.Message.Should().Be(message);
        raisedMessage.Type.Should().Be(ToastType.Success);
        raisedMessage.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void ShowError_AddsErrorMessage()
    {
        // Arrange
        var message = "An error occurred";
        var eventRaised = false;
        ToastMessage? raisedMessage = null;
        
        _toastService.OnShow += (msg) =>
        {
            eventRaised = true;
            raisedMessage = msg;
        };

        // Act
        _toastService.ShowError(message);

        // Assert
        eventRaised.Should().BeTrue();
        raisedMessage.Should().NotBeNull();
        raisedMessage!.Message.Should().Be(message);
        raisedMessage.Type.Should().Be(ToastType.Error);
    }

    [Fact]
    public void ShowWarning_AddsWarningMessage()
    {
        // Arrange
        var message = "Please be careful";
        var eventRaised = false;
        ToastMessage? raisedMessage = null;
        
        _toastService.OnShow += (msg) =>
        {
            eventRaised = true;
            raisedMessage = msg;
        };

        // Act
        _toastService.ShowWarning(message);

        // Assert
        eventRaised.Should().BeTrue();
        raisedMessage.Should().NotBeNull();
        raisedMessage!.Message.Should().Be(message);
        raisedMessage.Type.Should().Be(ToastType.Warning);
    }

    [Fact]
    public void ShowInfo_AddsInfoMessage()
    {
        // Arrange
        var message = "Here's some information";
        var eventRaised = false;
        ToastMessage? raisedMessage = null;
        
        _toastService.OnShow += (msg) =>
        {
            eventRaised = true;
            raisedMessage = msg;
        };

        // Act
        _toastService.ShowInfo(message);

        // Assert
        eventRaised.Should().BeTrue();
        raisedMessage.Should().NotBeNull();
        raisedMessage!.Message.Should().Be(message);
        raisedMessage.Type.Should().Be(ToastType.Info);
    }

    [Fact]
    public void Show_WithCustomType_AddsMessage()
    {
        // Arrange
        var message = "Custom message";
        var type = ToastType.Warning;
        var eventRaised = false;
        ToastMessage? raisedMessage = null;
        
        _toastService.OnShow += (msg) =>
        {
            eventRaised = true;
            raisedMessage = msg;
        };

        // Act
        _toastService.Show(message, type);

        // Assert
        eventRaised.Should().BeTrue();
        raisedMessage.Should().NotBeNull();
        raisedMessage!.Message.Should().Be(message);
        raisedMessage.Type.Should().Be(type);
    }

    [Fact]
    public void GetMessages_ReturnsAllActiveMessages()
    {
        // Arrange & Act
        _toastService.ShowSuccess("Message 1");
        _toastService.ShowError("Message 2");
        _toastService.ShowInfo("Message 3");

        var messages = _toastService.GetMessages();

        // Assert
        messages.Should().HaveCount(3);
        messages.Select(m => m.Message).Should().Contain(new[] { "Message 1", "Message 2", "Message 3" });
        messages.Select(m => m.Type).Should().Contain(new[] { ToastType.Success, ToastType.Error, ToastType.Info });
    }

    [Fact]
    public void Remove_RemovesMessageById()
    {
        // Arrange
        ToastMessage? addedMessage = null;
        _toastService.OnShow += (msg) => addedMessage = msg;
        
        _toastService.ShowSuccess("Test message");
        var messageId = addedMessage!.Id;

        // Act
        _toastService.Remove(messageId);
        var messages = _toastService.GetMessages();

        // Assert
        messages.Should().BeEmpty();
    }

    [Fact]
    public void Remove_WithInvalidId_DoesNothing()
    {
        // Arrange
        _toastService.ShowSuccess("Test message");
        var invalidId = Guid.NewGuid();

        // Act
        _toastService.Remove(invalidId);
        var messages = _toastService.GetMessages();

        // Assert
        messages.Should().HaveCount(1);
    }

    [Fact]
    public void Clear_RemovesAllMessages()
    {
        // Arrange
        _toastService.ShowSuccess("Message 1");
        _toastService.ShowError("Message 2");
        _toastService.ShowInfo("Message 3");

        // Act
        _toastService.Clear();
        var messages = _toastService.GetMessages();

        // Assert
        messages.Should().BeEmpty();
    }

    [Fact]
    public void MultipleSubscribers_AllReceiveNotifications()
    {
        // Arrange
        var subscriber1Received = false;
        var subscriber2Received = false;
        var subscriber3Received = false;
        
        _toastService.OnShow += (_) => subscriber1Received = true;
        _toastService.OnShow += (_) => subscriber2Received = true;
        _toastService.OnShow += (_) => subscriber3Received = true;

        // Act
        _toastService.ShowSuccess("Test message");

        // Assert
        subscriber1Received.Should().BeTrue();
        subscriber2Received.Should().BeTrue();
        subscriber3Received.Should().BeTrue();
    }

    [Fact]
    public void ShowSuccess_WithEmptyMessage_StillCreatesToast()
    {
        // Arrange
        var eventRaised = false;
        ToastMessage? raisedMessage = null;
        
        _toastService.OnShow += (msg) =>
        {
            eventRaised = true;
            raisedMessage = msg;
        };

        // Act
        _toastService.ShowSuccess(string.Empty);

        // Assert
        eventRaised.Should().BeTrue();
        raisedMessage.Should().NotBeNull();
        raisedMessage!.Message.Should().BeEmpty();
        raisedMessage.Type.Should().Be(ToastType.Success);
    }

    [Fact]
    public void ToastMessage_Properties_AreSetCorrectly()
    {
        // Arrange
        ToastMessage? capturedMessage = null;
        _toastService.OnShow += (msg) => capturedMessage = msg;

        // Act
        _toastService.ShowSuccess("Test");

        // Assert
        capturedMessage.Should().NotBeNull();
        capturedMessage!.Id.Should().NotBeEmpty();
        capturedMessage.Message.Should().Be("Test");
        capturedMessage.Type.Should().Be(ToastType.Success);
        capturedMessage.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void ConcurrentOperations_HandledSafely()
    {
        // Arrange
        var tasks = new Task[100];
        var messageCount = 0;
        _toastService.OnShow += (_) => Interlocked.Increment(ref messageCount);

        // Act
        for (int i = 0; i < tasks.Length; i++)
        {
            var index = i;
            tasks[i] = Task.Run(() =>
            {
                if (index % 4 == 0) _toastService.ShowSuccess($"Success {index}");
                else if (index % 4 == 1) _toastService.ShowError($"Error {index}");
                else if (index % 4 == 2) _toastService.ShowWarning($"Warning {index}");
                else _toastService.ShowInfo($"Info {index}");
            });
        }

        Task.WaitAll(tasks);

        // Assert
        messageCount.Should().Be(100);
        _toastService.GetMessages().Count.Should().Be(100);
    }
}