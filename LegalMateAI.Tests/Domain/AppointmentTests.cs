using LegalMateAI.Domain.Entities;
using LegalMateAI.Domain.Enums;
using LegalMateAI.Tests.Helpers;
using FluentAssertions;
using Xunit;

namespace LegalMateAI.Tests.Domain;

public class AppointmentTests
{
    [Fact]
    public void Appointment_Create_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var lawyerId = Guid.NewGuid();

        // Act
        var appointment = TestDataFactory.CreateTestAppointment(userId, lawyerId);

        // Assert
        appointment.Should().NotBeNull();
        appointment.Id.Should().NotBeEmpty();
        appointment.AppointmentNumber.Should().StartWith("APT-");
        appointment.UserID.Should().Be(userId);
        appointment.LawyerId.Should().Be(lawyerId);
        appointment.Status.Should().Be(AppointmentStatus.Pending);
        appointment.DurationMinutes.Should().Be(60);
    }

    [Fact]
    public void Appointment_Confirm_ShouldChangeStatusToConfirmed()
    {
        // Arrange
        var appointment = TestDataFactory.CreateTestAppointment(Guid.NewGuid(), Guid.NewGuid());
        appointment.Status = AppointmentStatus.Pending;

        // Act
        appointment.Status = AppointmentStatus.Confirmed;
        appointment.ConfirmedAt = DateTime.UtcNow;

        // Assert
        appointment.Status.Should().Be(AppointmentStatus.Confirmed);
        appointment.ConfirmedAt.Should().NotBeNull();
    }

    [Fact]
    public void Appointment_Cancel_ShouldChangeStatusToCancelled()
    {
        // Arrange
        var appointment = TestDataFactory.CreateTestAppointment(Guid.NewGuid(), Guid.NewGuid());
        appointment.Status = AppointmentStatus.Pending;

        // Act
        appointment.Status = AppointmentStatus.Cancelled;
        appointment.CancelledAt = DateTime.UtcNow;
        appointment.CancellationReason = "ظروف طارئة";

        // Assert
        appointment.Status.Should().Be(AppointmentStatus.Cancelled);
        appointment.CancelledAt.Should().NotBeNull();
        appointment.CancellationReason.Should().Be("ظروف طارئة");
    }
}