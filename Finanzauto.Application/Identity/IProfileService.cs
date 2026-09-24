namespace Finanzauto.Application.Identity;

public interface IProfileService
{
    Task<ProfileResponse> GetAsync(int userId, CancellationToken ct);
    Task<ProfileResponse> UpdateAsync(int userId, UpdateProfileRequest request, CancellationToken ct);
    Task ChangePasswordAsync(int userId, ChangeProfilePasswordRequest request, CancellationToken ct);
}
