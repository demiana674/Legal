using Microsoft.EntityFrameworkCore;
using LegalMateAI.DAL.DBContext;
using LegalMateAI.Domain.Entities;
using LegalMateAI.Domain.Enums;
using LegalMateAI.DTOs.CreateDTO;
using LegalMateAI.DTOs.ReadDTO;
using LegalMateAI.DTOs.UpdateDTO;
using LegalMateAI.BLL.Services.IService;
using Microsoft.Extensions.Logging;

namespace LegalMateAI.BLL.Services.Service
{
    public class AppointmentService : IAppointmentService
    {
        private readonly LegalMateDbContext _context;
        private readonly ILogger<AppointmentService> _logger;

        public AppointmentService(
            LegalMateDbContext context,
            ILogger<AppointmentService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<AppointmentResponseDto?> CreateAppointmentAsync(Guid userId, CreateAppointmentDto request)
        {
            _logger.LogInformation($"CreateAppointmentAsync START - UserId: {userId}, LawyerId: {request.LawyerId}");

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return null;

            var lawyerUser = await _context.Users
                .Include(u => u.LawyerProfile)
                .FirstOrDefaultAsync(u => u.UserID == request.LawyerId && u.Role == UserRole.Lawyer);

            if (lawyerUser?.LawyerProfile == null) return null;
            var lawyerProfile = lawyerUser.LawyerProfile;

            var isAvailable = await IsLawyerAvailableAsync(lawyerProfile.Id, request.Date, request.Time, request.DurationMinutes);
            if (!isAvailable) return null;

            var appointment = new Appointment
            {
                Id = Guid.NewGuid(),
                AppointmentNumber = GenerateAppointmentNumber(),
                UserID = userId,
                LawyerId = lawyerProfile.Id,
                AppointmentType = request.AppointmentType,
                Date = request.Date,
                Time = request.Time,
                DurationMinutes = request.DurationMinutes,
                Location = request.Location,
                Notes = request.Notes,
                Status = AppointmentStatus.Pending,
                IsUrgent = request.IsUrgent,
                RequestedAt = DateTime.UtcNow
            };

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();

            return await GetAppointmentByIdAsync(appointment.Id);
        }

        public async Task<List<AppointmentResponseDto>> GetUserAppointmentsAsync(Guid userId, string? status = null)
        {
            var query = _context.Appointments
                .Include(a => a.Lawyer)
                    .ThenInclude(l => l!.Specialties)
                        .ThenInclude(s => s.Specialty)
                .Include(a => a.Lawyer)
                    .ThenInclude(l => l!.User)
                .Include(a => a.Lawyer)
                    .ThenInclude(l => l!.Reviews)
                .Where(a => a.UserID == userId)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<AppointmentStatus>(status, true, out var statusEnum))
                query = query.Where(a => a.Status == statusEnum);

            var appointments = await query
                .OrderByDescending(a => a.Date)
                .ThenBy(a => a.Time)
                .ToListAsync();

            var result = new List<AppointmentResponseDto>();
            foreach (var appointment in appointments)
            {
                var user = await _context.Users.FindAsync(appointment.UserID);
                result.Add(MapToDto(appointment, user));
            }

            return result;
        }

        public async Task<List<AppointmentResponseDto>> GetLawyerAppointmentsAsync(Guid lawyerId, string? status = null)
        {
            var query = _context.Appointments
                .Include(a => a.Lawyer)
                    .ThenInclude(l => l!.Specialties)
                        .ThenInclude(s => s.Specialty)
                .Include(a => a.Lawyer)
                    .ThenInclude(l => l!.User)
                .Include(a => a.Lawyer)
                    .ThenInclude(l => l!.Reviews)
                .Where(a => a.LawyerId == lawyerId)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<AppointmentStatus>(status, true, out var statusEnum))
                query = query.Where(a => a.Status == statusEnum);

            var appointments = await query
                .OrderByDescending(a => a.Date)
                .ThenBy(a => a.Time)
                .ToListAsync();

            var result = new List<AppointmentResponseDto>();
            foreach (var appointment in appointments)
            {
                var user = await _context.Users.FindAsync(appointment.UserID);
                result.Add(MapToDto(appointment, user));
            }

            return result;
        }

