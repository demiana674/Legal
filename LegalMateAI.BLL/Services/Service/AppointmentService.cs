using Microsoft.EntityFrameworkCore;
using LegalMateAI.DAL.DBContext;
using LegalMateAI.Domain.Entities;
using LegalMateAI.Domain.Enums;
using LegalMateAI.DTOs.CreateDTO;
using LegalMateAI.DTOs.ReadDTO;
using LegalMateAI.DTOs.UpdateDTO;
using LegalMateAI.BLL.Services.IService;
using Microsoft.Extensions.Logging;
using System.Text;
using LegalMateAI.Infrastructure.Services.IService;

namespace LegalMateAI.BLL.Services.Service
{
    public class AppointmentService : IAppointmentService
    {
        private readonly LegalMateDbContext _context;
        private readonly ILogger<AppointmentService> _logger;
        private readonly IEncryptionService _encryption;

        public AppointmentService(
            LegalMateDbContext context, 
            ILogger<AppointmentService> logger,
            IEncryptionService encryption)
        {
            _context = context;
            _logger = logger;
            _encryption = encryption;
        }

        // ========== الحجز ==========

        public async Task<AppointmentResponseDto?> CreateAppointmentAsync(Guid userId, CreateAppointmentDto request)
        {
            _logger.LogInformation($"🔵 CreateAppointmentAsync - UserId: {userId}, LawyerId: {request.LawyerId}, BranchId: {request.BranchId}");
            _logger.LogInformation($"🔵 Date: {request.Date}, Time: {request.Time}, Duration: {request.DurationMinutes}");
            
            var lawyerProfile = await _context.LawyerProfiles
                .Include(l => l.User)
                .FirstOrDefaultAsync(l => l.UserId == request.LawyerId && l.User != null && l.User.Status == AccountStatus.Active);

            if (lawyerProfile == null)
            {
                _logger.LogWarning($"❌ Lawyer not found for UserId: {request.LawyerId}");
                return null;
            }

            _logger.LogInformation($"✅ Lawyer found: Id={lawyerProfile.Id}, Name={lawyerProfile.User?.FullName}");

            var isAvailable = await IsTimeSlotAvailableAsync(request.BranchId, request.Date, request.Time, request.DurationMinutes);
            if (!isAvailable)
            {
                _logger.LogWarning($"❌ Time slot not available for branch {request.BranchId} at {request.Date} {request.Time}");
                return null;
            }

            _logger.LogInformation($"✅ Time slot available");

            var appointment = new Appointment
            {
                Id = Guid.NewGuid(),
                AppointmentNumber = GenerateAppointmentNumber(),
                UserID = userId,
                LawyerId = lawyerProfile.Id,
                BranchId = request.BranchId,
                AppointmentType = request.AppointmentType,
                Date = request.Date,
                Time = request.Time,
                DurationMinutes = request.DurationMinutes,
                Location = request.Location,
                Notes = request.Notes,
                Status = AppointmentStatus.Pending,
                RequestedAt = DateTime.UtcNow,
                IsUrgent = request.IsUrgent
            };

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"✅ Appointment created successfully with ID: {appointment.Id}");
            return await GetAppointmentByIdAsync(appointment.Id);
        }

        // ========== الموافقة والرفض ==========

        public async Task<bool> ApproveAppointmentAsync(Guid lawyerId, Guid appointmentId)
        {
            _logger.LogInformation($"🔵 ApproveAppointmentAsync - lawyerId: {lawyerId}, appointmentId: {appointmentId}");
            
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
            _logger.LogInformation($"🔵 RejectAppointmentAsync - lawyerId: {lawyerId}, appointmentId: {appointmentId}");
            
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

        // ========== عرض المواعيد للمستخدم ==========

        public async Task<List<AppointmentResponseDto>> GetUserAppointmentsAsync(Guid userId, string? status = null)
        {
            var query = _context.Appointments
                .Include(a => a.User)
                .Include(a => a.Lawyer)
                    .ThenInclude(l => l!.User)
                .Include(a => a.Branch)
                .Where(a => a.UserID == userId);

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<AppointmentStatus>(status, true, out var s))
            {
                query = query.Where(a => a.Status == s);
            }

            var appointments = await query.OrderByDescending(a => a.Date).ToListAsync();
            return appointments.Select(a => MapToDto(a, a.User)).ToList();
        }

