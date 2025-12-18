using Microsoft.EntityFrameworkCore;
using RAMA_TMS.Data;
using RAMA_TMS.Interface;
using RAMA_TMS.Models;
using RAMA_TMS.Services;
using Swashbuckle.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

const string CorsPolicy = "TempleCorsPolicy";
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: CorsPolicy, policy =>
    {
        policy.WithOrigins("http://localhost:5173") // Vite dev URL
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddDbContext<TMSDBContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("TMSDBConnectionString")));

builder.Services.AddSingleton<IDonationReceiptPdfGenerator>(sp =>
{
    var env = sp.GetRequiredService<IWebHostEnvironment>();
    var logoPath = Path.Combine(env.ContentRootPath, "images", "rama_logo1.png"); // adjust
    return new DonationReceiptPdfGenerator(logoPath);
});
builder.Services.Configure<SmtpEmailSettings>(
    builder.Configuration.GetSection("EmailSettingsTest"));
//builder.Services.Configure<SmtpEmailSettings>(
//    builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddScoped<IEmailService, SmtpEmailService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// enable CORS before auth and endpoints
app.UseCors(CorsPolicy);

app.UseAuthorization();

app.MapControllers();

app.Run();
