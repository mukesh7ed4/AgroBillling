// ================================================
// COMPLETE WORKING PROGRAM.CS FOR RENDER
// ================================================

using AgroBilling.API.Services;
using AgroBilling.DAL.Context;
using AgroBilling.DAL.Repositories;
using AgroBilling.DAL.Repositories.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IO.Compression;
using System.Text;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// Disable file watching for Render
builder.Configuration.Sources
    .OfType<Microsoft.Extensions.Configuration.Json.JsonConfigurationSource>()
    .ToList()
    .ForEach(s => s.ReloadOnChange = false);

builder.Services.AddMemoryCache();
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});
builder.Services.Configure<BrotliCompressionProviderOptions>(o => o.Level = CompressionLevel.Fastest);
builder.Services.Configure<GzipCompressionProviderOptions>(o => o.Level = CompressionLevel.Fastest);

builder.Services.AddDbContext<AgroBillingDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Repositories
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IShopRepository, ShopRepository>();
builder.Services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<ISupplierRepository, SupplierRepository>();
builder.Services.AddScoped<IBillRepository, BillRepository>();
builder.Services.AddScoped<IPurchaseRepository, PurchaseRepository>();
builder.Services.AddScoped<IExpenseRepository, ExpenseRepository>();
builder.Services.AddScoped<ICreditNoteRepository, CreditNoteRepository>();
builder.Services.AddScoped<IReportRepository, ReportRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();

// Services
builder.Services.AddScoped<IBillService, BillService>();
builder.Services.AddScoped<IPurchaseService, PurchaseService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddHttpClient();
builder.Services.AddScoped<EmailValidationService>();

// JWT
var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new Exception("Jwt Key missing");
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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

// ================================================
// CORS - SIMPLE WORKING CONFIGURATION
// ================================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ================================================
// MANUAL CORS HEADERS (BACKUP)
// ================================================
app.Use(async (context, next) =>
{
    // Add CORS headers to every response
    context.Response.Headers["Access-Control-Allow-Origin"] = "*";
    context.Response.Headers["Access-Control-Allow-Methods"] = "GET, POST, PUT, DELETE, OPTIONS, PATCH";
    context.Response.Headers["Access-Control-Allow-Headers"] = "Content-Type, Authorization, X-Requested-With, Accept, Origin";
    context.Response.Headers["Access-Control-Expose-Headers"] = "Content-Length, Content-Type";

    // Handle preflight
    if (context.Request.Method == "OPTIONS")
    {
        context.Response.StatusCode = 200;
        await context.Response.CompleteAsync();
        return;
    }

    await next();
});

app.Use(async (context, next) =>
{
    Console.WriteLine($"[DEBUG] {context.Request.Method} {context.Request.Path}");
    Console.WriteLine($"[DEBUG] Origin: {context.Request.Headers.Origin}");
    await next();
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// IMPORTANT: Order matters
app.UseCors("AllowAll");
app.UseResponseCompression();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/health", () => "OK");

var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Run($"http://0.0.0.0:{port}");