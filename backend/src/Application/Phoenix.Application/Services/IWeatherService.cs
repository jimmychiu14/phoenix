using Phoenix.Domain.Entities;

namespace Phoenix.Application.Services;

public interface IWeatherService
{
    IEnumerable<WeatherForecast> GetForecasts();
}
