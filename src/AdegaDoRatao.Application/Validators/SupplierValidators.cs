using AdegaDoRatao.Application.DTOs;
using FluentValidation;
namespace AdegaDoRatao.Application.Validators;
public sealed class CreateSupplierRequestValidator : AbstractValidator<CreateSupplierRequest> { public CreateSupplierRequestValidator(){ RuleFor(x=>x.Name).NotEmpty().MaximumLength(150); RuleFor(x=>x.Email).EmailAddress().When(x=>!string.IsNullOrWhiteSpace(x.Email)); } }
public sealed class UpdateSupplierRequestValidator : AbstractValidator<UpdateSupplierRequest> { public UpdateSupplierRequestValidator(){ RuleFor(x=>x.Name).NotEmpty().MaximumLength(150); RuleFor(x=>x.Email).EmailAddress().When(x=>!string.IsNullOrWhiteSpace(x.Email)); } }
