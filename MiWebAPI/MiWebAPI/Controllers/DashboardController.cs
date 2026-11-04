using Microsoft.AspNetCore.Mvc;
using MiWebAPI.Data;

namespace MiWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DashboardController : ControllerBase
    {
        private readonly InventarioData _data;
        public DashboardController(InventarioData data) => _data = data;

        [HttpGet]
        public async Task<IActionResult> Resumen() => Ok(await _data.ObtenerResumen());
    }
}
