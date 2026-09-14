using AdegaDoRatao.Domain.Common;
using AdegaDoRatao.Domain.Exceptions;

namespace AdegaDoRatao.Domain.Entities;

/// <summary>
/// Produto vendido/comprado pela adega.
///
/// Esta é a entidade mais "rica" do domínio: concentra as regras de preço
/// (RN02) e de estoque (RN03, RN06, RN09, RN11) para que essas regras não
/// fiquem espalhadas em Services ou, pior, em Controllers (ver item 20 do
/// documento original: "lógica de estoque dentro de Controllers" é proibido).
///
/// IMPORTANTE sobre estoque: esta classe controla o valor de
/// <see cref="CurrentStock"/>, mas NÃO cria o registro de
/// <see cref="StockMovement"/> sozinha — quem orquestra isso é o
/// StockService/ProductService na camada de Application, que:
///   1. chama um dos métodos abaixo (ex.: <see cref="DarEntradaEmEstoque"/>);
///   2. usa o retorno (estoque anterior/novo) para montar o StockMovement;
///   3. salva as duas coisas na mesma transação.
/// Isso mantém a regra "todo estoque muda por movimentação" (RN09) sem
/// duplicar a validação de estoque negativo em dois lugares.
/// </summary>
public class Product : BaseAuditableEntity
{
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string Sku { get; private set; } = string.Empty;
    public string? Barcode { get; private set; }
    public Guid CategoryId { get; private set; }
    public Guid BrandId { get; private set; }
    public string UnitOfMeasure { get; private set; } = "UN";

    public decimal CostPrice { get; private set; }
    public decimal SalePrice { get; private set; }

    public int CurrentStock { get; private set; }
    public int MinStock { get; private set; }
    public int? MaxStock { get; private set; }

    /// <summary>
    /// Quando true, este produto pode ficar com estoque negativo (RN11).
    /// Configurável por produto, conforme decisão registrada em
    /// docs/02-REQUISITOS.md (seção 5).
    /// </summary>
    public bool AllowNegativeStock { get; private set; }

    /// <summary>
    /// Token de concorrência otimista (RNF11) — mapeado como `rowversion`
    /// no SQL Server pela Infrastructure. Evita que duas vendas simultâneas
    /// baixem o mesmo estoque de forma inconsistente.
    /// </summary>
    public byte[]? RowVersion { get; private set; }

    /// <summary>Estoque zerado (RN29).</summary>
    public bool SemEstoque => CurrentStock <= 0;

    /// <summary>Estoque positivo, mas abaixo (ou igual) ao mínimo (RN29).</summary>
    public bool EstoqueBaixo => CurrentStock > 0 && CurrentStock <= MinStock;

    private Product()
    {
    }

    public Product(
        string name,
        string? description,
        string sku,
        string? barcode,
        Guid categoryId,
        Guid brandId,
        string unitOfMeasure,
        decimal costPrice,
        decimal salePrice,
        int minStock,
        int? maxStock,
        bool allowNegativeStock = false)
    {
        SetName(name);
        Description = description?.Trim();
        SetSku(sku);
        SetBarcode(barcode);
        CategoryId = categoryId;
        BrandId = brandId;
        SetUnitOfMeasure(unitOfMeasure);
        SetPrices(costPrice, salePrice);
        SetLimitesDeEstoque(minStock, maxStock);
        AllowNegativeStock = allowNegativeStock;
        CurrentStock = 0; // todo produto nasce com estoque zero; entra por movimentação (RN09)
    }

    // ----------------------------------------------------------------
    // Dados cadastrais
    // ----------------------------------------------------------------

    public void AtualizarDadosCadastrais(
        string name,
        string? description,
        string unitOfMeasure,
        Guid categoryId,
        Guid brandId)
    {
        SetName(name);
        Description = description?.Trim();
        SetUnitOfMeasure(unitOfMeasure);
        CategoryId = categoryId;
        BrandId = brandId;
        MarcarComoAtualizada();
    }

    /// <summary>
    /// Altera os preços do produto (RN02: preços não podem ser negativos).
    /// Quem chama este método (ProductService) é responsável por gerar o
    /// registro de auditoria com o valor anterior/novo (RN05), pois a
    /// auditoria é uma preocupação de aplicação, não de domínio.
    /// </summary>
    public void AlterarPrecos(decimal costPrice, decimal salePrice)
    {
        SetPrices(costPrice, salePrice);
        MarcarComoAtualizada();
    }

    public void AlterarLimitesDeEstoque(int minStock, int? maxStock)
    {
        SetLimitesDeEstoque(minStock, maxStock);
        MarcarComoAtualizada();
    }

