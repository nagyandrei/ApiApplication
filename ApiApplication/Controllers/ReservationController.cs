using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ApiApplication.Database.Entities;
using ApiApplication.Database.Repositories.Abstractions;
using ApiApplication.Helpers;
using Microsoft.Extensions.Configuration;

namespace ApiApplication.Controllers
{
    [Route("api/reservation")]
    [ApiController]
    public class ReservationController : ControllerBase
    {
        private readonly ITicketsRepository _ticketsRepository;
        private readonly IShowtimesRepository _showtimesRepository;
        private readonly IAuditoriumsRepository _auditoriumsRepository;
        private readonly IReservationsRepository _reservationsRepository;
        private readonly int _reservationThresholdMinutes;

        public ReservationController(
            ITicketsRepository ticketsRepository,
            IShowtimesRepository showtimesRepository,
            IAuditoriumsRepository auditoriumsRepository,
            IReservationsRepository reservationsRepository,
            IConfiguration configuration)
        {
            _ticketsRepository = ticketsRepository;
            _showtimesRepository = showtimesRepository;
            _auditoriumsRepository = auditoriumsRepository;
            _reservationsRepository = reservationsRepository;
            _reservationThresholdMinutes = GenericHelper.TryParseValue(configuration["ApiSettings:ReservationThresholdMinutes"]);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetReservationById(Guid id, CancellationToken cancellationToken)
        {
            try
            {
                var reservation = await _reservationsRepository.GetAsync(id, cancellationToken);

                if (reservation == null)
                {
                    return NotFound("Reservation not found.");
                }

                var ticket = await _ticketsRepository.GetAsync(reservation.TicketId, cancellationToken);

                if (ticket == null)
                {
                    return NotFound("Ticket not found.");
                }

                if (ticket.CreatedTime.AddMinutes(_reservationThresholdMinutes) <= DateTime.Now)
                {
                    return Conflict($"Reservation found but it has expired. Reservation CreatedTime {ticket.CreatedTime}");
                }

                return Ok(new
                {
                    reservation.Id,
                    reservation.NoOfSeats,
                    reservation.AuditoriumId,
                    reservation.Movie.Title,
                    ticket.Seats
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost]
        public async Task<IActionResult> ReserveSeats([FromBody] ReserveSeatsRequest request, CancellationToken cancellationToken)
        {
            if (request == null || request.Seats == null || !request.Seats.Any())
            {
                return BadRequest("Invalid reservation request.");
            }

            try
            {
                var showtime = await _showtimesRepository.GetWithMoviesByIdAsync(request.ShowtimeId, cancellationToken);
                if (showtime == null)
                {
                    return NotFound("Showtime not found.");
                }

                var auditorium = await _auditoriumsRepository.GetAsync(showtime.AuditoriumId, cancellationToken);
                if (auditorium == null)
                {
                    return NotFound("Auditorium not found.");
                }

                var seats = auditorium.Seats
                    .Where(s => request.Seats.Contains(s.SeatNumber) && s.Row == request.SeatRow)
                    .ToList();

                if (seats.Count != request.Seats.Count)
                {
                    return NotFound("Some seats are not found.");
                }

                if (!TicketHelper.AreSeatsContiguous(seats))
                {
                    return BadRequest("Seats are not contiguous.");
                }

                var reservedTickets = await _ticketsRepository.GetEnrichedAsync(request.ShowtimeId, cancellationToken);

                if (!TicketHelper.CanReserveSeats(request,reservedTickets, _reservationThresholdMinutes))
                {
                    return Conflict("Some seats are already reserved or paid for.");
                }

                var ticket = await _ticketsRepository.CreateAsync(showtime, seats, cancellationToken);

                var reservation = new ReservationEntity
                {
                    Id = Guid.NewGuid(),
                    NoOfSeats = request.Seats.Count,
                    Auditorium = auditorium,
                    Movie = showtime.Movie,
                    CreatedTime = DateTime.Now,
                    TicketId = ticket.Id
                };

                await _reservationsRepository.CreateAsync(reservation, cancellationToken);

                return Ok(new
                {
                    message = $"Reservation successfully created, it is available for {_reservationThresholdMinutes} minutes.",
                    reservation.Id,
                    reservation.NoOfSeats,
                    reservation.AuditoriumId,
                    reservation.Movie.Title
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("confirm/{reservationId}")]
        public async Task<IActionResult> ConfirmReservation(Guid reservationId, CancellationToken cancellationToken)
        {
            try
            {
                var reservation = await _reservationsRepository.GetAsync(reservationId, cancellationToken);

                if (reservation == null)
                {
                    return NotFound("Reservation not found.");
                }

                var ticket = await _ticketsRepository.GetAsync(reservation.TicketId, cancellationToken);

                if (ticket == null)
                {
                    return NotFound("Ticket not found.");
                }

                if (DateTime.Now - reservation.CreatedTime > TimeSpan.FromMinutes(_reservationThresholdMinutes))
                {
                    return BadRequest("Reservation has expired.");
                }

                var auditoriumId = ticket.Seats.First().AuditoriumId;
                var paidTickets = await _ticketsRepository.GetPaidTicketsByAuditoriumAsync(auditoriumId, cancellationToken);

                if (!TicketHelper.CanConfirmReservation(ticket, paidTickets))
                {
                    return Conflict("Some seats are already bought.");
                }


                await _ticketsRepository.ConfirmPaymentAsync(ticket, cancellationToken);

                return Ok(new
                {
                    message = "Reservation confirmed successfully.",
                    ticket.Id,
                    ticket.Seats
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        public class ReserveSeatsRequest
        {
            public int ShowtimeId { get; set; }
            public int SeatRow { get; set; }
            public List<int> Seats { get; set; }
        }
    }
}
