using CompraVendas.DTOs;
using CompraVendas.Services;
using Microsoft.AspNetCore.Mvc;

namespace CompraVendas.Controllers;

[ApiController]
[Route("api/fornecedores")]
public class FornecedoresController(FornecedorService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<FornecedorResponse>>> Listar([FromQuery] string? busca, CancellationToken ct) =>
        Ok(await service.ListarAsync(busca, ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<FornecedorResponse>> Obter(int id, CancellationToken ct) =>
        Ok(await service.ObterAsync(id, ct));

    [HttpPost]
    public async Task<ActionResult<FornecedorResponse>> Criar(FornecedorRequest req, CancellationToken ct)
    {
        var criado = await service.CriarAsync(req, ct);
        return CreatedAtAction(nameof(Obter), new { id = criado.Id }, criado);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<FornecedorResponse>> Atualizar(int id, FornecedorRequest req, CancellationToken ct) =>
        Ok(await service.AtualizarAsync(id, req, ct));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Excluir(int id, CancellationToken ct)
    {
        await service.ExcluirAsync(id, ct);
        return NoContent();
    }
}
