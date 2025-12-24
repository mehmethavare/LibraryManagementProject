using System;

namespace Library.UI.Models
{
    public class BorrowRecordListViewModel
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string BookTitle { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public int BookId { get; set; }
        public DateTime BorrowDate { get; set; }
        public DateTime? ReturnDate { get; set; }
        public bool IsReturned { get; set; }
        public string? CoverImageUrl { get; set; }
        public int ReturnRequestStatus { get; set; }

    }
}