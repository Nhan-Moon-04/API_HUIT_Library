using HUIT_Library.Models;
using HUIT_Library.Services;
using HUIT_Library.Services.IServices;
using HUIT_Library.Services.BookingServices; // Add this import
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using HUIT_Library.Services.TaiNguyen.IServices;
using HUIT_Library.Services.TaiNguyen.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Add HttpClient for API calls
builder.Services.AddHttpClient();

// Add Memory Cache
builder.Services.AddMemoryCache();

// Add Entity Framework
builder.Services.AddDbContext<HuitThuVienContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Authentication services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPasswordHashService, PasswordHashService>();
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<ILoaiPhongServices, LoaiPhongServices>();

// Register the old BookingService for backward compatibility
builder.Services.AddScoped<IBookingService, BookingService>();

// Register new modular booking services
builder.Services.AddScoped<IBookingManagementService, BookingManagementService>();
builder.Services.AddScoped<IBookingViewService, BookingViewService>();
builder.Services.AddScoped<IViolationService, ViolationService>();
builder.Services.AddScoped<IRoomUsageService, RoomUsageService>();
builder.Services.AddScoped<IRoomService, RoomService>(); // Register Room service
builder.Services.AddScoped<IAvailableRoomService, AvailableRoomService>(); // ✅ Add available room search service
builder.Services.AddScoped<ITaiNguyenLoaiPhongServices, TaiNguyenLoaiPhongServices>(); // Register TaiNguyenLoaiPhong service

// Register notification service
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IChatService, ChatService>();
// Register BotpressService as a scoped service (BotpressService does not currently accept HttpClient)
builder.Services.AddScoped<IBotpressService, BotpressService>();
builder.Services.AddScoped<INotification, HUIT_Library.Services.Notification.NotificationServices>(); // Register NotificationServices
// Register Library Statistics service
builder.Services.AddScoped<ILibraryStatisticsService, LibraryStatisticsService>();

// Configure JWT authentication
var jwtKey = builder.Configuration.GetValue<string>("Jwt:Key") ?? "P6n@8X9z#A1k$F3q*L7v!R2y^C5m&E0w";
var jwtIssuer = builder.Configuration.GetValue<string>("Jwt:Issuer") ?? "HUIT_Library";
var jwtAudience = builder.Configuration.GetValue<string>("Jwt:Audience") ?? "HUIT_Library_Users";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", cors =>
    {
        cors.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

// ? Add Swagger + JWT Auth config
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "HUIT Library API", Version = "v1" });

    // ? C?u hình nút Authorize d? nh?p JWT Token
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Nh?p token theo d?ng: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new List<string>()
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Urls.Add("https://0.0.0.0:7100");

app.Run();
