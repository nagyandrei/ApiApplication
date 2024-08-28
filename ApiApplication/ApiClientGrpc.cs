using System;
using System.Net.Http;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using ApiApplication.Database.Repositories.Abstractions;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using ProtoDefinitions;

namespace ApiApplication
{
    public class ApiClientGrpc : IApiClientGrpc
    {
        private readonly IDistributedCache _cache;
        private readonly string _apiBaseUrl;
        private readonly string _apiKey;

        public ApiClientGrpc(IDistributedCache cache, IConfiguration configuration)
        {
            _cache = cache;
            _apiBaseUrl = configuration["ApiSettings:ProvidedApiBaseUrl"];
            _apiKey = configuration["ApiSettings:ApiKey"];
        }

        private async Task<GrpcChannel> CreateChannelAsync()
        {
            var httpHandler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback =
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };

            return await Task.FromResult(GrpcChannel.ForAddress(_apiBaseUrl, new GrpcChannelOptions
            {
                HttpHandler = httpHandler
            }));
        }

        public async Task<showListResponse> GetAll()
        {
            var cacheKey = "allMovies";
            var cachedData = await _cache.GetStringAsync(cacheKey);

            if (cachedData != null)
            {
                return JsonConvert.DeserializeObject<showListResponse>(cachedData);
            }

            try
            {
                var channel = await CreateChannelAsync();
                var client = new MoviesApi.MoviesApiClient(channel);
                var headers = new Metadata
            {
                { "X-Apikey", _apiKey }
            };

                var all = await client.GetAllAsync(new Empty(), headers);
                all.Data.TryUnpack<showListResponse>(out var data);

                var serializedData = JsonConvert.SerializeObject(data);
                await _cache.SetStringAsync(cacheKey, serializedData, new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                });

                return data;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to get data from gRPC service.", ex);
            }
        }

        public async Task<showResponse> GetById(string id)
        {
            var cacheKey = $"smovie-{id}";
            var cachedData = await _cache.GetStringAsync(cacheKey);

            if (cachedData != null)
            {
                return JsonConvert.DeserializeObject<showResponse>(cachedData);
            }

            try
            {
                var channel = await CreateChannelAsync();
                var client = new MoviesApi.MoviesApiClient(channel);
                var headers = new Metadata
            {
                { "X-Apikey", _apiKey }
            };

                var idRequest = new IdRequest { Id = id };
                var response = await client.GetByIdAsync(idRequest, headers);
                response.Data.TryUnpack<showResponse>(out var data);

                var serializedData = JsonConvert.SerializeObject(data);
                await _cache.SetStringAsync(cacheKey, serializedData, new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                });

                return data;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to get data from gRPC service.", ex);
            }
        }
    }

}