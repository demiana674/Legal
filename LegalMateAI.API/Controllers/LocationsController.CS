// LegalMateAI.API/Controllers/LocationsController.cs
using Microsoft.AspNetCore.Mvc;
using LegalMateAI.BLL.Services.IService;

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

        // [HttpGet("governorates/{id}/cities")]
        // public async Task<IActionResult> GetCitiesByGovernorate(int id)
        // {
        //     var cities = await _locationService.GetCitiesByGovernorateAsync(id);
        //     return Ok(cities);
        // }


        [HttpGet("governorates/{id}/cities")]
public async Task<IActionResult> GetCitiesByGovernorate(int id)
{
    if (id <= 0)
        return BadRequest("رقم المحافظة غير صحيح");

    var cities = await _locationService.GetCitiesByGovernorateAsync(id);
    
    // اختياري: إذا كنتِ تريدين إعلام الفرونت إند أن المحافظة لا تملك مدن مسجلة
    if (cities == null || !cities.Any())
        return NotFound("لا توجد مدن مسجلة لهذه المحافظة");

    return Ok(cities);
}
    }
}