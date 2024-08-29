using ApiApplication.Controllers;
using ApiApplication.Database.Entities;
using ApiApplication.Database.Repositories.Abstractions;
using ApiApplication.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace MoviesApi.Tests
{
    public class ReservationControllerTests
    {
        private readonly ReservationController _controller;
        private readonly Mock<ITicketsRepository> _ticketsRepositoryMock = new();
        private readonly Mock<IShowtimesRepository> _showtimesRepositoryMock = new();
        private readonly Mock<IAuditoriumsRepository> _auditoriumsRepositoryMock = new();
        private readonly Mock<IReservationsRepository> _reservationsRepositoryMock = new();
        private readonly Mock<IConfiguration> _configurationMock = new();

        public ReservationControllerTests()
        {
            _configurationMock.SetupGet(c => c["ApiSettings:ReservationThresholdMinutes"]).Returns("10");
            var reservationThresholdMinutes = GenericHelper.TryParseValue(_configurationMock.Object["ApiSettings:ReservationThresholdMinutes"]);

            _controller = new ReservationController(
                _ticketsRepositoryMock.Object,
                _showtimesRepositoryMock.Object,
                _auditoriumsRepositoryMock.Object,
                _reservationsRepositoryMock.Object,
                _configurationMock.Object
            );
        }

        [Fact]
        public async Task GetReservationById_ReturnsOk_WhenReservationAndTicketExist()
        {
            var reservationId = Guid.NewGuid();
            var reservation = new ReservationEntity { Id = reservationId, CreatedTime = DateTime.Now , Movie = new MovieEntity { Title = "title"} };
            var ticket = new TicketEntity { Id = Guid.NewGuid(), CreatedTime = DateTime.Now, Seats = new List<SeatEntity>()  };

            _reservationsRepositoryMock.Setup(r => r.GetAsync(reservationId, It.IsAny<CancellationToken>())).ReturnsAsync(reservation);
            _ticketsRepositoryMock.Setup(t => t.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(ticket);

            var result = await _controller.GetReservationById(reservationId, CancellationToken.None);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var jsonResponse = JsonConvert.SerializeObject(okResult.Value);
            var responseObject = JObject.Parse(jsonResponse);

            Assert.Equal(reservationId.ToString(), responseObject["Id"].ToString());
            Assert.Equal("title", responseObject["Title"].ToString());
        }

        [Fact]
        public async Task GetReservationById_ReturnsNotFound_WhenReservationDoesNotExist()
        {
            var reservationId = Guid.NewGuid();
            _reservationsRepositoryMock.Setup(r => r.GetAsync(reservationId, It.IsAny<CancellationToken>())).ReturnsAsync((ReservationEntity)null);

            var result = await _controller.GetReservationById(reservationId, CancellationToken.None);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetReservationById_ReturnsNotFound_WhenShowtimeDoesNotExist()
        {
            var request = new ReservationController.ReserveSeatsRequest
            {
                ShowtimeId = 1,
                SeatRow = 1,
                Seats = new List<int> { 1, 2, 3 }
            };

            _showtimesRepositoryMock.Setup(r => r.GetWithMoviesByIdAsync(request.ShowtimeId, It.IsAny<CancellationToken>())).ReturnsAsync((ShowtimeEntity)null);

            var result = await _controller.ReserveSeats(request, CancellationToken.None);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task ReserveSeats_ReturnsNotFound_WhenAuditoriumDoesNotExist()
        {
            var request = new ReservationController.ReserveSeatsRequest
            {
                ShowtimeId = 1,
                SeatRow = 1,
                Seats = new List<int> { 1, 2, 3 }
            };
            var showtime = new ShowtimeEntity { AuditoriumId = 1, Movie = new MovieEntity { Title = "title" } };
            _showtimesRepositoryMock.Setup(s => s.GetWithMoviesByIdAsync(request.ShowtimeId, It.IsAny<CancellationToken>())).ReturnsAsync(showtime);
            _auditoriumsRepositoryMock.Setup(s => s.GetAsync(showtime.Id, It.IsAny<CancellationToken>())).ReturnsAsync((AuditoriumEntity)null);
            var result = await _controller.ReserveSeats(request, CancellationToken.None);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task ReserveSeats_ReturnsConflict_WhenAuditoriumSeatsAreNotFound()
        {
            var request = new ReservationController.ReserveSeatsRequest
            {
                ShowtimeId = 1,
                SeatRow = 1,
                Seats = new List<int> { 1, 2, 3 }
            };
            var auditorium = new AuditoriumEntity
            {
                Id = 1,
                Seats = new List<SeatEntity>(),
                Showtimes = new List<ShowtimeEntity>(),
            };

            var showtime = new ShowtimeEntity { AuditoriumId = 1, Movie = new MovieEntity { Title = "title" } };
            _showtimesRepositoryMock.Setup(s => s.GetWithMoviesByIdAsync(request.ShowtimeId, It.IsAny<CancellationToken>())).ReturnsAsync(showtime);
            _auditoriumsRepositoryMock.Setup(s => s.GetAsync(showtime.AuditoriumId, It.IsAny<CancellationToken>())).ReturnsAsync(auditorium);
          
            var result = await _controller.ReserveSeats(request, CancellationToken.None);

            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Some seats are not found.", notFoundResult.Value);
        }



        [Fact]
        public async Task ReserveSeats_ReturnsNotFound_WhenRequestIsNull()
        {
            var result = await _controller.ReserveSeats(null, CancellationToken.None);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Invalid reservation request.", badRequestResult.Value);
        }

        [Fact]
        public async Task GetReservationById_ReturnsNotFound_WhenTicketDoesNotExist()
        {
            var reservationId = Guid.NewGuid();
            var reservation = new ReservationEntity { Id = reservationId, CreatedTime = DateTime.Now, Movie = new MovieEntity { Title = "title" } };
            _reservationsRepositoryMock.Setup(r => r.GetAsync(reservationId, It.IsAny<CancellationToken>())).ReturnsAsync(reservation);

            var result = await _controller.GetReservationById(reservationId, CancellationToken.None);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetReservationById_ReturnsInternalServerError_WhenExceptionIsThrown()
        {
            var reservationId = Guid.NewGuid();
            var reservation = new ReservationEntity { Id = reservationId, CreatedTime = DateTime.Now, Movie = new MovieEntity { Title = "title" } };
            _reservationsRepositoryMock.Setup(r => r.GetAsync(reservationId, It.IsAny<CancellationToken>())).ThrowsAsync(new NullReferenceException("test exception"));

            var result = await _controller.GetReservationById(reservationId, CancellationToken.None);

            var internalServerErrorResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, internalServerErrorResult.StatusCode);
            Assert.Equal("Internal server error: test exception", internalServerErrorResult.Value);
        }

        [Fact]
        public async Task ReserveSeats_ReturnsInternalServerError_WhenExceptionIsThrown()
        {
            var request = new ReservationController.ReserveSeatsRequest
            {
                ShowtimeId = 1,
                SeatRow = 1,
                Seats = new List<int> { 1, 2, 3 }
            };

            _showtimesRepositoryMock.Setup(r => r.GetWithMoviesByIdAsync(request.ShowtimeId, It.IsAny<CancellationToken>())).ThrowsAsync(new NullReferenceException("test exception"));

            var result = await _controller.ReserveSeats(request, CancellationToken.None);

            var internalServerErrorResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, internalServerErrorResult.StatusCode);
            Assert.Equal("Internal server error: test exception", internalServerErrorResult.Value);
        }

        [Fact]
        public async Task ConfirmReservation_ReturnsInternalServerError_WhenExceptionIsThrown()
        {
            var reservationId = Guid.NewGuid();

            _reservationsRepositoryMock.Setup(r => r.GetAsync(reservationId, It.IsAny<CancellationToken>())).ThrowsAsync(new NullReferenceException("test exception"));
            var result = await _controller.ConfirmReservation(reservationId, CancellationToken.None);

            var internalServerErrorResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, internalServerErrorResult.StatusCode);
            Assert.Equal("Internal server error: test exception", internalServerErrorResult.Value);
        }

        [Fact]
        public async Task GetReservationById_ReturnsConflict_WhenReservationExpired()
        {
            var reservationId = Guid.NewGuid();
            var reservation = new ReservationEntity
            {
                Id = reservationId,
                CreatedTime = DateTime.Now.AddMinutes(-10) 
            };
            var ticket = new TicketEntity
            {
                Id = Guid.NewGuid(),
                CreatedTime = DateTime.Now.AddMinutes(-10) 
            };

            _reservationsRepositoryMock.Setup(r => r.GetAsync(reservationId, It.IsAny<CancellationToken>())).ReturnsAsync(reservation);
            _ticketsRepositoryMock.Setup(t => t.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(ticket);

            var result = await _controller.GetReservationById(reservationId, CancellationToken.None);

            var okResult = Assert.IsType<ConflictObjectResult>(result);            
            var jsonResponse = JsonConvert.SerializeObject(okResult.Value);

            Assert.Equal(StatusCodes.Status409Conflict, okResult.StatusCode);
            Assert.Contains("expired", jsonResponse);
        }

        [Fact]
        public async Task ConfirmReservation_ReturnsNotFound_WhenReservationDoesNotExist()
        {
            var reservationId = Guid.NewGuid();
            _reservationsRepositoryMock.Setup(r => r.GetAsync(reservationId, It.IsAny<CancellationToken>())).ReturnsAsync((ReservationEntity)null);

            var result = await _controller.ConfirmReservation(reservationId, CancellationToken.None);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task ConfirmReservation_ReturnsNotFound_WhenTicketDoesNotExist()
        {
            var reservationId = Guid.NewGuid();
            var reservation = new ReservationEntity { Id = reservationId, CreatedTime = DateTime.Now, Movie = new MovieEntity { Title = "title" } };
            _reservationsRepositoryMock.Setup(r => r.GetAsync(reservationId, It.IsAny<CancellationToken>())).ReturnsAsync(reservation);
            _ticketsRepositoryMock.Setup(r => r.GetAsync(reservationId, It.IsAny<CancellationToken>())).ReturnsAsync((TicketEntity)null);

            var result = await _controller.ConfirmReservation(reservationId, CancellationToken.None);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task ReserveSeats_ReturnsOk_WhenSeatsAreAvailable()
        {
            var request = new ReservationController.ReserveSeatsRequest
            {
                ShowtimeId = 1,
                SeatRow = 1,
                Seats = new List<int> { 1, 2, 3 }
            };
            var showtime = new ShowtimeEntity { AuditoriumId = 1, Movie = new MovieEntity { Title = "title"} };
            var auditorium = new AuditoriumEntity
            {
                Seats = new List<SeatEntity>
            {
                new SeatEntity { SeatNumber = 1, Row = 1 },
                new SeatEntity { SeatNumber = 2, Row = 1 },
                new SeatEntity { SeatNumber = 3, Row = 1 }
            }
            };
            var ticket = new TicketEntity
            {
                CreatedTime = DateTime.Now,
                Id = Guid.NewGuid(),
                Seats = auditorium.Seats.ToList(),
                Showtime = showtime,
                ShowtimeId = showtime.Id
            };
            var reservation = new ReservationEntity
            {
                Id = Guid.NewGuid(),
                NoOfSeats = request.Seats.Count,
                Auditorium = auditorium,
                Movie = showtime.Movie,
                CreatedTime = DateTime.Now,
                TicketId = ticket.Id
            };

            _showtimesRepositoryMock.Setup(s => s.GetWithMoviesByIdAsync(request.ShowtimeId, It.IsAny<CancellationToken>())).ReturnsAsync(showtime);
            _auditoriumsRepositoryMock.Setup(a => a.GetAsync(showtime.AuditoriumId, It.IsAny<CancellationToken>())).ReturnsAsync(auditorium);
            _ticketsRepositoryMock.Setup(t => t.GetEnrichedAsync(request.ShowtimeId, It.IsAny<CancellationToken>())).ReturnsAsync(new List<TicketEntity>());
            _ticketsRepositoryMock.Setup(t => t.CreateAsync(showtime, auditorium.Seats, It.IsAny<CancellationToken>())).ReturnsAsync(ticket);
            _reservationsRepositoryMock.Setup(t => t.CreateAsync(reservation, It.IsAny<CancellationToken>())).ReturnsAsync(reservation);

            var result = await _controller.ReserveSeats(request, CancellationToken.None);
            
            var okResult = Assert.IsType<OkObjectResult>(result);
            var jsonResponse = JsonConvert.SerializeObject(okResult.Value);
            var responseObject = JObject.Parse(jsonResponse);

            Assert.Contains("Reservation successfully created", responseObject["message"].ToString());
        }

        [Fact]
        public async Task ConfirmReservation_ReturnsConflict_WhenSeatsAlreadyBought()
        {
            var reservationId = Guid.NewGuid();
            var ticketId = Guid.NewGuid();
            var auditoriumId = 1;

            var reservation = new ReservationEntity
            {
                Id = reservationId,
                TicketId = ticketId,
                CreatedTime = DateTime.Now.AddMinutes(10) 
            };

            var ticket = new TicketEntity
            {
                Id = ticketId,
                Seats = new List<SeatEntity>
                {
                    new SeatEntity { SeatNumber = 1, Row = 1, AuditoriumId = auditoriumId },
                    new SeatEntity { SeatNumber = 2, Row = 1, AuditoriumId = auditoriumId }
                }
            };

            var paidTickets = new List<TicketEntity>
            {
                new TicketEntity
                {
                    Id = Guid.NewGuid(),
                    Seats = new List<SeatEntity>
                    {
                        new SeatEntity { SeatNumber = 1, Row = 1, AuditoriumId = auditoriumId }
                    }
                }
            };

            _reservationsRepositoryMock.Setup(r => r.GetAsync(reservationId, It.IsAny<CancellationToken>())).ReturnsAsync(reservation);
            _ticketsRepositoryMock.Setup(t => t.GetAsync(ticketId, It.IsAny<CancellationToken>())).ReturnsAsync(ticket);
            _ticketsRepositoryMock.Setup(t => t.GetPaidTicketsByAuditoriumAsync(auditoriumId, It.IsAny<CancellationToken>())).ReturnsAsync(paidTickets);

            var result = await _controller.ConfirmReservation(reservationId, CancellationToken.None);

            var conflictResult = Assert.IsType<ConflictObjectResult>(result);
            Assert.Equal("Some seats are already bought.", conflictResult.Value);
        }

        [Fact]
        public async Task ReserveSeats_ReturnsConflict_WhenSeatsAlreadyBought()
        {
            var reservationId = Guid.NewGuid();
            var ticketId = Guid.NewGuid();
            var auditoriumId = 1;
            var showtime = new ShowtimeEntity { AuditoriumId = 1 };
            var request = new ReservationController.ReserveSeatsRequest
            {
                ShowtimeId = 1,
                SeatRow = 1,
                Seats = new List<int> { 1, 2, 3 }
            };
            var auditorium = new AuditoriumEntity
            {
                Seats = new List<SeatEntity>
                {
                    new SeatEntity { SeatNumber = 1, Row = 1 },
                    new SeatEntity { SeatNumber = 2, Row = 1 },
                    new SeatEntity { SeatNumber = 3, Row = 1 }
                }
            };
            var reservation = new ReservationEntity
            {
                Id = reservationId,
                TicketId = ticketId,
                CreatedTime = DateTime.Now.AddMinutes(10)
            };

            var paidTickets = new List<TicketEntity>
            {
                new TicketEntity
                {
                    Id = Guid.NewGuid(),
                    Seats = new List<SeatEntity>
                    {
                        new SeatEntity { SeatNumber = 1, Row = 1, AuditoriumId = auditoriumId }
                    }
                }
            };

            _showtimesRepositoryMock.Setup(s => s.GetWithMoviesByIdAsync(request.ShowtimeId, It.IsAny<CancellationToken>())).ReturnsAsync(showtime);
            _auditoriumsRepositoryMock.Setup(a => a.GetAsync(showtime.AuditoriumId, It.IsAny<CancellationToken>())).ReturnsAsync(auditorium);
            _reservationsRepositoryMock.Setup(r => r.GetAsync(reservationId, It.IsAny<CancellationToken>())).ReturnsAsync(reservation);
            _ticketsRepositoryMock.Setup(t => t.GetEnrichedAsync(request.ShowtimeId, It.IsAny<CancellationToken>())).ReturnsAsync(paidTickets);

            var result = await _controller.ReserveSeats(request, CancellationToken.None);

            var conflictResult = Assert.IsType<ConflictObjectResult>(result);
            Assert.Equal("Some seats are already reserved or paid for.", conflictResult.Value);
        }

        [Fact]
        public async Task ReserveSeats_ReturnsBadRequest_WhenSeatsAreNotContiguous()
        {
            var request = new ReservationController.ReserveSeatsRequest
            {
                ShowtimeId = 1,
                SeatRow = 1,
                Seats = new List<int> { 1, 3, 4 } 
            };
            var showtime = new ShowtimeEntity { AuditoriumId = 1 };
            var auditorium = new AuditoriumEntity
            {
                Seats = new List<SeatEntity>
                {
                    new SeatEntity { SeatNumber = 1, Row = 1 },
                    new SeatEntity { SeatNumber = 3, Row = 1 },
                    new SeatEntity { SeatNumber = 4, Row = 1 }
                }
            };

            _showtimesRepositoryMock.Setup(s => s.GetWithMoviesByIdAsync(request.ShowtimeId, It.IsAny<CancellationToken>())).ReturnsAsync(showtime);
            _auditoriumsRepositoryMock.Setup(a => a.GetAsync(showtime.AuditoriumId, It.IsAny<CancellationToken>())).ReturnsAsync(auditorium);

            var result = await _controller.ReserveSeats(request, CancellationToken.None);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Seats are not contiguous.", badRequestResult.Value);
        }

        [Fact]
        public async Task ConfirmReservation_ReturnsOk_WhenReservationIsValid()
        {
            var reservationId = Guid.NewGuid();
            var reservation = new ReservationEntity
            {
                Id = reservationId,
                CreatedTime = DateTime.Now
            };
            var ticket = new TicketEntity
            {
                Id = Guid.NewGuid(),
                CreatedTime = DateTime.Now,
                Seats = new List<SeatEntity> { new SeatEntity{ AuditoriumId = 1, Row = 1, SeatNumber = 1 } }
            };

            _reservationsRepositoryMock.Setup(r => r.GetAsync(reservationId, It.IsAny<CancellationToken>())).ReturnsAsync(reservation);
            _ticketsRepositoryMock.Setup(t => t.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(ticket);
            _ticketsRepositoryMock.Setup(t => t.GetPaidTicketsByAuditoriumAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<TicketEntity>());

            var result = await _controller.ConfirmReservation(reservationId, CancellationToken.None);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var jsonResponse = JsonConvert.SerializeObject(okResult.Value);
            var responseObject = JObject.Parse(jsonResponse);

            Assert.Contains("Reservation confirmed successfully.", responseObject["message"].ToString());
        }

        [Fact]
        public async Task ConfirmReservation_ReturnsBadRequest_WhenReservationExpired()
        {
            var reservationId = Guid.NewGuid();
            var reservation = new ReservationEntity
            {
                Id = reservationId,
                CreatedTime = DateTime.Now.AddMinutes(-10) 
            };
            var ticket = new TicketEntity
            {
                Id = Guid.NewGuid(),
                CreatedTime = DateTime.Now.AddMinutes(-10) 
            };

            _reservationsRepositoryMock.Setup(r => r.GetAsync(reservationId, It.IsAny<CancellationToken>())).ReturnsAsync(reservation);
            _ticketsRepositoryMock.Setup(t => t.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(ticket);

            var result = await _controller.ConfirmReservation(reservationId, CancellationToken.None);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Reservation has expired.", badRequestResult.Value);
        }

    }

}
