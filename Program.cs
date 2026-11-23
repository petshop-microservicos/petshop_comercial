using CompraVendas.Data;
using CompraVendas.Exceptions;
using CompraVendas.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<CompraVendasDbContext>(o =>
    o.UseSqlite(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddHttpClient<IEstoqueClient, EstoqueClient>(c =>
{
    var url = builder.Configuration["Servicos:Estoque:BaseUrl"]
        ?? throw new InvalidOperationException("Servicos:Estoque:BaseUrl não configurado");
    c.BaseAddress = new Uri(url); // a barra final é obrigatória
    c.Timeout = TimeSpan.FromSeconds(5);
});

builder.Services.AddScoped<CompraService>();
builder.Services.AddScoped<VendaService>();
builder.Services.AddScoped<FornecedorService>();
builder.Services.AddScoped<MovimentacaoService>();

builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

using (var scope = app.Services.CreateScope())
    scope.ServiceProvider.GetRequiredService<CompraVendasDbContext>().Database.EnsureCreated();

app.UseAuthorization();
app.MapControllers();
app.Run();