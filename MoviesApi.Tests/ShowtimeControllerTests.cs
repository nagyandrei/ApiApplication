using Microsoft.AspNetCore.Mvc;
using Moq;
using ApiApplication.Controllers;
using ApiApplication.Database.Repositories.Abstractions;
using ApiApplication.Database.Entities;
using System;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace ApiApplication.Tests
{
    public class ShowtimeControllerTests
    {
        private readonly Mock<IShowtimesRepository> _showtimesRepositoryMock;
        private readonly Mock<IAuditoriumsRepository> _auditoriumsRepositoryMock;
        private readonly Mock<IApiClientGrpc> _apiClientGrpcMock;
        private readonly ShowtimeController _controller;

        public ShowtimeControllerTests()
        {
            _showtimesRepositoryMock = new Mock<IShowtimesRepository>();
            _auditoriumsRepositoryMock = new Mock<IAuditoriumsRepository>();
            _apiClientGrpcMock = new Mock<IApiClientGrpc>();
            _controller = new ShowtimeController(
                _showtimesRepositoryMock.Object,
                _auditoriumsRepositoryMock.Object,
                _apiClientGrpcMock.Object);
        }

        [Fact]
        public async Task CreateShowtime_ReturnsNotFound_WhenMovieNotFound()
        {
            var request = new CreateShowtimeRequest { AuditoriumId = 1, MovieId = "movie1", SessionDate = DateTime.Now };
            _auditoriumsRepositoryMock
                .Setup(repo => repo.GetAsync(request.AuditoriumId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AuditoriumEntity());
            
            var result = await _controller.CreateShowtime(request, CancellationToken.None);

            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Movie not found.", notFoundResult.Value);
        }

        [Fact]
        public async Task CreateShowtime_ReturnsInternalServerError_WhenExceptionIsThrown()
        {
            var request = new CreateShowtimeRequest { AuditoriumId = 1, MovieId = "movie1", SessionDate = DateTime.Now };

            _auditoriumsRepositoryMock
                .Setup(repo => repo.GetAsync(request.AuditoriumId, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new NullReferenceException("test exception"));

            var result = await _controller.CreateShowtime(request, CancellationToken.None);

            var internalServerErrorResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, internalServerErrorResult.StatusCode);
            Assert.Equal("Internal server error: test exception", internalServerErrorResult.Value);
        }

        [Fact]
        public async Task CreateShowtime_ReturnsBadRequest_WhenRequestIsNull()
        {
            var result = await _controller.CreateShowtime(null, CancellationToken.None);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Invalid data.", badRequestResult.Value);
        }

        [Fact]
        public async Task CreateShowtime_ReturnsNotFound_WhenAuditoriumNotFound()
        {
            var request = new CreateShowtimeRequest { AuditoriumId = 1, MovieId = "movie1", SessionDate = DateTime.Now };

            var result = await _controller.CreateShowtime(request, CancellationToken.None);

            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Auditorium not found.", notFoundResult.Value);
        }

        [Fact]
        public async Task CreateShowtime_ReturnsOk_WhenShowtimeIsCreatedSuccessfully()
        {
            var request = new CreateShowtimeRequest { AuditoriumId = 1, MovieId = "movie1", SessionDate = DateTime.Now };
            var auditorium = new AuditoriumEntity();
            var movie = new MovieEntity
            {
                Id = 1,
                Title = "Movie Title",
                ImdbId = "movie1",
                Stars = "Director",
                ReleaseDate = new DateTime(2024, 1, 1)
            };
            var showtime = new ShowtimeEntity
            {
                Id = 1,
                SessionDate = request.SessionDate,
                AuditoriumId = request.AuditoriumId,
                Movie = movie
            };
            var showResponse = new ProtoDefinitions.showResponse
            {
                Rank = "1",
                Title = "Movie Title",
                Id = "movie1",
                Crew = "Director",
                Year = "2008"
            };


            _auditoriumsRepositoryMock
                .Setup(repo => repo.GetAsync(request.AuditoriumId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(auditorium);

            _apiClientGrpcMock.Setup(t => t.GetById(request.MovieId)).ReturnsAsync(showResponse);

            _showtimesRepositoryMock
                .Setup(repo => repo.CreateShowtime(It.IsAny<ShowtimeEntity>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(showtime);

            var result = await _controller.CreateShowtime(request, CancellationToken.None);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var jsonResponse = JsonConvert.SerializeObject(okResult.Value);
            var responseObject = JObject.Parse(jsonResponse);

            Assert.Contains("Showtime successfully created", responseObject["message"].ToString());
        }

        [Fact]
        public async Task GetAllShowtimes_ReturnsOk_WithShowtimes()
        {
            var showtimes = new List<ShowtimeEntity>
            {
                new ShowtimeEntity { Id = 1, SessionDate = DateTime.Now, AuditoriumId = 1 },
                new ShowtimeEntity { Id = 2, SessionDate = DateTime.Now.AddHours(1), AuditoriumId = 2 }
            };

            _showtimesRepositoryMock
                .Setup(repo => repo.GetAllAsync(s => true, It.IsAny<CancellationToken>()))
                .ReturnsAsync(showtimes);

            var result = await _controller.GetAllShowtimes(CancellationToken.None);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnValue = Assert.IsAssignableFrom<IEnumerable<ShowtimeEntity>>(okResult.Value);
            Assert.Equal(showtimes, returnValue);
        }

        [Fact]
        public async Task GetAllShowtimes_ReturnsInternalServerError_WhenExceptionIsThrown()
        {
            _showtimesRepositoryMock
                .Setup(repo => repo.GetAllAsync(s => true, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("test exception"));

            var result = await _controller.GetAllShowtimes(CancellationToken.None);

            var internalServerErrorResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, internalServerErrorResult.StatusCode);
            Assert.Equal("Internal server error: test exception", internalServerErrorResult.Value);
        }

        [Fact]
        public async Task GetShowtimeById_ReturnsOk_WhenShowtimeExists()
        {
            var showtimeId = 1;
            var showtime = new ShowtimeEntity
            {
                Id = showtimeId,
                Movie = new MovieEntity { Title = "title" },
                AuditoriumId = 1
            };

            _showtimesRepositoryMock.Setup(s => s.GetWithMoviesByIdAsync(showtimeId, It.IsAny<CancellationToken>())).ReturnsAsync(showtime);

            var result = await _controller.GetShowtimeById(showtimeId, CancellationToken.None);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedShowtime = Assert.IsType<ShowtimeEntity>(okResult.Value);
            
            Assert.Equal(showtimeId, returnedShowtime.Id);
        }

        [Fact]
        public async Task GetShowtimeById_ReturnsNotFound_WhenShowtimeDoesNotExist()
        {
            _showtimesRepositoryMock
                .Setup(repo => repo.GetWithMoviesByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync((ShowtimeEntity)null);

            var result = await _controller.GetShowtimeById(1, CancellationToken.None);

            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Showtime not found.", notFoundResult.Value);
        }

        [Fact]
        public async Task GetShowtimeById_ReturnsInternalServerError_WhenExceptionIsThrown()
        {
            _showtimesRepositoryMock
                .Setup(repo => repo.GetWithMoviesByIdAsync(1, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("test exception"));

            var result = await _controller.GetShowtimeById(1, CancellationToken.None);

            var internalServerErrorResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, internalServerErrorResult.StatusCode);
            Assert.Equal("Internal server error: test exception", internalServerErrorResult.Value);
        }
    }
}
