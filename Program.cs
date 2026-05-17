using TestProject.FileBrowser;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

string homeDirectory = builder.Configuration["FileBrowser:HomeDirectory"]
    ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "FileBrowserHome");

builder.Services.Configure<BrowseSystemOptions>(o => o.HomeDirectory = homeDirectory);
builder.Services.AddSingleton<BrowseSystem>();

WebApplication app = builder.Build();

//Immediatley load to construct radix tree
app.Services.GetRequiredService<BrowseSystem>();

//Handles the edgecase if the home directory in the config doesn't exist
if (!Directory.Exists(homeDirectory))
{
    Directory.CreateDirectory(homeDirectory);
}

app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapControllers();

app.Run();