        // ========== عرض المواعيد للمحامي ==========

        public async Task<List<AppointmentResponseDto>> GetLawyerAppointmentsAsync(Guid lawyerId, string? status = null)
        {
            var lawyerProfile = await _context.LawyerProfiles
                .FirstOrDefaultAsync(l => l.UserId == lawyerId);
            
            if (lawyerProfile == null) return new List<AppointmentResponseDto>();

            var query = _context.Appointments
                .Include(a => a.User)
                .Include(a => a.Lawyer)
                    .ThenInclude(l => l!.User)
                .Include(a => a.Branch)
                .Where(a => a.LawyerId == lawyerProfile.Id);

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<AppointmentStatus>(status, true, out var s))
            {
                query = query.Where(a => a.Status == s);
            }

            var appointments = await query.OrderByDescending(a => a.Date).ToListAsync();
            return appointments.Select(a => MapToDto(a, a.User)).ToList();
        }

        // ========== عرض موعد بالـ ID ==========

        public async Task<AppointmentResponseDto?> GetAppointmentByIdAsync(Guid appointmentId)
        {
            var appointment = await _context.Appointments
                .Include(a => a.User)
                .Include(a => a.Lawyer)
                    .ThenInclude(l => l!.User)
                .Include(a => a.Branch)
                .FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (appointment == null) return null;

            return MapToDto(appointment, appointment.User);
        }

        // ========== المواعيد المعلقة ==========

        public async Task<List<AppointmentResponseDto>> GetPendingAppointmentsForLawyerAsync(Guid lawyerId)
        {
            var lawyerProfile = await _context.LawyerProfiles
                .FirstOrDefaultAsync(l => l.UserId == lawyerId);
            if (lawyerProfile == null) return new List<AppointmentResponseDto>();

            var appointments = await _context.Appointments
                .Include(a => a.User)
                .Include(a => a.Lawyer)
                    .ThenInclude(l => l!.User)
                .Include(a => a.Branch)
                .Where(a => a.LawyerId == lawyerProfile.Id && a.Status == AppointmentStatus.Pending)
                .OrderByDescending(a => a.RequestedAt)
                .ToListAsync();

            return appointments.Select(a => MapToDto(a, a.User)).ToList();
        }

        public async Task<List<AppointmentResponseDto>> GetPendingAppointmentsForUserAsync(Guid userId)
        {
            var appointments = await _context.Appointments
                .Include(a => a.User)
                .Include(a => a.Lawyer)
                    .ThenInclude(l => l!.User)
                .Include(a => a.Branch)
                .Where(a => a.UserID == userId && a.Status == AppointmentStatus.Pending)
                .OrderByDescending(a => a.RequestedAt)
                .ToListAsync();

            return appointments.Select(a => MapToDto(a, a.User)).ToList();
        }

        // ========== إلغاء المواعيد ==========

        public async Task<bool> CancelAppointmentAsync(Guid appointmentId, Guid userId, string? reason = null)
        {
            var appointment = await _context.Appointments
                .Include(a => a.CancelRequests)
                .FirstOrDefaultAsync(a => a.Id == appointmentId);
                
            if (appointment == null) return false;
            
            if (appointment.Status == AppointmentStatus.Completed || 
                appointment.Status == AppointmentStatus.Cancelled)
                return false;

            var lawyerProfile = await _context.LawyerProfiles.FirstOrDefaultAsync(l => l.UserId == userId);
            
            bool isUser = appointment.UserID == userId;
            bool isLawyer = lawyerProfile != null && appointment.LawyerId == lawyerProfile.Id;
            
            if (!isUser && !isLawyer) return false;

            var existingRequest = await _context.AppointmentCancelRequests
                .FirstOrDefaultAsync(c => c.AppointmentId == appointmentId && c.Status == CancelRequestStatus.Pending);
            
            if (existingRequest != null) return false;

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
            if (appointment == null) return false;
            
            var lawyerProfile = await _context.LawyerProfiles.FirstOrDefaultAsync(l => l.UserId == userId);

            bool isUser = appointment.UserID == userId;
            bool isLawyer = lawyerProfile != null && appointment.LawyerId == lawyerProfile.Id;

            if (!isUser && !isLawyer) return false;
            
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
                .Include(c => c.Appointment)
                    .ThenInclude(a => a!.User)
                .Where(c => c.Appointment != null && 
                           c.Appointment.UserID == userId && 
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
                    .ThenInclude(a => a!.User)
                .Include(c => c.Appointment)
                    .ThenInclude(a => a!.Lawyer)
                    .ThenInclude(l => l!.User)
                .Where(c => c.Appointment != null && 
                           c.Appointment.LawyerId == lawyerProfile.Id && 
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

            var isAvailable = await IsTimeSlotAvailableAsync(appointment.BranchId, request.NewDate, request.NewTime, appointment.DurationMinutes, appointment.Id);
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
            if (appointment == null) return false;
            
            var lawyerProfile = await _context.LawyerProfiles.FirstOrDefaultAsync(l => l.UserId == userId);

            if (!isLawyer && appointment.UserID != userId) return false;
            if (isLawyer && (lawyerProfile == null || appointment.LawyerId != lawyerProfile.Id)) return false;

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
                .Where(r => r.Appointment != null && 
                           r.Appointment.LawyerId == lawyerProfile.Id && 
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
                .Where(r => r.Appointment != null && 
                           r.Appointment.UserID == userId && 
                           r.Status == RescheduleStatus.Pending &&
                           r.InitiatedBy != RescheduleInitiator.User)
                .OrderBy(r => r.RequestedAt)
                .ToListAsync();

            return requests.Select(MapRescheduleToDto).ToList();
        }

        // ========== Helpers ==========

        private async Task<bool> IsTimeSlotAvailableAsync(Guid branchId, DateTime date, string time, int duration, Guid? excludeAppointmentId = null)
        {
            var startTime = ParseTime(time);
            var endTime = startTime.Add(TimeSpan.FromMinutes(duration));

            var appointments = await _context.Appointments
                .Where(a => a.BranchId == branchId && 
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

        /// <summary>
        /// فك تشفير رقم الهاتف باستخدام IEncryptionService
        /// </summary>
        private string DecryptPhoneNumber(string? encryptedPhone)
        {
            if (string.IsNullOrEmpty(encryptedPhone))
                return "";
            
            try
            {
                return _encryption.Decrypt(encryptedPhone);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "فشل في فك تشفير رقم الهاتف");
                return encryptedPhone; // إرجاع القيمة المشفرة في حالة الفشل
            }
        }

        private AppointmentResponseDto MapToDto(Appointment a, User? user)
        {
            var userData = user ?? a.User;
            
            string lawyerInitials = "مح";
            if (a.Lawyer?.User?.FullName != null && !string.IsNullOrEmpty(a.Lawyer.User.FullName))
            {
                lawyerInitials = a.Lawyer.User.FullName.Length >= 2 
                    ? a.Lawyer.User.FullName.Substring(0, 2).ToUpper() 
                    : a.Lawyer.User.FullName.Substring(0, 1).ToUpper();
            }
            
            string userInitials = "عم";
            if (userData?.FullName != null && !string.IsNullOrEmpty(userData.FullName))
            {
                userInitials = userData.FullName.Length >= 2 
                    ? userData.FullName.Substring(0, 2).ToUpper() 
                    : userData.FullName.Substring(0, 1).ToUpper();
            }
            
            // ✅ فك تشفير رقم الهاتف
            string decryptedPhone = "";
            if (userData != null)
            {
                decryptedPhone = DecryptPhoneNumber(userData.Phone);
            }
            
            return new AppointmentResponseDto
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
                LawyerId = a.Lawyer?.UserId ?? Guid.Empty,
                BranchId = a.BranchId,
                Lawyer = new LawyerBriefDto 
                { 
                    Id = a.Lawyer?.UserId ?? Guid.Empty,
                    FullName = a.Lawyer?.User?.FullName ?? "غير معروف",
                    Specialization = a.Lawyer?.PracticeDegree ?? "محامي",
                    Rating = a.Lawyer?.Reviews != null && a.Lawyer.Reviews.Any() 
                        ? a.Lawyer.Reviews.Average(r => r.Rating) 
                        : 0,
                    Initials = lawyerInitials
                },
                User = new UserBriefDto 
                { 
                    Id = a.UserID,
                    FullName = userData?.FullName ?? "غير معروف",
                    Email = userData?.Email ?? "",
                    PhoneNumber = decryptedPhone,
                    Initials = userInitials
                }
            };
        }

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