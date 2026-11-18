using CompraVendas.DTOs;
using CompraVendas.Entities;
using CompraVendas.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace CompraVendas.Controllers;

[ApiController]
[Route("api/compras")]
public class ComprasController(CompraService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<CompraResponse>>> Listar(
        [FromQuery] StatusCompra? status, [FromQuery] int? fornecedorId, CancellationToken ct) =>
        Ok(await service.ListarAsync(status, fornecedorId, ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CompraResponse>> Obter(int id, CancellationToken ct) =>
        Ok(await service.ObterAsync(id, ct));

    [HttpPost]
    public async Task<ActionResult<CompraResponse>> Criar(CompraRequest req, CancellationToken ct)
    {
        var criada = await service.CriarAsync(req, ct);
        return CreatedAtAction(nameof(Obter), new { id = criada.Id }, criada);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<CompraResponse>> Atualizar(int id, CompraRequest req, CancellationToken ct) =>
        Ok(await service.AtualizarAsync(id, req, ct));

    [HttpPost("{id:int}/confirmar")]
    public async Task<ActionResult<CompraResponse>> Confirmar(
        int id, [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] ConfirmarCompraRequest? req,
        CancellationToken ct) =>
        Ok(await service.ConfirmarAsync(id, req, ct));

    [HttpPost("{id:int}/cancelar")]
    public async Task<ActionResult<CompraResponse>> Cancelar(int id, CancellationToken ct) =>
        Ok(await service.CancelarAsync(id, ct));
}
