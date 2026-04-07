using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegalMateAI.DTOs.ReadDTO
{
  public class GovernorateWithCitiesDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<CityListDto> Cities { get; set; } = new();
    }
}