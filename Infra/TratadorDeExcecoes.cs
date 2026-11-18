using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace CompraVendas.Infra;

public class TratadorDeExcecoes(ILogger<TratadorDeExcecoes> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext ctx, Exception ex, CancellationToken ct)
    {
        var (status, titulo) = ex switch
        {
            NaoEncontradoException => (404, "Recurso não encontrado"),
            ConflitoException => (409, "Conflito"),
            NegocioException => (422, "Regra de negócio violada"),
            ServicoIndisponivelException => (503, "Serviço dependente indisponível"),
            _ => (500, "Erro interno")
        };

        if (status >= 500) logger.LogError(ex, "Erro não tratado");

        ctx.Response.StatusCode = status;
        await ctx.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = status,
            Title = titulo,
            Detail = status == 500 ? "Erro inesperado." : ex.Message
        }, ct);
        return true;
    }
}
