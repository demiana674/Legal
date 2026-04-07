using LegalMateAI.BLL.Services.IService;
using LegalMateAI.BLL.Services.Service;
using LegalMateAI.DAL.DBContext;
using LegalMateAI.Domain.Entities;
using LegalMateAI.Domain.Enums;
using LegalMateAI.DTOs.CreateDTO;
using LegalMateAI.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using Xunit;

namespace LegalMateAI.Tests.Services;

public class AppointmentServiceTests : IDisposable
{
    private readonly LegalMateDbContext _context;
    private readonly Mock<INotificationService> _mockNotificationService;
    private readonly Mock<ILogger<AppointmentService>> _mockLogger;
    private readonly AppointmentService _service;
    private readonly User _testUser;
    private readonly LawyerProfile _testLawyer;

    public AppointmentServiceTests()
    {
        var options = new DbContextOptionsBuilder<LegalMateDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new LegalMateDbContext(options);
        _mockNotificationService = new Mock<INotificationService>();
        _mockLogger = new Mock<ILogger<AppointmentService>>();
        _service = new AppointmentService(_context, _mockNotificationService.Object, _mockLogger.Object);

        // Seed test data
        _testUser = TestDataFactory.CreateTestUser();  // User عادي
        _context.Users.Add(_testUser);
        
        _testLawyer = TestDataFactory.CreateTestLawyerProfile(_testUser.UserID);
        _context.LawyerProfiles.Add(_testLawyer);
        
        _context.SaveChanges();
    }

    [Fact]
    public async Task CreateAppointmentAsync_ValidRequest_ReturnsAppointment()
    {
        // Arrange
        // ✅ اخلق مستخدم محامي جديد (روله Lawyer)
        var lawyerUser = TestDataFactory.CreateTestUser(role: UserRole.Lawyer);
        _context.Users.Add(lawyerUser);
        
        var lawyerProfile = TestDataFactory.CreateTestLawyerProfile(lawyerUser.UserID);
        _context.LawyerProfiles.Add(lawyerProfile);
        await _context.SaveChangesAsync();

        var request = new CreateAppointmentDto
        {
            LawyerId = lawyerUser.UserID,
            AppointmentType = "استشارة قانونية",
            Date = DateTime.UtcNow.AddDays(3),
            Time = "10:00 AM",
            DurationMinutes = 60,
            Location = "المكتب الرئيسي",
            Notes = "مراجعة عقد",
            IsUrgent = false
        };

        // Act
        var result = await _service.CreateAppointmentAsync(_testUser.UserID, request);

        // Assert
        result.Should().NotBeNull();
        result!.AppointmentType.Should().Be("استشارة قانونية");
        result.Status.Should().Be(AppointmentStatus.Pending);
        result.AppointmentNumber.Should().StartWith("APT-");
    }

    [Fact]
    public async Task GetUserAppointmentsAsync_ReturnsUserAppointments()
    {
        // Arrange
        var appointment = TestDataFactory.CreateTestAppointment(_testUser.UserID, _testLawyer.Id);
        _context.Appointments.Add(appointment);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetUserAppointmentsAsync(_testUser.UserID);

        // Assert
        result.Should().NotBeNull();
        result.Count.Should().Be(1);
        result.First().AppointmentType.Should().Be("استشارة قانونية");
    }

    [Fact]
    public async Task CancelAppointmentAsync_ValidAppointment_ReturnsTrue()
    {
        // Arrange
        var appointment = TestDataFactory.CreateTestAppointment(_testUser.UserID, _testLawyer.Id);
        _context.Appointments.Add(appointment);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.CancelAppointmentAsync(appointment.Id, "ظروف طارئة");

        // Assert
        result.Should().BeTrue();
        
        var cancelledAppointment = await _context.Appointments.FindAsync(appointment.Id);
        cancelledAppointment!.Status.Should().Be(AppointmentStatus.Cancelled);
        cancelledAppointment.CancellationReason.Should().Be("ظروف طارئة");
    }

    [Fact]
    public async Task CancelAppointmentAsync_CompletedAppointment_ReturnsFalse()
    {
        // Arrange
        var appointment = TestDataFactory.CreateTestAppointment(_testUser.UserID, _testLawyer.Id, AppointmentStatus.Completed);
        _context.Appointments.Add(appointment);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.CancelAppointmentAsync(appointment.Id);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task RequestRescheduleAsync_ValidRequest_ReturnsRescheduleResponse()
    {
        // Arrange
        var appointment = TestDataFactory.CreateTestAppointment(_testUser.UserID, _testLawyer.Id, AppointmentStatus.Confirmed);
        _context.Appointments.Add(appointment);
        await _context.SaveChangesAsync();

        var request = new CreateRescheduleRequestDto
        {
            AppointmentId = appointment.Id,
            NewDate = DateTime.UtcNow.AddDays(5),
            NewTime = "02:00 PM",
            Reason = "تعارض في الموعد"
        };

        // Act
        var result = await _service.RequestRescheduleAsync(_testUser.UserID, request, isLawyer: false);

        // Assert
        result.Should().NotBeNull();
        result!.NewDate.Should().Be(request.NewDate);
        result.NewTime.Should().Be(request.NewTime);
        result.Status.Should().Be(RescheduleStatus.Pending);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}