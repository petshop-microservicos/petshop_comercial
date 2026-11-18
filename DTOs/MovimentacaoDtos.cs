using System.ComponentModel.DataAnnotations;
using CompraVendas.Entities;

namespace CompraVendas.DTOs;

/// <summary>Lançamento manual de receita/despesa (RF17).</summary>
public record MovimentacaoRequest(
    TipoMovimentacao Tipo,
    [Range(0.01, double.MaxValue)] decimal Valor,
    [Required, StringLength(250)] string Descricao,
    DateTime? Data,
    DateTime? DataVencimento);

public record PagamentoRequest(DateTime? DataPagamento);

public record MovimentacaoFiltro(
    TipoMovimentacao? Tipo,
    SituacaoPagamento? Situacao,
    OrigemMovimentacao? Origem,
    DateTime? De,
    DateTime? Ate);

public record MovimentacaoResponse(
    int Id, string Tipo, decimal Valor, string Descricao, DateTime Data,
    DateTime? DataVencimento, DateTime? DataPagamento, string Situacao, string Origem, int? OrigemId)
{
    public static MovimentacaoResponse De(Movimentacao m) => new(
        m.Id, m.Tipo.ToString(), m.Valor, m.Descricao, m.Data,
        m.DataVencimento, m.DataPagamento, m.Situacao.ToString(), m.Origem.ToString(), m.OrigemId);
}

public record ResumoFinanceiroResponse(
    decimal ReceitasRealizadas, decimal DespesasRealizadas, decimal Saldo,
    decimal AReceber, decimal APagar);
