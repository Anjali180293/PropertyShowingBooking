using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using PropertyShowingBooking.Api.Data;
using PropertyShowingBooking.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("BookingDb")
    ?? throw new InvalidOperationException("Connection string 'BookingDb' is not configured.");

connectionString = ResolveSqliteConnectionString(connectionString, builder.Environment.ContentRootPath);

builder.Services.AddDbContext<BookingDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddScoped<ISlotService, SlotService>();
builder.Services.AddScoped<IBookingService, BookingService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();

static string ResolveSqliteConnectionString(string connectionString, string contentRootPath)
{
    const string prefix = "Data Source=";
    if (!connectionString.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
    {
        return connectionString;
    }

    var dataSource = connectionString[prefix.Length..].Trim();
    if (Path.IsPathRooted(dataSource))
    {
        return connectionString;
    }

    return $"{prefix}{Path.Combine(contentRootPath, dataSource)}";
}
