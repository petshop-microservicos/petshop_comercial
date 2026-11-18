using System.Net;
using CompraVendas.Infra;

namespace CompraVendas.Services;

public class EstoqueClient(HttpClient http) : IEstoqueClient
{
    public async Task<ProdutoEstoque> ObterProdutoAsync(int produtoId, CancellationToken ct)
    {
        try
        {
            var resp = await http.GetAsync($"api/produtos/{produtoId}", ct);
            if (resp.StatusCode == HttpStatusCode.NotFound)
                throw new NaoEncontradoException($"Produto {produtoId} não encontrado no Estoque.");
            resp.EnsureSuccessStatusCode();
            return (await resp.Content.ReadFromJsonAsync<ProdutoEstoque>(ct))!;
        }
        catch (Exception ex) when ((ex is HttpRequestException or TaskCanceledException) && !ct.IsCancellationRequested)
        {
            throw new ServicoIndisponivelException("Serviço de Estoque indisponível.", ex);
        }
    }

    public async Task MovimentarLoteAsync(TipoMovimentoEstoque tipo, string origem, int origemId,
        IEnumerable<ItemEstoque> itens, CancellationToken ct)
    {
        try
        {
            var resp = await http.PostAsJsonAsync("api/estoque/movimentacoes/lote",
                new { tipo = tipo.ToString(), origem, origemId, itens }, ct);

            if (resp.StatusCode == HttpStatusCode.NotFound)
                throw new NaoEncontradoException("Produto inexistente no Estoque.");
            if (resp.StatusCode == HttpStatusCode.Conflict)
                throw new NegocioException("Estoque insuficiente para um ou mais itens.");
            resp.EnsureSuccessStatusCode();
        }
        catch (Exception ex) when ((ex is HttpRequestException or TaskCanceledException) && !ct.IsCancellationRequested)
        {
            throw new ServicoIndisponivelException("Serviço de Estoque indisponível.", ex);
        }
    }
}
