using System.Net.Mail;

namespace TaskManagement.Core.Users;

public sealed record User(int Id, string Name, string Email, string NormalizedEmail);

public interface IUserRepository
{
    Task<bool> ExistsByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken);
    Task<User?> FindByIdAsync(int id, CancellationToken cancellationToken);
    Task<User> AddAsync(User user, CancellationToken cancellationToken);
    Task<IReadOnlyList<User>> ListAsync(CancellationToken cancellationToken);
}

public sealed class UserService(IUserRepository repository)
{
    public async Task<User> CreateAsync(string? name, string? email, CancellationToken cancellationToken = default)
    {
        var trimmedName = name?.Trim() ?? string.Empty;
        var trimmedEmail = email?.Trim() ?? string.Empty;

        if (trimmedName.Length is 0 or > 120)
        {
            throw new UserValidationException("El nombre es obligatorio y no puede superar 120 caracteres.");
        }

        if (trimmedEmail.Length is 0 or > 254 ||
            !MailAddress.TryCreate(trimmedEmail, out var address) ||
            !string.Equals(address.Address, trimmedEmail, StringComparison.OrdinalIgnoreCase))
        {
            throw new UserValidationException("El correo electrónico no tiene un formato válido.");
        }

        var normalizedEmail = trimmedEmail.ToLowerInvariant();
        if (await repository.ExistsByNormalizedEmailAsync(normalizedEmail, cancellationToken))
        {
            throw new DuplicateUserEmailException();
        }

        return await repository.AddAsync(
            new User(0, trimmedName, trimmedEmail, normalizedEmail), cancellationToken);
    }

    public Task<IReadOnlyList<User>> ListAsync(CancellationToken cancellationToken = default) =>
        repository.ListAsync(cancellationToken);

    public async Task<User> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw new UserValidationException("El colaborador solicitado no es válido.");

        return await repository.FindByIdAsync(id, cancellationToken)
            ?? throw new UserResourceNotFoundException("El colaborador solicitado no existe.");
    }
}

public sealed class UserValidationException(string message) : Exception(message);

public sealed class DuplicateUserEmailException() : Exception("Ya existe un colaborador con el mismo correo electrónico.");

public sealed class UserResourceNotFoundException(string message) : Exception(message);

public sealed class UserPersistenceException(Exception innerException)
    : Exception("No fue posible persistir la información del colaborador.", innerException);
