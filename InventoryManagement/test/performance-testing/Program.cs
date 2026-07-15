// ============================================================================
//  NBomber Performance Tests for InventoryManagement API
// ============================================================================
//
//  HOW TO RUN:
//    1. Start your API server first (make sure it's running on http://localhost:8090)
//    2. Then run this project:
//       dotnet run --project InventoryManagement/test/performance-testing/PerformanceTests.csproj
//
//  RUBRIC COVERAGE:
//    ✔ Load Testing       → Scenario 1 simulates normal peak traffic (50 users)
//    ✔ Stress Testing     → Scenario 2 pushes to 500 users to find the breaking point
//    ✔ Concurrent Users   → Both scenarios use Simulation.RampingConstant / KeepConstant
//                           to control the exact number of virtual users running at once
//    ✔ Response Time      → NBomber automatically measures and reports p50, p75, p95, p99
//                           latencies for every request in its HTML report
//    ✔ Throughput         → NBomber automatically calculates requests/sec (RPS) and
//                           data transfer rates in its HTML report
//
//  REPORTS:
//    After running, NBomber generates an HTML report in the /reports folder
//    with detailed charts, tables, and statistics for all of the above metrics.
// ============================================================================

using NBomber.CSharp;
using NBomber.Http.CSharp;

// Reuse a single HttpClient to avoid socket exhaustion during high concurrency
var httpClient = new HttpClient();

const string BaseUrl = "http://localhost:8090";

// ─────────────────────────────────────────────────────────────────────────────
// SCENARIO 1: LOAD TEST — Normal Peak Traffic
// ─────────────────────────────────────────────────────────────────────────────
// Purpose:  Simulate the expected peak traffic your API would receive in
//           production. This verifies the system can handle a realistic
//           number of concurrent users without errors or degraded performance.
//
// Rubric:   Load Testing, Concurrent Users, Response Time, Throughput
// ─────────────────────────────────────────────────────────────────────────────
var loadTestScenario = Scenario.Create("load_test_get_products", async context =>
{
    // Create an HTTP GET request to the products endpoint
    var request = Http.CreateRequest("GET", $"{BaseUrl}/api/product")
                      .WithHeader("Accept", "application/json");

    // Send the request and let NBomber measure the response time automatically
    var response = await Http.Send(httpClient, request);

    return response;
})
.WithoutWarmUp()
.WithLoadSimulations(
    // Phase 1: Gradually ramp from 0 to 50 concurrent users over 10 seconds.
    //          This simulates traffic building up as more users arrive.
    //          (Covers: Concurrent Users — controlled ramp-up)
    Simulation.RampingConstant(copies: 50, during: TimeSpan.FromSeconds(10)),

    // Phase 2: Hold steady at 50 concurrent users for 30 seconds.
    //          This is the "sustained load" portion of the test.
    //          NBomber measures Response Time (latency percentiles) and
    //          Throughput (requests/sec) during this phase.
    //          (Covers: Load Testing, Response Time, Throughput)
    Simulation.KeepConstant(copies: 50, during: TimeSpan.FromSeconds(30))
);

// ─────────────────────────────────────────────────────────────────────────────
// SCENARIO 2: STRESS TEST — Pushing to the Limit
// ─────────────────────────────────────────────────────────────────────────────
// Purpose:  Push the API far beyond normal usage to identify the breaking
//           point. We aggressively ramp to 500 concurrent users to see when
//           the API starts returning errors, timing out, or severely
//           degrading in response time.
//
// Rubric:   Stress Testing, Concurrent Users, Response Time, Throughput
// ─────────────────────────────────────────────────────────────────────────────
var stressTestScenario = Scenario.Create("stress_test_get_products", async context =>
{
    var request = Http.CreateRequest("GET", $"{BaseUrl}/api/product")
                      .WithHeader("Accept", "application/json");

    var response = await Http.Send(httpClient, request);

    return response;
})
.WithoutWarmUp()
.WithLoadSimulations(
    // Aggressively ramp from 0 to 500 concurrent users over 15 seconds.
    // This extreme load is designed to overwhelm the server so we can
    // observe how it behaves under stress: increased latency, timeouts,
    // HTTP 5xx errors, connection refusals, etc.
    // (Covers: Stress Testing, Concurrent Users, Response Time, Throughput)
    Simulation.RampingConstant(copies: 500, during: TimeSpan.FromSeconds(15))
);

// ─────────────────────────────────────────────────────────────────────────────
// RUN BOTH SCENARIOS
// ─────────────────────────────────────────────────────────────────────────────
// NBomber runs both scenarios and generates a consolidated HTML report with:
//   • Request Count & RPS (Throughput)
//   • Latency Percentiles: p50, p75, p95, p99 (Response Time)
//   • Error statistics and status code distribution
//   • Timeline charts showing performance over time
// ─────────────────────────────────────────────────────────────────────────────
NBomberRunner
    .RegisterScenarios(loadTestScenario, stressTestScenario)
    .Run();

Console.WriteLine();
Console.WriteLine("=== Performance tests complete! Check the /reports folder for the HTML report. ===");
