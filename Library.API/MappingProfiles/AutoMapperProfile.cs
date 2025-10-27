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
            //Book
            CreateMap<Book, BookListDto>().ReverseMap();
            CreateMap<Book, BookCreateDto>().ReverseMap();
            CreateMap<Book, BookUpdateDto>().ReverseMap();

            //User
            CreateMap<User, UserListDto>().ReverseMap();
            CreateMap<User, UserCreateDto>().ReverseMap();
            CreateMap<User, UserUpdateDto>().ReverseMap();

            //BorrowRecord
            CreateMap<BorrowRecord, BorrowRecordCreateDto>().ReverseMap();
            CreateMap<BorrowRecord, BorrowRecordListDto>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => $"{src.User!.Name} {src.User!.Surname}"))
                .ForMember(dest => dest.BookTitle, opt => opt.MapFrom(src => src.Book!.Title));

            //BookReview
            CreateMap<BookReview, BookReviewCreateDto>().ReverseMap();
            CreateMap<BookReview, BookReviewListDto>()
                .ForMember(dest => dest.BookTitle, opt => opt.MapFrom(src => src.Book!.Title))
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => $"{src.User!.Name} {src.User!.Surname}"));
        }
    }
}
