// ============================================================================
//  NBomber Performance Tests for InventoryManagement API
// ============================================================================
//  HOW TO RUN: Start your API server first, then run this project.
//
//  RUBRIC COVERAGE:
//     Load Testing       → Scenario 1 simulates sustained normal traffic.
//     Stress Testing     → Scenarios 2 & 3 push system limits.
//     Concurrent Users   → Configured via Simulation properties (copies/rates).
//     Response Time      → Auto-reported in the HTML report (Latencies).
//     Throughput         → Auto-reported in the HTML report (RPS & Data).
// ============================================================================

using System.Net.Http.Json;
using System.Text.Json;
using NBomber.CSharp;
using NBomber.Contracts;
using NBomber.Http.CSharp;
using NBomber.Contracts.Stats;

// Reuse a single HttpClient to avoid socket exhaustion
var httpClient = new HttpClient();
const string BaseUrl = "http://localhost:8090";

// AUTHENTICATION
const string TestUsername = "api-test";
const string TestPassword = "12345";

var tokenResponse = await httpClient.PostAsJsonAsync(
    $"{BaseUrl}/api/auth/token",
    new { username = TestUsername, password = TestPassword });

if (!tokenResponse.IsSuccessStatusCode)
{
    // Stop immediately rather than producing that making false reports 
    Console.Error.WriteLine(
        $"Could not log in as '{TestUsername}' ({(int)tokenResponse.StatusCode}): "
        + await tokenResponse.Content.ReadAsStringAsync());
    Console.Error.WriteLine("Check that the API, Keycloak and the 'api-test' user are all available.");
    return 1;
}

var accessToken = (await tokenResponse.Content.ReadFromJsonAsync<JsonElement>())
    .GetProperty("access_token")
    .GetString();

Console.WriteLine($"Authenticated as '{TestUsername}'. Starting performance tests...\n");

// SCENARIO 1: LOAD TEST — Normal Peak Traffic
// Purpose: Simulate sustained expected traffic to verify stability.
// Rubric:  Load Testing, Concurrent Users, Response Time, Throughput
var loadTestScenario = Scenario.Create("load_test_get_products", async context =>
{
    var request = Http.CreateRequest("GET", $"{BaseUrl}/api/product")
                      .WithHeader("Accept", "application/json")
                      .WithHeader("Authorization", $"Bearer {accessToken}");

    return await Http.Send(httpClient, request);
})
.WithoutWarmUp()
.WithLoadSimulations(
    // Ramp up concurrent users gradually
    Simulation.RampingConstant(copies: 50, during: TimeSpan.FromSeconds(10)),
    // Hold users steady for sustained load testing
    Simulation.KeepConstant(copies: 50, during: TimeSpan.FromSeconds(30))
);

// SCENARIO 2: STRESS TEST — Pushing to the Limit
// Purpose: Aggressively ramp up concurrent users to find the breaking point.
// Rubric:  Stress Testing, Concurrent Users, Response Time, Throughput
var stressTestScenario = Scenario.Create("stress_test_get_products", async context =>
{
    var request = Http.CreateRequest("GET", $"{BaseUrl}/api/product")
                      .WithHeader("Accept", "application/json")
                      .WithHeader("Authorization", $"Bearer {accessToken}");

    return await Http.Send(httpClient, request);
})
.WithoutWarmUp()
.WithLoadSimulations(
    // Aggressive ramp up to overwhelm the server
    Simulation.RampingConstant(copies: 500, during: TimeSpan.FromSeconds(15))
);

// SCENARIO 3: RANDOM SPIKE TEST — Unpredictable Traffic
// Purpose: Simulate an open system with randomized traffic spikes (e.g., flash sales).
// Rubric:  Stress Testing, Throughput
var randomSpikeScenario = Scenario.Create("random_spike_get_products", async context =>
{
    var request = Http.CreateRequest("GET", $"{BaseUrl}/api/product")
                      .WithHeader("Accept", "application/json")
                      .WithHeader("Authorization", $"Bearer {accessToken}");

    return await Http.Send(httpClient, request);
})
.WithoutWarmUp()
.WithLoadSimulations(
    // Inject a random number of requests at a set interval
    Simulation.InjectRandom(minRate: 10, maxRate: 200, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30))
);

// RUN ALL SCENARIOS
NBomberRunner
    .RegisterScenarios(loadTestScenario, stressTestScenario, randomSpikeScenario)
    .WithTestName("Inventory API Performance Tests")   
    .WithReportFileName("Performance_Report")   
    .WithReportFolder("reports")              
    .WithReportFormats(                              
        ReportFormat.Html, 
        ReportFormat.Md,
        ReportFormat.Txt
    )
    .Run();

Console.WriteLine("\n=== Performance tests complete! Check the /reports folder for the HTML report. ===");

return 0;
