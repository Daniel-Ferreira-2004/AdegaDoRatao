using AdegaDoRatao.Application.DTOs;
using FluentValidation;

namespace AdegaDoRatao.Application.Validators;

public sealed class CreateSaleRequestValidator : AbstractValidator<CreateSaleRequest>
{
    public CreateSaleRequestValidator()
    {
        RuleFor(x => x.Items).NotEmpty();
        RuleFor(x => x.PaymentMethodId).NotEmpty();
        RuleFor(x => x.Discount).GreaterThanOrEqualTo(0);
        RuleForEach(x => x.Items).ChildRules(i => { i.RuleFor(x => x.ProductId).NotEmpty(); i.RuleFor(x => x.Quantity).GreaterThan(0); });
        RuleFor(x => x.Items).Must(i => i.Select(x => x.ProductId).Distinct().Count() == i.Count)
            .WithMessage("Um produto pode aparecer somente uma vez na venda.");
    }
}

public sealed class CreatePurchaseRequestValidator : AbstractValidator<CreatePurchaseRequest>
{
    public CreatePurchaseRequestValidator()
    {
        RuleFor(x => x.SupplierId).NotEmpty();
        RuleFor(x => x.PaymentMethodId).NotEmpty();
        RuleFor(x => x.Items).NotEmpty();
        RuleFor(x => x.Discount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Freight).GreaterThanOrEqualTo(0);
        RuleForEach(x => x.Items).ChildRules(i => { i.RuleFor(x => x.ProductId).NotEmpty(); i.RuleFor(x => x.Quantity).GreaterThan(0); i.RuleFor(x => x.UnitCost).GreaterThanOrEqualTo(0); });
        RuleFor(x => x.Items).Must(i => i.Select(x => x.ProductId).Distinct().Count() == i.Count)
            .WithMessage("Um produto pode aparecer somente uma vez na compra.");
    }
}
