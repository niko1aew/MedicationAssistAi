using FluentAssertions;
using MedicationAssist.Domain.Common;
using MedicationAssist.Domain.Entities;

namespace MedicationAssist.Tests.Unit.Domain;

public class MedicationIntakeTests
{
    [Fact]
    public void MedicationIntake_Constructor_Should_Create_Valid_Intake()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var medicationId = Guid.NewGuid();
        var intakeTime = DateTime.UtcNow;
        var notes = "Принято после еды";

        // Act
        var intake = new MedicationIntake(userId, medicationId, intakeTime, notes);

        // Assert
        intake.Should().NotBeNull();
        intake.Id.Should().NotBeEmpty();
        intake.UserId.Should().Be(userId);
        intake.MedicationId.Should().Be(medicationId);
        intake.IntakeTime.Should().Be(intakeTime);
        intake.Notes.Should().Be(notes);
        intake.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void SetIntakeTime_Should_Update_IntakeTime()
    {
        // Arrange
        var intake = new MedicationIntake(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);
        var newIntakeTime = DateTime.UtcNow.AddHours(-1);

        // Act
        intake.SetIntakeTime(newIntakeTime);

        // Assert
        intake.IntakeTime.Should().Be(newIntakeTime);
        intake.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void SetIntakeTime_Should_Throw_When_Too_Far_In_Future()
    {
        // Arrange
        var intake = new MedicationIntake(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);
        var farFutureTime = DateTime.UtcNow.AddDays(2);

        // Act
        Action act = () => intake.SetIntakeTime(farFutureTime);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*Время приема не может быть более чем через день*");
    }

    [Fact]
    public void SetNotes_Should_Update_Notes()
    {
        // Arrange
        var intake = new MedicationIntake(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);
        var newNotes = "Новое примечание";

        // Act
        intake.SetNotes(newNotes);

        // Assert
        intake.Notes.Should().Be(newNotes);
        intake.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void SetNotes_Should_Throw_When_Too_Long()
    {
        // Arrange
        var intake = new MedicationIntake(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);
        var tooLongNotes = new string('a', 501);

        // Act
        Action act = () => intake.SetNotes(tooLongNotes);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*не могут превышать 500 символов*");
    }

    [Fact]
    public void SetNotes_Should_Accept_Null()
    {
        // Arrange
        var intake = new MedicationIntake(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, "Старое примечание");

        // Act
        intake.SetNotes(null);

        // Assert
        intake.Notes.Should().BeNull();
    }
}

