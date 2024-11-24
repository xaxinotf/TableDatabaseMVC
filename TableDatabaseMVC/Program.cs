using TableDatabaseMVC.Services;

var builder = WebApplication.CreateBuilder(args);

// ������ ������ �� ����������.
builder.Services.AddControllersWithViews();

// ��������� TableService �� Singleton
builder.Services.AddSingleton<TableService>();

var app = builder.Build();

// ������������ HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // �� ������������� HSTS �������� 30 ���. ����� ������ ��� ������������ ���������.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

// ������������� ��� ����������
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
