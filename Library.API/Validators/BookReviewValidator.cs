using FluentValidation;
using Library.API.Dtos.BookReviewDtos;

namespace Library.API.Validators
{
    public class BookReviewCreateValidator : AbstractValidator<BookReviewCreateDto>
    {
        public BookReviewCreateValidator()
        {
            RuleFor(x => x.BookId).GreaterThan(0);
            RuleFor(x => x.UserId).GreaterThan(0);
            RuleFor(x => x.Comment)
                .NotEmpty().WithMessage("Comment cannot be empty.")
                .MaximumLength(1000);
            RuleFor(x => x.Rating)
                .InclusiveBetween(1, 5)
                .WithMessage("Rating must be between 1 and 5.");
        }
    }
}
