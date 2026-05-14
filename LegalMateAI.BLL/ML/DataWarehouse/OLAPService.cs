using LegalMateAI.DAL.DBContext;
using LegalMateAI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LegalMateAI.BLL.ML.DataWarehouse
{
    public class OLAPService
    {
        private readonly LegalMateDbContext _context;

        public OLAPService(LegalMateDbContext context)
        {
            _context = context;
        }

        public async Task<OlapResult> SliceAndDiceAsync(
            string? timeDimension = null,
            string? caseType = null,
            string? location = null)
        {
            var query = _context.DataWarehouseFacts
                .Include(f => f.TimeDim)
                .Include(f => f.CaseDim)
                .Include(f => f.LocationDim)
                .AsQueryable();
            
            if (!string.IsNullOrEmpty(caseType))
                query = query.Where(f => f.CaseDim != null && f.CaseDim.CaseType == caseType);
            
            if (!string.IsNullOrEmpty(location))
                query = query.Where(f => f.LocationDim != null && f.LocationDim.GovernorateName == location);
            
            List<object> dataList = new();
            
            if (timeDimension?.ToLower() == "year")
            {
                var result = await query
                    .GroupBy(f => f.TimeDim!.Year)
                    .Select(g => new { Period = g.Key, TotalCases = g.Sum(x => x.CaseCount), TotalContracts = g.Sum(x => x.ContractCount), AvgRating = g.Average(x => x.AverageRating) })
                    .ToListAsync();
                dataList = result.Cast<object>().ToList();
            }
            else if (timeDimension?.ToLower() == "quarter")
            {
                var result = await query
                    .GroupBy(f => new { f.TimeDim!.Year, f.TimeDim.Quarter })
                    .Select(g => new { Period = $"{g.Key.Year}-Q{g.Key.Quarter}", TotalCases = g.Sum(x => x.CaseCount), TotalContracts = g.Sum(x => x.ContractCount), AvgRating = g.Average(x => x.AverageRating) })
                    .ToListAsync();
                dataList = result.Cast<object>().ToList();
            }
            else
            {
                var result = await query
                    .GroupBy(f => new { f.TimeDim!.Year, f.TimeDim.Month })
                    .Select(g => new { Period = $"{g.Key.Year}-{g.Key.Month}", TotalCases = g.Sum(x => x.CaseCount), TotalContracts = g.Sum(x => x.ContractCount), AvgRating = g.Average(x => x.AverageRating) })
                    .ToListAsync();
                dataList = result.Cast<object>().ToList();
            }
            
            return new OlapResult
            {
                Data = dataList,
                Dimensions = new List<string> { timeDimension ?? "month" },
                Measures = new List<string> { "TotalCases", "TotalContracts", "AvgRating" }
            };
        }

        public async Task<OlapResult> RollUpAsync(string fromLevel = "day", string toLevel = "month")
        {
            var query = _context.DataWarehouseFacts.Include(f => f.TimeDim);
            List<object> dataList = new();
            
            if (toLevel == "month")
            {
                var result = await query
                    .GroupBy(f => new { f.TimeDim!.Year, f.TimeDim.Month })
                    .Select(g => new { Period = $"{g.Key.Year}-{g.Key.Month}", TotalCases = g.Sum(x => x.CaseCount), TotalFees = g.Sum(x => x.TotalFees) })
                    .ToListAsync();
                dataList = result.Cast<object>().ToList();
            }
            else if (toLevel == "quarter")
            {
                var result = await query
                    .GroupBy(f => new { f.TimeDim!.Year, f.TimeDim.Quarter })
                    .Select(g => new { Period = $"{g.Key.Year}-Q{g.Key.Quarter}", TotalCases = g.Sum(x => x.CaseCount), TotalFees = g.Sum(x => x.TotalFees) })
                    .ToListAsync();
                dataList = result.Cast<object>().ToList();
            }
            else if (toLevel == "year")
            {
                var result = await query
                    .GroupBy(f => f.TimeDim!.Year)
                    .Select(g => new { Period = g.Key.ToString(), TotalCases = g.Sum(x => x.CaseCount), TotalFees = g.Sum(x => x.TotalFees) })
                    .ToListAsync();
                dataList = result.Cast<object>().ToList();
            }
            else
            {
                var result = await query
                    .GroupBy(f => new { f.TimeDim!.Year, f.TimeDim.Month })
                    .Select(g => new { Period = $"{g.Key.Year}-{g.Key.Month}", TotalCases = g.Sum(x => x.CaseCount), TotalFees = g.Sum(x => x.TotalFees) })
                    .ToListAsync();
                dataList = result.Cast<object>().ToList();
            }
            
            return new OlapResult { Data = dataList };
        }

        public async Task<OlapResult> DrillDownAsync(int year, int month)
        {
            var result = await _context.DataWarehouseFacts
                .Include(f => f.TimeDim)
                .Include(f => f.CaseDim)
                .Where(f => f.TimeDim!.Year == year && f.TimeDim.Month == month && f.CaseDim != null)
                .GroupBy(f => f.CaseDim!.CaseType)
                .Select(g => new { CaseType = g.Key ?? "غير محدد", Count = g.Sum(x => x.CaseCount), AvgRating = g.Average(x => x.AverageRating) })
                .ToListAsync();
            
            var dataList = result.Cast<object>().ToList();
            
            return new OlapResult { Data = dataList };
        }
    }

    public class OlapResult
    {
        public List<object> Data { get; set; } = new();
        public List<string> Dimensions { get; set; } = new();
        public List<string> Measures { get; set; } = new();
    }
}