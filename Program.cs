using HUIT_Library.Hubs; // ✅ Add ChatHub import
using HUIT_Library.Models;
using HUIT_Library.Services;
using HUIT_Library.Services.IServices;
using HUIT_Library.Services.BookingServices; // Add this import
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Security.Claims; // ✅ Add this for ClaimTypes
using HUIT_Library.Services.TaiNguyen.IServices;
using HUIT_Library.Services.TaiNguyen.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// 📊 Add SignalR for realtime chat - MINIMAL CONFIG FIRST
builder.Services.AddSignalR();

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

// ✅ Register Rating service
builder.Services.AddScoped<IRatingService, RatingService>();

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
        ValidateLifetime = true, // ✅ We'll handle permanent tokens in events
  ClockSkew = TimeSpan.Zero
        };
      
        // 🎯 Add JWT support for SignalR and Permanent Token handling
     options.Events = new JwtBearerEvents
  {
        OnMessageReceived = context =>
   {
  var accessToken = context.Request.Query["access_token"];
  var path = context.HttpContext.Request.Path;
    
      // If the request is for our SignalR hub...
      if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/chathub"))
 {
  context.Token = accessToken;
     }
              return Task.CompletedTask;
          },
     // ✅ Handle permanent tokens (no expiration validation)
    OnTokenValidated = context =>
            {
           var tokenTypeClaim = context.Principal?.FindFirst("TokenType")?.Value;
     if (tokenTypeClaim == "Permanent")
    {
     // Skip lifetime validation for permanent tokens
       // Token is valid regardless of expiration
                 var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            var userClaim = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            logger.LogInformation("Validated permanent token for user {UserId}", userClaim);
                }
    return Task.CompletedTask;
      }
};
    });

builder.Services.AddAuthorization();

// 🔗 Update CORS to support SignalR
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", cors =>
    {
      cors.WithOrigins(
            "http://localhost:4200",
     "https://localhost:4200",
            "http://localhost:3000",
            "https://localhost:3000",
            "http://localhost:5173",  // Vite dev server
            "https://localhost:5173"
        )
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials(); // 🔐 Required for SignalR authentication
    });
});

// 📄 Add Swagger + JWT Auth config
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "HUIT Library API", Version = "v1" });

    // 🔐 Cấu hình nút Authorize để nhập JWT Token
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Nhập token theo dạng: Bearer {token}",
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

app.UseCors("AllowAll"); // 🌐 Enable CORS for SignalR

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// 🎯 Map SignalR ChatHub for realtime messaging
app.MapHub<ChatHub>("/chathub");

app.Urls.Add("https://0.0.0.0:7100");

app.Run();