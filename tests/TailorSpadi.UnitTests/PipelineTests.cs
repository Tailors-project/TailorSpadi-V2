using FluentValidation;
using Mediator;
using Microsoft.Extensions.Logging;
using TailorSpadi.Application.Behaviors;

namespace TailorSpadi.UnitTests;

public sealed class PipelineTests
{
    [Fact]
    public async Task InvalidRequestDoesNotReachHandler()
    {
        var behavior = new ValidationBehavior<TestMessage, string>([new TestValidator()]);
        bool called = false;

        await Assert.ThrowsAsync<ValidationException>(async () =>
            await behavior.Handle(new TestMessage(""), (message, token) =>
            {
                called = true;
                return ValueTask.FromResult("unexpected");
            }, CancellationToken.None));

        Assert.False(called);
    }

    [Fact]
    public async Task ValidRequestPassesOriginalCancellationToken()
    {
        var behavior = new ValidationBehavior<TestMessage, string>([new TestValidator()]);
        using var cancellation = new CancellationTokenSource();

        string result = await behavior.Handle(new TestMessage("valid"), (message, token) =>
        {
            Assert.Equal(cancellation.Token, token);
            return ValueTask.FromResult(message.Secret);
        }, cancellation.Token);

        Assert.Equal("valid", result);
    }

    [Fact]
    public async Task LoggingDoesNotIncludeRequestBodyOrExceptionMessage()
    {
        var logger = new RecordingLogger<RequestLoggingBehavior<TestMessage, string>>();
        var behavior = new RequestLoggingBehavior<TestMessage, string>(logger);
        const string secret = "private-customer-value";

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await behavior.Handle(new TestMessage(secret), (message, token) =>
                throw new InvalidOperationException(secret), CancellationToken.None));

        Assert.NotEmpty(logger.Messages);
        Assert.All(logger.Messages, entry => Assert.DoesNotContain(secret, entry));
        Assert.Contains(logger.Messages, entry => entry.Contains(nameof(TestMessage), StringComparison.Ordinal));
    }

    private sealed record TestMessage(string Secret) : IRequest<string>;

    private sealed class TestValidator : AbstractValidator<TestMessage>
    {
        public TestValidator()
        {
            RuleFor(message => message.Secret).NotEmpty();
        }
    }

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }
    }
}
