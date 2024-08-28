using ApiApplication.Database.Entities;
using ApiApplication.Database.Repositories.Abstractions;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System;
using Microsoft.EntityFrameworkCore;

namespace ApiApplication.Database.Repositories
{
    public class ReservationsRepository : IReservationsRepository
    {
        private readonly CinemaContext _context;

        public ReservationsRepository(CinemaContext context)
        {
            _context = context;
        }

        public async Task<ReservationEntity> CreateAsync(ReservationEntity reservation, CancellationToken cancel)
        {
            _context.Reservations.Add(reservation);
            await _context.SaveChangesAsync(cancel);
            return reservation;
        }

        public async Task<ReservationEntity> GetAsync(Guid id, CancellationToken cancel)
        {
            return await _context.Reservations
                .Include(r => r.Auditorium)
                .Include(r => r.Movie)
                .FirstOrDefaultAsync(r => r.Id == id, cancel);
        }

        public async Task<IEnumerable<ReservationEntity>> GetAllAsync(CancellationToken cancel)
        {
            return await _context.Reservations
                .Include(r => r.Auditorium)
                .Include(r => r.Movie)
                .ToListAsync(cancel);
        }
    }
}
