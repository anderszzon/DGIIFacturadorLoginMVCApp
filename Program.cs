using DGIIFacturadorLoginMVCApp.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography.X509Certificates;

var builder = WebApplication.CreateBuilder(args);


using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
store.Open(OpenFlags.ReadOnly);

// List all certificates in the store for debugging
foreach (var cert in store.Certificates)
{
    Console.WriteLine($"Subject: {cert.Subject}, Thumbprint: {cert.Thumbprint}");
}

//// Carga el certificado desde el almacén del servidor
//var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
//store.Open(OpenFlags.ReadOnly);
//try
//{
//    var certs = store.Certificates.Find(
//        X509FindType.FindByThumbprint,
//        "2BF6F9D3FF06FB3A4B5813885FF252BCB055AB6F",
//        validOnly: false // ← Usa 'true' en producción
//    );

//    if (certs.Count == 0)
//        throw new Exception("Certificado no encontrado en el almacén.");

//    var certificate = certs[0];

//    // Configura Kestrel para usar el certificado
//    builder.WebHost.ConfigureKestrel(serverOptions =>
//    {
//        serverOptions.ConfigureHttpsDefaults(httpsOptions =>
//        {
//            httpsOptions.ServerCertificate = certificate;
//        });
//    });
//}
//finally
//{
//    store.Close();
//}

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddControllersWithViews();

//builder.Environment.EnvironmentName = Environments.Production;

var app = builder.Build();

//Console.WriteLine($"ENTORNO ACTUAL: {app.Environment.EnvironmentName}");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();
