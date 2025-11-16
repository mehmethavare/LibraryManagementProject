using FluentValidation;
using Library.API.Dtos.BorrowRecordDtos;

namespace Library.API.Validators
{
    public class BorrowRecordCreateValidator : AbstractValidator<BorrowRecordCreateDto>
    {
        public BorrowRecordCreateValidator()
        {
            RuleFor(x => x.BookId)
                .GreaterThan(0).WithMessage("BookId is required.");
        }
    }
}
