using System.ComponentModel.DataAnnotations;
using CompraVendas.Entities;

namespace CompraVendas.DTOs;

public record ItemCompraRequest(
    [Range(1, int.MaxValue)] int ProdutoId,
    [Range(1, int.MaxValue)] int Quantidade,
    [Range(0.01, double.MaxValue)] decimal CustoUnitario);

public record CompraRequest(
    [Range(1, int.MaxValue)] int FornecedorId,
    [StringLength(500)] string? Observacao,
    [Required, MinLength(1)] List<ItemCompraRequest> Itens);

/// <summary>Define como a despesa será lançada no financeiro ao confirmar.</summary>
public record ConfirmarCompraRequest(bool PagamentoRealizado = true, DateTime? DataVencimento = null);

public record ItemCompraResponse(int ProdutoId, int Quantidade, decimal CustoUnitario, decimal Subtotal);

public record CompraResponse(
    int Id, int FornecedorId, string Fornecedor, string Status,
    DateTime DataCriacao, DateTime? DataConfirmacao, string? Observacao,
    decimal Total, List<ItemCompraResponse> Itens)
{
    public static CompraResponse De(Compra c) => new(
        c.Id, c.FornecedorId, c.Fornecedor.Nome, c.Status.ToString(),
        c.DataCriacao, c.DataConfirmacao, c.Observacao, c.Total,
        c.Itens.Select(i => new ItemCompraResponse(i.ProdutoId, i.Quantidade, i.CustoUnitario, i.Quantidade * i.CustoUnitario)).ToList());
}
