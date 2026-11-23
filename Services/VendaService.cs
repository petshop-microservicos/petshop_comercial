using CompraVendas.Data;
using CompraVendas.DTOs;
using CompraVendas.Entities;
using CompraVendas.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace CompraVendas.Services;

public class VendaService(CompraVendasDbContext db, IEstoqueClient estoque, ILogger<VendaService> logger)
{
    public async Task<List<VendaResponse>> ListarAsync(DateTime? de, DateTime? ate, CancellationToken ct)
    {
        var q = db.Vendas.AsNoTracking().Include(v => v.Itens).AsQueryable();
        if (de is { } d) q = q.Where(v => v.Data >= d);
        if (ate is { } a) q = q.Where(v => v.Data <= a);
        var lista = await q.OrderByDescending(v => v.Data).ToListAsync(ct);
        return lista.Select(VendaResponse.De).ToList();
    }

    public async Task<VendaResponse> ObterAsync(int id, CancellationToken ct)
    {
        var v = await db.Vendas.AsNoTracking().Include(x => x.Itens).FirstOrDefaultAsync(x => x.Id == id, ct)
                ?? throw new NaoEncontradoException($"Venda {id} não encontrada.");
        return VendaResponse.De(v);
    }

    /// <summary>
    /// RN13: só registra se TODOS os itens têm estoque suficiente.
    /// RN14: registrar gera saída no estoque (lote atômico no Estoque) + receita no financeiro.
    /// Mesma estratégia da confirmação de compra: gravar local sem commit → Estoque → commit.
    /// </summary>
    public async Task<VendaResponse> RegistrarAsync(VendaRequest req, CancellationToken ct)
    {
        var pedidos = req.Itens.GroupBy(i => i.ProdutoId)
                               .Select(g => new ItemEstoque(g.Key, g.Sum(x => x.Quantidade))).ToList();

        // Pré-validação (RN13) com mensagem clara; a decisão final é a saída atômica no Estoque.
        var venda = new Venda { Observacao = req.Observacao, Data = DateTime.UtcNow };
        var faltantes = new List<string>();
        foreach (var p in pedidos)
        {
            var prod = await estoque.ObterProdutoAsync(p.ProdutoId, ct);
            if (prod.QuantidadeAtual < p.Quantidade)
                faltantes.Add($"{prod.Nome} (disponível {prod.QuantidadeAtual}, solicitado {p.Quantidade})");
            venda.Itens.Add(new ItemVenda
            {
                ProdutoId = prod.Id, NomeProduto = prod.Nome,
                Quantidade = p.Quantidade, PrecoUnitario = prod.Preco
            });
        }
        if (faltantes.Count > 0)
            throw new NegocioException("Estoque insuficiente: " + string.Join("; ", faltantes));

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        db.Vendas.Add(venda);
        await db.SaveChangesAsync(ct);   // gera venda.Id

        db.Movimentacoes.Add(new Movimentacao
        {
            Tipo = TipoMovimentacao.Receita,
            Valor = venda.Total,
            Descricao = $"Venda #{venda.Id}",
            Data = venda.Data,
            Situacao = req.PagamentoRealizado ? SituacaoPagamento.Realizado : SituacaoPagamento.Pendente,
            DataPagamento = req.PagamentoRealizado ? venda.Data : null,
            Origem = OrigemMovimentacao.Venda,
            OrigemId = venda.Id
        });
        await db.SaveChangesAsync(ct);

        try
        {
            await estoque.MovimentarLoteAsync(TipoMovimentoEstoque.Saida, "Venda", venda.Id, pedidos, ct);
        }
        catch
        {
            await tx.RollbackAsync(CancellationToken.None);   // venda não é registrada
            throw;
        }

        try
        {
            await tx.CommitAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha no commit da venda {Id}; estornando saída no Estoque.", venda.Id);
            try
            {
                await estoque.MovimentarLoteAsync(TipoMovimentoEstoque.Entrada, "EstornoVenda", venda.Id, pedidos, CancellationToken.None);
            }
            catch (Exception ex2)
            {
                logger.LogCritical(ex2, "FALHA NO ESTORNO da venda {Id} no Estoque. Reconciliação manual necessária.", venda.Id);
            }
            throw;
        }

        return VendaResponse.De(venda);
    }
}
