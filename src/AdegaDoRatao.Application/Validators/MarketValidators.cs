using AdegaDoRatao.Application.DTOs;
using FluentValidation;

namespace AdegaDoRatao.Application.Validators;

/// <summary>
/// Valida a consulta de mercados próximos. O limite de raio (0,5 a 20 km)
/// evita abuso: raios maiores trariam o catálogo inteiro e anulariam o
/// propósito do filtro. Para alterar o teto, ajuste RaioMaximoKm.
/// </summary>
public sealed class BuscarMercadosProximosQueryValidator : AbstractValidator<BuscarMercadosProximosQuery>
{
    public const double RaioMinimoKm = 0.5;
    public const double RaioMaximoKm = 20.0;

    public BuscarMercadosProximosQueryValidator()
    {
        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90, 90)
            .WithMessage("A latitude deve estar entre -90 e 90.");

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180, 180)
            .WithMessage("A longitude deve estar entre -180 e 180.");

        RuleFor(x => x.RaioKm)
            .InclusiveBetween(RaioMinimoKm, RaioMaximoKm)
            .WithMessage($"O raio deve estar entre {RaioMinimoKm} km e {RaioMaximoKm} km.");
    }
}
