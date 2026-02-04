using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Quater.Shared.Models;

namespace Quater.Backend.Core.Tests.Helpers;

/// <summary>
/// Helper for creating mock UserManager instances for testing
/// </summary>
public static class MockUserManager
{
    /// <summary>
    /// Creates a mock UserManager with default setup
    /// </summary>
    public static Mock<UserManager<User>> Create()
    {
        var store = new Mock<IUserStore<User>>();
        var options = new Mock<IOptions<IdentityOptions>>();
        var passwordHasher = new Mock<IPasswordHasher<User>>();
        var userValidators = new List<IUserValidator<User>>();
        var passwordValidators = new List<IPasswordValidator<User>>();
        var keyNormalizer = new Mock<ILookupNormalizer>();
        var errors = new Mock<IdentityErrorDescriber>();
        var services = new Mock<IServiceProvider>();
        var logger = new Mock<ILogger<UserManager<User>>>();

        options.Setup(o => o.Value).Returns(new IdentityOptions());

        var userManager = new Mock<UserManager<User>>(
            store.Object,
            options.Object,
            passwordHasher.Object,
            userValidators,
            passwordValidators,
            keyNormalizer.Object,
            errors.Object,
            services.Object,
            logger.Object);

        return userManager;
    }

    /// <summary>
    /// Sets up successful user creation
    /// </summary>
    public static void SetupCreateSuccess(this Mock<UserManager<User>> mock)
    {
        mock.Setup(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
    }

    /// <summary>
    /// Sets up failed user creation
    /// </summary>
    public static void SetupCreateFailure(this Mock<UserManager<User>> mock, params string[] errors)
    {
        var identityErrors = errors.Select(e => new IdentityError { Description = e }).ToArray();
        mock.Setup(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(identityErrors));
    }

    /// <summary>
    /// Sets up successful user update
    /// </summary>
    public static void SetupUpdateSuccess(this Mock<UserManager<User>> mock)
    {
        mock.Setup(x => x.UpdateAsync(It.IsAny<User>()))
            .ReturnsAsync(IdentityResult.Success);
    }

    /// <summary>
    /// Sets up finding a user by ID
    /// </summary>
    public static void SetupFindById(this Mock<UserManager<User>> mock, User? user)
    {
        mock.Setup(x => x.FindByIdAsync(It.IsAny<string>()))
            .ReturnsAsync(user);
    }

    /// <summary>
    /// Sets up finding a user by email
    /// </summary>
    public static void SetupFindByEmail(this Mock<UserManager<User>> mock, User? user)
    {
        mock.Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(user);
    }

    /// <summary>
    /// Sets up password change
    /// </summary>
    public static void SetupChangePassword(this Mock<UserManager<User>> mock, bool success)
    {
        mock.Setup(x => x.ChangePasswordAsync(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(success ? IdentityResult.Success : IdentityResult.Failed());
    }
}
