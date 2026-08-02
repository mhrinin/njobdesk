using Microsoft.EntityFrameworkCore;

namespace NJobDesk.History.EFCore.Persistence;

public sealed class SqliteNJobDeskHistoryDbContext(DbContextOptions<SqliteNJobDeskHistoryDbContext> options)
    : NJobDeskHistoryDbContext(options);
