namespace Library.API.Entities
{
    public class BorrowRecord
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public int BookId { get; set; }

        public DateTime BorrowDate { get; set; } = DateTime.Now;
        public DateTime? ReturnDate { get; set; }
        public bool IsReturned { get; set; } = false;
        public ReturnRequestStatus ReturnRequestStatus { get; set; } = ReturnRequestStatus.None;

        // Navigation Properties
        public User? User { get; set; }
        public Book? Book { get; set; }
    }

    public enum ReturnRequestStatus
    {
        None = 0,    // henüz hiç iade isteği yok
        Pending = 1, // kullanıcı iade isteği açmış, admin bekleniyor
        Approved = 2,// admin onaylamış (kitap iade edilmiş)
        Rejected = 3 // admin reddetmiş (kullanıcıya uyarı gitmiş)
    }
}
