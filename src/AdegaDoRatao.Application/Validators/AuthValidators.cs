using AdegaDoRatao.Application.DTOs;
using FluentValidation;
namespace AdegaDoRatao.Application.Validators;
public sealed class LoginRequestValidator : AbstractValidator<LoginRequest> { public LoginRequestValidator() { RuleFor(x=>x.Email).NotEmpty().EmailAddress(); RuleFor(x=>x.Password).NotEmpty().MaximumLength(200); } }
