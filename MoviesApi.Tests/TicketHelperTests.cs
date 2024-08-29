using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using ApiApplication.Database.Entities;
using ApiApplication.Helpers;
using static ApiApplication.Controllers.ReservationController;

namespace ApiApplication.Tests
{
    public class TicketHelperTests
    {
        [Fact]
        public void AreSeatsContiguous_ReturnsTrue_ForContiguousSeats()
        {
            var seats = new List<SeatEntity>
            {
                new SeatEntity { SeatNumber = 1, Row = 1 },
                new SeatEntity { SeatNumber = 2, Row = 1 },
                new SeatEntity { SeatNumber = 3, Row = 1 }
            };

            var result = TicketHelper.AreSeatsContiguous(seats);

            Assert.True(result);
        }

        [Fact]
        public void AreSeatsContiguous_ReturnsFalse_ForNonContiguousSeats()
        {
            var seats = new List<SeatEntity>
            {
                new SeatEntity { SeatNumber = 1, Row = 1 },
                new SeatEntity { SeatNumber = 3, Row = 1 }
            };

            var result = TicketHelper.AreSeatsContiguous(seats);

            Assert.False(result);
        }

        [Fact]
        public void AreSeatsContiguous_ReturnsFalse_ForDifferentRows()
        {
            var seats = new List<SeatEntity>
            {
                new SeatEntity { SeatNumber = 1, Row = 1 },
                new SeatEntity { SeatNumber = 2, Row = 2 }
            };

            var result = TicketHelper.AreSeatsContiguous(seats);

            Assert.False(result);
        }

        [Fact]
        public void AreSeatsContiguous_ReturnsFalse_IfSeatsIsNull()
        {
            var result = TicketHelper.AreSeatsContiguous(null);

            Assert.False(result);
        }

        [Fact]
        public void AreSeatsContiguous_ReturnsFalse_ForEmptyList()
        {
            var result = TicketHelper.AreSeatsContiguous(new List<SeatEntity>());

            Assert.False(result);
        }

        [Fact]
        public void CanReserveSeats_ReturnsTrue_WhenNoConflictingSeats()
        {
            var request = new ReserveSeatsRequest
            {
                Seats = new List<int> { 1, 2 }
            };

            var reservedTickets = new List<TicketEntity>
            {
                new TicketEntity
                {
                    CreatedTime = DateTime.Now.AddMinutes(-5),
                    Paid = false,
                    Seats = new List<SeatEntity>
                    {
                        new SeatEntity { SeatNumber = 3 },
                        new SeatEntity { SeatNumber = 4 }
                    }
                }
            };

            var result = TicketHelper.CanReserveSeats(request, reservedTickets, 10);

            Assert.True(result);
        }

        [Fact]
        public void CanReserveSeats_ReturnsFalse_WhenConflictingSeats()
        {
            var request = new ReserveSeatsRequest
            {
                Seats = new List<int> { 1, 2 }
            };

            var reservedTickets = new List<TicketEntity>
            {
                new TicketEntity
                {
                    CreatedTime = DateTime.Now.AddMinutes(-5),
                    Paid = false,
                    Seats = new List<SeatEntity>
                    {
                        new SeatEntity { SeatNumber = 1 },
                        new SeatEntity { SeatNumber = 2 }
                    }
                }
            };

            var result = TicketHelper.CanReserveSeats(request, reservedTickets, 10);

            Assert.False(result);
        }

        [Fact]
        public void CanConfirmReservation_ReturnsTrue_WhenNoConflictingSeats()
        {
            var ticket = new TicketEntity
            {
                Seats = new List<SeatEntity>
                {
                    new SeatEntity { SeatNumber = 1 },
                    new SeatEntity { SeatNumber = 2 }
                }
            };

            var paidTickets = new List<TicketEntity>
            {
                new TicketEntity
                {
                    Seats = new List<SeatEntity>
                    {
                        new SeatEntity { SeatNumber = 3 },
                        new SeatEntity { SeatNumber = 4 }
                    }
                }
            };

            var result = TicketHelper.CanConfirmReservation(ticket, paidTickets);

            Assert.True(result);
        }

        [Fact]
        public void CanConfirmReservation_ReturnsFalse_WhenConflictingSeats()
        {
            var ticket = new TicketEntity
            {
                Seats = new List<SeatEntity>
                {
                    new SeatEntity { SeatNumber = 1 },
                    new SeatEntity { SeatNumber = 2 }
                }
            };

            var paidTickets = new List<TicketEntity>
            {
                new TicketEntity
                {
                    Seats = new List<SeatEntity>
                    {
                        new SeatEntity { SeatNumber = 1 },
                        new SeatEntity { SeatNumber = 2 }
                    }
                }
            };

            var result = TicketHelper.CanConfirmReservation(ticket, paidTickets);

            Assert.False(result);
        }
    }
}
