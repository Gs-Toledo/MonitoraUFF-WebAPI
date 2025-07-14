namespace MonitoraUFF_API.Controllers;

using Microsoft.AspNetCore.Mvc;
using MonitoraUFF_API.Application.DTOs;
using MonitoraUFF_API.Core.Entities;
using MonitoraUFF_API.Core.Interfaces;

//[Route("api/[controller]")]

[ApiController]
[Route("api/zoneminder-instances")]
public class ZoneminderController : ControllerBase
{
    private readonly IZoneminderRepository _zoneminderRepository;

    public ZoneminderController(IZoneminderRepository zoneminderRepository)
    {
        _zoneminderRepository = zoneminderRepository;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ZoneminderInstanceDto>>> GetAll()
    {
        var instances = await _zoneminderRepository.GetAllAsync();
        var dtos = new List<ZoneminderInstanceDto>();
        foreach (var instance in instances)
        {
            dtos.Add(new ZoneminderInstanceDto { Id = instance.Id, UrlServer = instance.UrlServer, User = instance.User });
        }
        return Ok(dtos);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var instance = await _zoneminderRepository.GetByIdAsync(id);
        if (instance == null)
        {
            return NotFound();
        }

        var instanceDto = new ZoneminderInstanceDto
        {
            Id = instance.Id,
            UrlServer = instance.UrlServer
        };
        return Ok(instanceDto);
    }



    [HttpPost]
    public async Task<ActionResult<ZoneminderInstanceDto>> Create(UpdateZoneminderInstanceDto dto)
    {
        // *****ATENÇÂO***** lembrete: a senha tem que ser armazenada de forma segura (ex: Azure Key Vault)
        // até então isso é uma demonstração mais simples

        var instance = new ZoneminderInstance { UrlServer = dto.UrlServer, User = dto.User, Password = dto.Password };
        await _zoneminderRepository.AddAsync(instance);
        var resultDto = new ZoneminderInstanceDto { Id = instance.Id, UrlServer = instance.UrlServer, User = instance.User };
        return CreatedAtAction(nameof(GetAll), new { id = instance.Id }, resultDto);
    }

    [ApiController]
    [Route("api/cameras")]
    public class CamerasController : ControllerBase
    {
        private readonly ICameraRepository _cameraRepository;
        private readonly IZoneminderRepository _zoneminderRepository;

        public CamerasController(ICameraRepository cameraRepository, IZoneminderRepository zoneminderRepository)
        {
            _cameraRepository = cameraRepository;
            _zoneminderRepository = zoneminderRepository;
        }

        [HttpGet("all")]
        public async Task<ActionResult<IEnumerable<CameraDto>>> GetAllCamerasFromAllInstances()
        {
            var allInstances = await _zoneminderRepository.GetAllAsync();
            var allCamerasDto = new List<CameraDto>();

            foreach (var instance in allInstances)
            {
                var camerasInInstance = await _cameraRepository.GetByZoneminderInstanceIdAsync(instance.Id);
                foreach (var cam in camerasInInstance)
                {
                    allCamerasDto.Add(new CameraDto
                    {
                        Id = cam.Id,
                        ZoneminderInstanceId = instance.Id,
                        ZoneminderInstanceUrl = instance.UrlServer,
                        Name = cam.Name,
                        Coordinates = cam.Coordinates,
                        IsSavingRecords = cam.IsSavingRecords
                    });
                }
            }
            return Ok(allCamerasDto);
        }
    }
}
