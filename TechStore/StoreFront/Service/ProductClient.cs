using StoreFront.Model;
using System.Net.Http.Headers;

namespace StoreFront.Service
{
    public class ProductClient
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContext;

        public ProductClient(HttpClient httpClient, IHttpContextAccessor httpContext)
        {
            _httpClient = httpClient;
            _httpContext = httpContext;

            // Default Base Address (Docker Internal DNS)
            // It points to NGINX, not directly to the microservices
            _httpClient.BaseAddress = new Uri("http://nginx:80/");
        }

        private void AddWarehouseHeader()
        {
            // Read the selected warehouse from a Cookie (Default to 'warehouse-a')
            var warehouse = _httpContext.HttpContext?.Request.Cookies["CurrentWarehouse"] ?? "warehouse-a";

            // Clear and add the header for the API to read
            _httpClient.DefaultRequestHeaders.Remove("X-Warehouse-ID");
            _httpClient.DefaultRequestHeaders.Add("X-Warehouse-ID", warehouse);
        }

        public async Task<List<ProductViewModel>> GetAllAsync()
        {
            AddWarehouseHeader();
            return await _httpClient.GetFromJsonAsync<List<ProductViewModel>>("/products")
                   ?? new List<ProductViewModel>();
        }

        public async Task<ProductViewModel?> GetByIdAsync(string id)
        {
            AddWarehouseHeader();
            try
            {
                return await _httpClient.GetFromJsonAsync<ProductViewModel>($"/products/{id}");
            }
            catch { return null; }
        }

        public async Task CreateAsync(ProductViewModel product)
        {
            AddWarehouseHeader();
            await _httpClient.PostAsJsonAsync("/products", product);
        }

        public async Task UpdateAsync(string id, ProductViewModel product)
        {
            AddWarehouseHeader();
            await _httpClient.PutAsJsonAsync($"/products/{id}", product);
        }

        public async Task DeleteAsync(string id)
        {
            AddWarehouseHeader();
            await _httpClient.DeleteAsync($"/products/{id}");
        }
    }
}