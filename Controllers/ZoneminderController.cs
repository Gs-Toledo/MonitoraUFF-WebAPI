namespace MonitoraUFF_API.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using MonitoraUFF_API.Application.DTOs;
    using MonitoraUFF_API.Core.Entities;
    using MonitoraUFF_API.Core.Interfaces;

    [ApiController]
    [Route("api/[controller]")]
    public class ZoneminderController : ControllerBase
    {
        private readonly IZoneminderRepository _zoneminderRepository;

        public ZoneminderController(IZoneminderRepository zoneminderRepository)
        {
            _zoneminderRepository = zoneminderRepository;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var instance = await _zoneminderRepository.GetByIdAsync(id);
            if (instance == null)
            {
                return NotFound();
            }

            // Mapear a entidade para o DTO antes de retornar
            var instanceDto = new ZoneminderDto
            {
                Id = instance.Id,
                UrlServer = instance.UrlServer
            };
            return Ok(instanceDto);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateZoneminderDto createDto)
        {
            var newInstance = new ZoneminderInstance
            {
                UrlServer = createDto.UrlServer,
                User = createDto.User,
                Password = createDto.Password // Criptografar antes de salvar!
            };

            await _zoneminderRepository.AddAsync(newInstance);


            var instanceDto = new ZoneminderDto
            {
                Id = newInstance.Id,
                UrlServer = newInstance.UrlServer
            };
            return CreatedAtAction(nameof(GetById), new { id = newInstance.Id }, instanceDto);
        }
    }
}
