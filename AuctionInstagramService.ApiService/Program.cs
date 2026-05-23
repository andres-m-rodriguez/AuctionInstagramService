using AuctionInstagramService.Contracts;
using AuctionInstagramService.Database;
using AuctionInstagramService.DataAccess;
using AuctionInstagramService.ServiceDefaults.Auth;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

builder.AddAuctionDatabase();
builder.AddRedisClient("redis");
builder.AddAzureBlobServiceClient("blobs");
builder.Services.AddAuctionDataAccess();
builder.Services.AddMockAuth();

// Add services to the container.
builder.Services.AddProblemDetails();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AuctionDbContext>();
    await db.Database.MigrateAsync();
}

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => "Auction API is running.");

var auctions = app.MapGroup("/auctions").RequireAuthorization();

auctions.MapGet("/", (AuctionService svc, CancellationToken ct) => svc.ListAsync(ct));

auctions.MapGet("/{id:guid}", async (Guid id, AuctionService svc, CancellationToken ct) =>
    await svc.GetAsync(id, ct) is { } a ? Results.Ok(a) : Results.NotFound());

auctions.MapPost("/", async (AuctionDto dto, ICurrentUser user, AuctionService svc, CancellationToken ct) =>
{
    var created = await svc.CreateAsync(dto with { CreatedByUserId = user.Get().UserId }, ct);
    return Results.Created($"/auctions/{created.Id}", created);
});

var images = app.MapGroup("/auctions/{auctionId:guid}/images").RequireAuthorization();

images.MapGet("/", (Guid auctionId, AuctionImageService svc, CancellationToken ct) =>
    svc.ListForAuctionAsync(auctionId, ct));

images.MapPost("/", async (Guid auctionId, IFormFile file, AuctionImageService svc, CancellationToken ct) =>
{
    await using var stream = file.OpenReadStream();
    var created = await svc.UploadAsync(auctionId, stream, file.ContentType, ct);
    return Results.Created($"/images/{created.Id}", created);
}).DisableAntiforgery();

app.MapGet("/images/{id:guid}", async (Guid id, AuctionImageService svc, CancellationToken ct) =>
{
    var result = await svc.DownloadAsync(id, ct);
    return result is { } r ? Results.File(r.Content, r.ContentType) : Results.NotFound();
}).RequireAuthorization();

var bids = app.MapGroup("/auctions/{auctionId:guid}/bids").RequireAuthorization();

bids.MapGet("/", (Guid auctionId, BidService svc, CancellationToken ct) =>
    svc.ListForAuctionAsync(auctionId, ct));

bids.MapPost("/", async (Guid auctionId, PlaceBidRequest req, ICurrentUser user, BidService svc, CancellationToken ct) =>
{
    var result = await svc.PlaceAsync(auctionId, req.Amount, user.Get().UserId, ct);
    return result.Match<IResult>(
        bid => Results.Created($"/auctions/{auctionId}/bids/{bid.Id}", bid),
        _ => Results.NotFound(new { error = "We couldn't find that auction." }),
        _ => Results.Conflict(new { error = "This auction isn't open for bidding yet." }),
        _ => Results.Conflict(new { error = "This auction has ended." }),
        _ => Results.UnprocessableEntity(new { error = "Your bid must be higher than the current highest bid." }));
});

app.MapDefaultEndpoints();

app.Run();
