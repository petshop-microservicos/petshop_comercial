using Microsoft.AspNetCore.Diagnostics;

namespace CompraVendas.Exceptions;

public class ApiExceptionHandler(IProblemDetailsService problems) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext ctx, Exception ex, CancellationToken ct)
    {
        var status = ex switch
        {
            NaoEncontradoException => StatusCodes.Status404NotFound,
            NegocioException => StatusCodes.Status422UnprocessableEntity,
            ConflitoException => StatusCodes.Status409Conflict,
            ServicoIndisponivelException => StatusCodes.Status503ServiceUnavailable,
            _ => 0
        };
        if (status == 0) return false;

        ctx.Response.StatusCode = status;
        return await problems.TryWriteAsync(new()
        {
            HttpContext = ctx,
            Exception = ex,
            ProblemDetails = { Status = status, Detail = ex.Message }
        });
    }
}