        public async Task<AppointmentResponseDto?> GetAppointmentByIdAsync(Guid appointmentId)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Lawyer)
                    .ThenInclude(l => l!.Specialties)
                        .ThenInclude(s => s.Specialty)
                .Include(a => a.Lawyer)
                    .ThenInclude(l => l!.User)
                .Include(a => a.Lawyer)
                    .ThenInclude(l => l!.Reviews)
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (appointment == null) return null;

            var user = await _context.Users.FindAsync(appointment.UserID);
            return MapToDto(appointment, user);
        }

        public async Task<bool> CancelAppointmentAsync(Guid appointmentId, string? reason = null)
        {
            var appointment = await _context.Appointments.FirstOrDefaultAsync(a => a.Id == appointmentId);
            if (appointment == null || appointment.Status == AppointmentStatus.Completed) return false;

            appointment.Status = AppointmentStatus.Cancelled;
            appointment.CancelledAt = DateTime.UtcNow;
            appointment.CancellationReason = reason;

            await _context.SaveChangesAsync();
            _logger.LogInformation("Appointment cancelled: {AppointmentId}", appointmentId);
            return true;
        }

        public async Task<RescheduleResponseDto?> RequestRescheduleAsync(Guid userId, CreateRescheduleRequestDto request, bool isLawyer = false)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Reschedules)
                .FirstOrDefaultAsync(a => a.Id == request.AppointmentId);
                
            if (appointment == null) return null;
            
            if (!isLawyer && appointment.UserID != userId) return null;
            if (isLawyer && appointment.LawyerId != userId) return null;
            
            if (appointment.Status is AppointmentStatus.Completed or AppointmentStatus.Cancelled) return null;

            var isAvailable = await IsLawyerAvailableAsync(appointment.LawyerId, request.NewDate, request.NewTime, appointment.DurationMinutes, appointment.Id);
            if (!isAvailable) return null;

            var reschedule = new AppointmentReschedule
            {
                Id = Guid.NewGuid(),
                AppointmentId = request.AppointmentId,
                OldDate = appointment.Date,
                OldTime = appointment.Time,
                NewDate = request.NewDate,
                NewTime = request.NewTime,
                InitiatedBy = isLawyer ? RescheduleInitiator.Lawyer : RescheduleInitiator.User,
                Status = RescheduleStatus.Pending,
                Reason = request.Reason,
                RequestedAt = DateTime.UtcNow
            };

            _context.AppointmentReschedules.Add(reschedule);
            appointment.Status = AppointmentStatus.Rescheduled;
            await _context.SaveChangesAsync();

            return new RescheduleResponseDto
            {
                Id = reschedule.Id,
                OldDate = reschedule.OldDate,
                OldTime = reschedule.OldTime,
                NewDate = reschedule.NewDate,
                NewTime = reschedule.NewTime,
                InitiatedBy = reschedule.InitiatedBy,
                Status = reschedule.Status,
                Reason = reschedule.Reason,
                RequestedAt = reschedule.RequestedAt
            };
        }

        public async Task<bool> RespondToRescheduleAsync(Guid userId, Guid rescheduleId, UpdateRescheduleRequestDto request, bool isLawyer = false)
        {
            var reschedule = await _context.AppointmentReschedules
                .Include(r => r.Appointment)
                .FirstOrDefaultAsync(r => r.Id == rescheduleId);
                
            if (reschedule == null) return false;

            var appointment = reschedule.Appointment;
            if (!isLawyer && appointment.LawyerId != userId) return false;
            if (isLawyer && appointment.UserID != userId) return false;

            reschedule.Status = request.Status;
            reschedule.RespondedAt = DateTime.UtcNow;

            if (request.Status == RescheduleStatus.Approved)
            {
                appointment.Date = reschedule.NewDate;
                appointment.Time = reschedule.NewTime;
                appointment.Status = AppointmentStatus.Confirmed;
            }
            else if (request.Status == RescheduleStatus.Rejected)
            {
                appointment.Status = AppointmentStatus.Confirmed;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<RescheduleResponseDto>> GetRescheduleRequestsForLawyerAsync(Guid lawyerId)
        {
            var reschedules = await _context.AppointmentReschedules
                .Include(r => r.Appointment)
                .Where(r => r.Appointment.LawyerId == lawyerId && r.Status == RescheduleStatus.Pending)
                .OrderBy(r => r.RequestedAt)
                .ToListAsync();

            return reschedules.Select(r => new RescheduleResponseDto
            {
                Id = r.Id,
                OldDate = r.OldDate,
                OldTime = r.OldTime,
                NewDate = r.NewDate,
                NewTime = r.NewTime,
                InitiatedBy = r.InitiatedBy,
                Status = r.Status,
                Reason = r.Reason,
                RequestedAt = r.RequestedAt
            }).ToList();
        }

        public async Task<List<RescheduleResponseDto>> GetRescheduleRequestsForUserAsync(Guid userId)
        {
            var reschedules = await _context.AppointmentReschedules
                .Include(r => r.Appointment)
                .Where(r => r.Appointment.UserID == userId && r.Status == RescheduleStatus.Pending)
                .OrderBy(r => r.RequestedAt)
                .ToListAsync();

            return reschedules.Select(r => new RescheduleResponseDto
            {
                Id = r.Id,
                OldDate = r.OldDate,
                OldTime = r.OldTime,
                NewDate = r.NewDate,
                NewTime = r.NewTime,
                InitiatedBy = r.InitiatedBy,
                Status = r.Status,
                Reason = r.Reason,
                RequestedAt = r.RequestedAt
            }).ToList();
        }

        // ===== Private Helpers =====

        private async Task<bool> IsLawyerAvailableAsync(Guid lawyerProfileId, DateTime date, string time, int duration, Guid? excludeAppointmentId = null)
        {
            var startTime = ParseTime(time);
            var endTime = startTime.Add(TimeSpan.FromMinutes(duration));

            var appointments = await _context.Appointments
                .Where(a => a.LawyerId == lawyerProfileId && 
                           a.Date == date &&
                           a.Status != AppointmentStatus.Cancelled && 
                           a.Status != AppointmentStatus.Completed &&
                           (!excludeAppointmentId.HasValue || a.Id != excludeAppointmentId.Value))
                .ToListAsync();

            return !appointments.Any(a =>
            {
                var aStart = ParseTime(a.Time);
                var aEnd = aStart.Add(TimeSpan.FromMinutes(a.DurationMinutes));
                return startTime < aEnd && endTime > aStart;
            });
        }

        private string GenerateAppointmentNumber()
        {
            var year = DateTime.UtcNow.Year;
            var month = DateTime.UtcNow.Month;
            var count = _context.Appointments.Count() + 1;
            return $"APT-{year}{month:D2}-{count:D6}";
        }

        private TimeSpan ParseTime(string time)
        {
            time = time.Trim();
            if (time.Contains("AM") || time.Contains("PM"))
            {
                var parts = time.Split(' ');
                var timePart = parts[0].Split(':');
                var hour = int.Parse(timePart[0]);
                var minute = int.Parse(timePart[1]);
                if (parts[1] == "PM" && hour != 12) hour += 12;
                else if (parts[1] == "AM" && hour == 12) hour = 0;
                return new TimeSpan(hour, minute, 0);
            }
            var parts24 = time.Split(':');
            return new TimeSpan(int.Parse(parts24[0]), int.Parse(parts24[1]), 0);
        }

        private AppointmentResponseDto MapToDto(Appointment appointment, User? user)
        {
            var averageRating = appointment.Lawyer?.Reviews?.Any() == true 
                ? appointment.Lawyer.Reviews.Average(r => r.Rating) 
                : 0;

            return new AppointmentResponseDto
            {
                Id = appointment.Id,
                AppointmentNumber = appointment.AppointmentNumber,
                AppointmentType = appointment.AppointmentType,
                Date = appointment.Date,
                DateFormatted = appointment.Date.ToString("dd MMM yyyy"),
                Time = appointment.Time,
                DurationMinutes = appointment.DurationMinutes,
                Location = appointment.Location,
                Notes = appointment.Notes,
                Status = appointment.Status,
                RequestedAt = appointment.RequestedAt,
                ConfirmedAt = appointment.ConfirmedAt,
                IsUrgent = appointment.IsUrgent,
                UserId = appointment.UserID,
                LawyerId = appointment.LawyerId,
                User = new UserBriefDto
                {
                    Id = appointment.UserID,
                    FullName = user?.FullName ?? "غير معروف",
                    Email = user?.Email ?? "",
                    PhoneNumber = user?.Phone ?? ""
                },
                Lawyer = new LawyerBriefDto
                {
                    Id = appointment.Lawyer?.UserId ?? Guid.Empty,
                    FullName = appointment.Lawyer?.User?.FullName ?? "",
                    Specialization = appointment.Lawyer?.Specialties?.FirstOrDefault()?.Specialty?.NameAr ?? "",
                    Rating = averageRating
                }
            };
        }
    }
}