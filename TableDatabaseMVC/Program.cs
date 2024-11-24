using TableDatabaseMVC.Services;

var builder = WebApplication.CreateBuilder(args);

// Додати сервіси до контейнера.
builder.Services.AddControllersWithViews();

// Реєстрація TableService як Singleton
builder.Services.AddSingleton<TableService>();

var app = builder.Build();

// Налаштування HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // За замовчуванням HSTS значення 30 днів. Можна змінити для продукційних середовищ.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

// Маршрутизація для контролерів
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
