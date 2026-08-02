namespace NJobDesk.Core.Contracts;

public sealed record PagedResult<T>(long Total, IEnumerable<T> Items);
