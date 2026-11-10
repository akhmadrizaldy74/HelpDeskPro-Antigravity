namespace HelpDeskPro.UnitTests.Features.Tickets;

using FluentAssertions;
using HelpDeskPro.Domain.Entities;
using HelpDeskPro.Infrastructure.Services;
using Xunit;

public class TicketNumberGeneratorTests
{
    [Theory]
    [InlineData(2026, 1, "HD-2026-000001")]
    [InlineData(2026, 42, "HD-2026-000042")]
    [InlineData(2026, 123, "HD-2026-000123")]
    [InlineData(2026, 999999, "HD-2026-999999")]
    [InlineData(2027, 1, "HD-2027-000001")]
    public void Format_ValidYearAndSequence_ReturnsExpectedTicketNumber(int year, int sequence, string expected)
    {
        // Act
        var result = TicketNumberGenerator.Format(year, sequence);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void Sequence_WhenNewYearBegins_ResetsLastNumberTo1()
    {
        // Arrange
        var sequences = new Dictionary<int, TicketNumberSequence>
        {
            [2026] = new TicketNumberSequence { Year = 2026, LastNumber = 500 }
        };

        // Act - Simulate next ticket in year 2026
        var year2026 = 2026;
        var seq2026 = ++sequences[year2026].LastNumber;
        var ticket2026 = TicketNumberGenerator.Format(year2026, seq2026);

        // Act - Simulate next ticket when year changes to 2027
        var year2027 = 2027;
        if (!sequences.TryGetValue(year2027, out var sequence2027))
        {
            sequence2027 = new TicketNumberSequence { Year = year2027, LastNumber = 1 };
            sequences[year2027] = sequence2027;
        }
        else
        {
            sequence2027.LastNumber++;
        }

        var ticket2027 = TicketNumberGenerator.Format(year2027, sequence2027.LastNumber);

        // Assert
        ticket2026.Should().Be("HD-2026-000501");
        ticket2027.Should().Be("HD-2027-000001");
        ticket2027.Should().StartWith("HD-2027-000001");
    }
}

