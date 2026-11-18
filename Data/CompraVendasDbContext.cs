using CompraVendas.Entities;
using Microsoft.EntityFrameworkCore;

namespace CompraVendas.Data;

public class CompraVendasDbContext(DbContextOptions<CompraVendasDbContext> options) : DbContext(options)
{
    public DbSet<Fornecedor> Fornecedores => Set<Fornecedor>();
    public DbSet<Compra> Compras => Set<Compra>();
    public DbSet<ItemCompra> ItensCompra => Set<ItemCompra>();
    public DbSet<Venda> Vendas => Set<Venda>();
    public DbSet<ItemVenda> ItensVenda => Set<ItemVenda>();
    public DbSet<Movimentacao> Movimentacoes => Set<Movimentacao>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<Fornecedor>(e =>
        {
            e.Property(f => f.Nome).HasMaxLength(150).IsRequired();
            e.Property(f => f.CpfCnpj).HasMaxLength(18).IsRequired();
            e.HasIndex(f => f.CpfCnpj).IsUnique();
        });

        mb.Entity<Compra>(e =>
        {
            e.Property(c => c.Status).HasConversion<string>().HasMaxLength(20);
            e.Ignore(c => c.Total);
            // Fornecedor com compras não pode ser excluído
            e.HasOne(c => c.Fornecedor).WithMany().HasForeignKey(c => c.FornecedorId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(c => c.Itens).WithOne().HasForeignKey(i => i.CompraId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(c => c.Status);
        });
        mb.Entity<ItemCompra>().Property(i => i.CustoUnitario).HasPrecision(18, 2);

        mb.Entity<Venda>(e =>
        {
            e.Ignore(v => v.Total);
            e.HasMany(v => v.Itens).WithOne().HasForeignKey(i => i.VendaId).OnDelete(DeleteBehavior.Cascade);
        });
        mb.Entity<ItemVenda>().Property(i => i.PrecoUnitario).HasPrecision(18, 2);

        mb.Entity<Movimentacao>(e =>
        {
            e.Property(m => m.Valor).HasPrecision(18, 2);
            e.Property(m => m.Tipo).HasConversion<string>().HasMaxLength(10);
            e.Property(m => m.Situacao).HasConversion<string>().HasMaxLength(10);
            e.Property(m => m.Origem).HasConversion<string>().HasMaxLength(10);
            // Garante no máximo UM efeito financeiro por compra/venda (idempotência)
            e.HasIndex(m => new { m.Origem, m.OrigemId }).IsUnique().HasFilter("\"OrigemId\" IS NOT NULL");
            e.HasIndex(m => m.Data);
        });
    }
}
