using Masarak.Application.Interfaces;
using Masarak.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Masarak.Infrastructure.Persistence.Repositories
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly Context _context;

        public PaymentRepository(Context context)
        {
            _context = context;
        }

        public async Task AddAsync(Payment payment, CancellationToken ct = default)
        {
            await _context.Payments.AddAsync(payment, ct);
            await _context.SaveChangesAsync(ct);
        }

        public async Task UpdateAsync(Payment payment, CancellationToken ct = default)
        {
            _context.Payments.Update(payment);
            await _context.SaveChangesAsync(ct);
        }

        public async Task<Payment?> GetByStripePaymentIntentIdAsync(string paymentIntentId, CancellationToken ct = default)
        {
            return await _context.Payments
                .FirstOrDefaultAsync(p => p.StripePaymentIntentId == paymentIntentId, ct);
        }
    }
}
