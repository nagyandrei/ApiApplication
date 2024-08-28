using ApiApplication.Database.Repositories.Abstractions;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace ApiApplication.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MoviesController : ControllerBase
    {
        private readonly IApiClientGrpc _apiClientGrpc;

        public MoviesController(IApiClientGrpc apiClientGrpc)
        {
            _apiClientGrpc = apiClientGrpc;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllMovies()
        {
            try
            {
                var response = await _apiClientGrpc.GetAll();
                if (response != null && response.Shows.Count > 0)
                {
                    return Ok(response.Shows);
                }

                return NoContent(); 
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetMovieById(string id)
        {
            try
            {
                var movie = await _apiClientGrpc.GetById(id);
                if (movie != null)
                {
                    return Ok(movie);
                }

                return NotFound(); 
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}