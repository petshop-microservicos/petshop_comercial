using System.ComponentModel.DataAnnotations;
using CompraVendas.Entities;

namespace CompraVendas.DTOs;

public record ItemVendaRequest(
    [Range(1, int.MaxValue)] int ProdutoId,
    [Range(1, int.MaxValue)] int Quantidade);

/// <summary>O preço NÃO vem do cliente: é lido do Estoque (preço é atributo do produto).</summary>
public record VendaRequest(
    [StringLength(500)] string? Observacao,
    [Required, MinLength(1)] List<ItemVendaRequest> Itens,
    bool PagamentoRealizado = true);

public record ItemVendaResponse(int ProdutoId, string NomeProduto, int Quantidade, decimal PrecoUnitario, decimal Subtotal);

public record VendaResponse(int Id, DateTime Data, string? Observacao, decimal Total, List<ItemVendaResponse> Itens)
{
    public static VendaResponse De(Venda v) => new(
        v.Id, v.Data, v.Observacao, v.Total,
        v.Itens.Select(i => new ItemVendaResponse(i.ProdutoId, i.NomeProduto, i.Quantidade, i.PrecoUnitario, i.Quantidade * i.PrecoUnitario)).ToList());
}
