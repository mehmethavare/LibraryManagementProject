using Library.API.Context;
using Library.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace Library.API.Services
{
    public class BookReturnBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<BookReturnBackgroundService> _logger;

        public BookReturnBackgroundService(IServiceProvider serviceProvider, ILogger<BookReturnBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("BookReturnBackgroundService started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                        // 🔹 Süresi geçmiş, hâlâ iade edilmemiş kayıtlar
                        var overdueRecords = await context.BorrowRecords
                            .Include(x => x.Book)
                            .Include(x => x.User)
                            .Where(x =>
                                !x.IsReturned &&
                                x.ReturnDate < DateTime.Now)
                            .ToListAsync(stoppingToken);

                        if (overdueRecords.Any())
                        {
                            _logger.LogInformation("{Count} adet gecikmiş ödünç kaydı bulundu.", overdueRecords.Count);

                            // 🔹 1) Kitapları otomatik iade et
                            foreach (var record in overdueRecords)
                            {
                                if (record.Book != null)
                                {
                                    record.IsReturned = true;
                                    record.Book.Status = BookStatus.Available;
                                    record.Book.ReturnedAt = DateTime.Now;

                                    _logger.LogInformation("Book '{Title}' automatically returned (BorrowRecordId: {Id}).",
                                        record.Book.Title, record.Id);
                                }
                            }

                            // 🔹 2) Kullanıcı bazında uyarı / kilit / silme işlemleri
                            // Her kullanıcı için bu döngüde sadece 1 uyarı veriyoruz (birden fazla geç kitap olsa bile)
                            var groupedByUser = overdueRecords
                                .Where(r => r.User != null)
                                .GroupBy(r => r.UserId);

                            foreach (var group in groupedByUser)
                            {
                                var user = group.First().User!;
                                if (user.IsDeleted)
                                {
                                    // Zaten silinmiş hesabın uyarı durumunu değiştirmiyoruz
                                    continue;
                                }

                                user.WarningCount++;

                                _logger.LogInformation("User {UserId} için uyarı sayısı {WarningCount} oldu.",
                                    user.Id, user.WarningCount);

                                // 2. uyarıda hesap kilitlenir
                                if (user.WarningCount == 2)
                                {
                                    user.IsLocked = true;
                                    _logger.LogWarning("User {UserId} hesabı 2. uyarı sonrası kilitlendi.", user.Id);
                                }
                                // 3. uyarıda hesap silinmiş sayılır (soft delete)
                                else if (user.WarningCount >= 3)
                                {
                                    user.IsLocked = true;
                                    user.IsDeleted = true;
                                    _logger.LogWarning("User {UserId} hesabı 3. uyarı sonrası silinmiş olarak işaretlendi.", user.Id);
                                }
                            }

                            await context.SaveChangesAsync(stoppingToken);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during automatic return & warning check.");
                }

                // Her saat başı kontrol et
                //await Task.Delay(TimeSpan.FromHours(1), stoppingToken);    
                await Task.Delay(TimeSpan.FromSeconds(59), stoppingToken);            //TEST İÇİN SÜREYİ KISALTTIM

            }
        }
    }
}
