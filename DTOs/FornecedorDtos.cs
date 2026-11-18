using System.ComponentModel.DataAnnotations;
using CompraVendas.Entities;

namespace CompraVendas.DTOs;

public record FornecedorRequest(
    [Required, StringLength(150)] string Nome,
    [Required, StringLength(18)] string CpfCnpj,
    [StringLength(20)] string? Telefone,
    [EmailAddress, StringLength(150)] string? Email,
    [StringLength(250)] string? Endereco);

public record FornecedorResponse(int Id, string Nome, string CpfCnpj, string? Telefone, string? Email, string? Endereco)
{
    public static FornecedorResponse De(Fornecedor f) => new(f.Id, f.Nome, f.CpfCnpj, f.Telefone, f.Email, f.Endereco);
}
