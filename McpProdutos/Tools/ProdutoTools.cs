using System.ComponentModel;
using System.Net.Http.Json;
using ModelContextProtocol.Server;

// Defina um record para o modelo de produto, para desserialização.
public record Produto(int Id, string Nome, decimal Preco, int Estoque);

[McpServerToolType]
public static class ProdutoTools
{
    // Use um HttpClient estático para eficiência.
    private static readonly HttpClient _httpClient = new HttpClient
    {
        // IMPORTANTE: Substitua pela URL correta da sua APIProdutos!
        BaseAddress = new Uri("https://localhost:7058")
    };

    [McpServerTool, Description("Obtém a lista de todos os produtos disponíveis na loja.")]
    public static async Task<IEnumerable<Produto>> ListarProdutos()
    {
        try
        {
            var produtos = await _httpClient.GetFromJsonAsync<IEnumerable<Produto>>("/produtos");
            return produtos ?? Enumerable.Empty<Produto>();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Erro ao buscar produtos: {ex.Message}");
            return Enumerable.Empty<Produto>();
        }
    }

    [McpServerTool, Description("Busca um produto específico pelo seu ID numérico.")]
    public static async Task<Produto?> BuscarProdutoPorId(
        [Description("O ID numérico do produto a ser buscado.")] int id)
    {
        try
        {
            var produto = await _httpClient.GetFromJsonAsync<Produto>($"/produtos/{id}");
            return produto;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            Console.Error.WriteLine($"Produto com ID {id} não encontrado.");
            return null;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Erro ao buscar produto por ID: {ex.Message}");
            return null;
        }
    }
}
