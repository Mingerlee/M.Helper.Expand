using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using M.Simple.AspNetCore.Response.Swagger;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace M.Simple.AspNetCore.Response.Test;

[TestClass]
public sealed class ResponseIntegrationTests
{
    private WebApplication _app = null!;
    private HttpClient _client = null!;

    [TestInitialize]
    public async Task Initialize()
    {
        _app = await CreateApp();
        _client = _app.GetTestClient();
    }

    private static async Task<WebApplication> CreateApp(Action<UnifiedResponseOptions>? configure = null)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders().AddConsole();
        builder.Services.AddUnifiedResponse(configure);
        builder.Services.AddUnifiedResponse(); // Repeated registration must not double-wrap responses.
        builder.Services.AddControllers().AddApplicationPart(typeof(ResponseProbeController).Assembly)
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = null;
                options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            });
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddUnifiedResponseSwagger();
        builder.Services.AddUnifiedResponseSwagger();
        var app = builder.Build();
        app.UseUnifiedResponse();
        app.UseUnifiedResponse();
        app.UseSwagger();
        app.MapControllers();
        await app.StartAsync();
        return app;
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        _client.Dispose();
        await _app.DisposeAsync();
    }

    private static async Task<JsonElement> Read(HttpResponseMessage response)
        => (await response.Content.ReadFromJsonAsync<JsonElement>());

    [TestMethod]
    public async Task TypedResultIsWrappedOnceAndUsesFixedFieldNames()
    {
        var response = await _client.GetAsync("/probe/typed");
        var json = await Read(response);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual(200, json.GetProperty("code").GetInt32());
        Assert.AreEqual("成功", json.GetProperty("message").GetString());
        Assert.AreEqual("example", json.GetProperty("data").GetProperty("Name").GetString());
        Assert.AreEqual(3, json.EnumerateObject().Count());
    }

    [TestMethod]
    public async Task ArrayAndJsonResultsAreWrapped()
    {
        var array = await Read(await _client.GetAsync("/probe/array"));
        Assert.AreEqual(2, array.GetProperty("data").GetArrayLength());
        var json = await Read(await _client.GetAsync("/probe/json"));
        Assert.AreEqual("json", json.GetProperty("data").GetString());
    }

    [TestMethod]
    public async Task CreatedResultKeepsStatusAndLocation()
    {
        var response = await _client.GetAsync("/probe/created");
        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
        Assert.AreEqual("/resource/1", response.Headers.Location!.OriginalString);
        Assert.AreEqual(201, (await Read(response)).GetProperty("code").GetInt32());
    }

    [TestMethod]
    public async Task NullAndNoContentReturnEnvelopeWithExplicitNull()
    {
        foreach (var route in new[] { "null", "empty" })
        {
            var response = await _client.GetAsync("/probe/" + route);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.AreEqual(JsonValueKind.Null, (await Read(response)).GetProperty("data").ValueKind);
        }
    }

    [TestMethod]
    public async Task ValidationFailureRetainsErrorsAnd400Status()
    {
        var response = await _client.PostAsJsonAsync("/probe/validation", new { });
        var json = await Read(response);
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.AreEqual(400, json.GetProperty("code").GetInt32());
        Assert.AreEqual("参数校验失败", json.GetProperty("message").GetString());
        Assert.IsTrue(json.GetProperty("data").GetProperty("Name").GetArrayLength() > 0);
    }

    [TestMethod]
    public async Task MalformedJsonReturnsValidationEnvelope()
    {
        var response = await _client.PostAsync("/probe/validation", new StringContent("{invalid", Encoding.UTF8, "application/json"));
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.AreEqual(400, (await Read(response)).GetProperty("code").GetInt32());
    }

    [TestMethod]
    public async Task ExceptionReturnsSafe500WithNullData()
    {
        var response = await _client.GetAsync("/probe/exception");
        Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode);
        var raw = await response.Content.ReadAsStringAsync();
        Assert.IsFalse(raw.Contains("PRIVATE_EXCEPTION_DETAIL"));
        var json = JsonSerializer.Deserialize<JsonElement>(raw);
        Assert.AreEqual(500, json.GetProperty("code").GetInt32());
        Assert.AreEqual("服务器内部错误", json.GetProperty("message").GetString());
        Assert.AreEqual(JsonValueKind.Null, json.GetProperty("data").ValueKind);
    }

    [TestMethod]
    public async Task UnknownRouteAndUnauthorizedKeepErrorStatus()
    {
        foreach (var item in new[] { ("/missing-route", 404), ("/probe/unauthorized", 401) })
        {
            var response = await _client.GetAsync(item.Item1);
            Assert.AreEqual(item.Item2, (int)response.StatusCode);
            Assert.AreEqual(item.Item2, (await Read(response)).GetProperty("code").GetInt32());
        }
    }

    [TestMethod]
    public async Task ProblemDetailsAreNormalizedWithoutLeakingDetails()
    {
        var response = await _client.GetAsync("/probe/problem");
        var raw = await response.Content.ReadAsStringAsync();
        Assert.AreEqual(HttpStatusCode.Conflict, response.StatusCode);
        Assert.IsFalse(raw.Contains("PRIVATE_PROBLEM_DETAIL"));
        Assert.AreEqual(409, JsonSerializer.Deserialize<JsonElement>(raw).GetProperty("code").GetInt32());
    }

    [TestMethod]
    public async Task PreWrappedResultIsNotWrappedAgain()
    {
        var json = await Read(await _client.GetAsync("/probe/wrapped"));
        Assert.AreEqual("manual", json.GetProperty("message").GetString());
        Assert.AreEqual("value", json.GetProperty("data").GetString());
    }

    [TestMethod]
    public async Task SkipAttributePreservesResultAndValidationFormat()
    {
        var json = await Read(await _client.GetAsync("/probe/raw"));
        Assert.AreEqual("raw", json.GetProperty("Name").GetString());
        Assert.IsFalse(json.TryGetProperty("code", out _));
        var invalid = await Read(await _client.PostAsJsonAsync("/probe/raw-validation", new { }));
        Assert.IsTrue(invalid.TryGetProperty("errors", out _) || invalid.TryGetProperty("Errors", out _));
        Assert.IsFalse(invalid.TryGetProperty("code", out _));
        var classResult = await _client.GetStringAsync("/raw-class");
        Assert.AreEqual("raw-class", classResult);
    }

    [TestMethod]
    public async Task FileTextRedirectAnd304KeepOriginalSemantics()
    {
        var file = await _client.GetAsync("/probe/file");
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, await file.Content.ReadAsByteArrayAsync());
        Assert.AreEqual("application/octet-stream", file.Content.Headers.ContentType!.MediaType);
        var text = await _client.GetAsync("/probe/text");
        Assert.AreEqual("plain text", await text.Content.ReadAsStringAsync());
        var redirect = await _client.GetAsync("/probe/redirect");
        Assert.AreEqual(HttpStatusCode.Redirect, redirect.StatusCode);
        Assert.AreEqual("/probe/typed", redirect.Headers.Location!.OriginalString);
        var notModified = await _client.GetAsync("/probe/not-modified");
        Assert.AreEqual(HttpStatusCode.NotModified, notModified.StatusCode);
        Assert.AreEqual("", await notModified.Content.ReadAsStringAsync());
    }

    [TestMethod]
    public async Task SkippedExceptionKeepsEmpty500()
    {
        var response = await _client.GetAsync("/probe/raw-exception");
        Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.AreEqual("", await response.Content.ReadAsStringAsync());
    }

    [TestMethod]
    public async Task StartedResponseIsNotWrapped()
    {
        var response = await _client.GetAsync("/probe/started");
        Assert.AreEqual("already started", await response.Content.ReadAsStringAsync());
    }

    [TestMethod]
    public async Task ObjectResultWith204IsChangedTo200AndKeepsData()
    {
        var response = await _client.GetAsync("/probe/empty-object");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual("discarded", (await Read(response)).GetProperty("data").GetProperty("Name").GetString());
    }
    [TestMethod]
    public async Task CustomMessagesApplyToSuccessValidationAndException()
    {
        await using var app = await CreateApp(options =>
        {
            options.SuccessMessage = "OK";
            options.ValidationErrorMessage = "Invalid";
            options.ExceptionMessage = "Failed";
            options.StatusMessageFactory = status => status == 404 ? "Missing" : null;
        });
        using var client = app.GetTestClient();
        Assert.AreEqual("OK", (await Read(await client.GetAsync("/probe/typed"))).GetProperty("message").GetString());
        Assert.AreEqual("Failed", (await Read(await client.GetAsync("/probe/exception"))).GetProperty("message").GetString());
        Assert.AreEqual("Missing", (await Read(await client.GetAsync("/missing"))).GetProperty("message").GetString());
        Assert.AreEqual("Invalid", (await Read(await client.PostAsJsonAsync("/probe/validation", new { }))).GetProperty("message").GetString());
    }

    [TestMethod]
    public async Task SwaggerReflectsEnvelopeAndPreservesDataTypeAndOptOut()
    {
        var doc = await Read(await _client.GetAsync("/swagger/v1/swagger.json"));
        var paths = doc.GetProperty("paths");
        var responses = paths.GetProperty("/probe/typed").GetProperty("get").GetProperty("responses");
        var schema = responses.GetProperty("200").GetProperty("content").GetProperty("application/json").GetProperty("schema");
        var props = schema.GetProperty("properties");
        Assert.IsTrue(props.TryGetProperty("code", out _));
        Assert.IsTrue(props.GetProperty("data").GetProperty("allOf")[0].TryGetProperty("$ref", out _));
        Assert.IsTrue(responses.TryGetProperty("400", out _));
        Assert.IsTrue(responses.TryGetProperty("500", out _));
        var raw = paths.GetProperty("/probe/raw").GetProperty("get").GetProperty("responses").GetProperty("200").GetProperty("content").GetProperty("application/json").GetProperty("schema");
        Assert.IsTrue(raw.TryGetProperty("$ref", out _));
        var empty = paths.GetProperty("/probe/empty").GetProperty("get").GetProperty("responses");
        Assert.IsTrue(empty.TryGetProperty("200", out _));
        Assert.IsFalse(empty.TryGetProperty("204", out _));
    }
}

