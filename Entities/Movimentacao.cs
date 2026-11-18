namespace CompraVendas.Entities;

public class Movimentacao
{
    public int Id { get; set; }
    public TipoMovimentacao Tipo { get; set; }
    public decimal Valor { get; set; }
    public string Descricao { get; set; } = "";
    public DateTime Data { get; set; } = DateTime.UtcNow;
    public DateTime? DataVencimento { get; set; }
    public DateTime? DataPagamento { get; set; }
    public SituacaoPagamento Situacao { get; set; } = SituacaoPagamento.Pendente;
    public OrigemMovimentacao Origem { get; set; } = OrigemMovimentacao.Manual;
    /// <summary>Id da Compra ou Venda que originou a movimentação (null se manual).</summary>
    public int? OrigemId { get; set; }
}
