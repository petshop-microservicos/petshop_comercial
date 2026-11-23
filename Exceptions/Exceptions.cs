namespace CompraVendas.Exceptions;

public class NaoEncontradoException(string message) : Exception(message);
public class NegocioException(string message) : Exception(message);
public class ServicoIndisponivelException(string message, Exception inner) : Exception(message, inner);

public class ConflitoException(string message) : Exception(message);