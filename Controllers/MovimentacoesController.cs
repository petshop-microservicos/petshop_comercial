using CompraVendas.DTOs;
using CompraVendas.Services;
using Microsoft.AspNetCore.Mvc;

namespace CompraVendas.Controllers;

[ApiController]
[Route("api/movimentacoes")]
public class MovimentacoesController(MovimentacaoService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<MovimentacaoResponse>>> Listar([FromQuery] MovimentacaoFiltro filtro, CancellationToken ct) =>
        Ok(await service.ListarAsync(filtro, ct));

    [HttpGet("resumo")]
    public async Task<ActionResult<ResumoFinanceiroResponse>> Resumo(
        [FromQuery] DateTime? de, [FromQuery] DateTime? ate, CancellationToken ct) =>
        Ok(await service.ResumoAsync(de, ate, ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<MovimentacaoResponse>> Obter(int id, CancellationToken ct) =>
        Ok(await service.ObterAsync(id, ct));

    [HttpPost]
    public async Task<ActionResult<MovimentacaoResponse>> Criar(MovimentacaoRequest req, CancellationToken ct)
    {
        var criada = await service.CriarManualAsync(req, ct);
        return CreatedAtAction(nameof(Obter), new { id = criada.Id }, criada);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<MovimentacaoResponse>> Atualizar(int id, MovimentacaoRequest req, CancellationToken ct) =>
        Ok(await service.AtualizarManualAsync(id, req, ct));

    [HttpPatch("{id:int}/pagamento")]
    public async Task<ActionResult<MovimentacaoResponse>> RegistrarPagamento(int id, PagamentoRequest req, CancellationToken ct) =>
        Ok(await service.RegistrarPagamentoAsync(id, req, ct));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Excluir(int id, CancellationToken ct)
    {
        await service.ExcluirManualAsync(id, ct);
        return NoContent();
    }
}
