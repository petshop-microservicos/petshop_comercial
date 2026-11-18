namespace CompraVendas.Entities;

public class Venda
{
    public int Id { get; set; }
    public DateTime Data { get; set; } = DateTime.UtcNow;
    public string? Observacao { get; set; }
    public List<ItemVenda> Itens { get; set; } = [];
    public decimal Total => Itens.Sum(i => i.Quantidade * i.PrecoUnitario);
}

public class ItemVenda
{
    public int Id { get; set; }
    public int VendaId { get; set; }
    public int ProdutoId { get; set; }
    public string NomeProduto { get; set; } = "";   // snapshot do nome no momento da venda
    public int Quantidade { get; set; }
    public decimal PrecoUnitario { get; set; }      // snapshot do preço vindo do Estoque
}
