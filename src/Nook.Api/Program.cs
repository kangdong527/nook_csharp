using Nook.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// 연결 문자열: 개발은 user-secrets, 배포는 환경변수 ConnectionStrings__Nook
var connectionString = builder.Configuration.GetConnectionString("Nook")
    ?? throw new InvalidOperationException("연결 문자열 'Nook'이 설정되지 않았습니다.");
builder.Services.AddInfrastructure(connectionString);

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
