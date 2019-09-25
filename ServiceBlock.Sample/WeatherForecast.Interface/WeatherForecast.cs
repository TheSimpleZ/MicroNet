using System;
using ServiceBlock.Interface.Resource;
using ServiceBlock.Interface.Storage;
using ServiceBlock.Storage;

namespace WeatherForecast.Interface
{
    [Storage(typeof(MemoryStorage<WeatherForecasts>))]
    public class WeatherForecasts : AbstractResource
    {
        public DateTime Date { get; set; }

        public int TemperatureC { get; set; }

        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

        public string Summary { get; set; }


    }
}