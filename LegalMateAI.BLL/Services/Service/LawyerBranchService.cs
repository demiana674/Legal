// LegalMateAI.BLL/Services/Service/LawyerBranchService.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LegalMateAI.DAL.DBContext;
using LegalMateAI.Domain.Entities;
using LegalMateAI.DTOs.ReadDTO;
using LegalMateAI.DTOs.CreateDTO;
using LegalMateAI.DTOs.UpdateDTO;
using LegalMateAI.BLL.Services.IService;

namespace LegalMateAI.BLL.Services.Service
{
    public class LawyerBranchService : ILawyerBranchService
    {
        private readonly LegalMateDbContext _context;
        private readonly ILogger<LawyerBranchService> _logger;

        public LawyerBranchService(
            LegalMateDbContext context,
            ILogger<LawyerBranchService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ========== للجميع ==========

        public async Task<List<LawyerBranchDto>> GetLawyerBranchesAsync(Guid userId)
        {
            _logger.LogInformation($"🔵 GetLawyerBranchesAsync called with UserId: {userId}");

            // ✅ نجيب الـ LawyerProfile من الـ UserId
            var lawyerProfile = await _context.LawyerProfiles
                .FirstOrDefaultAsync(l => l.UserId == userId);

            if (lawyerProfile == null)
            {
                _logger.LogWarning($"❌ No LawyerProfile found for UserId: {userId}");
                return new List<LawyerBranchDto>();
            }

            _logger.LogInformation($"✅ LawyerProfile found: {lawyerProfile.Id}");

            var branches = await _context.LawyerBranches
                .Include(b => b.Governorate)
                .Include(b => b.City)
                .Where(b => b.LawyerId == lawyerProfile.Id && b.IsActive)
                .OrderBy(b => b.BranchName)
                .ToListAsync();

            _logger.LogInformation($"✅ Retrieved {branches.Count} branches");

            return branches.Select(MapToDto).ToList();
        }

        public async Task<List<BranchAvailabilityDto>> GetBranchAvailabilityAsync(Guid branchId)
        {
            var availabilities = await _context.BranchAvailabilities
                .Where(a => a.BranchId == branchId && a.IsAvailable)
                .OrderBy(a => a.DayOfWeek)
                .ThenBy(a => a.StartTime)
                .ToListAsync();

            return availabilities.Select(MapAvailabilityToDto).ToList();
        }

        public async Task<List<AvailableTimeSlotDto>> GetAvailableTimeSlotsAsync(Guid branchId, DateTime date)
        {
            var dayOfWeek = date.DayOfWeek;
            
            var availability = await _context.BranchAvailabilities
                .FirstOrDefaultAsync(a => a.BranchId == branchId && 
                                         a.DayOfWeek == dayOfWeek && 
                                         a.IsAvailable);

            if (availability == null)
                return new List<AvailableTimeSlotDto>();

            var bookedAppointments = await _context.Appointments
                .Where(a => a.BranchId == branchId && 
                           a.Date.Date == date.Date &&
                           a.Status != Domain.Enums.AppointmentStatus.Cancelled)
                .Select(a => a.Time)
                .ToListAsync();

            var slots = new List<AvailableTimeSlotDto>();
            var currentTime = availability.StartTime;
            var slotDuration = TimeSpan.FromMinutes(availability.SlotDurationMinutes);

            while (currentTime.Add(slotDuration) <= availability.EndTime)
            {
                var timeString = currentTime.ToString(@"hh\:mm");
                var isBooked = bookedAppointments.Contains(timeString) || 
                              bookedAppointments.Contains(FormatTime12Hour(currentTime));

                slots.Add(new AvailableTimeSlotDto
                {
                    Time = timeString,
                    DisplayTime = FormatTime12Hour(currentTime),
                    IsAvailable = !isBooked
                });

                currentTime = currentTime.Add(slotDuration);
            }

            return slots;
        }

        // ========== للمحامي فقط ==========

        public async Task<LawyerBranchDto?> CreateBranchAsync(Guid userId, CreateLawyerBranchDto request)
        {
            _logger.LogInformation($"🔵 CreateBranchAsync called for UserId: {userId}");
            
            var lawyerProfile = await _context.LawyerProfiles
                .FirstOrDefaultAsync(l => l.UserId == userId);

            if (lawyerProfile == null)
            {
                _logger.LogWarning($"❌ LawyerProfile not found for UserId: {userId}");
                return null;
            }

            var branch = new LawyerBranch
            {
                Id = Guid.NewGuid(),
                LawyerId = lawyerProfile.Id,
                BranchName = request.BranchName,
                GovernorateId = request.GovernorateId,
                CityId = request.CityId,
                Address = request.Address,
                PhoneNumber = request.PhoneNumber,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.LawyerBranches.Add(branch);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"✅ Branch created: {branch.BranchName}");
            return await GetBranchByIdAsync(branch.Id);
        }

        public async Task<LawyerBranchDto?> UpdateBranchAsync(Guid userId, Guid branchId, UpdateLawyerBranchDto request)
        {
            var lawyerProfile = await _context.LawyerProfiles
                .FirstOrDefaultAsync(l => l.UserId == userId);

            if (lawyerProfile == null) return null;

            var branch = await _context.LawyerBranches
                .FirstOrDefaultAsync(b => b.Id == branchId && b.LawyerId == lawyerProfile.Id);

            if (branch == null) return null;

            if (request.BranchName != null) branch.BranchName = request.BranchName;
            if (request.GovernorateId.HasValue) branch.GovernorateId = request.GovernorateId;
            if (request.CityId.HasValue) branch.CityId = request.CityId;
            if (request.Address != null) branch.Address = request.Address;
            if (request.PhoneNumber != null) branch.PhoneNumber = request.PhoneNumber;
            if (request.IsActive.HasValue) branch.IsActive = request.IsActive.Value;

            await _context.SaveChangesAsync();
            return await GetBranchByIdAsync(branchId);
        }

        public async Task<bool> DeleteBranchAsync(Guid userId, Guid branchId)
        {
            var lawyerProfile = await _context.LawyerProfiles
                .FirstOrDefaultAsync(l => l.UserId == userId);

            if (lawyerProfile == null) return false;

            var branch = await _context.LawyerBranches
                .FirstOrDefaultAsync(b => b.Id == branchId && b.LawyerId == lawyerProfile.Id);

            if (branch == null) return false;

            _context.LawyerBranches.Remove(branch);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Branch deleted: {branchId}");
            return true;
        }

        public async Task<bool> UpdateBranchAvailabilityAsync(Guid userId, Guid branchId, List<CreateBranchAvailabilityDto> availabilities)
        {
            var lawyerProfile = await _context.LawyerProfiles
                .FirstOrDefaultAsync(l => l.UserId == userId);

            if (lawyerProfile == null) return false;

            var branch = await _context.LawyerBranches
                .FirstOrDefaultAsync(b => b.Id == branchId && b.LawyerId == lawyerProfile.Id);

            if (branch == null) return false;

            var oldAvailabilities = await _context.BranchAvailabilities
                .Where(a => a.BranchId == branchId)
                .ToListAsync();
            _context.BranchAvailabilities.RemoveRange(oldAvailabilities);

            foreach (var avail in availabilities)
            {
                _context.BranchAvailabilities.Add(new BranchAvailability
                {
                    Id = Guid.NewGuid(),
                    BranchId = branchId,
                    DayOfWeek = avail.DayOfWeek,
                    StartTime = avail.StartTime,
                    EndTime = avail.EndTime,
                    SlotDurationMinutes = avail.SlotDurationMinutes,
                    IsAvailable = avail.IsAvailable
                });
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation($"Availability updated for branch {branchId}");
            return true;
        }

        // ========== Helper Methods ==========

        private async Task<LawyerBranchDto?> GetBranchByIdAsync(Guid branchId)
        {
            var branch = await _context.LawyerBranches
                .Include(b => b.Governorate)
                .Include(b => b.City)
                .FirstOrDefaultAsync(b => b.Id == branchId);

            return branch != null ? MapToDto(branch) : null;
        }

        private string FormatTime12Hour(TimeSpan time)
        {
            var hours = time.Hours;
            var minutes = time.Minutes;
            var ampm = hours >= 12 ? "PM" : "AM";
            hours = hours % 12;
            if (hours == 0) hours = 12;
            return $"{hours:D2}:{minutes:D2} {ampm}";
        }

        private LawyerBranchDto MapToDto(LawyerBranch branch)
        {
            return new LawyerBranchDto
            {
                Id = branch.Id,
                BranchName = branch.BranchName,
                GovernorateId = branch.GovernorateId,
                GovernorateName = branch.Governorate?.Name,
                CityId = branch.CityId,
                CityName = branch.City?.Name,
                Address = branch.Address,
                PhoneNumber = branch.PhoneNumber,
                IsActive = branch.IsActive
            };
        }

        private BranchAvailabilityDto MapAvailabilityToDto(BranchAvailability availability)
        {
            return new BranchAvailabilityDto
            {
                Id = availability.Id,
                DayOfWeek = availability.DayOfWeek,
                DayName = GetDayNameArabic(availability.DayOfWeek),
                StartTime = availability.StartTime.ToString(@"hh\:mm"),
                EndTime = availability.EndTime.ToString(@"hh\:mm"),
                SlotDurationMinutes = availability.SlotDurationMinutes,
                IsAvailable = availability.IsAvailable
            };
        }

        private string GetDayNameArabic(DayOfWeek day)
        {
            return day switch
            {
                DayOfWeek.Sunday => "الأحد",
                DayOfWeek.Monday => "الاثنين",
                DayOfWeek.Tuesday => "الثلاثاء",
                DayOfWeek.Wednesday => "الأربعاء",
                DayOfWeek.Thursday => "الخميس",
                DayOfWeek.Friday => "الجمعة",
                DayOfWeek.Saturday => "السبت",
                _ => day.ToString()
            };
        }
    }
}