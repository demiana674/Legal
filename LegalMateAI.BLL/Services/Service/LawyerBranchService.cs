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

        // وقت بدء العمل (9 صباحاً)
        private static readonly TimeSpan WorkStartTime = new TimeSpan(9, 0, 0);
        // وقت انتهاء العمل (10 مساءً)
        private static readonly TimeSpan WorkEndTime = new TimeSpan(22, 0, 0);

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
            
            // ✅ جلب جميع فترات التوفر في هذا اليوم
            var availabilities = await _context.BranchAvailabilities
                .Where(a => a.BranchId == branchId && 
                           a.DayOfWeek == dayOfWeek && 
                           a.IsAvailable)
                .OrderBy(a => a.StartTime)
                .ToListAsync();

            if (availabilities == null || !availabilities.Any())
                return new List<AvailableTimeSlotDto>();

            var bookedAppointments = await _context.Appointments
                .Where(a => a.BranchId == branchId && 
                           a.Date.Date == date.Date &&
                           a.Status != Domain.Enums.AppointmentStatus.Cancelled)
                .Select(a => a.Time)
                .ToListAsync();

            var slots = new List<AvailableTimeSlotDto>();
            
            // ✅ التكرار على كل فترات التوفر
            foreach (var availability in availabilities)
            {
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

            // ✅ التحقق من عدم وجود فرع بنفس الاسم لنفس المحامي
            var existingBranch = await _context.LawyerBranches
                .FirstOrDefaultAsync(b => b.LawyerId == lawyerProfile.Id && 
                                         b.BranchName == request.BranchName && 
                                         b.IsActive);

            if (existingBranch != null)
            {
                _logger.LogWarning($"❌ Branch with name '{request.BranchName}' already exists for this lawyer");
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

            // ✅ إذا كان الاسم بيتغير، نتأكد من عدم وجود اسم مكرر
            if (request.BranchName != null && request.BranchName != branch.BranchName)
            {
                var existingBranch = await _context.LawyerBranches
                    .FirstOrDefaultAsync(b => b.LawyerId == lawyerProfile.Id && 
                                             b.BranchName == request.BranchName && 
                                             b.Id != branchId &&
                                             b.IsActive);
                if (existingBranch != null)
                {
                    _logger.LogWarning($"❌ Branch with name '{request.BranchName}' already exists");
                    return null;
                }
                branch.BranchName = request.BranchName;
            }

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

            // ✅ التحقق من صحة الأوقات أولاً (من 9 ص إلى 10 م)
            foreach (var avail in availabilities)
            {
                if (!IsValidTimeRange(avail.StartTime, avail.EndTime))
                {
                    _logger.LogWarning($"❌ Invalid time range: {avail.StartTime} - {avail.EndTime}. Must be between {WorkStartTime} and {WorkEndTime}");
                    return false;
                }

                // ✅ التحقق من عدم تكرار نفس الموعد في فرع آخر
                var isOverlapping = await IsTimeSlotOverlappingWithOtherBranches(
                    lawyerProfile.Id, 
                    branchId, 
                    avail.DayOfWeek, 
                    avail.StartTime, 
                    avail.EndTime);

                if (isOverlapping)
                {
                    _logger.LogWarning($"❌ Time slot {avail.StartTime} - {avail.EndTime} on {avail.DayOfWeek} already exists in another branch");
                    return false;
                }
            }

            // ✅ التحقق من عدم وجود تعارض بين المواعيد داخل نفس الفرع
            var groupedByDay = availabilities.GroupBy(a => a.DayOfWeek);
            foreach (var group in groupedByDay)
            {
                var slots = group.ToList();
                for (int i = 0; i < slots.Count; i++)
                {
                    for (int j = i + 1; j < slots.Count; j++)
                    {
                        if (IsTimeOverlapping(slots[i].StartTime, slots[i].EndTime, slots[j].StartTime, slots[j].EndTime))
                        {
                            _logger.LogWarning($"❌ Time slots overlap within the same branch on day {group.Key}");
                            return false;
                        }
                    }
                }
            }

            // ✅ بعد كل التحققات - نقوم بالحذف والإضافة
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
            _logger.LogInformation($"✅ Availability updated for branch {branchId}");
            return true;
        }

        // ========== Helper Methods ==========

        /// <summary>
        /// ✅ التحقق من أن الوقت بين 9 صباحاً و 10 مساءً
        /// </summary>
        private bool IsValidTimeRange(TimeSpan startTime, TimeSpan endTime)
        {
            if (startTime < WorkStartTime)
                return false;
            
            if (endTime > WorkEndTime)
                return false;
            
            if (startTime >= endTime)
                return false;
            
            return true;
        }

        /// <summary>
        /// ✅ التحقق من عدم وجود نفس الموعد في أي فرع آخر لنفس المحامي
        /// </summary>
        private async Task<bool> IsTimeSlotOverlappingWithOtherBranches(Guid lawyerProfileId, Guid currentBranchId, DayOfWeek dayOfWeek, TimeSpan startTime, TimeSpan endTime)
        {
            // جلب جميع فروع المحامي الأخرى (ما عدا الفرع الحالي)
            var otherBranches = await _context.LawyerBranches
                .Where(b => b.LawyerId == lawyerProfileId && b.Id != currentBranchId && b.IsActive)
                .Select(b => b.Id)
                .ToListAsync();

            if (!otherBranches.Any())
                return false;

            // التحقق من وجود نفس الموعد في أي فرع آخر
            var overlapping = await _context.BranchAvailabilities
                .Where(a => otherBranches.Contains(a.BranchId) &&
                           a.DayOfWeek == dayOfWeek &&
                           a.IsAvailable &&
                           ((startTime >= a.StartTime && startTime < a.EndTime) ||
                            (endTime > a.StartTime && endTime <= a.EndTime) ||
                            (startTime <= a.StartTime && endTime >= a.EndTime)))
                .AnyAsync();

            return overlapping;
        }

        /// <summary>
        /// ✅ التحقق من تداخل موعدين مع بعض
        /// </summary>
        private bool IsTimeOverlapping(TimeSpan start1, TimeSpan end1, TimeSpan start2, TimeSpan end2)
        {
            return (start1 < end2 && end1 > start2);
        }

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