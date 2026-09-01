using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MisterTicketApi.Database;
using MisterTicketApi.Entities;
using MisterTicketApi.Services;
using MisterTicketApi.Services.ServicesInterfaces;
using MisterTicketApi.BackgroundJobs;
using MisterTicketApi.Hubs;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add DbContext
builder.Services.AddDbContext<MisterTicketContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add services to the container.
builder.Services.AddScoped<IVenueService, VenueService>();
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IPricingZoneService, PricingZoneService>();
builder.Services.AddScoped<ISeatService, SeatService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IReservationService, ReservationService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<ITicketService, TicketService>();

// Password hashing: ships with ASP.NET Core, no extra package needed.
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();


// ---------- JWT authentication ----------
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key is missing from the configuration.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,            // check who issued the token
            ValidateAudience = true,          // check the token was meant for this API
            ValidateLifetime = true,          // reject expired tokens
            ValidateIssuerSigningKey = true,  // check the signature
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero         // default is 5 minutes of tolerance
        };
    });


builder.Services.AddControllers();
builder.Services.AddAuthorization();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ---------- Swagger with a "Authorize" button ----------
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Name = "Authorization",
        Description = "Paste only the token here, Swagger adds the \"Bearer \" prefix.",
        Type = SecuritySchemeType.Http
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Id = "Bearer",
                    Type = ReferenceType.SecurityScheme
                }
            },
            new List<string>()
        }
    });
});


// ---------- CORS for the Angular dev server ----------
builder.Services.AddCors(options =>
{
    options.AddPolicy("Angular", policy => policy
        .WithOrigins("http://localhost:64448")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()
        .WithExposedHeaders("Content-Disposition"));
});

builder.Services.AddSignalR();
builder.Services.AddHostedService<ExpiredHoldSweeper>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

app.UseCors("Angular");


app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<SeatHub>("/hubs/seats");

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<MisterTicketContext>();
    var hasher = services.GetRequiredService<IPasswordHasher<User>>();

    await context.Database.MigrateAsync();   // applies pending migrations
    await DbSeeder.SeedAsync(context, hasher);
}


app.Run();
