using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ApiApplication.Database.Repositories.Abstractions;
using ApiApplication.Database.Entities;

namespace ApiApplication.Controllers
{
    [Route("api/showtimes")]
    [ApiController]
    public class ShowtimeController : ControllerBase
    {
        private readonly IShowtimesRepository _showtimeRepository;
        private readonly IAuditoriumsRepository _auditoriumsRepository;
        private readonly IApiClientGrpc _apiClientGrpc; 

        public ShowtimeController(IShowtimesRepository showtimesRepository, IAuditoriumsRepository auditoriumsRepository, IApiClientGrpc apiClientGrpc)
        {
            _showtimeRepository = showtimesRepository;
            _auditoriumsRepository = auditoriumsRepository;
            _apiClientGrpc = apiClientGrpc;
        }

        [HttpPost]
        public async Task<IActionResult> CreateShowtime([FromBody] CreateShowtimeRequest request, CancellationToken cancellationToken)
        {
            if (request == null)
            {
                return BadRequest("Invalid data.");
            }

            var auditorium = await _auditoriumsRepository.GetAsync(request.AuditoriumId, cancellationToken);
            if (auditorium == null)
            {
                return NotFound("Auditorium not found.");
            }

            var movie = await _apiClientGrpc.GetById(request.MovieId);
            if (movie == null)
            {
                return NotFound("Movie not found.");
            }

            var showtime = new ShowtimeEntity
            {
                SessionDate = request.SessionDate,
                AuditoriumId = request.AuditoriumId,
                Movie = new MovieEntity
                {
                    Id = Convert.ToInt32(movie.Rank),
                    Title = movie.FullTitle,
                    ImdbId = movie.Id,
                    Stars = movie.Crew,
                    ReleaseDate = DateTime.ParseExact(movie.Year, "yyyy", null)
                },
                Tickets = new List<TicketEntity>() 
            };

            try
            {
                var createdShowtime = await _showtimeRepository.CreateShowtime(showtime, cancellationToken);
                return Ok(new
                {
                    createdShowtime.Id,
                    createdShowtime.SessionDate,
                    createdShowtime.AuditoriumId,
                    createdShowtime.Movie
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllShowtimes(CancellationToken cancellationToken)
        {
            try
            {
                var showtimes = await _showtimeRepository.GetAllAsync(s => true, cancellationToken);
                return Ok(showtimes);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetShowtimeById(int id, CancellationToken cancellationToken)
        {
            var showtime = await _showtimeRepository.GetWithMoviesByIdAsync(id, cancellationToken);
            if (showtime != null)
            {
                return Ok(showtime);
            }

            return NotFound("Showtime not found.");
        }
    }

    public class CreateShowtimeRequest
    {
        public DateTime SessionDate { get; set; }
        public int AuditoriumId { get; set; }
        public string MovieId { get; set; }
    }
}
