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
            _logger.LogInformation("📘 BookReturnBackgroundService started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                        var overdueRecords = await context.BorrowRecords
                            .Include(x => x.Book)
                            .Where(x =>
                                !x.IsReturned &&
                                x.ReturnDate < DateTime.Now)
                            .ToListAsync(stoppingToken);

                        foreach (var record in overdueRecords)
                        {
                            record.IsReturned = true;
                            record.Book!.Status = BookStatus.Available;
                            record.Book.ReturnedAt = DateTime.Now;

                            _logger.LogInformation($"📗 Book '{record.Book.Title}' automatically returned.");
                        }

                        if (overdueRecords.Any())
                        {
                            await context.SaveChangesAsync(stoppingToken);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during automatic return check.");
                }

                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }
    }
}
