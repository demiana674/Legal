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

        public AppointmentService(LegalMateDbContext context, ILogger<AppointmentService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ========== الحجز ==========

        public async Task<AppointmentResponseDto?> CreateAppointmentAsync(Guid userId, CreateAppointmentDto request)
        {
            var lawyerProfile = await _context.LawyerProfiles
                .Include(l => l.User)
                .FirstOrDefaultAsync(l => l.UserId == request.LawyerId && l.VerificationStatus == LawyerVerificationStatus.Active);

            if (lawyerProfile == null) return null;

            var isAvailable = await IsTimeSlotAvailableAsync(lawyerProfile.Id, request.Date, request.Time, request.DurationMinutes);
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
                RequestedAt = DateTime.UtcNow
            };

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();

            return await GetAppointmentByIdAsync(appointment.Id);
        }

        // ========== الموافقة والرفض (المحامي) ==========

        public async Task<bool> ApproveAppointmentAsync(Guid lawyerId, Guid appointmentId)
        {
            var lawyerProfile = await _context.LawyerProfiles.FirstOrDefaultAsync(l => l.UserId == lawyerId);
            if (lawyerProfile == null) return false;

            var appointment = await _context.Appointments.FirstOrDefaultAsync(a => a.Id == appointmentId && a.LawyerId == lawyerProfile.Id);
            if (appointment == null || appointment.Status != AppointmentStatus.Pending) return false;

            appointment.Status = AppointmentStatus.Confirmed;
            appointment.ConfirmedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RejectAppointmentAsync(Guid lawyerId, Guid appointmentId, string? reason)
        {
            var lawyerProfile = await _context.LawyerProfiles.FirstOrDefaultAsync(l => l.UserId == lawyerId);
            if (lawyerProfile == null) return false;

            var appointment = await _context.Appointments.FirstOrDefaultAsync(a => a.Id == appointmentId && a.LawyerId == lawyerProfile.Id);
            if (appointment == null || appointment.Status != AppointmentStatus.Pending) return false;

            appointment.Status = AppointmentStatus.Cancelled;
            appointment.CancelledAt = DateTime.UtcNow;
            appointment.CancellationReason = reason ?? "تم الرفض من قبل المحامي";
            await _context.SaveChangesAsync();
            return true;
        }

        // ========== عرض المواعيد ==========

        public async Task<List<AppointmentResponseDto>> GetUserAppointmentsAsync(Guid userId, string? status = null)
        {
            var query = _context.Appointments
                .Include(a => a.Lawyer)
                .ThenInclude(l => l!.User)
                .Where(a => a.UserID == userId);

            if (string.IsNullOrEmpty(status))
            {
                query = query.Where(a => a.Status != AppointmentStatus.Pending);
            }
            else if (Enum.TryParse<AppointmentStatus>(status, true, out var s))
            {
                query = query.Where(a => a.Status == s);
            }

            return (await query.OrderByDescending(a => a.Date).ToListAsync())
                .Select(a => MapToDto(a, null))
                .ToList();
        }

        public async Task<List<AppointmentResponseDto>> GetLawyerAppointmentsAsync(Guid lawyerId, string? status = null)
        {
            var lawyerProfile = await _context.LawyerProfiles.FirstOrDefaultAsync(l => l.UserId == lawyerId);
            if (lawyerProfile == null) return new List<AppointmentResponseDto>();

            var query = _context.Appointments
                .Include(a => a.Lawyer)
                .ThenInclude(l => l!.User)
                .Where(a => a.LawyerId == lawyerProfile.Id);

            if (string.IsNullOrEmpty(status))
            {
                query = query.Where(a => a.Status != AppointmentStatus.Pending);
            }
            else if (Enum.TryParse<AppointmentStatus>(status, true, out var s))
            {
                query = query.Where(a => a.Status == s);
            }

            return (await query.OrderByDescending(a => a.Date).ToListAsync())
                .Select(a => MapToDto(a, null))
                .ToList();
        }

        public async Task<AppointmentResponseDto?> GetAppointmentByIdAsync(Guid appointmentId)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Lawyer)
                .ThenInclude(l => l!.User)
                .FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (appointment == null) return null;

            var user = await _context.Users.FindAsync(appointment.UserID);
            return MapToDto(appointment, user);
        }

        // ========== المواعيد المعلقة فقط ==========

        public async Task<List<AppointmentResponseDto>> GetPendingAppointmentsForLawyerAsync(Guid lawyerId)
        {
            var lawyerProfile = await _context.LawyerProfiles.FirstOrDefaultAsync(l => l.UserId == lawyerId);
            if (lawyerProfile == null) return new List<AppointmentResponseDto>();

            var appointments = await _context.Appointments
                .Include(a => a.Lawyer)
                .ThenInclude(l => l!.User)
                .Where(a => a.LawyerId == lawyerProfile.Id && a.Status == AppointmentStatus.Pending)
                .OrderByDescending(a => a.RequestedAt)
                .ToListAsync();

            return appointments.Select(a => MapToDto(a, null)).ToList();
        }

        public async Task<List<AppointmentResponseDto>> GetPendingAppointmentsForUserAsync(Guid userId)
        {
            var appointments = await _context.Appointments
                .Include(a => a.Lawyer)
                .ThenInclude(l => l!.User)
                .Where(a => a.UserID == userId && a.Status == AppointmentStatus.Pending)
                .OrderByDescending(a => a.RequestedAt)
                .ToListAsync();

            return appointments.Select(a => MapToDto(a, null)).ToList();
        }

        // ========== إلغاء (يتطلب موافقة الطرفين) ==========

        public async Task<bool> CancelAppointmentAsync(Guid appointmentId, Guid userId, string? reason = null)
        {
            var appointment = await _context.Appointments
                .Include(a => a.CancelRequests)
                .FirstOrDefaultAsync(a => a.Id == appointmentId);
                
            if (appointment == null) return false;
            
            if (appointment.Status == AppointmentStatus.Completed || 
                appointment.Status == AppointmentStatus.Cancelled ||
                appointment.Status == AppointmentStatus.Pending)
                return false;

            var lawyerProfile = await _context.LawyerProfiles.FirstOrDefaultAsync(l => l.UserId == userId);
            
            bool isUser = appointment.UserID == userId;
            bool isLawyer = lawyerProfile != null && appointment.LawyerId == lawyerProfile.Id;
            
            if (!isUser && !isLawyer)
                return false;

            var existingRequest = await _context.AppointmentCancelRequests
                .FirstOrDefaultAsync(c => c.AppointmentId == appointmentId && c.Status == CancelRequestStatus.Pending);
            
            if (existingRequest != null)
                return false;

            var cancelRequest = new AppointmentCancelRequest
            {
                Id = Guid.NewGuid(),
                AppointmentId = appointmentId,
                RequestedBy = isLawyer ? CancelInitiator.Lawyer : CancelInitiator.User,
                Reason = reason ?? "لم يتم تقديم سبب",
                Status = CancelRequestStatus.Pending,
                RequestedAt = DateTime.UtcNow
            };

            _context.AppointmentCancelRequests.Add(cancelRequest);
            appointment.Status = AppointmentStatus.CancellationRequested;
            
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RespondToCancelRequestAsync(Guid userId, Guid cancelRequestId, bool approve, string? responseReason = null)
        {
            var cancelRequest = await _context.AppointmentCancelRequests
                .Include(c => c.Appointment)
                .ThenInclude(a => a!.Lawyer)
                .ThenInclude(l => l!.User)
                .FirstOrDefaultAsync(c => c.Id == cancelRequestId);

            if (cancelRequest == null) return false;
            if (cancelRequest.Status != CancelRequestStatus.Pending) return false;

            var appointment = cancelRequest.Appointment;
            var lawyerProfile = await _context.LawyerProfiles.FirstOrDefaultAsync(l => l.UserId == userId);

            bool isUser = appointment.UserID == userId;
            bool isLawyer = lawyerProfile != null && appointment.LawyerId == lawyerProfile.Id;

            if (!isUser && !isLawyer) return false;
            
            // لا يمكن للشخص الذي طلب الإلغاء أن يرد على طلبه
            if ((cancelRequest.RequestedBy == CancelInitiator.Lawyer && isLawyer) ||
                (cancelRequest.RequestedBy == CancelInitiator.User && isUser))
                return false;

            cancelRequest.Status = approve ? CancelRequestStatus.Approved : CancelRequestStatus.Rejected;
            cancelRequest.RespondedAt = DateTime.UtcNow;
            cancelRequest.ResponseReason = responseReason;

            if (approve)
            {
                appointment.Status = AppointmentStatus.Cancelled;
                appointment.CancelledAt = DateTime.UtcNow;
                appointment.CancellationReason = cancelRequest.Reason;
            }
            else
            {
                appointment.Status = AppointmentStatus.Confirmed;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<CancelRequestResponseDto>> GetPendingCancelRequestsForUserAsync(Guid userId)
        {
            var cancelRequests = await _context.AppointmentCancelRequests
                .Include(c => c.Appointment)
                .ThenInclude(a => a!.Lawyer)
                .ThenInclude(l => l!.User)
                .Where(c => c.Appointment.UserID == userId && 
                           c.Status == CancelRequestStatus.Pending &&
                           c.RequestedBy != CancelInitiator.User)
                .OrderByDescending(c => c.RequestedAt)
                .ToListAsync();

            return cancelRequests.Select(MapCancelRequestToDto).ToList();
        }

        public async Task<List<CancelRequestResponseDto>> GetPendingCancelRequestsForLawyerAsync(Guid lawyerId)
        {
            var lawyerProfile = await _context.LawyerProfiles.FirstOrDefaultAsync(l => l.UserId == lawyerId);
            if (lawyerProfile == null) return new List<CancelRequestResponseDto>();

            var cancelRequests = await _context.AppointmentCancelRequests
                .Include(c => c.Appointment)
                .ThenInclude(a => a!.Lawyer)
                .ThenInclude(l => l!.User)
                .Where(c => c.Appointment.LawyerId == lawyerProfile.Id && 
                           c.Status == CancelRequestStatus.Pending &&
                           c.RequestedBy != CancelInitiator.Lawyer)
                .OrderByDescending(c => c.RequestedAt)
                .ToListAsync();

            return cancelRequests.Select(MapCancelRequestToDto).ToList();
        }

        // ========== إعادة الجدولة ==========

        public async Task<RescheduleResponseDto?> RequestRescheduleAsync(Guid userId, CreateRescheduleRequestDto request, bool isLawyer = false)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Reschedules)
                .FirstOrDefaultAsync(a => a.Id == request.AppointmentId);

            if (appointment == null || appointment.Status == AppointmentStatus.Completed || appointment.Status == AppointmentStatus.Cancelled)
                return null;

            var lawyerProfile = await _context.LawyerProfiles.FirstOrDefaultAsync(l => l.UserId == userId);
            
            if (!isLawyer && appointment.UserID != userId) return null;
            if (isLawyer && (lawyerProfile == null || appointment.LawyerId != lawyerProfile.Id)) return null;

            var isAvailable = await IsTimeSlotAvailableAsync(appointment.LawyerId, request.NewDate, request.NewTime, appointment.DurationMinutes, appointment.Id);
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

            return MapRescheduleToDto(reschedule);
        }

        public async Task<bool> RespondToRescheduleAsync(Guid userId, Guid rescheduleId, UpdateRescheduleRequestDto request, bool isLawyer = false)
        {
            var reschedule = await _context.AppointmentReschedules
                .Include(r => r.Appointment)
                .FirstOrDefaultAsync(r => r.Id == rescheduleId);

            if (reschedule == null) return false;

            var appointment = reschedule.Appointment;
            var lawyerProfile = await _context.LawyerProfiles.FirstOrDefaultAsync(l => l.UserId == userId);

            if (!isLawyer && appointment.UserID != userId) return false;
            if (isLawyer && (lawyerProfile == null || appointment.LawyerId != lawyerProfile.Id)) return false;

            // لا يمكن للشخص الذي طلب إعادة الجدولة أن يرد على طلبه
            if ((reschedule.InitiatedBy == RescheduleInitiator.Lawyer && isLawyer) ||
                (reschedule.InitiatedBy == RescheduleInitiator.User && !isLawyer))
                return false;

            reschedule.Status = request.Status;
            reschedule.RespondedAt = DateTime.UtcNow;

            if (request.Status == RescheduleStatus.Approved)
            {
                appointment.Date = reschedule.NewDate;
                appointment.Time = reschedule.NewTime;
                appointment.Status = AppointmentStatus.Confirmed;
            }
            else
            {
                appointment.Status = AppointmentStatus.Confirmed;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<RescheduleResponseDto>> GetRescheduleRequestsForLawyerAsync(Guid lawyerId)
        {
            var lawyerProfile = await _context.LawyerProfiles.FirstOrDefaultAsync(l => l.UserId == lawyerId);
            if (lawyerProfile == null) return new List<RescheduleResponseDto>();

            var requests = await _context.AppointmentReschedules
                .Include(r => r.Appointment)
                .Where(r => r.Appointment.LawyerId == lawyerProfile.Id && 
                           r.Status == RescheduleStatus.Pending &&
                           r.InitiatedBy != RescheduleInitiator.Lawyer)
                .OrderBy(r => r.RequestedAt)
                .ToListAsync();

            return requests.Select(MapRescheduleToDto).ToList();
        }

        public async Task<List<RescheduleResponseDto>> GetRescheduleRequestsForUserAsync(Guid userId)
        {
            var requests = await _context.AppointmentReschedules
                .Include(r => r.Appointment)
                .Where(r => r.Appointment.UserID == userId && 
                           r.Status == RescheduleStatus.Pending &&
                           r.InitiatedBy != RescheduleInitiator.User)
                .OrderBy(r => r.RequestedAt)
                .ToListAsync();

            return requests.Select(MapRescheduleToDto).ToList();
        }

        // ========== Helpers ==========

        private async Task<bool> IsTimeSlotAvailableAsync(Guid lawyerProfileId, DateTime date, string time, int duration, Guid? excludeAppointmentId = null)
        {
            var startTime = ParseTime(time);
            var endTime = startTime.Add(TimeSpan.FromMinutes(duration));

            var appointments = await _context.Appointments
                .Where(a => a.LawyerId == lawyerProfileId && 
                           a.Date.Date == date.Date &&
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
            var count = _context.Appointments.Count() + 1;
            return $"APT-{DateTime.UtcNow.Year}{DateTime.UtcNow.Month:D2}-{count:D6}";
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

        private AppointmentResponseDto MapToDto(Appointment a, User? user) => new()
        {
            Id = a.Id,
            AppointmentNumber = a.AppointmentNumber,
            AppointmentType = a.AppointmentType,
            Date = a.Date,
            DateFormatted = a.Date.ToString("dd MMM yyyy"),
            Time = a.Time,
            DurationMinutes = a.DurationMinutes,
            Location = a.Location,
            Notes = a.Notes,
            Status = a.Status,
            RequestedAt = a.RequestedAt,
            ConfirmedAt = a.ConfirmedAt,
            UserId = a.UserID,
            // ✅ نرجع UserId بتاع المحامي مش LawyerProfile.Id
            LawyerId = a.Lawyer?.UserId ?? Guid.Empty,
            User = new UserBriefDto 
            { 
                Id = a.UserID, 
                FullName = user?.FullName ?? "غير معروف", 
                Email = user?.Email ?? "" 
            },
            Lawyer = new LawyerBriefDto 
            { 
                Id = a.Lawyer?.UserId ?? Guid.Empty, 
                FullName = a.Lawyer?.User?.FullName ?? "" 
            }
        };

        private static RescheduleResponseDto MapRescheduleToDto(AppointmentReschedule r) => new()
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
        };

        private static CancelRequestResponseDto MapCancelRequestToDto(AppointmentCancelRequest c) => new()
        {
            Id = c.Id,
            AppointmentId = c.AppointmentId,
            RequestedBy = c.RequestedBy,
            Reason = c.Reason,
            Status = c.Status,
            RequestedAt = c.RequestedAt,
            RespondedAt = c.RespondedAt,
            ResponseReason = c.ResponseReason,
            AppointmentDate = c.Appointment?.Date ?? default,
            AppointmentTime = c.Appointment?.Time ?? "",
            LawyerName = c.Appointment?.Lawyer?.User?.FullName ?? "",
            UserName = c.Appointment?.User?.FullName ?? ""
        };
    }
}