public sealed record ProbeData(string Name);
public sealed record ProbeInput([Required] string Name);

[ApiController, Route("probe")]
public sealed class ResponseProbeController : ControllerBase
{
    [HttpGet("typed")]
    public ActionResult<ProbeData> Typed() => Ok(new ProbeData("example"));
    [HttpGet("array")]
    public int[] ArrayResult() => new[] { 1, 2 };
    [HttpGet("json")]
    public JsonResult JsonResponse() => new("json");
    [HttpGet("created")]
    public IActionResult CreatedResponse() => Created("/resource/1", new ProbeData("created"));
    [HttpGet("null")]
    public IActionResult NullResponse() => Ok(null);
    [HttpGet("empty"), ProducesResponseType(204)]
    public IActionResult EmptyResponse() => NoContent();
    [HttpGet("empty-object")]
    public IActionResult EmptyObject() => StatusCode(204, new ProbeData("discarded"));
    [HttpPost("validation")]
    public IActionResult Validate(ProbeInput input) => Ok(input);
    [HttpGet("exception")]
    public IActionResult Fail() => throw new InvalidOperationException("PRIVATE_EXCEPTION_DETAIL");
    [HttpGet("unauthorized")]
    public IActionResult Denied() => Unauthorized();
    [HttpGet("problem")]
    public IActionResult ProblemResponse() => Problem(statusCode: 409, detail: "PRIVATE_PROBLEM_DETAIL");
    [HttpGet("wrapped")]
    public ActionResult<ApiResponse<string>> Wrapped() => Ok(new ApiResponse<string>(200, "manual", "value"));
    [HttpGet("raw"), SkipUnifiedResponse]
    public ActionResult<ProbeData> Raw() => Ok(new ProbeData("raw"));
    [HttpPost("raw-validation"), SkipUnifiedResponse]
    public IActionResult RawValidation(ProbeInput input) => Ok(input);
    [HttpGet("file"), Produces("application/octet-stream")]
    public IActionResult Download() => File(new byte[] { 1, 2, 3 }, "application/octet-stream", "sample.bin");
    [HttpGet("text"), Produces("text/plain")]
    public IActionResult Text() => Content("plain text");
    [HttpGet("redirect")]
    public IActionResult RedirectResponse() => Redirect("/probe/typed");
    [HttpGet("raw-exception"), SkipUnifiedResponse]
    public IActionResult RawException() => throw new InvalidOperationException("PRIVATE_EXCEPTION_DETAIL");
    [HttpGet("started"), Produces("text/plain")]
    public async Task<IActionResult> Started()
    {
        Response.ContentType = "text/plain";
        await Response.WriteAsync("already started");
        return new EmptyResult();
    }
    [HttpGet("not-modified")]
    public IActionResult NotModified() => StatusCode(304);
}

[ApiController, Route("raw-class"), SkipUnifiedResponse]
public sealed class RawProbeController : ControllerBase
{
    [HttpGet]
    public ActionResult<string> Get() => "raw-class";
}
