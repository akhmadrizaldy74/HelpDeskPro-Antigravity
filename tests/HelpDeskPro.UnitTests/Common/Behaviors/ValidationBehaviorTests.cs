namespace HelpDeskPro.UnitTests.Common.Behaviors;

using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using HelpDeskPro.Application.Common.Behaviors;
using MediatR;
using NSubstitute;
using Xunit;

public class ValidationBehaviorTests
{
    public record TestRequest : IRequest<string>;

    [Fact]
    public async Task Handle_WhenRequestIsValid_ShouldCallNextAndReturnResponse()
    {
        // Arrange
        var request = new TestRequest();
        var expectedResponse = "success";

        var validator = Substitute.For<IValidator<TestRequest>>();
        validator.ValidateAsync(Arg.Any<ValidationContext<TestRequest>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        var validators = new[] { validator };
        var behavior = new ValidationBehavior<TestRequest, string>(validators);
        var next = Substitute.For<RequestHandlerDelegate<string>>();
        next().Returns(Task.FromResult(expectedResponse));

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        result.Should().Be(expectedResponse);
        await next.Received(1).Invoke();
    }

    [Fact]
    public async Task Handle_WhenRequestIsInvalid_ShouldThrowValidationExceptionWithErrors()
    {
        // Arrange
        var request = new TestRequest();
        var failure = new ValidationFailure("Title", "Judul wajib diisi.");

        var validator = Substitute.For<IValidator<TestRequest>>();
        validator.ValidateAsync(Arg.Any<ValidationContext<TestRequest>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(new[] { failure }));

        var validators = new[] { validator };
        var behavior = new ValidationBehavior<TestRequest, string>(validators);
        var next = Substitute.For<RequestHandlerDelegate<string>>();

        // Act
        var act = () => behavior.Handle(request, next, CancellationToken.None);

        // Assert
        var exception = await act.Should().ThrowAsync<ValidationException>();
        exception.Which.Errors.Should().ContainSingle(e => e.PropertyName == "Title" && e.ErrorMessage == "Judul wajib diisi.");
        await next.DidNotReceive().Invoke();
    }
}

