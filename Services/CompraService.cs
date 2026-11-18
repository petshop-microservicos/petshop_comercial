using CompraVendas.Data;
using CompraVendas.DTOs;
using CompraVendas.Entities;
using CompraVendas.Infra;
using Microsoft.EntityFrameworkCore;

namespace CompraVendas.Services;

public class CompraService(CompraVendasDbContext db, IEstoqueClient estoque, ILogger<CompraService> logger)
{
    public async Task<List<CompraResponse>> ListarAsync(StatusCompra? status, int? fornecedorId, CancellationToken ct)
    {
        var q = db.Compras.AsNoTracking().Include(c => c.Fornecedor).Include(c => c.Itens).AsQueryable();
        if (status is { } s) q = q.Where(c => c.Status == s);
        if (fornecedorId is { } f) q = q.Where(c => c.FornecedorId == f);
        var lista = await q.OrderByDescending(c => c.DataCriacao).ToListAsync(ct);
        return lista.Select(CompraResponse.De).ToList();
    }

    public async Task<CompraResponse> ObterAsync(int id, CancellationToken ct) =>
        CompraResponse.De(await CarregarAsync(id, rastrear: false, ct));

    // RN10: toda compra nasce Pendente. RN11: não toca em estoque nem financeiro.
    public async Task<CompraResponse> CriarAsync(CompraRequest req, CancellationToken ct)
    {
        await GarantirFornecedorAsync(req.FornecedorId, ct);
        await GarantirProdutosAsync(req.Itens.Select(i => i.ProdutoId), ct);

        var compra = new Compra { FornecedorId = req.FornecedorId, Observacao = req.Observacao, Status = StatusCompra.Pendente };
        compra.Itens.AddRange(Consolidar(req.Itens));
        db.Compras.Add(compra);
        await db.SaveChangesAsync(ct);
        return await ObterAsync(compra.Id, ct);
    }

    /// <summary>Só compras Pendentes podem ser editadas (Confirmada/Cancelada são imutáveis).</summary>
    public async Task<CompraResponse> AtualizarAsync(int id, CompraRequest req, CancellationToken ct)
    {
        var compra = await CarregarAsync(id, rastrear: true, ct);
        GarantirPendente(compra, "editada");
        await GarantirFornecedorAsync(req.FornecedorId, ct);
        await GarantirProdutosAsync(req.Itens.Select(i => i.ProdutoId), ct);

        compra.FornecedorId = req.FornecedorId;
        compra.Observacao = req.Observacao;
        db.ItensCompra.RemoveRange(compra.Itens);
        compra.Itens = Consolidar(req.Itens);
        await db.SaveChangesAsync(ct);
        return await ObterAsync(id, ct);
    }

    // RN10: Pendente → Cancelada. Não altera estoque (RN11).
    public async Task<CompraResponse> CancelarAsync(int id, CancellationToken ct)
    {
        var compra = await CarregarAsync(id, rastrear: true, ct);
        GarantirPendente(compra, "cancelada");
        compra.Status = StatusCompra.Cancelada;
        await db.SaveChangesAsync(ct);
        return CompraResponse.De(compra);
    }

