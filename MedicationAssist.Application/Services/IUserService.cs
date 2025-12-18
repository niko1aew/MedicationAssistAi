using MedicationAssist.Application.Common;
using MedicationAssist.Application.DTOs;

namespace MedicationAssist.Application.Services;

public interface IUserService
{
    Task<Result<UserDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<UserDto>> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<Result<UserDto>> GetByTelegramIdAsync(long telegramUserId, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<UserDto>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Result<UserDto>> CreateAsync(CreateUserDto dto, CancellationToken cancellationToken = default);
    Task<Result<UserDto>> UpdateAsync(Guid id, UpdateUserDto dto, CancellationToken cancellationToken = default);
    Task<Result<UserDto>> LinkTelegramAsync(Guid userId, LinkTelegramDto dto, CancellationToken cancellationToken = default);
    Task<Result<UserDto>> UnlinkTelegramAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Result<UserDto>> SetTimeZoneAsync(Guid userId, string timeZoneId, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    // Link Token methods
    Task<Result<string>> GenerateLinkTokenAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Result<UserDto>> LinkTelegramByTokenAsync(string token, LinkTelegramDto dto, CancellationToken cancellationToken = default);
}

