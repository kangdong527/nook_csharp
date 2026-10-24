using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Nook.Api.Auth;
using Nook.Application.Places;
using Nook.Integration.Tests.Infrastructure;

namespace Nook.Integration.Tests.Api;

// PL-01을 HTTP로. 흐름·동시성은 ConfirmPlaceTests가 본다. 여기서는 Api 계층의 몫(바인딩 검증·응답 모양)만.
public sealed class PlacesApiTests : IntegrationTestBase, IDisposable
{
    // 테스트 목록
    // - Confirm_Returns200WithPlace
    // - Confirm_WhenLatitudeMissing_Returns400ValidationProblem   (double이면 0으로 바인딩돼 통과했을 경우)

    private readonly NookApiFactory _factory;
    private readonly HttpClient _client;

    public PlacesApiTests(PostgresFixture fixture) : base(fixture)
    {
        _factory = new NookApiFactory(fixture.ConnectionString);
        _client = _factory.CreateClient();
        var token = _factory.Services.GetRequiredService<AccessTokenIssuer>().Issue(Guid.NewGuid());
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Confirm_Returns200WithPlace()
    {
        var response = await _client.PostAsJsonAsync("/places", new
        {
            kakaoPlaceId = "8130394", name = "누크 카페", address = "서울", latitude = 37.5665, longitude = 126.978,
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var place = await response.Content.ReadFromJsonAsync<PlaceView>();
        Assert.Equal("8130394", place?.KakaoPlaceId);
        Assert.NotEqual(Guid.Empty, place?.Id);
    }

    [Fact]
    public async Task Confirm_WhenLatitudeMissing_Returns400ValidationProblem()
    {
        var response = await _client.PostAsJsonAsync("/places", new
        {
            kakaoPlaceId = "8130394", name = "누크 카페", address = "서울", longitude = 126.978,
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Contains("Latitude", problem!.Errors.Keys);
    }
}
