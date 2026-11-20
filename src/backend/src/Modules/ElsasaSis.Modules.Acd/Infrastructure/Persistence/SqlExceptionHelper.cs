using System.Data.Common;

namespace ElsasaSis.Modules.Acd.Infrastructure.Persistence;

internal static class SqlExceptionHelper
{
    public static bool IsUniqueConstraintViolation(Exception exception, string? constraintName = null)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is not DbException dbException)
            {
                continue;
            }

            if (current.GetType().Name == "SqlException" &&
                current.GetType().GetProperty("Number")?.GetValue(current) is int number &&
                number is 2601 or 2627)
            {
                return string.IsNullOrWhiteSpace(constraintName)
                    || dbException.Message.Contains(constraintName, StringComparison.OrdinalIgnoreCase);
            }
        }

        return false;
    }
}
