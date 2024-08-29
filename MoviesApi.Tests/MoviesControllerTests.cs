using Moq;
using ProtoDefinitions;
using Microsoft.AspNetCore.Mvc;
using ApiApplication.Controllers;
using ApiApplication.Database.Repositories.Abstractions;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

public class MoviesControllerTests
{
    private readonly Mock<IApiClientGrpc> _apiClientGrpcMock;
    private readonly MoviesController _controller;

    public MoviesControllerTests()
    {
        _apiClientGrpcMock = new Mock<IApiClientGrpc>();
        _controller = new MoviesController(_apiClientGrpcMock.Object);
    }

    [Fact]
    public async Task GetAllMovies_ReturnsOk_WithMovies()
    {
        var showResponse = new ProtoDefinitions.showResponse
        {
            Rank = "1",
            Title = "Movie Title",
            Id = "movie1",
            Crew = "Director",
            Year = "2008"
        };
        var response = new ProtoDefinitions.showListResponse
        {
            
        };
        response.Shows.Add(showResponse);

        _apiClientGrpcMock.Setup(api => api.GetAll()).ReturnsAsync(response);

        var result = await _controller.GetAllMovies();

        var okResult = Assert.IsType<OkObjectResult>(result);
        
        var jsonResponse = JsonConvert.SerializeObject(okResult.Value);
        var jArray = JArray.Parse(jsonResponse);
        var firstItem = jArray.FirstOrDefault() as JObject;

        Assert.Equal(showResponse.Rank, firstItem["Rank"].ToString());
        Assert.Equal(showResponse.Title, firstItem["Title"].ToString());
        Assert.Equal(showResponse.Crew, firstItem["Crew"].ToString());

    }

    [Fact]
    public async Task GetAllMovies_ReturnsNoContent_WhenNoMovies()
    {
        var response = new ProtoDefinitions.showListResponse
        {
        };

        _apiClientGrpcMock.Setup(api => api.GetAll()).ReturnsAsync(response);

        var result = await _controller.GetAllMovies();

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task GetAllMovies_ReturnsInternalServerError_WhenExceptionIsThrown()
    {
        _apiClientGrpcMock.Setup(api => api.GetAll()).ThrowsAsync(new System.Exception("test exception"));

        var result = await _controller.GetAllMovies();

        var internalServerErrorResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, internalServerErrorResult.StatusCode);
        Assert.Equal("Internal server error: test exception", internalServerErrorResult.Value);
    }

    [Fact]
    public async Task GetMovieById_ReturnsOk_WithMovie()
    {
        var showResponse = new ProtoDefinitions.showResponse
        {
            Rank = "1",
            Title = "Movie Title",
            Id = "movie1",
            Crew = "Director",
            Year = "2008"
        };

        _apiClientGrpcMock.Setup(api => api.GetById("movie1")).ReturnsAsync(showResponse);

        var result = await _controller.GetMovieById("movie1");

        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnValue = Assert.IsType<ProtoDefinitions.showResponse>(okResult.Value);
        Assert.Equal(showResponse.Id, returnValue.Id);
    }

    [Fact]
    public async Task GetMovieById_ReturnsNotFound_WhenMovieDoesNotExist()
    {
        _apiClientGrpcMock.Setup(api => api.GetById("movie1")).ReturnsAsync((ProtoDefinitions.showResponse)null);

        var result = await _controller.GetMovieById("movie1");

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetMovieById_ReturnsInternalServerError_WhenExceptionIsThrown()
    {
        _apiClientGrpcMock.Setup(api => api.GetById("movie1")).ThrowsAsync(new NullReferenceException("test exception"));

        var result = await _controller.GetMovieById("movie1");

        var internalServerErrorResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, internalServerErrorResult.StatusCode);
        Assert.Equal("Internal server error: test exception", internalServerErrorResult.Value);
    }
}