    public void PermitirEstoqueNegativo(bool permitir)
    {
        AllowNegativeStock = permitir;
        MarcarComoAtualizada();
    }

    // ----------------------------------------------------------------
    // Estoque (RN09/RN11) — ver observação na doc da classe
    // ----------------------------------------------------------------

    /// <summary>
    /// Dá entrada em estoque (ex.: recebimento de compra). Retorna o
    /// estoque anterior e o novo, para o chamador montar o StockMovement.
    /// </summary>
    public (int estoqueAnterior, int estoqueNovo) DarEntradaEmEstoque(int quantidade)
    {
        if (quantidade <= 0)
        {
            throw new QuantidadeInvalidaException("A quantidade de entrada em estoque deve ser maior que zero.");
        }

        var estoqueAnterior = CurrentStock;
        CurrentStock += quantidade;
        MarcarComoAtualizada();

        return (estoqueAnterior, CurrentStock);
    }

    /// <summary>
    /// Dá saída em estoque (ex.: venda). Valida RN03 (produto inativo não
    /// pode ser movimentado) e RN11 (estoque não pode ficar negativo, salvo
    /// permissão explícita).
    /// </summary>
    public (int estoqueAnterior, int estoqueNovo) DarSaidaEmEstoque(int quantidade)
    {
        if (quantidade <= 0)
        {
            throw new QuantidadeInvalidaException("A quantidade de saída em estoque deve ser maior que zero.");
        }

        if (!IsActive)
        {
            throw new ProdutoInativoException(Name);
        }

        var estoqueResultante = CurrentStock - quantidade;
        if (estoqueResultante < 0 && !AllowNegativeStock)
        {
            throw new EstoqueInsuficienteException(Name, CurrentStock, quantidade);
        }

        var estoqueAnterior = CurrentStock;
        CurrentStock = estoqueResultante;
        MarcarComoAtualizada();

        return (estoqueAnterior, CurrentStock);
    }

    /// <summary>
    /// Ajusta o estoque para um valor absoluto informado (ex.: contagem de
    /// inventário) ou estorno de uma venda/compra. Exige motivo (RN12).
    /// </summary>
    public (int estoqueAnterior, int estoqueNovo) AjustarEstoque(int novoEstoque, string motivo)
    {
        if (string.IsNullOrWhiteSpace(motivo))
        {
            throw new MotivoObrigatorioException();
        }

        if (novoEstoque < 0 && !AllowNegativeStock)
        {
            throw new EstoqueInsuficienteException(Name, CurrentStock, CurrentStock - novoEstoque);
        }

        var estoqueAnterior = CurrentStock;
        CurrentStock = novoEstoque;
        MarcarComoAtualizada();

        return (estoqueAnterior, CurrentStock);
    }

    // ----------------------------------------------------------------
    // Validações privadas (invariantes da entidade)
    // ----------------------------------------------------------------

    private void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("O nome do produto é obrigatório.", nameof(name));
        }

        Name = name.Trim();
    }

    private void SetSku(string sku)
    {
        if (string.IsNullOrWhiteSpace(sku))
        {
            throw new ArgumentException("O SKU do produto é obrigatório.", nameof(sku));
        }

        // Unicidade de SKU é uma regra que depende do banco (consultar outros
        // produtos), então é validada na camada de Application (RN01),
        // não aqui — o domínio só garante que o valor não é vazio.
        Sku = sku.Trim().ToUpperInvariant();
    }

    private void SetBarcode(string? barcode)
    {
        Barcode = string.IsNullOrWhiteSpace(barcode) ? null : barcode.Trim();
    }

    private void SetUnitOfMeasure(string unitOfMeasure)
    {
        if (string.IsNullOrWhiteSpace(unitOfMeasure))
        {
            throw new ArgumentException("A unidade de medida é obrigatória.", nameof(unitOfMeasure));
        }

        UnitOfMeasure = unitOfMeasure.Trim().ToUpperInvariant();
    }

    private void SetPrices(decimal costPrice, decimal salePrice)
    {
        if (costPrice < 0)
        {
            throw new ValorInvalidoException("O preço de custo não pode ser negativo.");
        }

        if (salePrice < 0)
        {
            throw new ValorInvalidoException("O preço de venda não pode ser negativo.");
        }

        CostPrice = costPrice;
        SalePrice = salePrice;
    }

    private void SetLimitesDeEstoque(int minStock, int? maxStock)
    {
        if (minStock < 0)
        {
            throw new ValorInvalidoException("O estoque mínimo não pode ser negativo.");
        }

        if (maxStock.HasValue && maxStock.Value < minStock)
        {
            throw new ValorInvalidoException("O estoque máximo não pode ser menor que o estoque mínimo.");
        }

        MinStock = minStock;
        MaxStock = maxStock;
    }
}
