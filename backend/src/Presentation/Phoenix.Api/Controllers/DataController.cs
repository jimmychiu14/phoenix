using Microsoft.AspNetCore.Mvc;
using Phoenix.Application.Services;
using Phoenix.Domain.Entities;

namespace Phoenix.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DataController : ControllerBase
{
    private readonly IWeatherService _weatherService;

    public DataController(IWeatherService weatherService)
    {
        _weatherService = weatherService;
    }

    [HttpGet]
    public ActionResult<IEnumerable<WeatherForecast>> Get()
    {
        return Ok(_weatherService.GetForecasts());
    }
}
