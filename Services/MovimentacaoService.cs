using CompraVendas.Data;
using CompraVendas.DTOs;
using CompraVendas.Entities;
using CompraVendas.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace CompraVendas.Services;

/// <summary>RF17 (registrar), RF18 (consultar) e RF19 (situação do pagamento).</summary>
public class MovimentacaoService(CompraVendasDbContext db)
{
    public async Task<List<MovimentacaoResponse>> ListarAsync(MovimentacaoFiltro f, CancellationToken ct)
    {
        var q = db.Movimentacoes.AsNoTracking().AsQueryable();
        if (f.Tipo is { } t) q = q.Where(m => m.Tipo == t);
        if (f.Situacao is { } s) q = q.Where(m => m.Situacao == s);
        if (f.Origem is { } o) q = q.Where(m => m.Origem == o);
        if (f.De is { } de) q = q.Where(m => m.Data >= de);
        if (f.Ate is { } ate) q = q.Where(m => m.Data <= ate);

        var lista = await q.OrderByDescending(m => m.Data).ThenByDescending(m => m.Id).ToListAsync(ct);
        return lista.Select(MovimentacaoResponse.De).ToList();
    }

    public async Task<MovimentacaoResponse> ObterAsync(int id, CancellationToken ct) =>
        MovimentacaoResponse.De(await BuscarAsync(id, ct));

    public async Task<MovimentacaoResponse> CriarManualAsync(MovimentacaoRequest req, CancellationToken ct)
    {
        var m = new Movimentacao { Origem = OrigemMovimentacao.Manual, Situacao = SituacaoPagamento.Pendente };
        Aplicar(m, req);
        db.Movimentacoes.Add(m);
        await db.SaveChangesAsync(ct);
        return MovimentacaoResponse.De(m);
    }

    public async Task<MovimentacaoResponse> AtualizarManualAsync(int id, MovimentacaoRequest req, CancellationToken ct)
    {
        var m = await BuscarAsync(id, ct);
        GarantirManual(m);
        Aplicar(m, req);
        await db.SaveChangesAsync(ct);
        return MovimentacaoResponse.De(m);
    }

    public async Task ExcluirManualAsync(int id, CancellationToken ct)
    {
        var m = await BuscarAsync(id, ct);
        GarantirManual(m);
        db.Movimentacoes.Remove(m);
        await db.SaveChangesAsync(ct);
    }

    /// <summary>RF19: Pendente → Realizado.</summary>
    public async Task<MovimentacaoResponse> RegistrarPagamentoAsync(int id, PagamentoRequest req, CancellationToken ct)
    {
        var m = await BuscarAsync(id, ct);
        if (m.Situacao == SituacaoPagamento.Realizado)
            throw new ConflitoException("Pagamento já foi realizado.");
        m.Situacao = SituacaoPagamento.Realizado;
        m.DataPagamento = req.DataPagamento ?? DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return MovimentacaoResponse.De(m);
    }

    public async Task<ResumoFinanceiroResponse> ResumoAsync(DateTime? de, DateTime? ate, CancellationToken ct)
    {
        var q = db.Movimentacoes.AsNoTracking().AsQueryable();
        if (de is { } d) q = q.Where(m => m.Data >= d);
        if (ate is { } a) q = q.Where(m => m.Data <= a);

        // SQLite não suporta SUM em decimal no servidor: agrega em memória.
        var itens = await q.Select(m => new { m.Tipo, m.Situacao, m.Valor }).ToListAsync(ct);
        decimal Soma(TipoMovimentacao t, SituacaoPagamento s) =>
            itens.Where(i => i.Tipo == t && i.Situacao == s).Sum(i => i.Valor);

        var rec = Soma(TipoMovimentacao.Receita, SituacaoPagamento.Realizado);
        var desp = Soma(TipoMovimentacao.Despesa, SituacaoPagamento.Realizado);
        return new(rec, desp, rec - desp,
            Soma(TipoMovimentacao.Receita, SituacaoPagamento.Pendente),
            Soma(TipoMovimentacao.Despesa, SituacaoPagamento.Pendente));
    }

    private async Task<Movimentacao> BuscarAsync(int id, CancellationToken ct) =>
        await db.Movimentacoes.FindAsync([id], ct)
        ?? throw new NaoEncontradoException($"Movimentação {id} não encontrada.");

    private static void GarantirManual(Movimentacao m)
    {
        if (m.Origem != OrigemMovimentacao.Manual)
            throw new NegocioException("Movimentações geradas por compra/venda não podem ser alteradas ou excluídas manualmente.");
    }

    private static void Aplicar(Movimentacao m, MovimentacaoRequest r)
    {
        m.Tipo = r.Tipo;
        m.Valor = r.Valor;
        m.Descricao = r.Descricao.Trim();
        m.Data = r.Data ?? DateTime.UtcNow;
        m.DataVencimento = r.DataVencimento;
    }
}
