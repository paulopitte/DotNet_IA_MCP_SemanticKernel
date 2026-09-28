using ApiProdutos.Entities;
using Microsoft.AspNetCore.Mvc;

namespace APIProdutos.Controllers;

[ApiController]
[Route("[controller]")]
public class ProdutosController : ControllerBase
{
    // Usando uma lista estática para simular um banco de dados.
    private static readonly List<Produto> _produtos = new()
    {
        new Produto { Id = 1, Nome = "Laptop Gamer", Preco = 7500.00m, Estoque = 15 },
        new Produto { Id = 2, Nome = "Mouse sem Fio", Preco = 150.00m, Estoque = 120 },
        new Produto { Id = 3, Nome = "Teclado Mecânico", Preco = 450.00m, Estoque = 75 },
        new Produto { Id = 4, Nome = "Monitor 17 polegadas", Preco = 575.00m, Estoque = 25 }
    };

    [HttpGet(Name = "GetProdutos")]
    public IEnumerable<Produto> Get()
    {
        return _produtos;
    }

    [HttpGet("{id}", Name = "GetProdutoPorId")]
    public ActionResult<Produto> Get(int id)
    {
        var produto = _produtos.FirstOrDefault(p => p.Id == id);
        if (produto == null)
        {
            return NotFound();
        }
        return produto;
    }
}