    /// <summary>
    /// RN10/RN11/RN12: Pendente → Confirmada, com entrada no estoque + despesa no financeiro.
    /// Estratégia "tudo ou nada" do ponto de vista do usuário:
    ///  1) transação local: status + despesa gravados mas NÃO commitados;
    ///  2) chama o Estoque (lote atômico/idempotente); se falhar → rollback, compra segue Pendente;
    ///  3) commit; se o commit falhar após o Estoque ter aplicado → estorno compensatório no Estoque.
    /// </summary>
    public async Task<CompraResponse> ConfirmarAsync(int id, ConfirmarCompraRequest? req, CancellationToken ct)
    {
        req ??= new ConfirmarCompraRequest();
        var compra = await CarregarAsync(id, rastrear: false, ct);
        GarantirPendente(compra, "confirmada");
        if (compra.Itens.Count == 0) throw new NegocioException("Compra sem itens não pode ser confirmada.");

        var agora = DateTime.UtcNow;
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        // UPDATE condicional: se outra requisição já confirmou/cancelou, afeta 0 linhas.
        var afetadas = await db.Compras
            .Where(c => c.Id == id && c.Status == StatusCompra.Pendente)
            .ExecuteUpdateAsync(s => s
                .SetProperty(c => c.Status, StatusCompra.Confirmada)
                .SetProperty(c => c.DataConfirmacao, agora), ct);
        if (afetadas == 0) throw new ConflitoException("A compra mudou de status durante a confirmação.");

        db.Movimentacoes.Add(new Movimentacao
        {
            Tipo = TipoMovimentacao.Despesa,
            Valor = compra.Total,
            Descricao = $"Compra #{compra.Id} - {compra.Fornecedor.Nome}",
            Data = agora,
            DataVencimento = req.DataVencimento,
            Situacao = req.PagamentoRealizado ? SituacaoPagamento.Realizado : SituacaoPagamento.Pendente,
            DataPagamento = req.PagamentoRealizado ? agora : null,
            Origem = OrigemMovimentacao.Compra,
            OrigemId = compra.Id
        });
        await db.SaveChangesAsync(ct);

        var itensEstoque = compra.Itens.Select(i => new ItemEstoque(i.ProdutoId, i.Quantidade)).ToList();
        try
        {
            await estoque.MovimentarLoteAsync(TipoMovimentoEstoque.Entrada, "Compra", compra.Id, itensEstoque, ct);
        }
        catch
        {
            await tx.RollbackAsync(CancellationToken.None);   // compra permanece Pendente
            throw;
        }

        try
        {
            await tx.CommitAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha no commit da compra {Id}; estornando entrada no Estoque.", id);
            await EstornarEntradaAsync(compra.Id, itensEstoque);
            throw;
        }

        return await ObterAsync(id, CancellationToken.None);
    }

    private async Task EstornarEntradaAsync(int compraId, List<ItemEstoque> itens)
    {
        try
        {
            await estoque.MovimentarLoteAsync(TipoMovimentoEstoque.Saida, "EstornoCompra", compraId, itens, CancellationToken.None);
        }
        catch (Exception ex)
        {
            // Inconsistência residual: exige reconciliação manual/Outbox.
            logger.LogCritical(ex, "FALHA NO ESTORNO da compra {Id} no Estoque. Reconciliação manual necessária.", compraId);
        }
    }

    private async Task<Compra> CarregarAsync(int id, bool rastrear, CancellationToken ct)
    {
        var q = db.Compras.Include(c => c.Fornecedor).Include(c => c.Itens).AsQueryable();
        if (!rastrear) q = q.AsNoTracking();
        return await q.FirstOrDefaultAsync(c => c.Id == id, ct)
               ?? throw new NaoEncontradoException($"Compra {id} não encontrada.");
    }

    private static void GarantirPendente(Compra c, string acao)
    {
        if (c.Status != StatusCompra.Pendente)
            throw new ConflitoException($"Compra {c.Status} não pode ser {acao}. Apenas compras Pendentes.");
    }

    private async Task GarantirFornecedorAsync(int fornecedorId, CancellationToken ct)
    {
        if (!await db.Fornecedores.AnyAsync(f => f.Id == fornecedorId, ct))
            throw new NaoEncontradoException($"Fornecedor {fornecedorId} não encontrado.");
    }

    // Valida existência dos produtos no Estoque já na criação (falha cedo).
    private async Task GarantirProdutosAsync(IEnumerable<int> ids, CancellationToken ct)
    {
        foreach (var pid in ids.Distinct())
            await estoque.ObterProdutoAsync(pid, ct);
    }

    // Soma itens repetidos do mesmo produto e mesmo custo.
    private static List<ItemCompra> Consolidar(List<ItemCompraRequest> itens) =>
        itens.GroupBy(i => (i.ProdutoId, i.CustoUnitario))
             .Select(g => new ItemCompra { ProdutoId = g.Key.ProdutoId, CustoUnitario = g.Key.CustoUnitario, Quantidade = g.Sum(x => x.Quantidade) })
             .ToList();
}
