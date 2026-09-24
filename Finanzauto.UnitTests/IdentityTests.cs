using Finanzauto.Application.Identity;
using Finanzauto.Domain.Entities;
using Moq;

namespace Finanzauto.UnitTests;

public class IdentityTests
{
    private readonly Mock<IIdentityStore> store = new();
    private readonly Mock<IPasswordService> passwords = new();
    private readonly Mock<ITokenService> tokens = new();
    private static Employee User() => new()
    {
        EmployeeId = 42,
        Email = "user@example.test",
        PasswordHash = "original-hash",
        RoleId = 2,
        Role = new Role { RoleId = 2, Name = "User" }
    };

    [Fact]
    public async Task Login_normalizes_email_and_returns_issued_token()
    {
        var user = User();
        store.Setup(s => s.FindByEmailAsync("USER@EXAMPLE.TEST", default)).ReturnsAsync(user);
        passwords.Setup(p => p.Verify(user, "password")).Returns(true);
        var issued = new IssuedToken("signed-token", DateTime.UtcNow.AddMinutes(10));
        tokens.Setup(t => t.Issue(user)).Returns(issued);
        var result = await new LoginService(store.Object, passwords.Object, tokens.Object)
            .LoginAsync(new() { Email = " user@example.test ", Password = "password" }, default);
        Assert.Equal(issued.Value, result.AccessToken);
        Assert.Equal(issued.ExpiresAtUtc, result.ExpiresAtUtc);
        Assert.Equal("Bearer", result.TokenType);
        Assert.Equal(42, result.User.Id);
    }

