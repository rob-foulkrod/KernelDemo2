var builder = WebApplication.CreateBuilder(args);


// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>();
}

// Create HTTP named Factory for HomeAssistant
builder.Services.AddHttpClient("HomeAssistantClient", client =>
{
    var baseAddress = builder.Configuration["HOMEASSISTANTAPI:BASEADDRESS"] ?? "notfound";
    var apiKey = builder.Configuration["HOMEASSISTANTAPI:TOKEN"] ?? "notfound";

    client.BaseAddress = new Uri(baseAddress);
    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection();

var lightsInRooms = new Dictionary<string, string>
{
    {"light.studio_fan_light", "Studio"},
    {"light.studio_overhead_light", "Studio"},
    {"light.greatroomfanlights", "GreatRoom"},
    {"light.smartbulb1", "Bedroom"},
    {"light.cornerlamp1_studio_plug", "Studio"},
    {"light.cornerlamp2_studio_plug", "Studio"},
    {"light.lamp_frontroom_plug", "FrontRoom"},
    {"light.lamp_greatroom_plug", "GreatRoom"},
    {"light.greatroom_lamp_plug", "GreatRoom"},
    {"light.frontroom_lamp_plug", "FrontRoom"},
    {"light.bedroom_nightstand_bulb", "Bedroom"},
    {"light.bulb_hue2", "Office"},
    {"light.left_light", "Office" },
    {"light.office_desk_lightstrip", "Office" }
};

app.MapGet("/lights", (string room) =>
{
    var filteredLights = lightsInRooms
        .Where(pair => pair.Value.Equals(room, StringComparison.OrdinalIgnoreCase))
        .Select(pair => pair.Key)
        .ToList();

    if (filteredLights.Any())
    {
        return Results.Ok(filteredLights);
    }
    else
    {
        return Results.NotFound($"No lights found for room: {room}");
    }
})
.WithName("GetLightsByRoom")
.WithDescription("Get the EntityID of lights by room")
.Produces<List<string>>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound);

app.MapGet("/lightstate/{entityId}", async (string entityId, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient("HomeAssistantClient");
    try
    {
        // Assuming the HomeAssistant API has an endpoint like `/api/states/{entity_id}` to get the state of a light
        var response = await client.GetAsync($"/api/states/{entityId}");

        if (response.IsSuccessStatusCode)
        {
            var lightState = await response.Content.ReadFromJsonAsync<LightState>();
            return Results.Ok(lightState);
        }
        else
        {
            // Log the error or handle it as needed
            return Results.Problem("Failed to retrieve the light state from HomeAssistant API.", statusCode: (int)response.StatusCode);
        }
    }
    catch (Exception)
    {
        // Log the exception or handle it as needed
        return Results.Problem("An error occurred while attempting to retrieve the light state.", statusCode: 500);
    }
})
.WithName("GetLightState")
.WithDescription("Get the state of a light by its entity ID")
.Produces<LightState>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound)
.Produces(StatusCodes.Status500InternalServerError);

app.MapPost("/lightsoff/{entityId}", async (string entityId, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient("HomeAssistantClient");
    try
    {
        // Assuming the HomeAssistant API has an endpoint like `/api/services/light/turn_off` to turn a light off
        // and requires a JSON body with the entity_id
        var content = JsonContent.Create(new { entity_id = entityId });
        var response = await client.PostAsync("/api/services/light/turn_off", content);



        if (response.IsSuccessStatusCode)
        {
            return Results.Ok($"The light with EntityID {entityId} has been turned off.");
        }
        else
        {
            // Log the error or handle it as needed
            return Results.Problem("Failed to turn off the light using the HomeAssistant API.", statusCode: (int)response.StatusCode);
        }
    }
    catch (Exception)
    {
        // Log the exception or handle it as needed
        return Results.Problem("An error occurred while attempting to turn off the light.", statusCode: 500);
    }
})
.WithName("TurnLightOff")
.WithDescription("Turn off a light by its entity ID")
.Produces<string>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound)
.Produces(StatusCodes.Status500InternalServerError);

app.MapPost("/lightson/{entityId}", async (string entityId, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient("HomeAssistantClient");
    try
    {
        // Assuming the HomeAssistant API has an endpoint like `/api/services/light/turn_on` to turn a light on
        // and requires a JSON body with the entity_id
        var content = JsonContent.Create(new { entity_id = entityId });
        var response = await client.PostAsync("/api/services/light/turn_on", content);

        if (response.IsSuccessStatusCode)
        {
            return Results.Ok($"The light with EntityID {entityId} has been turned on.");
        }
        else
        {
            // Log the error or handle it as needed
            return Results.Problem("Failed to turn on the light using the HomeAssistant API.", statusCode: (int)response.StatusCode);
        }
    }
    catch (Exception)
    {
        // Log the exception or handle it as needed
        return Results.Problem("An error occurred while attempting to turn on the light.", statusCode: 500);
    }
})
.WithName("TurnLightOn")
.WithDescription("Turn on a light by its entity ID")
.Produces<string>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound)
.Produces(StatusCodes.Status500InternalServerError);

app.MapPost("/setbrightness/{entityId}/{brightnessLevel}", async (string entityId, int brightnessLevel, IHttpClientFactory httpClientFactory) =>
{
    // Validate the brightness level
    if (brightnessLevel < 0 || brightnessLevel > 255)
    {
        return Results.BadRequest("Brightness level must be between 0 and 255.");
    }
    var client = httpClientFactory.CreateClient("HomeAssistantClient");
    try
    {
        var content = JsonContent.Create(new { entity_id = entityId, brightness = brightnessLevel });
        var response = await client.PostAsync("/api/services/light/turn_on", content);

        if (response.IsSuccessStatusCode)
        {
            return Results.Ok($"The light with EntityID {entityId} has been turned on.");
        }
        else
        {
            // Log the error or handle it as needed
            return Results.Problem("Failed to turn on the light using the HomeAssistant API.", statusCode: (int)response.StatusCode);
        }
    }
    catch (Exception)
    {
        // Log the exception or handle it as needed
        return Results.Problem("An error occurred while attempting to turn on the light.", statusCode: 500);
    }
})
.WithName("SetLightBrightness")
.WithDescription("Set the brightness of a light by its entity ID where 0 of off and 255 is full brightness")
.Produces<string>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound)
.Produces(StatusCodes.Status500InternalServerError); 

app.Run();


public class LightState
{
    public string entity_id { get; set; }
    public string state { get; set; }
    public Attributes attributes { get; set; }
    public DateTime last_changed { get; set; }
    public DateTime last_reported { get; set; }
    public DateTime last_updated { get; set; }
}

public class Attributes
{
    public string[] supported_color_modes { get; set; }
    public string color_mode { get; set; }
    public byte brightness { get; set; }
    public string friendly_name { get; set; }
}

