using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;

namespace cacheapidemo.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DemoCacheController : ControllerBase
    {
        private readonly ILogger<WeatherForecastController> _logger;
        private IDistributedCache _redisCache;

        public DemoCacheController(ILogger<WeatherForecastController> logger, IDistributedCache cache)
        {
            _logger = logger;
            _redisCache = cache;
        }

        private double GetStockDatabase(string stockId)
        {
            // Demo time consume on database
            System.Threading.Thread.Sleep(1000);

            // Generate random number
            Random rnd = new Random();
            return rnd.NextDouble() * 100;
        }

        [HttpGet("GetStockById")]
        public StockData? Get(string stockId)
        {
            // Validate input
            if (string.IsNullOrEmpty(stockId))
            {
                throw new Exception("Please provide stockId");
            }

            // Get data from redis
            string jsonStockData = _redisCache.GetString(stockId);

            // If no data in redis, get it from dummy database
            if(string.IsNullOrEmpty(jsonStockData))
            {
                var newStock = new StockData();
                newStock.StockId = stockId;
                newStock.StockValue = GetStockDatabase(stockId);
                newStock.LastUpdated = DateTime.Now;

                jsonStockData = JsonConvert.SerializeObject(newStock);

                var options = new DistributedCacheEntryOptions();
                options.SetAbsoluteExpiration(DateTimeOffset.Now.AddSeconds(60*10));
                _redisCache.SetString(stockId, jsonStockData, options);
            }

            var theStock = JsonConvert.DeserializeObject<StockData>(jsonStockData);

            return theStock;
        }
    }

    public class StockData
    {
        public string? StockId { get; set; }
        public double? StockValue { get; set; }
        public DateTime? LastUpdated { get; set; }
    }
}
