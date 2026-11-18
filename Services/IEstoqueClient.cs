namespace CompraVendas.Services;

public record ProdutoEstoque(int Id, string Nome, decimal Preco, int QuantidadeAtual);
public record ItemEstoque(int ProdutoId, int Quantidade);

public enum TipoMovimentoEstoque { Entrada, Saida }

/// <summary>
/// Contrato HTTP esperado do serviço Estoque:
///   GET  /api/produtos/{id}                    -> 200 ProdutoEstoque | 404
///   POST /api/estoque/movimentacoes/lote       -> 204 | 404 (produto) | 409 (estoque insuficiente)
///        body: { tipo, origem, origemId, itens:[{produtoId, quantidade}] }
///   O lote é ATÔMICO no Estoque e IDEMPOTENTE por (tipo, origem, origemId).
/// </summary>
public interface IEstoqueClient
{
    Task<ProdutoEstoque> ObterProdutoAsync(int produtoId, CancellationToken ct);
    Task MovimentarLoteAsync(TipoMovimentoEstoque tipo, string origem, int origemId,
        IEnumerable<ItemEstoque> itens, CancellationToken ct);
}
