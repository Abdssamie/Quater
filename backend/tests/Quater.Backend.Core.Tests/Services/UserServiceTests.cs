using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Moq;
using Quater.Backend.Core.DTOs;
using Quater.Backend.Core.Exceptions;
using Quater.Backend.Core.Tests.Helpers;
using Quater.Backend.Data;
using Quater.Backend.Services;
using Quater.Shared.Enums;
using Quater.Shared.Models;
using Xunit;

namespace Quater.Backend.Core.Tests.Services;

public class UserServiceTests : IDisposable
{
    private readonly QuaterDbContext _context;
    private readonly Mock<UserManager<User>> _userManager;
    private readonly FakeTimeProvider _timeProvider;
    private readonly UserService _service;

    public UserServiceTests()
    {
        _context = TestDbContextFactory.CreateSeededContext();
        _userManager = MockUserManager.Create();
        _timeProvider = new FakeTimeProvider();
        _service = new UserService(_context, _userManager.Object, _timeProvider);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingUser_ReturnsUser()
    {
        // Arrange
        var lab = _context.Labs.First();
        var user = new User
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "testuser",
            Email = "test@example.com",
            Role = UserRole.Technician,
            LabId = lab.Id,
            IsActive = true,

            CreatedAt = DateTime.UtcNow,
            CreatedBy = "system"
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetByIdAsync(user.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(user.Id);
        result.Email.Should().Be(user.Email);
        result.LabName.Should().Be(lab.Name);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentUser_ReturnsNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid().ToString();

        // Act
        var result = await _service.GetByIdAsync(nonExistentId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsPagedResults()
    {
        // Arrange
        var lab = _context.Labs.First();
        var users = new List<User>
        {
            new() { Id = Guid.NewGuid().ToString(), UserName = "user1", Email = "user1@test.com", 
                    Role = UserRole.Technician, LabId = lab.Id, IsActive = true, 
         CreatedAt = DateTime.UtcNow, CreatedBy = "system" },
            new() { Id = Guid.NewGuid().ToString(), UserName = "user2", Email = "user2@test.com", 
                    Role = UserRole.Viewer, LabId = lab.Id, IsActive = true, 
         CreatedAt = DateTime.UtcNow, CreatedBy = "system" }
        };
        _context.Users.AddRange(users);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetAllAsync(1, 10);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCountGreaterOrEqualTo(2);
        result.TotalCount.Should().BeGreaterOrEqualTo(2);
    }

    [Fact]
    public async Task GetByLabIdAsync_ReturnsOnlyUsersForLab()
    {
        // Arrange
        var lab1 = _context.Labs.First();
        var lab2 = _context.Labs.Skip(1).First();
        
        var users = new List<User>
        {
            new() { Id = Guid.NewGuid().ToString(), UserName = "lab1user", Email = "lab1@test.com", 
                    Role = UserRole.Technician, LabId = lab1.Id, IsActive = true, 
         CreatedAt = DateTime.UtcNow, CreatedBy = "system" },
            new() { Id = Guid.NewGuid().ToString(), UserName = "lab2user", Email = "lab2@test.com", 
                    Role = UserRole.Technician, LabId = lab2.Id, IsActive = true, 
         CreatedAt = DateTime.UtcNow, CreatedBy = "system" }
        };
        _context.Users.AddRange(users);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetByLabIdAsync(lab1.Id);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().OnlyContain(u => u.LabId == lab1.Id);
    }

    [Fact]
    public async Task GetActiveAsync_ReturnsOnlyActiveUsers()
    {
        // Arrange
        var lab = _context.Labs.First();
        var users = new List<User>
        {
            new() { Id = Guid.NewGuid().ToString(), UserName = "active", Email = "active@test.com", 
                    Role = UserRole.Technician, LabId = lab.Id, IsActive = true, 
         CreatedAt = DateTime.UtcNow, CreatedBy = "system" },
            new() { Id = Guid.NewGuid().ToString(), UserName = "inactive", Email = "inactive@test.com", 
                    Role = UserRole.Technician, LabId = lab.Id, IsActive = false, 
         CreatedAt = DateTime.UtcNow, CreatedBy = "system" }
        };
        _context.Users.AddRange(users);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetActiveAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().OnlyContain(u => u.IsActive);
    }

    [Fact]
    public async Task CreateAsync_ValidUser_CreatesUser()
    {
        // Arrange
        var lab = _context.Labs.First();
        var dto = new CreateUserDto
        {
            UserName = "newuser",
            Email = "newuser@test.com",
            Password = "Password123!",
            Role = UserRole.Technician,
            LabId = lab.Id
        };

        _userManager.SetupCreateSuccess();
        _userManager.Setup(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
            .Callback<User, string>((user, password) =>
            {
                user.Id = Guid.NewGuid().ToString();
                _context.Users.Add(user);
                _context.SaveChanges();
            })
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _service.CreateAsync(dto, "admin");

        // Assert
        result.Should().NotBeNull();
        result.Email.Should().Be(dto.Email);
        result.Role.Should().Be(dto.Role);
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task CreateAsync_NonExistentLab_ThrowsNotFoundException()
    {
        // Arrange
        var dto = new CreateUserDto
        {
            UserName = "newuser",
            Email = "newuser@test.com",
            Password = "Password123!",
            Role = UserRole.Technician,
            LabId = Guid.NewGuid() // Non-existent lab
        };

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => 
            _service.CreateAsync(dto, "admin"));
    }

    [Fact]
    public async Task CreateAsync_UserCreationFails_ThrowsBadRequestException()
    {
        // Arrange
        var lab = _context.Labs.First();
        var dto = new CreateUserDto
        {
            UserName = "newuser",
            Email = "newuser@test.com",
            Password = "weak",
            Role = UserRole.Technician,
            LabId = lab.Id
        };

        _userManager.SetupCreateFailure("Password too weak");

        // Act & Assert
        await Assert.ThrowsAsync<BadRequestException>(() => 
            _service.CreateAsync(dto, "admin"));
    }

    [Fact]
    public async Task UpdateAsync_ExistingUser_UpdatesUser()
    {
        // Arrange
        var lab = _context.Labs.First();
        var user = new User
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "oldname",
            Email = "old@test.com",
            Role = UserRole.Viewer,
            LabId = lab.Id,
            IsActive = true,

            CreatedAt = DateTime.UtcNow,
            CreatedBy = "system"
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var dto = new UpdateUserDto
        {
            UserName = "newname",
            Email = "new@test.com",
            Role = UserRole.Technician
        };

        _userManager.SetupFindById(user);
        _userManager.SetupUpdateSuccess();

        // Act
        var result = await _service.UpdateAsync(user.Id, dto, "admin");

        // Assert
        result.Should().NotBeNull();
        result!.UserName.Should().Be(dto.UserName);
        result.Email.Should().Be(dto.Email);
        result.Role.Should().Be(dto.Role);
    }

    [Fact]
    public async Task UpdateAsync_NonExistentUser_ReturnsNull()
    {
        // Arrange
        var dto = new UpdateUserDto { UserName = "test" };
        _userManager.SetupFindById(null);

        // Act
        var result = await _service.UpdateAsync(Guid.NewGuid().ToString(), dto, "admin");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_ExistingUser_MarksInactive()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "testuser",
            Email = "test@test.com",
            Role = UserRole.Technician,
            LabId = _context.Labs.First().Id,
            IsActive = true,

            CreatedAt = DateTime.UtcNow,
            CreatedBy = "system"
        };

        _userManager.SetupFindById(user);
        _userManager.SetupUpdateSuccess();

        // Act
        var result = await _service.DeleteAsync(user.Id);

        // Assert
        result.Should().BeTrue();
        user.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_NonExistentUser_ReturnsFalse()
    {
        // Arrange
        _userManager.SetupFindById(null);

        // Act
        var result = await _service.DeleteAsync(Guid.NewGuid().ToString());

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ChangePasswordAsync_ValidPassword_ChangesPassword()
    {
        // Arrange
        var user = new User { Id = Guid.NewGuid().ToString() };
        var dto = new ChangePasswordDto
        {
            CurrentPassword = "OldPassword123!",
            NewPassword = "NewPassword123!"
        };

        _userManager.SetupFindById(user);
        _userManager.SetupChangePassword(true);

        // Act
        var result = await _service.ChangePasswordAsync(user.Id, dto);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ChangePasswordAsync_InvalidPassword_ReturnsFalse()
    {
        // Arrange
        var user = new User { Id = Guid.NewGuid().ToString() };
        var dto = new ChangePasswordDto
        {
            CurrentPassword = "WrongPassword",
            NewPassword = "NewPassword123!"
        };

        _userManager.SetupFindById(user);
        _userManager.SetupChangePassword(false);

        // Act
        var result = await _service.ChangePasswordAsync(user.Id, dto);

        // Assert
        result.Should().BeFalse();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
