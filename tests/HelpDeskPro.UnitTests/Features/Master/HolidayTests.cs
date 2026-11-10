namespace HelpDeskPro.UnitTests.Features.Master;

using FluentAssertions;
using FluentValidation.TestHelper;
using HelpDeskPro.Application.Common.Interfaces;
using HelpDeskPro.Application.Features.Master.Holidays.Commands.CreateHoliday;
using HelpDeskPro.Application.Features.Master.Holidays.Commands.DeleteHoliday;
using HelpDeskPro.Application.Features.Master.Holidays.Commands.UpdateHoliday;
using HelpDeskPro.Application.Features.Master.Holidays.Queries.GetHolidays;
using HelpDeskPro.Domain.Entities;
using HelpDeskPro.Domain.Exceptions;
using HelpDeskPro.UnitTests.Common;
using NSubstitute;
using Xunit;

public class HolidayTests
{
    private readonly IApplicationDbContext _context;

    public HolidayTests()
    {
        _context = Substitute.For<IApplicationDbContext>();
    }

    [Fact]
    public async Task GetHolidays_ReturnsAllHolidaysOrderedByDate()
    {
        // Arrange
        var holidays = new List<Holiday>
        {
            new() { Id = 2, Date = new DateOnly(2026, 8, 17), Name = "Hari Kemerdekaan RI" },
            new() { Id = 1, Date = new DateOnly(2026, 1, 1), Name = "Tahun Baru Masehi" }
        };

        var holidaySet = holidays.AsTestDbSet();
        _context.Holidays.Returns(holidaySet);

        var handler = new GetHolidaysQueryHandler(_context);

        // Act
        var result = await handler.Handle(new GetHolidaysQuery(), CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result[0].Date.Should().Be(new DateOnly(2026, 1, 1));
        result[1].Date.Should().Be(new DateOnly(2026, 8, 17));
    }

    [Fact]
    public async Task GetHolidays_WithYearFilter_ReturnsOnlyHolidaysInYear()
    {
        // Arrange
        var holidays = new List<Holiday>
        {
            new() { Id = 1, Date = new DateOnly(2025, 12, 25), Name = "Hari Natal 2025" },
            new() { Id = 2, Date = new DateOnly(2026, 1, 1), Name = "Tahun Baru Masehi 2026" },
            new() { Id = 3, Date = new DateOnly(2026, 8, 17), Name = "Hari Kemerdekaan RI 2026" },
            new() { Id = 4, Date = new DateOnly(2027, 1, 1), Name = "Tahun Baru Masehi 2027" }
        };

        var holidaySet = holidays.AsTestDbSet();
        _context.Holidays.Returns(holidaySet);

        var handler = new GetHolidaysQueryHandler(_context);

        // Act
        var result = await handler.Handle(new GetHolidaysQuery(2026), CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(h => h.Date.Year == 2026);
    }

    [Fact]
    public async Task CreateHoliday_WhenValid_AddsHoliday()
    {
        // Arrange
        var emptySet = new List<Holiday>().AsTestDbSet();
        _context.Holidays.Returns(emptySet);

        var handler = new CreateHolidayCommandHandler(_context);
        var command = new CreateHolidayCommand(new DateOnly(2026, 5, 1), "Hari Buruh");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Date.Should().Be(new DateOnly(2026, 5, 1));
        result.Name.Should().Be("Hari Buruh");
        _context.Holidays.Received(1).Add(Arg.Is<Holiday>(h => h.Date == new DateOnly(2026, 5, 1) && h.Name == "Hari Buruh"));
        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateHoliday_WhenDateExists_ThrowsBusinessRuleException()
    {
        // Arrange
        var existing = new List<Holiday>
        {
            new() { Id = 1, Date = new DateOnly(2026, 5, 1), Name = "Hari Buruh Internasional" }
        }.AsTestDbSet();
        _context.Holidays.Returns(existing);

        var handler = new CreateHolidayCommandHandler(_context);
        var command = new CreateHolidayCommand(new DateOnly(2026, 5, 1), "Hari Buruh Lain");

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*sudah terdaftar*");
    }

    [Fact]
    public void CreateHolidayValidator_WhenValid_PassesValidation()
    {
        var validator = new CreateHolidayCommandValidator();
        var command = new CreateHolidayCommand(new DateOnly(2026, 12, 25), "Hari Raya Natal");

        var result = validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateHolidayValidator_WhenNameEmpty_FailsValidation(string name)
    {
        var validator = new CreateHolidayCommandValidator();
        var command = new CreateHolidayCommand(new DateOnly(2026, 12, 25), name);

        var result = validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public async Task UpdateHoliday_WhenValid_UpdatesHoliday()
    {
        // Arrange
        var holiday = new Holiday { Id = 1, Date = new DateOnly(2026, 8, 17), Name = "Kemerdekaan" };
        var set = new List<Holiday> { holiday }.AsTestDbSet();
        _context.Holidays.Returns(set);

        var handler = new UpdateHolidayCommandHandler(_context);
        var command = new UpdateHolidayCommand(1, new DateOnly(2026, 8, 17), "Hari Kemerdekaan Republik Indonesia");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Name.Should().Be("Hari Kemerdekaan Republik Indonesia");
        holiday.Name.Should().Be("Hari Kemerdekaan Republik Indonesia");
        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateHoliday_WhenNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var emptySet = new List<Holiday>().AsTestDbSet();
        _context.Holidays.Returns(emptySet);

        var handler = new UpdateHolidayCommandHandler(_context);
        var command = new UpdateHolidayCommand(999, new DateOnly(2026, 8, 17), "Kemerdekaan");

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateHoliday_WhenDateConflictWithOtherHoliday_ThrowsBusinessRuleException()
    {
        // Arrange
        var holidays = new List<Holiday>
        {
            new() { Id = 1, Date = new DateOnly(2026, 1, 1), Name = "Tahun Baru" },
            new() { Id = 2, Date = new DateOnly(2026, 1, 2), Name = "Cuti Bersama" }
        };
        var holidaySet = holidays.AsTestDbSet();
        _context.Holidays.Returns(holidaySet);

        var handler = new UpdateHolidayCommandHandler(_context);
        var command = new UpdateHolidayCommand(2, new DateOnly(2026, 1, 1), "Cuti Bersama Diubah ke 1 Jan");

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*sudah terdaftar*");
    }

    [Fact]
    public void UpdateHolidayValidator_WhenValid_PassesValidation()
    {
        var validator = new UpdateHolidayCommandValidator();
        var command = new UpdateHolidayCommand(1, new DateOnly(2026, 12, 25), "Hari Raya Natal");

        var result = validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void UpdateHolidayValidator_WhenIdZero_FailsValidation()
    {
        var validator = new UpdateHolidayCommandValidator();
        var command = new UpdateHolidayCommand(0, new DateOnly(2026, 12, 25), "Hari Raya Natal");

        var result = validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Id);
    }

    [Fact]
    public async Task DeleteHoliday_WhenExists_RemovesHoliday()
    {
        // Arrange
        var holiday = new Holiday { Id = 1, Date = new DateOnly(2026, 8, 17), Name = "Kemerdekaan" };
        var set = new List<Holiday> { holiday }.AsTestDbSet();
        _context.Holidays.Returns(set);

        var handler = new DeleteHolidayCommandHandler(_context);

        // Act
        await handler.Handle(new DeleteHolidayCommand(1), CancellationToken.None);

        // Assert
        _context.Holidays.Received(1).Remove(holiday);
        await _context.SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteHoliday_WhenNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var emptySet = new List<Holiday>().AsTestDbSet();
        _context.Holidays.Returns(emptySet);

        var handler = new DeleteHolidayCommandHandler(_context);

        // Act
        var act = () => handler.Handle(new DeleteHolidayCommand(999), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }
}
