using FluentAssertions;
using MedicationAssist.Application.Common;
using MedicationAssist.Application.DTOs;
using MedicationAssist.Application.Services;
using MedicationAssist.Domain.Entities;
using MedicationAssist.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Moq;

namespace MedicationAssist.Tests.Unit.Application;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly Mock<IJwtTokenService> _jwtTokenServiceMock;
    private readonly Mock<ILogger<AuthService>> _loggerMock;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _jwtTokenServiceMock = new Mock<IJwtTokenService>();
        _loggerMock = new Mock<ILogger<AuthService>>();

        _unitOfWorkMock.Setup(u => u.Users).Returns(_userRepositoryMock.Object);

        _authService = new AuthService(
            _userRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _passwordHasherMock.Object,
            _jwtTokenServiceMock.Object,
            _loggerMock.Object
        );
    }

    #region RegisterAsync Tests

    [Fact]
    public async Task RegisterAsync_Should_Return_Success_When_Valid_Data()
    {
        // Arrange
        var dto = new RegisterDto
        {
            Name = "Иван Иванов",
            Email = "ivan@example.com",
            Password = "password123"
        };
        var passwordHash = "hashed_password_123";
        var token = "jwt_token_123";

        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync(dto.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        _passwordHasherMock
            .Setup(h => h.HashPassword(dto.Password))
            .Returns(passwordHash);

        _jwtTokenServiceMock
            .Setup(j => j.GenerateToken(It.IsAny<User>()))
            .Returns(token);

        // Act
        var result = await _authService.RegisterAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Token.Should().Be(token);
        result.Data.User.Should().NotBeNull();
        result.Data.User.Name.Should().Be(dto.Name);
        result.Data.User.Email.Should().Be(dto.Email);
        result.Data.User.Role.Should().Be(UserRole.User);

        _userRepositoryMock.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_Should_Return_Failure_When_Email_Already_Exists()
    {
        // Arrange
        var dto = new RegisterDto
        {
            Name = "Иван Иванов",
            Email = "ivan@example.com",
            Password = "password123"
        };
        var existingUser = new User("Existing User", dto.Email, "old_hash", UserRole.User);

        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync(dto.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        // Act
        var result = await _authService.RegisterAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Пользователь с таким email уже существует");

        _userRepositoryMock.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_Should_Hash_Password_Before_Storing()
    {
        // Arrange
        var dto = new RegisterDto
        {
            Name = "Иван Иванов",
            Email = "ivan@example.com",
            Password = "password123"
        };
        var passwordHash = "bcrypt_hashed_password";
        User? capturedUser = null;

        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync(dto.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        _passwordHasherMock
            .Setup(h => h.HashPassword(dto.Password))
            .Returns(passwordHash);

        _userRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((user, _) => capturedUser = user);

        _jwtTokenServiceMock
            .Setup(j => j.GenerateToken(It.IsAny<User>()))
            .Returns("token");

        // Act
        await _authService.RegisterAsync(dto);

        // Assert
        _passwordHasherMock.Verify(h => h.HashPassword(dto.Password), Times.Once);
        capturedUser.Should().NotBeNull();
        capturedUser!.PasswordHash.Should().Be(passwordHash);
    }

    [Fact]
    public async Task RegisterAsync_Should_Create_User_With_Default_User_Role()
    {
        // Arrange
        var dto = new RegisterDto
        {
            Name = "Иван Иванов",
            Email = "ivan@example.com",
            Password = "password123"
        };
        User? capturedUser = null;

        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync(dto.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        _passwordHasherMock
            .Setup(h => h.HashPassword(dto.Password))
            .Returns("hashed");

        _userRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((user, _) => capturedUser = user);

        _jwtTokenServiceMock
            .Setup(j => j.GenerateToken(It.IsAny<User>()))
            .Returns("token");

        // Act
        await _authService.RegisterAsync(dto);

        // Assert
        capturedUser.Should().NotBeNull();
        capturedUser!.Role.Should().Be(UserRole.User);
    }

    [Fact]
    public async Task RegisterAsync_Should_Generate_JWT_Token_After_User_Creation()
    {
        // Arrange
        var dto = new RegisterDto
        {
            Name = "Иван Иванов",
            Email = "ivan@example.com",
            Password = "password123"
        };
        var expectedToken = "jwt_token_xyz";
        User? capturedUserForToken = null;

        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync(dto.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        _passwordHasherMock
            .Setup(h => h.HashPassword(dto.Password))
            .Returns("hashed");

        _jwtTokenServiceMock
            .Setup(j => j.GenerateToken(It.IsAny<User>()))
            .Callback<User>(user => capturedUserForToken = user)
            .Returns(expectedToken);

        // Act
        var result = await _authService.RegisterAsync(dto);

        // Assert
        _jwtTokenServiceMock.Verify(j => j.GenerateToken(It.IsAny<User>()), Times.Once);
        capturedUserForToken.Should().NotBeNull();
        capturedUserForToken!.Email.Should().Be(dto.Email);
        result.Data!.Token.Should().Be(expectedToken);
    }

    #endregion

    #region LoginAsync Tests

    [Fact]
    public async Task LoginAsync_Should_Return_Success_When_Credentials_Are_Valid()
    {
        // Arrange
        var dto = new LoginDto
        {
            Email = "ivan@example.com",
            Password = "password123"
        };
        var passwordHash = "hashed_password_123";
        var user = new User("Иван Иванов", dto.Email, passwordHash, UserRole.User);
        var token = "jwt_token_123";

        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync(dto.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _passwordHasherMock
            .Setup(h => h.VerifyPassword(dto.Password, passwordHash))
            .Returns(true);

        _jwtTokenServiceMock
            .Setup(j => j.GenerateToken(user))
            .Returns(token);

        // Act
        var result = await _authService.LoginAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Token.Should().Be(token);
        result.Data.User.Should().NotBeNull();
        result.Data.User.Email.Should().Be(dto.Email);
        result.Data.User.Name.Should().Be(user.Name);
    }

    [Fact]
    public async Task LoginAsync_Should_Return_Failure_When_User_Not_Found()
    {
        // Arrange
        var dto = new LoginDto
        {
            Email = "nonexistent@example.com",
            Password = "password123"
        };

        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync(dto.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _authService.LoginAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Неверный email или пароль");

        _passwordHasherMock.Verify(h => h.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _jwtTokenServiceMock.Verify(j => j.GenerateToken(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_Should_Return_Failure_When_Password_Is_Invalid()
    {
        // Arrange
        var dto = new LoginDto
        {
            Email = "ivan@example.com",
            Password = "wrong_password"
        };
        var passwordHash = "hashed_password_123";
        var user = new User("Иван Иванов", dto.Email, passwordHash, UserRole.User);

        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync(dto.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _passwordHasherMock
            .Setup(h => h.VerifyPassword(dto.Password, passwordHash))
            .Returns(false);

        // Act
        var result = await _authService.LoginAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Неверный email или пароль");

        _jwtTokenServiceMock.Verify(j => j.GenerateToken(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_Should_Verify_Password_Against_Stored_Hash()
    {
        // Arrange
        var dto = new LoginDto
        {
            Email = "ivan@example.com",
            Password = "password123"
        };
        var passwordHash = "hashed_password_123";
        var user = new User("Иван Иванов", dto.Email, passwordHash, UserRole.User);

        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync(dto.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _passwordHasherMock
            .Setup(h => h.VerifyPassword(dto.Password, passwordHash))
            .Returns(true);

        _jwtTokenServiceMock
            .Setup(j => j.GenerateToken(user))
            .Returns("token");

        // Act
        await _authService.LoginAsync(dto);

        // Assert
        _passwordHasherMock.Verify(h => h.VerifyPassword(dto.Password, passwordHash), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_Should_Generate_JWT_Token_After_Successful_Verification()
    {
        // Arrange
        var dto = new LoginDto
        {
            Email = "ivan@example.com",
            Password = "password123"
        };
        var passwordHash = "hashed_password_123";
        var user = new User("Иван Иванов", dto.Email, passwordHash, UserRole.User);
        var expectedToken = "jwt_token_xyz";

        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync(dto.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _passwordHasherMock
            .Setup(h => h.VerifyPassword(dto.Password, passwordHash))
            .Returns(true);

        _jwtTokenServiceMock
            .Setup(j => j.GenerateToken(user))
            .Returns(expectedToken);

        // Act
        var result = await _authService.LoginAsync(dto);

        // Assert
        _jwtTokenServiceMock.Verify(j => j.GenerateToken(user), Times.Once);
        result.Data!.Token.Should().Be(expectedToken);
    }

    [Fact]
    public async Task LoginAsync_Should_Return_User_With_Correct_Role()
    {
        // Arrange
        var dto = new LoginDto
        {
            Email = "admin@example.com",
            Password = "password123"
        };
        var passwordHash = "hashed_password_123";
        var adminUser = new User("Admin User", dto.Email, passwordHash, UserRole.Admin);

        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync(dto.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(adminUser);

        _passwordHasherMock
            .Setup(h => h.VerifyPassword(dto.Password, passwordHash))
            .Returns(true);

        _jwtTokenServiceMock
            .Setup(j => j.GenerateToken(adminUser))
            .Returns("token");

        // Act
        var result = await _authService.LoginAsync(dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data!.User.Role.Should().Be(UserRole.Admin);
    }

    [Fact]
    public async Task LoginAsync_Should_Not_Reveal_Whether_Email_Exists_In_Error_Message()
    {
        // Arrange
        var dtoUserNotFound = new LoginDto
        {
            Email = "nonexistent@example.com",
            Password = "password123"
        };
        var dtoWrongPassword = new LoginDto
        {
            Email = "ivan@example.com",
            Password = "wrong_password"
        };
        
        var user = new User("Иван Иванов", "ivan@example.com", "hashed", UserRole.User);

        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync(dtoUserNotFound.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync(dtoWrongPassword.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _passwordHasherMock
            .Setup(h => h.VerifyPassword(dtoWrongPassword.Password, user.PasswordHash))
            .Returns(false);

        // Act
        var resultUserNotFound = await _authService.LoginAsync(dtoUserNotFound);
        var resultWrongPassword = await _authService.LoginAsync(dtoWrongPassword);

        // Assert - Both should return the same generic error message
        resultUserNotFound.Error.Should().Be("Неверный email или пароль");
        resultWrongPassword.Error.Should().Be("Неверный email или пароль");
        resultUserNotFound.Error.Should().Be(resultWrongPassword.Error);
    }

    #endregion

    #region Logging Tests

    [Fact]
    public async Task RegisterAsync_Should_Log_Information_On_Success()
    {
        // Arrange
        var dto = new RegisterDto
        {
            Name = "Иван Иванов",
            Email = "ivan@example.com",
            Password = "password123"
        };

        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync(dto.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        _passwordHasherMock
            .Setup(h => h.HashPassword(dto.Password))
            .Returns("hashed");

        _jwtTokenServiceMock
            .Setup(j => j.GenerateToken(It.IsAny<User>()))
            .Returns("token");

        // Act
        await _authService.RegisterAsync(dto);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("успешно зарегистрирован")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task RegisterAsync_Should_Log_Warning_On_Duplicate_Email()
    {
        // Arrange
        var dto = new RegisterDto
        {
            Name = "Иван Иванов",
            Email = "ivan@example.com",
            Password = "password123"
        };
        var existingUser = new User("Existing User", dto.Email, "old_hash", UserRole.User);

        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync(dto.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        // Act
        await _authService.RegisterAsync(dto);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Попытка регистрации с существующим email")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task LoginAsync_Should_Log_Information_On_Success()
    {
        // Arrange
        var dto = new LoginDto
        {
            Email = "ivan@example.com",
            Password = "password123"
        };
        var user = new User("Иван Иванов", dto.Email, "hashed", UserRole.User);

        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync(dto.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _passwordHasherMock
            .Setup(h => h.VerifyPassword(dto.Password, user.PasswordHash))
            .Returns(true);

        _jwtTokenServiceMock
            .Setup(j => j.GenerateToken(user))
            .Returns("token");

        // Act
        await _authService.LoginAsync(dto);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("успешно вошел в систему")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task LoginAsync_Should_Log_Warning_On_Invalid_Credentials()
    {
        // Arrange
        var dto = new LoginDto
        {
            Email = "ivan@example.com",
            Password = "wrong_password"
        };
        var user = new User("Иван Иванов", dto.Email, "hashed", UserRole.User);

        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync(dto.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _passwordHasherMock
            .Setup(h => h.VerifyPassword(dto.Password, user.PasswordHash))
            .Returns(false);

        // Act
        await _authService.LoginAsync(dto);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Неверный пароль для пользователя")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion
}
