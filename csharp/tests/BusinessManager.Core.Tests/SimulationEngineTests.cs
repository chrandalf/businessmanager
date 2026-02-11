using BusinessManager.Core;

namespace BusinessManager.Core.Tests;

public sealed class SimulationEngineTests
{
    [Fact]
    public void SeededRunsStayDeterministic()
    {
        var a = SimulationEngine.FromJson("../../../../data/mvp_scenario.json", 123);
        var b = SimulationEngine.FromJson("../../../../data/mvp_scenario.json", 123);

        for (var i = 0; i < 12; i++)
        {
            a.ApplyInitiatives(new[] { "Automation Upgrade", "Leadership Development" });
            b.ApplyInitiatives(new[] { "Automation Upgrade", "Leadership Development" });
            a.RunMonth();
            b.RunMonth();
        }

        Assert.Equal(a.Kpis(), b.Kpis());
    }

    [Fact]
    public void DashboardHasOverviewAndBusinessUnits()
    {
        var sim = SimulationEngine.FromJson("../../../../data/mvp_scenario.json", 42);
        sim.RunMonth();
        var dashboard = sim.Dashboard();

        Assert.True(dashboard.ContainsKey("overview"));
        Assert.True(dashboard.ContainsKey("businessUnits"));
        Assert.True(dashboard.ContainsKey("initiatives"));
    }
}
