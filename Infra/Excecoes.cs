namespace CompraVendas.Infra;

/// <summary>Violação de regra de negócio (HTTP 422).</summary>
public class NegocioException(string message) : Exception(message);
/// <summary>Conflito de estado/duplicidade (HTTP 409).</summary>
public class ConflitoException(string message) : Exception(message);
public class NaoEncontradoException(string message) : Exception(message);
/// <summary>Serviço dependente (Estoque) indisponível ou respondeu erro inesperado (HTTP 503).</summary>
public class ServicoIndisponivelException(string message, Exception? inner = null) : Exception(message, inner);
