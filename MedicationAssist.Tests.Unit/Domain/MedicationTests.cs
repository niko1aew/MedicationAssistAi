using FluentAssertions;
using MedicationAssist.Domain.Common;
using MedicationAssist.Domain.Entities;

namespace MedicationAssist.Tests.Unit.Domain;

public class MedicationTests
{
    [Fact]
    public void Medication_Constructor_Should_Create_Valid_Medication()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var name = "Аспирин";
        var description = "Обезболивающее";
        var dosage = "500mg";

        // Act
        var medication = new Medication(userId, name, description, dosage);

        // Assert
        medication.Should().NotBeNull();
        medication.Id.Should().NotBeEmpty();
        medication.UserId.Should().Be(userId);
        medication.Name.Should().Be(name);
        medication.Description.Should().Be(description);
        medication.Dosage.Should().Be(dosage);
        medication.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Medication_Constructor_Should_Throw_When_Name_Invalid(string invalidName)
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        Action act = () => new Medication(userId, invalidName);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*Название лекарства не может быть пустым*");
    }

    [Fact]
    public void SetName_Should_Update_Name()
    {
        // Arrange
        var medication = new Medication(Guid.NewGuid(), "Старое название");
        var newName = "Новое название";

        // Act
        medication.SetName(newName);

        // Assert
        medication.Name.Should().Be(newName);
        medication.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void SetDescription_Should_Update_Description()
    {
        // Arrange
        var medication = new Medication(Guid.NewGuid(), "Аспирин");
        var newDescription = "Новое описание";

        // Act
        medication.SetDescription(newDescription);

        // Assert
        medication.Description.Should().Be(newDescription);
        medication.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void SetDosage_Should_Update_Dosage()
    {
        // Arrange
        var medication = new Medication(Guid.NewGuid(), "Аспирин");
        var newDosage = "1000mg";

        // Act
        medication.SetDosage(newDosage);

        // Assert
        medication.Dosage.Should().Be(newDosage);
        medication.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void SetDescription_Should_Throw_When_Too_Long()
    {
        // Arrange
        var medication = new Medication(Guid.NewGuid(), "Аспирин");
        var tooLongDescription = new string('a', 1001);

        // Act
        Action act = () => medication.SetDescription(tooLongDescription);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*не может превышать 1000 символов*");
    }

    [Fact]
    public void SetDosage_Should_Throw_When_Too_Long()
    {
        // Arrange
        var medication = new Medication(Guid.NewGuid(), "Аспирин");
        var tooLongDosage = new string('a', 101);

        // Act
        Action act = () => medication.SetDosage(tooLongDosage);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*не может превышать 100 символов*");
    }
}

