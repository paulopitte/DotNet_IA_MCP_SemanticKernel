using McpProdutosClient;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using ModelContextProtocol.Client;

Console.WriteLine("=== DEMO MCP + Semantic Kernel + Ollama ===");

// ------------------------------------------------------------
// 1. KERNEL → Ollama via conector OpenAI
// ------------------------------------------------------------
var builder = Kernel.CreateBuilder();

// Configuração CORRETA para Ollama
builder.Services.AddOpenAIChatCompletion(
    modelId: "llama3.1:latest",  // Use o modelo que você tem instalado
    apiKey: "sk-ollama",     // API key fictícia (Ollama não requer chave real)
    endpoint: new Uri("http://localhost:11434/v1") // Endpoint do Ollama
);

var kernel = builder.Build();

// ------------------------------------------------------------
// 2. CLIENTE MCP via STDIO
// ------------------------------------------------------------
Console.WriteLine("Conectando ao servidor MCP via Stdio...");

// AJUSTE ESTE CAMINHO para o seu projeto MCP
var mcpServerProject = @".\McpProdutos\McpProdutos.csproj";

McpClient mcpClient = null!;

try
{
    // Configuração do transporte STDIO
    var transport = new StdioClientTransport(
        new StdioClientTransportOptions
        {
            Command = "dotnet",
            Arguments = new List<string> { "run", "--project", mcpServerProject }
        });

    Console.WriteLine("Inicializando cliente MCP...");
    mcpClient = await McpClient.CreateAsync(transport);

    Console.WriteLine("✅ Cliente MCP conectado com sucesso!");

    // ------------------------------------------------------------
    // 3. LISTAR E REGISTRAR FERRAMENTAS
    // ------------------------------------------------------------
    Console.WriteLine("Carregando ferramentas MCP...");

    //var tools = await mcpClient.ListToolsAsync();

    //Console.WriteLine($"\n🔧 Ferramentas encontradas: {tools.Count()}");
    //foreach (var tool in tools)
    //{
    //    Console.WriteLine($"  - {tool.Name}: {tool.Description}");
    //}

    //// Registra TODAS as ferramentas no Kernel
    //foreach (var tool in tools)
    //{
    //    //var plugin = KernelPluginFactory.CreateFromObject(tool, tool.Name);
    //    var plugin = await mcpClient.CreateKernelPluginAsync(tool.Name);
    //    kernel.Plugins.Add(plugin);

    //}

    //Console.WriteLine("✅ Plugins MCP registrados no Kernel!");
    Console.WriteLine("Carregando ferramentas MCP...");

    var tools = await mcpClient.ListToolsAsync();

    Console.WriteLine($"\nFerramentas encontradas: {tools.Count()}");
    foreach (var t in tools)
    {
        Console.WriteLine($"  - {t.Name}: {t.Description}");
    }

    // REGISTRO CORRETO (manual, mas funciona 100%)
    foreach (var tool in tools)
    {
        var adapter = new McpToolAdapter(mcpClient, tool.Name);

        var function = KernelFunctionFactory.CreateFromMethod(
            (Func<string?, Task<string>>)adapter.ExecuteAsync,
            functionName: tool.Name,
            description: tool.Description
        );

        kernel.ImportPluginFromFunctions(tool.Name, new[] { function });
    }
    Console.WriteLine("✅ Plugins MCP registrados no Kernel!");

    // ------------------------------------------------------------
    // 4. CHAT COM TOOL-CALLING AUTOMÁTICO
    // ------------------------------------------------------------
    var chatService = kernel.GetRequiredService<IChatCompletionService>();
    var history = new ChatHistory();

    // System message melhorada
    history.AddSystemMessage(@"
    Você é um assistente especializado em produtos de uma loja.
    
    FERRAMENTAS DISPONÍVEIS:
    - listar_produtos        → Obtém a lista de todos os produtos disponíveis
    - buscar_produto_por_id  → Busca um produto específico pelo ID
    
    INSTRUÇÕES:
    1. Use as ferramentas para obter informações atualizadas sobre produtos
    2. Se o usuário pedir para listar produtos, use ListarProdutos
    3. Se o usuário mencionar um ID específico, use BuscarProdutoPorId
    4. Seja útil e forneça informações detalhadas sobre os produtos
    ");

    Console.WriteLine("\n💬 Digite suas perguntas sobre produtos (Enter vazio para sair):");
    Console.WriteLine("Exemplos:");
    Console.WriteLine("  - 'Mostre todos os produtos'");
    Console.WriteLine("  - 'Qual é o produto com ID 1?'");
    Console.WriteLine("  - 'Quais produtos vocês têm?'\n");

    while (true)
    {
        Console.Write("👤 Você: ");
        var input = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(input))
            break;

        history.AddUserMessage(input);

        try
        {
            var settings = new OpenAIPromptExecutionSettings
            {
                ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
                Temperature = 0.3,  // Baixa temperatura para mais precisão
                MaxTokens = 1000
            };

            Console.Write("\n🤖 Assistente: ");

            var result = await chatService.GetChatMessageContentAsync(
                history, settings, kernel);

            Console.WriteLine($"{result.Content}\n");
            history.Add(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Erro: {ex.Message}");

            // Log mais detalhado para debug
            if (ex.InnerException != null)
            {
                Console.WriteLine($"🔍 Detalhes: {ex.InnerException.Message}");
            }
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Erro crítico: {ex.Message}");
    Console.WriteLine($"🔍 StackTrace: {ex.StackTrace}");

    if (ex.InnerException != null)
    {
        Console.WriteLine($"🔍 Inner Exception: {ex.InnerException.Message}");
    }
}
finally
{
    if (mcpClient != null)
    {
        Console.WriteLine("\n🔌 Encerrando conexão MCP...");
        await mcpClient.DisposeAsync();
    }
}

Console.WriteLine("✅ Aplicação finalizada.");