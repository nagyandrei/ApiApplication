using ApiApplication.Database.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApiApplication.Helpers
{
    public static class TicketHelper
    {
        public static bool AreSeatsContiguous(List<SeatEntity> seats)
        {
            if (seats == null || seats.Count == 0)
            {
                return false;
            }

            if (seats.Select(s => s.Row).Distinct().Count() > 1)
            {
                return false;
            }

            seats = seats.OrderBy(s => s.SeatNumber).ToList();

            for (int i = 1; i < seats.Count; i++)
            {
                if (seats[i].SeatNumber != seats[i - 1].SeatNumber + 1)
                {
                    return false;
                }
            }

            return true;
        }

        public static bool TicketsCanBeBought(TicketEntity ticket, IEnumerable<TicketEntity> paidTickets)
        {
            var paidSeats = paidTickets
                            .SelectMany(t => t.Seats)
                            .Select(seat => new { seat.Row, seat.SeatNumber })
                            .ToList();

            var reservedSeats = ticket.Seats
                .Select(seat => new { seat.Row, seat.SeatNumber })
                .ToList();

            return !reservedSeats
                .Any(reservedSeat => paidSeats
                    .Any(paidSeat => reservedSeat.Row == paidSeat.Row && reservedSeat.SeatNumber == paidSeat.SeatNumber));
        }

        public static bool CanTicketBeBought(TicketEntity ticket)
        {
            if (ticket == null) throw new ArgumentNullException(nameof(ticket));
            return ticket.Paid || (DateTime.UtcNow - ticket.CreatedTime).TotalMinutes < 10;
        }
    }
}

