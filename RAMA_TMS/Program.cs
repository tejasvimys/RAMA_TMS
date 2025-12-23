using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QuestPDF.Infrastructure;
using RAMA_TMS.Data;
using RAMA_TMS.Interface;
using RAMA_TMS.Models;
using RAMA_TMS.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

QuestPDF.Settings.License = LicenseType.Community;

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
builder.Services.AddScoped<IEndOfDayReportService, EndOfDayReportService>();
builder.Services.AddScoped<IPdfGeneratorService, PdfGeneratorService>();

var jwtSection = builder.Configuration.GetSection("Jwt");
var signingKey = jwtSection["SigningKey"];

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSection["Issuer"],      // MUST match token
            ValidAudience = jwtSection["Audience"],  // MUST match token
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey!)),
            ClockSkew = TimeSpan.Zero // remove 5min tolerance
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
    options.AddPolicy("CollectorOnly", p => p.RequireRole("Collector"));
});

builder.Services.AddSingleton<IGoogleTokenValidator, GoogleTokenValidator>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRouting();
// enable CORS before auth and endpoints
app.UseCors(CorsPolicy);
app.UseAuthentication(); // MUST come BEFORE UseAuthorization
app.UseAuthorization();

app.MapControllers();

app.Run();
