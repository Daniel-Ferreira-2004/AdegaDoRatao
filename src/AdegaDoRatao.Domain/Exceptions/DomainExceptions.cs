namespace AdegaDoRatao.Domain.Exceptions;

/// <summary>
/// Lançada quando uma operação tenta deixar o estoque de um produto negativo
/// e o produto não permite estoque negativo (RN11).
/// </summary>
public sealed class EstoqueInsuficienteException : DomainException
{
    public EstoqueInsuficienteException(string nomeProduto, int estoqueAtual, int quantidadeSolicitada)
        : base($"Estoque insuficiente para o produto '{nomeProduto}'. " +
               $"Estoque atual: {estoqueAtual}, quantidade solicitada: {quantidadeSolicitada}.")
    {
    }
}

/// <summary>
/// Lançada ao tentar vender, comprar ou movimentar um produto inativo (RN03).
/// </summary>
public sealed class ProdutoInativoException : DomainException
{
    public ProdutoInativoException(string nomeProduto)
        : base($"O produto '{nomeProduto}' está inativo e não pode ser movimentado.")
    {
    }
}

/// <summary>
/// Lançada ao tentar associar um produto novo a uma categoria inativa (RN04).
/// </summary>
public sealed class CategoriaInativaException : DomainException
{
    public CategoriaInativaException(string nomeCategoria)
        : base($"A categoria '{nomeCategoria}' está inativa e não pode receber novos produtos.")
    {
    }
}

/// <summary>
/// Lançada quando um preço ou valor monetário informado é inválido (RN02/RN24).
/// </summary>
public sealed class ValorInvalidoException : DomainException
{
    public ValorInvalidoException(string mensagem) : base(mensagem)
    {
    }
}

/// <summary>
/// Lançada quando uma quantidade informada é inválida (deve ser maior que zero
/// em operações de movimento — RN34).
/// </summary>
public sealed class QuantidadeInvalidaException : DomainException
{
    public QuantidadeInvalidaException(string mensagem) : base(mensagem)
    {
    }
}

/// <summary>
/// Lançada ao tentar registrar um ajuste de estoque sem informar o motivo (RN12).
/// </summary>
public sealed class MotivoObrigatorioException : DomainException
{
    public MotivoObrigatorioException()
        : base("O motivo é obrigatório para um ajuste de estoque.")
    {
    }
}

/// <summary>
/// Lançada ao tentar confirmar/cancelar uma compra ou venda em um status
/// que não permite a operação (ex.: cancelar uma compra que ainda não foi
/// recebida, ou confirmar uma compra já cancelada).
/// </summary>
public sealed class TransicaoDeStatusInvalidaException : DomainException
{
    public TransicaoDeStatusInvalidaException(string mensagem) : base(mensagem)
    {
    }
}

/// <summary>
/// Lançada ao tentar cancelar uma compra cujos itens já foram parcialmente
/// consumidos por vendas (RN17), exigindo ajuste manual de estoque.
/// </summary>
public sealed class EstornoNaoPermitidoException : DomainException
{
    public EstornoNaoPermitidoException(string mensagem) : base(mensagem)
    {
    }
}