    [Theory]
    [InlineData(false, true, true, true)]
    [InlineData(true, false, true, true)]
    [InlineData(true, true, false, true)]
    [InlineData(true, true, true, false)]
    public async Task Invalid_login_never_issues_token(bool exists, bool correctPassword, bool active, bool roleActive)
    {
        var user = User();
        user.Active = active;
        user.Role.Active = roleActive;
        store.Setup(s => s.FindByEmailAsync(It.IsAny<string>(), default)).ReturnsAsync(exists ? user : null);
        passwords.Setup(p => p.Verify(user, It.IsAny<string>())).Returns(correctPassword);
        var error = await Assert.ThrowsAsync<IdentityException>(() =>
            new LoginService(store.Object, passwords.Object, tokens.Object).LoginAsync(
                new() { Email = user.Email, Password = "wrong" }, default));
        Assert.Equal(401, error.StatusCode);
        tokens.Verify(t => t.Issue(It.IsAny<Employee>()), Times.Never);
        store.Verify(s => s.SaveAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Registration_assigns_User_and_stores_only_hash()
    {
        var role = User().Role;
        store.Setup(s => s.FindRoleAsync(2, default)).ReturnsAsync(role);
        passwords.Setup(p => p.Hash(It.IsAny<Employee>(), "long-password")).Returns("hashed");
        Employee? saved = null;
        store.Setup(s => s.AddUser(It.IsAny<Employee>())).Callback<Employee>(u => saved = u);
        var result = await new UserAdministrationService(store.Object, passwords.Object).RegisterAsync(
            new() { Email = " user@example.test ", FirstName = " Ana ", LastName = " Perez ", Password = "long-password" }, default);
        Assert.Equal(2, result.RoleId);
        Assert.NotNull(saved);
        Assert.Equal("hashed", saved.PasswordHash);
        Assert.Equal("Ana", saved.FirstName);
        store.Verify(s => s.EmailExistsAsync("USER@EXAMPLE.TEST", null, default), Times.Once);
        store.Verify(s => s.SaveAsync(default), Times.Once);
    }

    [Fact]
    public async Task Duplicate_email_prevents_registration()
    {
        store.Setup(s => s.EmailExistsAsync(It.IsAny<string>(), null, default)).ReturnsAsync(true);
        var error = await Assert.ThrowsAsync<IdentityException>(() =>
            new UserAdministrationService(store.Object, passwords.Object).RegisterAsync(
                new() { Email = "duplicate@example.test" }, default));
        Assert.Equal(409, error.StatusCode);
        store.Verify(s => s.AddUser(It.IsAny<Employee>()), Times.Never);
        store.Verify(s => s.SaveAsync(default), Times.Never);
        passwords.Verify(p => p.Hash(It.IsAny<Employee>(), It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task Base_roles_cannot_be_renamed_or_disabled(int id)
    {
        store.Setup(s => s.FindRoleAsync(id, default)).ReturnsAsync(new Role { RoleId = id });
        var service = new UserAdministrationService(store.Object, passwords.Object);
        Assert.Equal(409, (await Assert.ThrowsAsync<IdentityException>(() =>
            service.UpdateRoleAsync(id, new() { Name = "Other" }, default))).StatusCode);
        Assert.Equal(409, (await Assert.ThrowsAsync<IdentityException>(() =>
            service.SetRoleActiveAsync(id, false, default))).StatusCode);
        store.Verify(s => s.SaveAsync(default), Times.Never);
    }

    [Fact]
    public async Task Cannot_disable_own_account_or_change_own_role()
    {
        var user = User();
        store.Setup(s => s.FindUserAsync(42, default)).ReturnsAsync(user);
        var service = new UserAdministrationService(store.Object, passwords.Object);
        Assert.Equal(409, (await Assert.ThrowsAsync<IdentityException>(() =>
            service.SetUserActiveAsync(42, false, 42, default))).StatusCode);
        Assert.Equal(409, (await Assert.ThrowsAsync<IdentityException>(() =>
            service.UpdateAsync(42, new() { RoleId = 1 }, 42, default))).StatusCode);
        Assert.True(user.Active);
        Assert.Equal(2, user.RoleId);
        store.Verify(s => s.SaveAsync(default), Times.Never);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Password_change_requires_current_password(bool correct)
    {
        var user = User();
        store.Setup(s => s.FindUserAsync(42, default)).ReturnsAsync(user);
        passwords.Setup(p => p.Verify(user, "current")).Returns(correct);
        passwords.Setup(p => p.Hash(user, "replacement")).Returns("new-hash");
        var service = new ProfileService(store.Object, passwords.Object);
        var request = new ChangeProfilePasswordRequest { CurrentPassword = "current", NewPassword = "replacement" };
        if (correct)
        {
            await service.ChangePasswordAsync(42, request, default);
            Assert.Equal("new-hash", user.PasswordHash);
            store.Verify(s => s.SaveAsync(default), Times.Once);
        }
        else
        {
            Assert.Equal(400, (await Assert.ThrowsAsync<IdentityException>(() =>
                service.ChangePasswordAsync(42, request, default))).StatusCode);
            Assert.Equal("original-hash", user.PasswordHash);
            passwords.Verify(p => p.Hash(It.IsAny<Employee>(), It.IsAny<string>()), Times.Never);
            store.Verify(s => s.SaveAsync(default), Times.Never);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Profile_update_checks_duplicate_email_and_preserves_role(bool duplicate)
    {
        var user = User();
        store.Setup(s => s.FindUserAsync(42, default)).ReturnsAsync(user);
        store.Setup(s => s.EmailExistsAsync("NEW@EXAMPLE.TEST", 42, default)).ReturnsAsync(duplicate);
        var service = new ProfileService(store.Object, passwords.Object);
        var request = new UpdateProfileRequest { FirstName = " Ana ", LastName = " Perez ", Email = " new@example.test ", City = " Bogota " };
        if (duplicate)
        {
            Assert.Equal(409, (await Assert.ThrowsAsync<IdentityException>(() =>
                service.UpdateAsync(42, request, default))).StatusCode);
            Assert.Equal("user@example.test", user.Email);
            store.Verify(s => s.SaveAsync(default), Times.Never);
        }
        else
        {
            var result = await service.UpdateAsync(42, request, default);
            Assert.Equal("new@example.test", result.Email);
            Assert.Equal("Ana", result.FirstName);
            Assert.Equal("Bogota", result.City);
            store.Verify(s => s.SaveAsync(default), Times.Once);
        }
        Assert.Equal(2, user.RoleId);
        Assert.Equal("original-hash", user.PasswordHash);
    }
}

