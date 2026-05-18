using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using EAPD7111_PART2.Data;
using EAPD7111_PART2.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 52_428_800;
});

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 52_428_800;
});

builder.Services.AddControllersWithViews()
    .AddRazorRuntimeCompilation();

builder.Services.AddDbContext<GLMSDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IFileUploadService, FileUploadService>();
builder.Services.AddScoped<IContractWorkflowService, ContractWorkflowService>();
builder.Services.AddScoped<IContractStatusAutomationService, ContractStatusAutomationService>();
builder.Services.AddHttpClient<ICurrencyConversionService, CurrencyConversionService>();

var webRootPath = Path.Combine(builder.Environment.ContentRootPath, "wwwroot");
Directory.CreateDirectory(webRootPath);
Directory.CreateDirectory(Path.Combine(webRootPath, "uploads", "contracts"));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<GLMSDbContext>();
    db.Database.Migrate();
    app.Logger.LogInformation("Database migration completed successfully.");
}
catch (Exception ex)
{
    app.Logger.LogWarning(ex,
        "Database migration failed. Data features require SQL Server LocalDB. " +
        "Run: dotnet ef database update");
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Logger.LogInformation("GLMS is running. Open https://localhost:7159 or http://localhost:5064");

app.Run();
