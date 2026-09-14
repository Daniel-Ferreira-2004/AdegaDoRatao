using AdegaDoRatao.Application.DTOs;
using FluentValidation;

namespace AdegaDoRatao.Application.Validators;

public sealed class NamedEntityRequestValidator : AbstractValidator<NamedEntityRequest>
{
    public NamedEntityRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}
