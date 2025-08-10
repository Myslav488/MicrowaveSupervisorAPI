using MicrowaveSupervisorAPI.Server.Interfaces;
using MicrowaveSupervisorAPI.Server.Controllers;
using MicrowaveSupervisorAPI.Server.Hardware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register the IMicrowaveOvenHW mock as a singleton
builder.Services.AddSingleton<IMicrowaveOvenHW, MicrowaveOven>();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapFallbackToFile("/index.html");

app.Run();