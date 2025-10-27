using FluentValidation;
using Library.API.Dtos.BorrowRecordDtos;

namespace Library.API.Validators
{
    public class BorrowRecordCreateValidator : AbstractValidator<BorrowRecordCreateDto>
    {
        public BorrowRecordCreateValidator()
        {
            RuleFor(x => x.UserId)
                .GreaterThan(0).WithMessage("UserId is required.");

            RuleFor(x => x.BookId)
                .GreaterThan(0).WithMessage("BookId is required.");
        }
    }
}
