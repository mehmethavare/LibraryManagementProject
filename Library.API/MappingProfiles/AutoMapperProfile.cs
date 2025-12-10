using AutoMapper;
using Library.API.Dtos.AnnouncementDtos;
using Library.API.Dtos.BookDtos;
using Library.API.Dtos.BookReviewDtos;
using Library.API.Dtos.BorrowRecordDtos;
using Library.API.Dtos.RequestDtos;
using Library.API.Dtos.UserDtos;
using Library.API.Entities;

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
            // Create için DTO -> Entity mapping (UserId ve tarihleri controller'da handle ediyoruz)
            CreateMap<BorrowRecordCreateDto, BorrowRecord>();

            CreateMap<BorrowRecord, BorrowRecordListDto>()
                .ForMember(dest => dest.UserName,
                    opt => opt.MapFrom(src => $"{src.User!.Name} {src.User!.Surname}"))
                .ForMember(dest => dest.BookTitle,
                    opt => opt.MapFrom(src => src.Book!.Title));

            // 🔹 BookReview
            // Create sırasında DTO -> Entity (UserId controller'da JWT'den set ediliyor)
            CreateMap<BookReviewCreateDto, BookReview>();

            // Update sırasında sadece Comment + Rating güncelliyoruz
            CreateMap<BookReviewUpdateDto, BookReview>();

            CreateMap<BookReview, BookReviewListDto>()
                .ForMember(dest => dest.BookTitle,
                    opt => opt.MapFrom(src => src.Book!.Title))
                .ForMember(dest => dest.UserName,
                    opt => opt.MapFrom(src => $"{src.User!.Name} {src.User!.Surname}"));

            // 🔹 UserRequest (İstek Kutusu)

            // Kullanıcının oluşturduğu istek: DTO -> Entity
            CreateMap<RequestCreateDto, UserRequest>();

            // Liste ekranları için: Entity -> RequestListDto
            CreateMap<UserRequest, RequestListDto>()
                .ForMember(dest => dest.UserName,
                    opt => opt.MapFrom(src =>
                        src.User != null
                            ? src.User.Name + " " + src.User.Surname
                            : null));

            // Detay ekranı için: Entity -> RequestDetailDto
            CreateMap<UserRequest, RequestDetailDto>()
                .ForMember(dest => dest.UserName,
                    opt => opt.MapFrom(src =>
                        src.User != null
                            ? src.User.Name + " " + src.User.Surname
                            : string.Empty))
                .ForMember(dest => dest.UserEmail,
                    opt => opt.MapFrom(src =>
                        src.User != null
                            ? src.User.Email
                            : string.Empty));

            // 🔹 Announcement
            CreateMap<Announcement, AnnouncementCreateDto>().ReverseMap();
            CreateMap<Announcement, AnnouncementUpdateDto>().ReverseMap();
            CreateMap<Announcement, AnnouncementListDto>()
                .ForMember(dest => dest.CreatedByName,
                    opt => opt.MapFrom(src =>
                        src.CreatedByUser != null
                            ? src.CreatedByUser.Name + " " + src.CreatedByUser.Surname
                            : null));
        }
    }
}
