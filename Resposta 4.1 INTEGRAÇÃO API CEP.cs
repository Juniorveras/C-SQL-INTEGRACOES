using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

public class Endereco
{
    public string Cep { get; set; }
    public string Street { get; set; }
    public string City { get; set; }
    public string State { get; set; }
}

public class CepService
{
    private readonly HttpClient _httpClient;

    // Idealmente, injetar via IHttpClientFactory no construtor
    public CepService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<Endereco> BuscarCepAsync(string cep)
    {
        try
        {
            var response = await _httpClient.GetAsync($"https://brasilapi.com.br/api/cep/v1/{cep}");
            
            if (!response.IsSuccessStatusCode)
            {
                // Tratar 404 ou outros erros específicos
                return null; 
            }

            var json = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<Endereco>(json, options);
        }
        catch (HttpRequestException)
        {
            throw new Exception("Não foi possível conectar ao serviço de CEP no momento. Tente novamente mais tarde.");
        }
    }
}