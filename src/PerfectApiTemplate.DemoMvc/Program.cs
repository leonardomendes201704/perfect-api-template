var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.Configure<PerfectApiTemplate.DemoMvc.Infrastructure.DemoOptions>(builder.Configuration);
builder.Services.AddSingleton<PerfectApiTemplate.DemoMvc.Infrastructure.ApiUrlProvider>();

builder.Services.AddHttpClient();
builder.Services.AddScoped<PerfectApiTemplate.DemoMvc.ApiClients.HealthApiClient>();
builder.Services.AddScoped<PerfectApiTemplate.DemoMvc.ApiClients.AuthApiClient>();
builder.Services.AddScoped<PerfectApiTemplate.DemoMvc.ApiClients.CustomersApiClient>();
builder.Services.AddScoped<PerfectApiTemplate.DemoMvc.ApiClients.EmailApiClient>();
builder.Services.AddScoped<PerfectApiTemplate.DemoMvc.ApiClients.LogsApiClient>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
