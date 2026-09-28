using Microsoft.SemanticKernel;
using ModelContextProtocol.Client;
using System.ComponentModel;
using System.Text;
using System.Text.Json;

public class McpToolAdapter
{
    private readonly McpClient _client;
    private readonly string _toolName;

    public McpToolAdapter(McpClient client, string toolName)
    {
        _client = client;
        _toolName = toolName;
    }

    [KernelFunction, Description("Executa a ferramenta MCP pelo nome.")]
    public async Task<string> ExecuteAsync(
        [Description("Parâmetros JSON, se houver.")] string? parametersJson = null)
    {
        try
        {
            IReadOnlyDictionary<string, object?>? parameters = null;
            if (!string.IsNullOrWhiteSpace(parametersJson))
                parameters = JsonSerializer.Deserialize<Dictionary<string, object?>>(parametersJson);

            var result = await _client.CallToolAsync(_toolName, parameters);

            if (result is null)
                return "Sem resultado.";

            // ✅ StructuredContent → JSON legível
            if (result.StructuredContent != null)
            {
                var json = result.StructuredContent.ToJsonString(new JsonSerializerOptions { WriteIndented = true });

                try
                {
                    // tenta converter para lista de produtos
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.ValueKind == JsonValueKind.Array)
                    {
                        var sb = new StringBuilder();
                        foreach (var item in doc.RootElement.EnumerateArray())
                        {
                            var id = item.GetProperty("Id").GetInt32();
                            var nome = item.GetProperty("Nome").GetString();
                            var preco = item.GetProperty("Preco").GetDecimal();
                            var estoque = item.GetProperty("Estoque").GetInt32();
                            sb.AppendLine($"ID: {id} | {nome} - R${preco} (Estoque: {estoque})");
                        }
                        return sb.ToString();
                    }
                }
                catch { /* fallback */ }

                return json;
            }

            // ✅ Fallback: se vier em Content, mostra texto direto
            if (result.Content is not null && result.Content.Count > 0)
            {
                var textos = result.Content
                    .OfType<TextContent>() // ← Novo tipo correto!
                    .Select(t => t.Text)
                    .ToList();

                if (textos.Count > 0)
                    return string.Join(Environment.NewLine, textos);

                // se não for texto, exibe como JSON bruto
                return JsonSerializer.Serialize(result.Content, new JsonSerializerOptions { WriteIndented = true });
            }

            return "Sem resultado.";
        }
        catch (Exception ex)
        {
            return $"Erro ao executar a ferramenta '{_toolName}': {ex.Message}";
        }
    }
}
