using BusinessManager.App.Graphics;
using BusinessManager.Core;

namespace BusinessManager.App.Pages;

public partial class DashboardPage : ContentPage
{
    private readonly SimulationEngine _sim;
    private readonly List<float> _trend = new();

    public DashboardPage()
    {
        InitializeComponent();
        var scenarioPath = Path.Combine(FileSystem.AppDataDirectory, "mvp_scenario.json");
        if (!File.Exists(scenarioPath))
        {
            using var source = FileSystem.OpenAppPackageFileAsync("mvp_scenario.json").GetAwaiter().GetResult();
            using var target = File.Create(scenarioPath);
            source.CopyTo(target);
        }

        _sim = SimulationEngine.FromJson(scenarioPath, seed: 42);
        for (var i = 0; i < 6; i++)
        {
            _sim.ApplyInitiatives(new[] { "Automation Upgrade", "Leadership Development" });
            _sim.RunMonth();
            _trend.Add((float)_sim.Kpis()["enterpriseResilienceIndex"]);
        }

        BindDashboard();
    }

    private void OnAdvanceMonthClicked(object sender, EventArgs e)
    {
        if (_sim.State.Month % 3 == 1)
            _sim.ApplyInitiatives(new[] { "Automation Upgrade", "Tech Debt Cleanup" });

        _sim.RunMonth();
        _trend.Add((float)_sim.Kpis()["enterpriseResilienceIndex"]);
        if (_trend.Count > 24) _trend.RemoveAt(0);
        BindDashboard();
    }

    private void BindDashboard()
    {
        var kpi = _sim.Kpis();
        RevenueGrowthLabel.Text = $"{kpi["revenueGrowth"]}%";
        ResilienceLabel.Text = $"{kpi["enterpriseResilienceIndex"]}";
        TrendChart.Drawable = new KpiTrendDrawable(_trend, "Enterprise Resilience Index");
        TrendChart.Invalidate();
    }
}
