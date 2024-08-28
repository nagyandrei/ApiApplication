using ApiApplication.Database.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System;

namespace ApiApplication.Database.Repositories.Abstractions
{
    public interface IReservationsRepository
    {
        Task<ReservationEntity> CreateAsync(ReservationEntity reservation, CancellationToken cancel);
        Task<ReservationEntity> GetAsync(Guid id, CancellationToken cancel);
        Task<IEnumerable<ReservationEntity>> GetAllAsync(CancellationToken cancel);
    }
}
