// LegalMateAI.API/Controllers/LocationsController.cs
using Microsoft.AspNetCore.Mvc;
using LegalMateAI.BLL.Services.IService;
using LegalMateAI.DTOs.ReadDTO;
namespace LegalMateAI.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LocationsController : ControllerBase
    {
        private readonly ILocationService _locationService;

        public LocationsController(ILocationService locationService)
        {
            _locationService = locationService;
        }

        [HttpGet("governorates")]
        public async Task<IActionResult> GetAllGovernorates()
        {
            var governorates = await _locationService.GetAllGovernoratesAsync();
            return Ok(governorates);
        }

        [HttpGet("governorates/{id}/cities")]
        public async Task<IActionResult> GetCitiesByGovernorate(int id)
        {
            if (id <= 0)
                return BadRequest(new { message = "رقم المحافظة غير صحيح" });

            var cities = await _locationService.GetCitiesByGovernorateAsync(id);
            
            if (cities == null || !cities.Any())
                return Ok(new List<CityListDto>()); // إرجاع قائمة فارغة بدلاً من NotFound

            return Ok(cities);
        }
    }
}