using Azure.Identity;
using FluentValidation.AspNetCore;
using MFC.Configurations;
using MFC.DataAccessLayer;
using MFC.DataAccessLayer.Repository;
using MFC.Interfaces;
using MFC.Services;
using Microsoft.Extensions.Options;
using Refit;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

var builder = WebApplication.CreateBuilder(args);

// Add the HttpClient service
builder.Services.AddHttpClient();
// Add services to the container.

var settings = new RefitSettings();
settings.ContentSerializer = new NewtonsoftJsonContentSerializer(new JsonSerializerSettings
{
    ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
});

builder.Services.AddRefitClient<IAllegroApiClient>(settings)
    .ConfigureHttpClient((sp, c) =>
    {
        var options = sp.GetRequiredService<IOptions<AllegroApiSettings>>().Value;
        c.BaseAddress = new Uri(options.AllegroApiBaseUrl);
    });


builder.Services.Configure<AllegroApiSettings>(builder.Configuration.GetSection("AllegroApiSettings"));

builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.MinimumSameSitePolicy = SameSiteMode.Lax; 
    options.HttpOnly = HttpOnlyPolicy.Always;
    options.Secure = CookieSecurePolicy.SameAsRequest;
});

builder.Services.Configure<GoogleOAuthSettings>(builder.Configuration.GetSection("GoogleOAuth"));
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<GoogleOAuthSettings>>().Value);

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
    })
    .AddCookie()
    .AddGoogle(options =>
    {
        var googleSettings = builder.Configuration.GetSection("GoogleOAuth").Get<GoogleOAuthSettings>();
        options.ClientId = googleSettings.ClientId;
        options.ClientSecret = googleSettings.ClientSecret;
    });

builder.Services.AddScoped<IMfcDbRepository, MfcDbRepository>();
builder.Services.AddScoped<IAccessTokenProvider>(sp =>
{
    var options = sp.GetRequiredService<IOptions<AllegroApiSettings>>().Value;
    var httpClient = sp.GetRequiredService<HttpClient>();
    return new AccessTokenProvider(httpClient, options.ClientId, options.ClientSecret, options.TokenUrl, options.AuthorizationEndpoint, sp.GetRequiredService<IMfcDbRepository>());
});

builder.Services.AddScoped<IAllegroApiService, AllegroApiService>();
builder.Services.AddTransient<ICategoryService, CategoryService>();
builder.Services.AddTransient<ICalculationService, CalculationService>();

builder.Services.AddDbContext<MfcDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("AZURE_SQL_CONNECTIONSTRING")));

builder.Services.AddControllers();

builder.Services.AddFluentValidationAutoValidation();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors();

var app = builder.Build();
app.UseCookiePolicy();
app.UseAuthentication();

// Configure the HTTP request pipeline.
// if (app.Environment.IsDevelopment())
// {
    app.UseSwagger();
    app.UseSwaggerUI();
// }

app.UseCors(builder => builder
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader());

//app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();