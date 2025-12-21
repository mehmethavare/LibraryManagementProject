using System;

namespace Library.UI.Models
{
    public class BorrowRecordListViewModel
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string BookTitle { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
   
        public DateTime BorrowDate { get; set; }
        public DateTime? ReturnDate { get; set; }
        public bool IsReturned { get; set; }
        public string? CoverImageUrl { get; set; }
    }
}