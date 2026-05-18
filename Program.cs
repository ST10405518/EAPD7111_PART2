using Microsoft.EntityFrameworkCore;
using EAPD7111_PART2.Data;
using EAPD7111_PART2.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews()
    .AddRazorRuntimeCompilation();

builder.Services.AddDbContext<GLMSDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IFileUploadService, FileUploadService>();
builder.Services.AddScoped<IContractWorkflowService, ContractWorkflowService>();
builder.Services.AddScoped<IContractStatusAutomationService, ContractStatusAutomationService>();
builder.Services.AddHttpClient<ICurrencyConversionService, CurrencyConversionService>();

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
        "Database migration failed. The site will still run, but data pages need SQL Server. " +
        "Check your connection string in appsettings.json and ensure LocalDB/SQL Server is running.");
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Logger.LogInformation("GLMS is running. Open https://localhost:7159 or http://localhost:5064 in your browser.");

app.Run();
