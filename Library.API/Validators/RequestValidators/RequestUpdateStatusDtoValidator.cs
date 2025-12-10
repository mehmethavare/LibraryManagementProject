using FluentValidation;
using Library.API.Dtos.RequestDtos;
using Library.API.Entities;

namespace Library.API.Validators.RequestValidators
{
    public class RequestUpdateStatusDtoValidator : AbstractValidator<RequestUpdateStatusDto>
    {
        public RequestUpdateStatusDtoValidator()
        {
            RuleFor(x => x.Status)
                .IsInEnum().WithMessage("Geçersiz istek durumu.");

            // Eğer Resolved veya Rejected ise admin cevabı boş olmasın (istersen)
            RuleFor(x => x.AdminResponse)
                .NotEmpty().WithMessage("Bu durum için admin mesajı boş olamaz.")
                .When(x => x.Status == RequestStatus.Resolved || x.Status == RequestStatus.Rejected)
                .MaximumLength(2000).WithMessage("Admin cevabı en fazla 2000 karakter olabilir.");
        }
    }
}
