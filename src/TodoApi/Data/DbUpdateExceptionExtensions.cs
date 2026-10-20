using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace TodoApi.Data;

public static class DbUpdateExceptionExtensions
{
    // SQL Server error numbers for duplicate key in a unique index (2601) or unique constraint (2627).
    public static bool IsUniqueConstraintViolation(this DbUpdateException ex) =>
        ex.InnerException is SqlException { Number: 2601 or 2627 };
}
