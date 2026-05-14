using TestProject.FileBrowser;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

string homeDirectory = builder.Configuration["FileBrowser:HomeDirectory"]
    ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "FileBrowserHome");
builder.Services.AddSingleton(new BrowseSystem(homeDirectory));

WebApplication app = builder.Build();

app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapControllers();

app.Run();
