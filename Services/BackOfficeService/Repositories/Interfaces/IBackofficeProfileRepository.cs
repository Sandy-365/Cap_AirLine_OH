using BackOfficeService.Models;

namespace BackOfficeService.Repositories.Interfaces;

/// <summary>
/// Interface for back-office user profile repository operations.
/// </summary>
public interface IBackofficeProfileRepository
{
    /// <summary>
    /// Retrieves a back-office user profile by ID.
    /// </summary>
    Task<BackofficeProfile?> GetByIdAsync(int id);

    /// <summary>
    /// Retrieves a back-office user profile by email address (case-insensitive).
    /// </summary>
    Task<BackofficeProfile?> GetByEmailAsync(string email);

    /// <summary>
    /// Retrieves all back-office user profiles, optionally filtered by roles.
    /// </summary>
    Task<List<BackofficeProfile>> GetAllAsync(string[]? roles = null);

    /// <summary>
    /// Adds a new back-office user profile entity.
    /// </summary>
    Task AddAsync(BackofficeProfile profile);

    /// <summary>
    /// Updates an existing back-office user profile entity.
    /// </summary>
    Task UpdateAsync(BackofficeProfile profile);

    /// <summary>
    /// Deletes a back-office user profile entity by ID.
    /// </summary>
    Task DeleteAsync(int id);
}
