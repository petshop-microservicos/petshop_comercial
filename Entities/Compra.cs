namespace CompraVendas.Entities;

public class Compra
{
    public int Id { get; set; }
    public int FornecedorId { get; set; }
    public Fornecedor Fornecedor { get; set; } = null!;
    public StatusCompra Status { get; set; } = StatusCompra.Pendente;
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
    public DateTime? DataConfirmacao { get; set; }
    public string? Observacao { get; set; }
    public List<ItemCompra> Itens { get; set; } = [];
    public decimal Total => Itens.Sum(i => i.Quantidade * i.CustoUnitario);
}

public class ItemCompra
{
    public int Id { get; set; }
    public int CompraId { get; set; }
    /// <summary>Referência lógica ao produto no serviço Estoque (sem FK entre bancos).</summary>
    public int ProdutoId { get; set; }
    public int Quantidade { get; set; }
    public decimal CustoUnitario { get; set; }
}
