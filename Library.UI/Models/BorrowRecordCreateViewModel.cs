using System;

namespace Library.UI.Models
{
    public class BorrowRecordCreateViewModel
    {
        public int Id { get; set; }
        public int BookId { get; set; }
        public int UserId { get; set; }
        public DateTime BorrowDate { get; set; }
    }
}