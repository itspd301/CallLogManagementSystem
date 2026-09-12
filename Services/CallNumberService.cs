using System.Data;
using CallLogManagementSystem.Data;
using CallLogManagementSystem.Interfaces;
using CallLogManagementSystem.Models.Entities.System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

namespace CallLogManagementSystem.Services
{
    // Section 10 — CALL-YYYY-NNNNNN, unique/sequential/never reused, safe under concurrent
    // Create Call submissions. Uses a Serializable transaction against a one-row-per-year
    // counter table: SQL Server will block (or abort with a serialization failure that we
    // retry) a second concurrent transaction touching the same year row, so two callers can
    // never walk away with the same number.
    public class CallNumberService : ICallNumberService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<CallNumberService> _logger;

        public CallNumberService(ApplicationDbContext context, IConfiguration configuration, ILogger<CallNumberService> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<string> GenerateAsync()
        {
            var prefix = _configuration["AppSettings:CallNumberPrefix"] ?? "CALL";
            var year = DateTime.Now.Year; // local business year, not UTC — see class remarks.

            const int maxAttempts = 5;
            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

                    var sequence = await _context.CallNumberSequences.FirstOrDefaultAsync(s => s.Year == year);
                    if (sequence == null)
                    {
                        sequence = new CallNumberSequence { Year = year, LastNumber = 1 };
                        _context.CallNumberSequences.Add(sequence);
                    }
                    else
                    {
                        sequence.LastNumber++;
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return $"{prefix}-{year}-{sequence.LastNumber:D6}";
                }
                catch (Exception ex) when (attempt < maxAttempts && IsTransientConcurrencyFailure(ex))
                {
                    _logger.LogWarning(ex, "Call number generation contended on attempt {Attempt}, retrying", attempt);
                    await Task.Delay(Random.Shared.Next(20, 80) * attempt);
                }
            }

            throw new InvalidOperationException("Failed to generate a unique call number after multiple attempts.");
        }

        private static bool IsTransientConcurrencyFailure(Exception ex)
        {
            // SQL Server: 1205 = deadlock victim, 41302/41305 = serializable validation failures.
            return ex is DbUpdateException { InnerException: SqlException sqlEx } &&
                   (sqlEx.Number == 1205 || sqlEx.Number == 41302 || sqlEx.Number == 41305 || sqlEx.Number == 2627);
        }
    }
}
