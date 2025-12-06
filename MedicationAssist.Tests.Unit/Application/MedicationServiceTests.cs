using FluentAssertions;
using MedicationAssist.Application.DTOs;
using MedicationAssist.Application.Services;
using MedicationAssist.Domain.Entities;
using MedicationAssist.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Moq;

namespace MedicationAssist.Tests.Unit.Application;

public class MedicationServiceTests
{
    private const string TestPasswordHash = "$2a$11$TestHashForUnitTests123456789";
    
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMedicationRepository> _medicationRepositoryMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<ILogger<MedicationService>> _loggerMock;
    private readonly MedicationService _medicationService;

    public MedicationServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _medicationRepositoryMock = new Mock<IMedicationRepository>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _loggerMock = new Mock<ILogger<MedicationService>>();

        _unitOfWorkMock.Setup(u => u.Medications).Returns(_medicationRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Users).Returns(_userRepositoryMock.Object);

        _medicationService = new MedicationService(_unitOfWorkMock.Object, _loggerMock.Object);
    }
    
    private static User CreateTestUser(string name = "Тест", string email = "test@example.com", UserRole role = UserRole.User)
    {
        return new User(name, email, TestPasswordHash, role);
    }

    [Fact]
    public async Task GetByIdAsync_Should_Return_Medication_When_Exists()
    {
        // Arrange
        var medicationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var medication = new Medication(userId, "Аспирин", "Обезболивающее", "500mg");
        _medicationRepositoryMock.Setup(r => r.GetByIdAsync(medicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(medication);

        // Act
        var result = await _medicationService.GetByIdAsync(medicationId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Name.Should().Be("Аспирин");
    }

    [Fact]
    public async Task GetByUserIdAsync_Should_Return_Medications_List()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var medications = new List<Medication>
        {
            new Medication(userId, "Аспирин"),
            new Medication(userId, "Парацетамол")
        };

        _userRepositoryMock.Setup(r => r.ExistsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _medicationRepositoryMock.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(medications);

        // Act
        var result = await _medicationService.GetByUserIdAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateAsync_Should_Create_Medication_Successfully()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = CreateTestUser();
        var dto = new CreateMedicationDto("Аспирин", "Обезболивающее", "500mg");

        _userRepositoryMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _medicationRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Medication>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Medication m, CancellationToken ct) => m);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _medicationService.CreateAsync(userId, dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Name.Should().Be(dto.Name);
        result.Data.UserId.Should().Be(userId);
        _medicationRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Medication>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_Should_Return_Failure_When_User_Not_Found()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var dto = new CreateMedicationDto("Аспирин", "Обезболивающее", "500mg");

        _userRepositoryMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _medicationService.CreateAsync(userId, dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("не найден");
        _medicationRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Medication>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_Should_Update_Medication_Successfully()
    {
        // Arrange
        var medicationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var medication = new Medication(userId, "Старое название");
        var dto = new UpdateMedicationDto("Новое название", "Новое описание", "1000mg");

        _medicationRepositoryMock.Setup(r => r.GetByIdAsync(medicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(medication);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _medicationService.UpdateAsync(medicationId, dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Name.Should().Be(dto.Name);
        _medicationRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Medication>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_Should_Delete_Medication_Successfully()
    {
        // Arrange
        var medicationId = Guid.NewGuid();
        _medicationRepositoryMock.Setup(r => r.ExistsAsync(medicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _medicationService.DeleteAsync(medicationId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _medicationRepositoryMock.Verify(r => r.DeleteAsync(medicationId, It.IsAny<CancellationToken>()), Times.Once);
    }
}

