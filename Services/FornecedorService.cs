using CompraVendas.Data;
using CompraVendas.DTOs;
using CompraVendas.Entities;
using CompraVendas.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace CompraVendas.Services;

public class FornecedorService(CompraVendasDbContext db)
{
    public async Task<List<FornecedorResponse>> ListarAsync(string? busca, CancellationToken ct)
    {
        var q = db.Fornecedores.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(busca))
            q = q.Where(f => f.Nome.Contains(busca) || f.CpfCnpj.Contains(busca));
        var lista = await q.OrderBy(f => f.Nome).ToListAsync(ct);
        return lista.Select(FornecedorResponse.De).ToList();
    }

    public async Task<FornecedorResponse> ObterAsync(int id, CancellationToken ct) =>
        FornecedorResponse.De(await BuscarAsync(id, ct));

    public async Task<FornecedorResponse> CriarAsync(FornecedorRequest req, CancellationToken ct)
    {
        await GarantirDocumentoUnicoAsync(req.CpfCnpj.Trim(), null, ct);
        var f = new Fornecedor();
        Aplicar(f, req);
        db.Fornecedores.Add(f);
        await db.SaveChangesAsync(ct);
        return FornecedorResponse.De(f);
    }

    public async Task<FornecedorResponse> AtualizarAsync(int id, FornecedorRequest req, CancellationToken ct)
    {
        var f = await BuscarAsync(id, ct);
        await GarantirDocumentoUnicoAsync(req.CpfCnpj.Trim(), id, ct);
        Aplicar(f, req);
        await db.SaveChangesAsync(ct);
        return FornecedorResponse.De(f);
    }

    public async Task ExcluirAsync(int id, CancellationToken ct)
    {
        var f = await BuscarAsync(id, ct);
        if (await db.Compras.AnyAsync(c => c.FornecedorId == id, ct))
            throw new ConflitoException("Fornecedor possui compras registradas e não pode ser excluído.");
        db.Fornecedores.Remove(f);
        await db.SaveChangesAsync(ct);
    }

    private async Task<Fornecedor> BuscarAsync(int id, CancellationToken ct) =>
        await db.Fornecedores.FindAsync([id], ct)
        ?? throw new NaoEncontradoException($"Fornecedor {id} não encontrado.");

    private async Task GarantirDocumentoUnicoAsync(string doc, int? ignorarId, CancellationToken ct)
    {
        if (await db.Fornecedores.AnyAsync(f => f.CpfCnpj == doc && f.Id != ignorarId, ct))
            throw new ConflitoException("Já existe fornecedor com este CPF/CNPJ.");
    }

    private static void Aplicar(Fornecedor f, FornecedorRequest r)
    {
        f.Nome = r.Nome.Trim();
        f.CpfCnpj = r.CpfCnpj.Trim();
        f.Telefone = r.Telefone;
        f.Email = r.Email;
        f.Endereco = r.Endereco;
    }
}
