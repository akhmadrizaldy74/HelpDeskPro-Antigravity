namespace HelpDeskPro.UnitTests.Features.Master;

using FluentAssertions;
using HelpDeskPro.Application.Common.Interfaces;
using HelpDeskPro.Application.Features.Master.Categories.Commands.CreateCategory;
using HelpDeskPro.Application.Features.Master.Categories.Commands.DeleteCategory;
using HelpDeskPro.Application.Features.Master.Categories.Commands.ToggleCategoryStatus;
using HelpDeskPro.Application.Features.Master.Categories.Commands.UpdateCategory;
using HelpDeskPro.Application.Features.Master.Categories.Queries.GetCategories;
using HelpDeskPro.Application.Features.Master.Categories.Queries.GetCategoryById;
using HelpDeskPro.Domain.Entities;
using HelpDeskPro.Domain.Exceptions;
using HelpDeskPro.UnitTests.Common;
using NSubstitute;
using Xunit;

public class CategoryTests
{
    private readonly IApplicationDbContext _context;

    public CategoryTests()
    {
        _context = Substitute.For<IApplicationDbContext>();
    }

    [Fact]
    public async Task GetCategories_ReturnsAllCategories()
    {
        // Arrange
        var categories = new List<Category>
        {
            new() { Id = 1, Name = "Hardware", IsActive = true },
            new() { Id = 2, Name = "Software", IsActive = true }
        };

        var catSet = categories.AsTestDbSet();
        _context.Categories.Returns(catSet);

        var handler = new GetCategoriesQueryHandler(_context);

        // Act
        var result = await handler.Handle(new GetCategoriesQuery(), CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateCategory_WhenValid_AddsCategory()
    {
        // Arrange
        var emptySet = new List<Category>().AsTestDbSet();
        _context.Categories.Returns(emptySet);

        var handler = new CreateCategoryCommandHandler(_context);

        // Act
        var result = await handler.Handle(new CreateCategoryCommand("Network"), CancellationToken.None);

        // Assert
        result.Name.Should().Be("Network");
        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateCategory_DuplicateName_ThrowsBusinessRuleException()
    {
        // Arrange
        var categories = new List<Category> { new() { Id = 1, Name = "Hardware" } };
        var catSet = categories.AsTestDbSet();
        _context.Categories.Returns(catSet);

        var handler = new CreateCategoryCommandHandler(_context);

        // Act
        var act = async () => await handler.Handle(new CreateCategoryCommand("Hardware"), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BusinessRuleException>();
    }

    [Fact]
    public async Task DeleteCategory_WhenUsedByTickets_ThrowsBusinessRuleException()
    {
        // Arrange
        var categories = new List<Category> { new() { Id = 1, Name = "Hardware" } };
        var tickets = new List<Ticket> { new() { Id = 1, CategoryId = 1 } };

        var catSet = categories.AsTestDbSet();
        var ticketSet = tickets.AsTestDbSet();
        _context.Categories.Returns(catSet);
        _context.Tickets.Returns(ticketSet);

        var handler = new DeleteCategoryCommandHandler(_context);

        // Act
        var act = async () => await handler.Handle(new DeleteCategoryCommand(1), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*Kategori masih digunakan oleh tiket*");
    }

    [Fact]
    public async Task DeleteCategory_WhenClean_DeletesSuccessfully()
    {
        // Arrange
        var categories = new List<Category> { new() { Id = 1, Name = "Empty Category" } };
        var catSet = categories.AsTestDbSet();
        var ticketSet = new List<Ticket>().AsTestDbSet();

        _context.Categories.Returns(catSet);
        _context.Tickets.Returns(ticketSet);

        var handler = new DeleteCategoryCommandHandler(_context);

        // Act
        await handler.Handle(new DeleteCategoryCommand(1), CancellationToken.None);

        // Assert
        _context.Categories.Received(1).Remove(Arg.Any<Category>());
        await _context.SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetCategoryById_WhenFound_ReturnsCategoryDto()
    {
        // Arrange
        var category = new Category { Id = 1, Name = "Hardware", IsActive = true, Tickets = new List<Ticket>() };
        var catSet = new List<Category> { category }.AsTestDbSet();
        _context.Categories.Returns(catSet);

        var handler = new GetCategoryByIdQueryHandler(_context);

        // Act
        var result = await handler.Handle(new GetCategoryByIdQuery(1), CancellationToken.None);

        // Assert
        result.Id.Should().Be(1);
        result.Name.Should().Be("Hardware");
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetCategoryById_WhenNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var catSet = new List<Category>().AsTestDbSet();
        _context.Categories.Returns(catSet);

        var handler = new GetCategoryByIdQueryHandler(_context);

        // Act
        var act = async () => await handler.Handle(new GetCategoryByIdQuery(999), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task ToggleCategoryStatus_WhenFound_TogglesActiveState()
    {
        // Arrange
        var category = new Category { Id = 1, Name = "Hardware", IsActive = true, Tickets = new List<Ticket>() };
        var catSet = new List<Category> { category }.AsTestDbSet();
        _context.Categories.Returns(catSet);

        var handler = new ToggleCategoryStatusCommandHandler(_context);

        // Act
        var result = await handler.Handle(new ToggleCategoryStatusCommand(1, false), CancellationToken.None);

        // Assert
        result.IsActive.Should().BeFalse();
        category.IsActive.Should().BeFalse();
        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ToggleCategoryStatus_WhenNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var catSet = new List<Category>().AsTestDbSet();
        _context.Categories.Returns(catSet);

        var handler = new ToggleCategoryStatusCommandHandler(_context);

        // Act
        var act = async () => await handler.Handle(new ToggleCategoryStatusCommand(999), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateCategory_WhenValid_UpdatesSuccessfully()
    {
        // Arrange
        var category = new Category { Id = 1, Name = "Old Name", IsActive = true, Tickets = new List<Ticket>() };
        var catSet = new List<Category> { category }.AsTestDbSet();
        _context.Categories.Returns(catSet);

        var handler = new UpdateCategoryCommandHandler(_context);

        // Act
        var result = await handler.Handle(new UpdateCategoryCommand(1, "Updated Name", false), CancellationToken.None);

        // Assert
        result.Name.Should().Be("Updated Name");
        result.IsActive.Should().BeFalse();
        category.Name.Should().Be("Updated Name");
        category.IsActive.Should().BeFalse();
        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateCategory_DuplicateName_ThrowsBusinessRuleException()
    {
        // Arrange
        var category1 = new Category { Id = 1, Name = "Hardware", IsActive = true, Tickets = new List<Ticket>() };
        var category2 = new Category { Id = 2, Name = "Software", IsActive = true, Tickets = new List<Ticket>() };
        var catSet = new List<Category> { category1, category2 }.AsTestDbSet();
        _context.Categories.Returns(catSet);

        var handler = new UpdateCategoryCommandHandler(_context);

        // Act - rename category 2 to "Hardware" which already exists
        var act = async () => await handler.Handle(new UpdateCategoryCommand(2, "Hardware", true), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*sudah digunakan*");
    }

    [Fact]
    public async Task UpdateCategory_WhenNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var catSet = new List<Category>().AsTestDbSet();
        _context.Categories.Returns(catSet);

        var handler = new UpdateCategoryCommandHandler(_context);

        // Act
        var act = async () => await handler.Handle(new UpdateCategoryCommand(999, "NonExistent", true), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }
}
