using ApiApplication.Database.Entities;
using Microsoft.EntityFrameworkCore.Internal;
using ProtoDefinitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using static ApiApplication.Controllers.ReservationController;

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

        public static bool CanReserveSeats(ReserveSeatsRequest request, IEnumerable<TicketEntity> reservedTickets, int reservationThreshold)
        {
            var activeReservedTickets = reservedTickets
                                .Where(ticket => ticket.CreatedTime.AddMinutes(reservationThreshold) >= DateTime.Now || ticket.Paid)
                                .ToList();

            var conflictingSeats = activeReservedTickets
                .SelectMany(ticket => ticket.Seats)
                .Where(seat => request.Seats.Contains(seat.SeatNumber))
                .ToList();

            return !conflictingSeats.Any();
        }

        public static bool CanConfirmReservation(TicketEntity ticket, IEnumerable<TicketEntity> paidTickets)
        {
            var conflictingSeats = paidTickets
                .SelectMany(t => t.Seats)
                .Where(seat => ticket.Seats.Select(s => s.SeatNumber).Contains(seat.SeatNumber))
                .ToList();

            return !conflictingSeats.Any();
        }
        
    }
}

