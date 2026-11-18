using CompraVendas.DTOs;
using CompraVendas.Services;
using Microsoft.AspNetCore.Mvc;

namespace CompraVendas.Controllers;

[ApiController]
[Route("api/vendas")]
public class VendasController(VendaService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<VendaResponse>>> Listar(
        [FromQuery] DateTime? de, [FromQuery] DateTime? ate, CancellationToken ct) =>
        Ok(await service.ListarAsync(de, ate, ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<VendaResponse>> Obter(int id, CancellationToken ct) =>
        Ok(await service.ObterAsync(id, ct));

    [HttpPost]
    public async Task<ActionResult<VendaResponse>> Registrar(VendaRequest req, CancellationToken ct)
    {
        var venda = await service.RegistrarAsync(req, ct);
        return CreatedAtAction(nameof(Obter), new { id = venda.Id }, venda);
    }
}
