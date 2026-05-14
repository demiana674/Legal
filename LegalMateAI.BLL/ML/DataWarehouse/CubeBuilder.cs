using LegalMateAI.DAL.DBContext;
using Microsoft.EntityFrameworkCore;

namespace LegalMateAI.BLL.ML.DataWarehouse
{
    /// <summary>
    /// بناء المكعبات متعددة الأبعاد للتحليل السريع
    /// </summary>
    public class CubeBuilder
    {
        private readonly LegalMateDbContext _context;

        public CubeBuilder(LegalMateDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// بناء مكعب القضايا
        /// </summary>
        public async Task<Cube> BuildCaseCubeAsync()
        {
            var cube = new Cube { Name = "CaseCube" };
            
            // Dimensions
            cube.Dimensions.Add(new Dimension { Name = "Time", Hierarchy = new[] { "Year", "Quarter", "Month" } });
            cube.Dimensions.Add(new Dimension { Name = "CaseType", Hierarchy = new[] { "CaseType" } });
            cube.Dimensions.Add(new Dimension { Name = "Location", Hierarchy = new[] { "Governorate", "City" } });
            
            // Measures
            cube.Measures.Add(new Measure { Name = "TotalCases", Aggregation = "SUM" });
            cube.Measures.Add(new Measure { Name = "SuccessRate", Aggregation = "AVG" });
            
            // Pre-aggregate data
            var data = await _context.DataWarehouseFacts
                .Include(f => f.TimeDim)
                .Include(f => f.CaseDim)
                .Include(f => f.LocationDim)
                .GroupBy(f => new { f.TimeDim!.Year, f.TimeDim.Month, f.CaseDim!.CaseType, f.LocationDim!.GovernorateName })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    g.Key.CaseType,
                    g.Key.GovernorateName,
                    TotalCases = g.Sum(x => x.CaseCount),
                    SuccessRate = g.Average(x => x.SuccessRate)
                })
                .ToListAsync();
            
            cube.Data = data;
            return cube;
        }
    }

    public class Cube
    {
        public string Name { get; set; } = string.Empty;
        public List<Dimension> Dimensions { get; set; } = new();
        public List<Measure> Measures { get; set; } = new();
        public object Data { get; set; } = new();
    }

    public class Dimension
    {
        public string Name { get; set; } = string.Empty;
        public string[] Hierarchy { get; set; } = Array.Empty<string>();
    }

    public class Measure
    {
        public string Name { get; set; } = string.Empty;
        public string Aggregation { get; set; } = string.Empty;
    }
}