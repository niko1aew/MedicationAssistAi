using FluentAssertions;
using MedicationAssist.Domain.Common;
using MedicationAssist.Domain.Entities;

namespace MedicationAssist.Tests.Unit.Domain;

public class UserTests
{
    [Fact]
    public void User_Constructor_Should_Create_Valid_User()
    {
        // Arrange
        var name = "Иван Иванов";
        var email = "ivan@example.com";

        // Act
        var user = new User(name, email);

        // Assert
        user.Should().NotBeNull();
        user.Id.Should().NotBeEmpty();
        user.Name.Should().Be(name);
        user.Email.Should().Be(email);
        user.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void User_Constructor_Should_Throw_When_Name_Invalid(string invalidName)
    {
        // Arrange
        var email = "test@example.com";

        // Act
        Action act = () => new User(invalidName, email);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*Имя пользователя не может быть пустым*");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    [InlineData("invalidemail")]
    public void User_Constructor_Should_Throw_When_Email_Invalid(string invalidEmail)
    {
        // Arrange
        var name = "Test User";

        // Act
        Action act = () => new User(name, invalidEmail);

        // Assert
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void SetName_Should_Update_Name_And_UpdatedAt()
    {
        // Arrange
        var user = new User("Старое имя", "test@example.com");
        var newName = "Новое имя";
        var oldUpdatedAt = user.UpdatedAt;

        // Act
        user.SetName(newName);

        // Assert
        user.Name.Should().Be(newName);
        user.UpdatedAt.Should().NotBe(oldUpdatedAt);
        user.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void SetEmail_Should_Update_Email_And_UpdatedAt()
    {
        // Arrange
        var user = new User("Test", "old@example.com");
        var newEmail = "new@example.com";

        // Act
        user.SetEmail(newEmail);

        // Assert
        user.Email.Should().Be(newEmail);
        user.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void AddMedication_Should_Add_Medication_To_User()
    {
        // Arrange
        var user = new User("Test", "test@example.com");
        var medicationName = "Аспирин";

        // Act
        var medication = user.AddMedication(medicationName, "От головной боли", "500mg");

        // Assert
        medication.Should().NotBeNull();
        medication.Name.Should().Be(medicationName);
        medication.UserId.Should().Be(user.Id);
        user.Medications.Should().Contain(medication);
    }

    [Fact]
    public void AddMedication_Should_Throw_When_Duplicate_Name()
    {
        // Arrange
        var user = new User("Test", "test@example.com");
        var medicationName = "Аспирин";
        user.AddMedication(medicationName);

        // Act
        Action act = () => user.AddMedication(medicationName);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*уже существует*");
    }

    [Fact]
    public void RemoveMedication_Should_Remove_Medication()
    {
        // Arrange
        var user = new User("Test", "test@example.com");
        var medication = user.AddMedication("Аспирин");

        // Act
        user.RemoveMedication(medication.Id);

        // Assert
        user.Medications.Should().NotContain(medication);
    }

    [Fact]
    public void RemoveMedication_Should_Throw_When_Not_Found()
    {
        // Arrange
        var user = new User("Test", "test@example.com");
        var nonExistentId = Guid.NewGuid();

        // Act
        Action act = () => user.RemoveMedication(nonExistentId);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*не найдено*");
    }

    [Fact]
    public void RecordMedicationIntake_Should_Add_Intake_Record()
    {
        // Arrange
        var user = new User("Test", "test@example.com");
        var medication = user.AddMedication("Аспирин");
        var intakeTime = DateTime.UtcNow;

        // Act
        var intake = user.RecordMedicationIntake(medication.Id, intakeTime, "Принято после еды");

        // Assert
        intake.Should().NotBeNull();
        intake.UserId.Should().Be(user.Id);
        intake.MedicationId.Should().Be(medication.Id);
        intake.IntakeTime.Should().Be(intakeTime);
        user.MedicationIntakes.Should().Contain(intake);
    }

    [Fact]
    public void RecordMedicationIntake_Should_Use_Current_Time_When_Not_Specified()
    {
        // Arrange
        var user = new User("Test", "test@example.com");
        var medication = user.AddMedication("Аспирин");

        // Act
        var intake = user.RecordMedicationIntake(medication.Id);

        // Assert
        intake.IntakeTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void RecordMedicationIntake_Should_Throw_When_Medication_Not_Found()
    {
        // Arrange
        var user = new User("Test", "test@example.com");
        var nonExistentMedicationId = Guid.NewGuid();

        // Act
        Action act = () => user.RecordMedicationIntake(nonExistentMedicationId);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*не найдено*");
    }

    [Fact]
    public void RemoveMedicationIntake_Should_Remove_Intake_Record()
    {
        // Arrange
        var user = new User("Test", "test@example.com");
        var medication = user.AddMedication("Аспирин");
        var intake = user.RecordMedicationIntake(medication.Id);

        // Act
        user.RemoveMedicationIntake(intake.Id);

        // Assert
        user.MedicationIntakes.Should().NotContain(intake);
    }
}

