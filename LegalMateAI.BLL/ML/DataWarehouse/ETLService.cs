using LegalMateAI.DAL.DBContext;
using LegalMateAI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LegalMateAI.BLL.ML.DataWarehouse
{
    /// <summary>
    /// استخراج وتحويل وتحميل البيانات إلى Data Warehouse
    /// </summary>
    public class ETLService
    {
        private readonly LegalMateDbContext _context;
        private readonly ILogger<ETLService> _logger;

        public ETLService(LegalMateDbContext context, ILogger<ETLService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// تنفيذ عملية ETL كاملة
        /// </summary>
        public async Task<bool> RunETLAsync()
        {
            try
            {
                _logger.LogInformation("Starting ETL process...");
                
                // 1. Extract - جلب البيانات
                var cases = await _context.Cases.ToListAsync();
                var contracts = await _context.Contracts.ToListAsync();
                var appointments = await _context.Appointments.ToListAsync();
                
                // 2. Transform - تحويل البيانات
                var facts = new List<DataWarehouseFact>();
                
                var groupedByMonth = cases
                    .GroupBy(c => new { c.CreatedAt.Year, c.CreatedAt.Month })
                    .ToList();
                
                foreach (var group in groupedByMonth)
                {
                    facts.Add(new DataWarehouseFact
                    {
                        Id = Guid.NewGuid(),
                        TimeDimId = GetOrCreateTimeDimension(group.Key.Year, group.Key.Month).Id,
                        CaseCount = group.Count(),
                        ContractCount = contracts.Count(c => c.CreatedAt.Year == group.Key.Year && c.CreatedAt.Month == group.Key.Month),
                        AppointmentCount = appointments.Count(a => a.RequestedAt.Year == group.Key.Year && a.RequestedAt.Month == group.Key.Month),
                        RecordedAt = DateTime.UtcNow
                    });
                }
                
                // 3. Load - تحميل البيانات
                await _context.DataWarehouseFacts.AddRangeAsync(facts);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation($"ETL completed. Loaded {facts.Count} facts.");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ETL failed");
                return false;
            }
        }

        private TimeDimension GetOrCreateTimeDimension(int year, int month)
        {
            var existing = _context.TimeDimensions.FirstOrDefault(t => t.Year == year && t.Month == month);
            if (existing != null) return existing;
            
            var newDim = new TimeDimension
            {
                Id = (_context.TimeDimensions.Max(t => (int?)t.Id) ?? 0) + 1,
                Year = year,
                Quarter = (month - 1) / 3 + 1,
                Month = month,
                MonthName = GetMonthName(month),
                Day = 1,
                Week = 1,
                IsWeekend = false,
                Season = GetSeason(month)
            };
            
            _context.TimeDimensions.Add(newDim);
            _context.SaveChanges();
            return newDim;
        }

        private string GetMonthName(int month) => month switch
        {
            1 => "January", 2 => "February", 3 => "March", 4 => "April",
            5 => "May", 6 => "June", 7 => "July", 8 => "August",
            9 => "September", 10 => "October", 11 => "November", 12 => "December",
            _ => month.ToString()
        };

        private string GetSeason(int month) => month switch
        {
            12 or 1 or 2 => "Winter",
            3 or 4 or 5 => "Spring",
            6 or 7 or 8 => "Summer",
            9 or 10 or 11 => "Fall",
            _ => "Unknown"
        };
    }
}