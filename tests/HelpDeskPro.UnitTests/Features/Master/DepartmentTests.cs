namespace HelpDeskPro.UnitTests.Features.Master;

using FluentAssertions;
using HelpDeskPro.Application.Common.Interfaces;
using HelpDeskPro.Application.Features.Master.Departments.Commands.CreateDepartment;
using HelpDeskPro.Application.Features.Master.Departments.Commands.DeleteDepartment;
using HelpDeskPro.Application.Features.Master.Departments.Commands.ToggleDepartmentStatus;
using HelpDeskPro.Application.Features.Master.Departments.Commands.UpdateDepartment;
using HelpDeskPro.Application.Features.Master.Departments.Queries.GetDepartmentById;
using HelpDeskPro.Application.Features.Master.Departments.Queries.GetDepartments;
using HelpDeskPro.Domain.Entities;
using HelpDeskPro.Domain.Exceptions;
using HelpDeskPro.UnitTests.Common;
using NSubstitute;
using Xunit;

public class DepartmentTests
{
    private readonly IApplicationDbContext _context;

    public DepartmentTests()
    {
        _context = Substitute.For<IApplicationDbContext>();
    }

    [Fact]
    public async Task GetDepartments_ReturnsAllDepartments()
    {
        // Arrange
        var departments = new List<Department>
        {
            new() { Id = 1, Code = "IT", Name = "Information Technology", IsActive = true },
            new() { Id = 2, Code = "HR", Name = "Human Resources", IsActive = false }
        };

        var deptSet = departments.AsTestDbSet();
        var ticketSet = new List<Ticket>().AsTestDbSet();
        _context.Departments.Returns(deptSet);
        _context.Tickets.Returns(ticketSet);

        var handler = new GetDepartmentsQueryHandler(_context);

        // Act
        var result = await handler.Handle(new GetDepartmentsQuery(), CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetDepartmentById_WhenExists_ReturnsDepartment()
    {
        // Arrange
        var departments = new List<Department>
        {
            new() { Id = 1, Code = "IT", Name = "Information Technology", IsActive = true }
        };

        var deptSet = departments.AsTestDbSet();
        var ticketSet = new List<Ticket>().AsTestDbSet();
        _context.Departments.Returns(deptSet);
        _context.Tickets.Returns(ticketSet);

        var handler = new GetDepartmentByIdQueryHandler(_context);

        // Act
        var result = await handler.Handle(new GetDepartmentByIdQuery(1), CancellationToken.None);

        // Assert
        result.Id.Should().Be(1);
        result.Code.Should().Be("IT");
    }

    [Fact]
    public async Task CreateDepartment_WhenValid_AddsDepartment()
    {
        // Arrange
        var emptySet = new List<Department>().AsTestDbSet();
        _context.Departments.Returns(emptySet);

        var handler = new CreateDepartmentCommandHandler(_context);

        // Act
        var result = await handler.Handle(new CreateDepartmentCommand("FIN", "Finance"), CancellationToken.None);

        // Assert
        result.Code.Should().Be("FIN");
        result.Name.Should().Be("Finance");
        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateDepartment_DuplicateCode_ThrowsBusinessRuleException()
    {
        // Arrange
        var departments = new List<Department>
        {
            new() { Id = 1, Code = "IT", Name = "Information Technology" }
        };

        var deptSet = departments.AsTestDbSet();
        _context.Departments.Returns(deptSet);

        var handler = new CreateDepartmentCommandHandler(_context);

        // Act
        var act = async () => await handler.Handle(new CreateDepartmentCommand("IT", "IT Support"), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BusinessRuleException>();
    }

    [Fact]
    public async Task DeleteDepartment_WhenUsedByUsers_ThrowsBusinessRuleException()
    {
        // Arrange
        var departments = new List<Department> { new() { Id = 1, Code = "IT", Name = "IT" } };
        var users = new List<ApplicationUser> { new() { Id = Guid.NewGuid(), FullName = "Budi", DepartmentId = 1 } };

        var deptSet = departments.AsTestDbSet();
        var userSet = users.AsTestDbSet();
        var ticketSet = new List<Ticket>().AsTestDbSet();

        _context.Departments.Returns(deptSet);
        _context.Users.Returns(userSet);
        _context.Tickets.Returns(ticketSet);

        var handler = new DeleteDepartmentCommandHandler(_context);

        // Act
        var act = async () => await handler.Handle(new DeleteDepartmentCommand(1), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*Departemen masih digunakan oleh pengguna*");
    }

    [Fact]
    public async Task DeleteDepartment_WhenClean_DeletesSuccessfully()
    {
        // Arrange
        var departments = new List<Department> { new() { Id = 1, Code = "TEMP", Name = "Temporary" } };

        var deptSet = departments.AsTestDbSet();
        var userSet = new List<ApplicationUser>().AsTestDbSet();
        var ticketSet = new List<Ticket>().AsTestDbSet();

        _context.Departments.Returns(deptSet);
        _context.Users.Returns(userSet);
        _context.Tickets.Returns(ticketSet);

        var handler = new DeleteDepartmentCommandHandler(_context);

        // Act
        await handler.Handle(new DeleteDepartmentCommand(1), CancellationToken.None);

        // Assert
        _context.Departments.Received(1).Remove(Arg.Any<Department>());
        await _context.SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetDepartmentById_WhenNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var deptSet = new List<Department>().AsTestDbSet();
        var ticketSet = new List<Ticket>().AsTestDbSet();
        _context.Departments.Returns(deptSet);
        _context.Tickets.Returns(ticketSet);

        var handler = new GetDepartmentByIdQueryHandler(_context);

        // Act
        var act = async () => await handler.Handle(new GetDepartmentByIdQuery(999), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task ToggleDepartmentStatus_WhenFound_TogglesActiveState()
    {
        // Arrange
        var dept = new Department { Id = 1, Code = "IT", Name = "IT Support", IsActive = true, Users = new List<ApplicationUser>() };
        var deptSet = new List<Department> { dept }.AsTestDbSet();
        var ticketSet = new List<Ticket>().AsTestDbSet();
        _context.Departments.Returns(deptSet);
        _context.Tickets.Returns(ticketSet);

        var handler = new ToggleDepartmentStatusCommandHandler(_context);

        // Act
        var result = await handler.Handle(new ToggleDepartmentStatusCommand(1, false), CancellationToken.None);

        // Assert
        result.IsActive.Should().BeFalse();
        dept.IsActive.Should().BeFalse();
        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ToggleDepartmentStatus_WhenNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var deptSet = new List<Department>().AsTestDbSet();
        _context.Departments.Returns(deptSet);

        var handler = new ToggleDepartmentStatusCommandHandler(_context);

        // Act
        var act = async () => await handler.Handle(new ToggleDepartmentStatusCommand(999), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateDepartment_WhenValid_UpdatesSuccessfully()
    {
        // Arrange
        var dept = new Department { Id = 1, Code = "IT", Name = "IT Support", IsActive = true, Users = new List<ApplicationUser>() };
        var deptSet = new List<Department> { dept }.AsTestDbSet();
        var ticketSet = new List<Ticket>().AsTestDbSet();
        _context.Departments.Returns(deptSet);
        _context.Tickets.Returns(ticketSet);

        var handler = new UpdateDepartmentCommandHandler(_context);

        // Act
        var result = await handler.Handle(new UpdateDepartmentCommand(1, "TECH", "Technology Support", false), CancellationToken.None);

        // Assert
        result.Code.Should().Be("TECH");
        result.Name.Should().Be("Technology Support");
        result.IsActive.Should().BeFalse();
        dept.Code.Should().Be("TECH");
        dept.Name.Should().Be("Technology Support");
        dept.IsActive.Should().BeFalse();
        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateDepartment_DuplicateCode_ThrowsBusinessRuleException()
    {
        // Arrange
        var dept1 = new Department { Id = 1, Code = "IT", Name = "IT", Users = new List<ApplicationUser>() };
        var dept2 = new Department { Id = 2, Code = "HR", Name = "HR", Users = new List<ApplicationUser>() };
        var deptSet = new List<Department> { dept1, dept2 }.AsTestDbSet();
        _context.Departments.Returns(deptSet);

        var handler = new UpdateDepartmentCommandHandler(_context);

        // Act
        var act = async () => await handler.Handle(new UpdateDepartmentCommand(2, "IT", "Human Resources", true), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*Kode departemen 'IT' sudah digunakan*");
    }

    [Fact]
    public async Task UpdateDepartment_DuplicateName_ThrowsBusinessRuleException()
    {
        // Arrange
        var dept1 = new Department { Id = 1, Code = "IT", Name = "Information Technology", Users = new List<ApplicationUser>() };
        var dept2 = new Department { Id = 2, Code = "TECH", Name = "Technology", Users = new List<ApplicationUser>() };
        var deptSet = new List<Department> { dept1, dept2 }.AsTestDbSet();
        _context.Departments.Returns(deptSet);

        var handler = new UpdateDepartmentCommandHandler(_context);

        // Act
        var act = async () => await handler.Handle(new UpdateDepartmentCommand(2, "TECH", "Information Technology", true), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*Nama departemen 'Information Technology' sudah digunakan*");
    }

    [Fact]
    public async Task UpdateDepartment_WhenNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var deptSet = new List<Department>().AsTestDbSet();
        _context.Departments.Returns(deptSet);

        var handler = new UpdateDepartmentCommandHandler(_context);

        // Act
        var act = async () => await handler.Handle(new UpdateDepartmentCommand(999, "NON", "Non Existent", true), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }
}
