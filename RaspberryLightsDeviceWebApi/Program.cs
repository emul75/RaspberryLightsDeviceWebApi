using RaspberryLightsDeviceWebApi.Interfaces;
using RaspberryLightsDeviceWebApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<IOBD2Service, OBD2Service>();
builder.Services.AddSingleton<ILedStripService, LedStripService>();
builder.Services.AddSingleton<INgrokService, NgrokService>();

var app = builder.Build();

var ngrokService = app.Services.GetRequiredService<INgrokService>();
await ngrokService.StartupSetup();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
