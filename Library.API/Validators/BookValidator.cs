using FluentValidation;
using Library.API.Dtos.BookDtos;

namespace Library.API.Validators
{
    public class BookCreateValidator : AbstractValidator<BookCreateDto>
    {
        public BookCreateValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Book title is required.")
                .MaximumLength(200);

            RuleFor(x => x.PublishYear)
                .GreaterThan(0).When(x => x.PublishYear.HasValue)
                .WithMessage("Publish year must be greater than 0.");
        }
    }

    public class BookUpdateValidator : AbstractValidator<BookUpdateDto>
    {
        public BookUpdateValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Book title cannot be empty.");
        }
    }
}
