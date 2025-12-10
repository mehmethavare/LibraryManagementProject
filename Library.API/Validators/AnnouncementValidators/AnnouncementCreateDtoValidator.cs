using FluentValidation;
using Library.API.Dtos.AnnouncementDtos;

namespace Library.API.Validators.AnnouncementValidators
{
    public class AnnouncementCreateDtoValidator : AbstractValidator<AnnouncementCreateDto>
    {
        public AnnouncementCreateDtoValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Başlık boş olamaz.")
                .MaximumLength(200).WithMessage("Başlık en fazla 200 karakter olabilir.");

            RuleFor(x => x.Content)
                .NotEmpty().WithMessage("İçerik boş olamaz.")
                .MaximumLength(4000).WithMessage("İçerik en fazla 4000 karakter olabilir.");
        }
    }
}
