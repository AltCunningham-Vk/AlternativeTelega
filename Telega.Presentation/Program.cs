using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using StackExchange.Redis;
using System.Text;
using Telega.Application.Repositories;
using Telega.Application.Services;
using Telega.Infrastructure.Caching;
using Telega.Infrastructure.Data;
using Telega.Infrastructure.Repositories;
using Telega.Infrastructure.Storage;
using System.Text.Json.Serialization;
using Telega.Infrastructure.Utilities;
using Telega.Infrastructure.Services;
using Telega.Presentation.Hubs;
using Telega.Presentation.Services;
using Microsoft.Extensions.DependencyInjection;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"),
    npgsqlOptions => npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorCodesToAdd: null))); // Подключение к базе данных
builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect("Redis")); // Подключение к Redis
builder.Services.AddScoped<IFileStorageService, MinioService>(); // Регистрация к Minio
builder.Services.AddScoped<ICacheService, RedisCacheService>(); // Регистрация к Redis
builder.Services.AddScoped<IChatRepository, ChatRepository>(); // Регистрация репозитория чатов
builder.Services.AddScoped<IMessageRepository, MessageRepository>(); // Регистрация репозитория сообщений
builder.Services.AddScoped<IEncryptionService, AesEncryptionService>();// Регистрация сервиса шифрования
builder.Services.AddScoped<IChatService, ChatService>(); // Регистрация сервиса чатов
builder.Services.AddScoped<IMessageService, MessageService>(); // Регистрация сервиса сообщений
builder.Services.AddScoped<IUserRepository, UserRepository>(); // Регистрация репозитория пользователей
builder.Services.AddScoped<IAuthService, AuthService>();// Регистрация сервиса аутентификации
builder.Services.AddHostedService<ExpiredMessageCleanupService>(); // Регистрация сервиса удаления просроченных сообщений
builder.Services.AddScoped<IMessageAuditLogRepository, MessageAuditLogRepository>();// Регистрация репозитория аудита сообщений
builder.Services.AddScoped<IChatNotificationService, ChatNotificationService>(); // Регистрация сервиса уведомлений о новых сообщениях
builder.Services.AddOpenApi();
// Кэширование Redis
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration["Redis"];
    options.InstanceName = "Telega_";
});
builder.Services.AddSignalR();
builder.Services.AddMetrics();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});


// Настройка JWT-аутентификации
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };
        // Для отладки
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine($"Authentication failed: {context.Exception.Message}");
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                Console.WriteLine("Token validated successfully.");
                return Task.CompletedTask;
            }
        };
    });

// Добавление контроллеров и Swagger
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Telega API", Version = "v1" });
    // Добавляем поддержку авторизации в Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer' [space] and then your token in the text input below."
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
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();
// Configure the HTTP request pipeline.
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate(); // Применяет все миграции
}
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
app.UseSwagger();
    app.UseSwaggerUI(c =>
    { 
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Telega API v1");
    });


app.UseRouting();
app.UseMetricServer();
app.UseHttpMetrics();
//app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();


app.MapHub<ChatHub>("/chathub");



app.MapControllers();


app.Run();

