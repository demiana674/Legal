// LegalMateAI.BLL.Services.IService/ILocationService.cs
using LegalMateAI.DTOs.ReadDTO;

namespace LegalMateAI.BLL.Services.IService
{
    public interface ILocationService
    {
        Task<List<GovernorateListDto>> GetAllGovernoratesAsync();
        Task<List<CityListDto>> GetCitiesByGovernorateAsync(int governorateId);
    }
}