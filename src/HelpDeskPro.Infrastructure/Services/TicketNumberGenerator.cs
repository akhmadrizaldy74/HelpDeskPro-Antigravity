namespace HelpDeskPro.Infrastructure.Services;

using System.Data;
using System.Globalization;
using HelpDeskPro.Application.Common.Interfaces;
using HelpDeskPro.Domain.Entities;
using HelpDeskPro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

public class TicketNumberGenerator : ITicketNumberGenerator
{
    private static readonly SemaphoreSlim _fallbackLock = new(1, 1);
    private readonly AppDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;

    public TicketNumberGenerator(AppDbContext context, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
    }

    public static string Format(int year, int sequenceNumber) => $"HD-{year}-{sequenceNumber:D6}";

    public async Task<string> GenerateTicketNumberAsync(CancellationToken cancellationToken = default)
    {
        var year = _dateTimeProvider.UtcNow.Year;

        if (_context.Database.IsSqlServer() && _context.Database.IsRelational())
        {
            var connection = _context.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync(cancellationToken);
            }

            await using var command = connection.CreateCommand();
            command.Transaction = _context.Database.CurrentTransaction?.GetDbTransaction();
            command.CommandText = @"
                SET NOCOUNT ON;
                DECLARE @NextNumber INT;

                UPDATE TicketNumberSequences WITH (UPDLOCK, ROWLOCK)
                SET @NextNumber = LastNumber = LastNumber + 1
                WHERE [Year] = @Year;

                IF @@ROWCOUNT = 0
                BEGIN
                    BEGIN TRY
                        INSERT INTO TicketNumberSequences ([Year], LastNumber) VALUES (@Year, 1);
                        SET @NextNumber = 1;
                    END TRY
                    BEGIN CATCH
                        UPDATE TicketNumberSequences WITH (UPDLOCK, ROWLOCK)
                        SET @NextNumber = LastNumber = LastNumber + 1
                        WHERE [Year] = @Year;
                    END CATCH
                END

                SELECT @NextNumber;
            ";

            var parameter = command.CreateParameter();
            parameter.ParameterName = "@Year";
            parameter.Value = year;
            command.Parameters.Add(parameter);

            var scalar = await command.ExecuteScalarAsync(cancellationToken);
            var nextNumber = Convert.ToInt32(scalar, CultureInfo.InvariantCulture);
            return Format(year, nextNumber);
        }

        // Thread-safe fallback for testing or non-relational providers
        await _fallbackLock.WaitAsync(cancellationToken);
        try
        {
            var sequence = await _context.TicketNumberSequences
                .FirstOrDefaultAsync(s => s.Year == year, cancellationToken);

            if (sequence == null)
            {
                sequence = new TicketNumberSequence { Year = year, LastNumber = 1 };
                _context.TicketNumberSequences.Add(sequence);
            }
            else
            {
                sequence.LastNumber++;
            }

            await _context.SaveChangesAsync(cancellationToken);
            return Format(year, sequence.LastNumber);
        }
        finally
        {
            _fallbackLock.Release();
        }
    }
}
