using AutoMapper;
using Library.API.Entities;
using Library.API.Dtos.BookDtos;
using Library.API.Dtos.UserDtos;
using Library.API.Dtos.BorrowRecordDtos;
using Library.API.Dtos.BookReviewDtos;

namespace Library.API.MappingProfiles
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            // 🔹 Book
            CreateMap<Book, BookListDto>().ReverseMap();
            CreateMap<Book, BookCreateDto>().ReverseMap();
            CreateMap<Book, BookUpdateDto>().ReverseMap();

            // 🔹 User
            CreateMap<User, UserListDto>().ReverseMap();
            CreateMap<User, UserCreateDto>().ReverseMap();
            CreateMap<User, UserUpdateDto>().ReverseMap();

            // 🔹 BorrowRecord
            // Artık Create için DTO'da sadece BookId var ve controller'da entity'yi manuel oluşturuyoruz.
            // Yine de ihtiyaç olursa diye DTO -> Entity mapping dursun:
            CreateMap<BorrowRecordCreateDto, BorrowRecord>();

            CreateMap<BorrowRecord, BorrowRecordListDto>()
                .ForMember(dest => dest.UserName,
                    opt => opt.MapFrom(src => $"{src.User!.Name} {src.User!.Surname}"))
                .ForMember(dest => dest.BookTitle,
                    opt => opt.MapFrom(src => src.Book!.Title));

            // 🔹 BookReview
            // Create sırasında DTO -> Entity mapping (UserId'yi yine controller'da JWT'den set ediyoruz)
            CreateMap<BookReviewCreateDto, BookReview>();

            // Update sırasında sadece Comment + Rating güncelliyoruz, BookId/UserId değiştirmiyoruz
            CreateMap<BookReviewUpdateDto, BookReview>();

            CreateMap<BookReview, BookReviewListDto>()
                .ForMember(dest => dest.BookTitle,
                    opt => opt.MapFrom(src => src.Book!.Title))
                .ForMember(dest => dest.UserName,
                    opt => opt.MapFrom(src => $"{src.User!.Name} {src.User!.Surname}"));
        }
    }
}
