using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Core.Users;

namespace TaskManagement.Infrastructure.Persistence;

public sealed class UserRepository(TaskManagementDbContext context) : IUserRepository
{
    public async Task<bool> ExistsByNormalizedEmailAsync(
        string normalizedEmail, CancellationToken cancellationToken)
    {
        try
        {
            return await context.Users.AnyAsync(
                row => row.NormalizedEmail == normalizedEmail, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new UserPersistenceException(exception);
        }
    }

    public async Task<User> AddAsync(User user, CancellationToken cancellationToken)
    {
        var row = new UserRow
        {
            Name = user.Name,
            Email = user.Email,
            NormalizedEmail = user.NormalizedEmail
        };
        context.Users.Add(row);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
            return new User(row.Id, row.Name, row.Email, row.NormalizedEmail);
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            throw new DuplicateUserEmailException();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new UserPersistenceException(exception);
        }
    }

    public async Task<IReadOnlyList<User>> ListAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await context.Users
                .AsNoTracking()
                .OrderBy(row => row.Id)
                .Select(row => new User(row.Id, row.Name, row.Email, row.NormalizedEmail))
                .ToListAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new UserPersistenceException(exception);
        }
    }
}
