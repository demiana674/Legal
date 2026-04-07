// LegalMateAI.BLL.Services.Service/LocationService.cs
using Microsoft.EntityFrameworkCore;
using LegalMateAI.DAL.DBContext;
using LegalMateAI.DTOs.ReadDTO;
using LegalMateAI.BLL.Services.IService;

namespace LegalMateAI.BLL.Services.Service
{
    public class LocationService : ILocationService
    {
        private readonly LegalMateDbContext _context;

        public LocationService(LegalMateDbContext context)
        {
            _context = context;
        }

        public async Task<List<GovernorateListDto>> GetAllGovernoratesAsync()
        {
            return await _context.Governorates
                .OrderBy(g => g.Name)
                .Select(g => new GovernorateListDto
                {
                    Id = g.Id,
                    Name = g.Name
                })
                .ToListAsync();
        }

        public async Task<List<CityListDto>> GetCitiesByGovernorateAsync(int governorateId)
        {
            return await _context.Cities
                .Where(c => c.GovernorateId == governorateId)
                .OrderBy(c => c.Name)
                .Select(c => new CityListDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    GovernorateId = c.GovernorateId
                })
                .ToListAsync();
        }
    }
}