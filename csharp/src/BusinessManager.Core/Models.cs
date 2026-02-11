namespace BusinessManager.Core;

public record FinanceState(double Cash, double Debt, double Equity, double Revenue, double Cogs, double Opex, double Capex, double InterestRate);
public record WorkforceState(int Headcount, double Morale, double Productivity, double Burnout, double Attrition);
public record RegionState(string Name, double DemandIndex, double RegulatoryRisk);
public record BusinessUnitState(string Name, double RevenueShare, double BaseMargin, double InnovationFocus);
public record Initiative(string Name, double Budget, string ShortTermImpact, string DelayedImpact, double Confidence, Dictionary<string, double> Effects);
public record SimEvent(string Name, string Category, string Severity, Dictionary<string, double> Effects);

public sealed class CompanyState
{
    public int Month { get; set; } = 1;
    public int Quarter { get; set; } = 1;
    public int Year { get; set; } = 1;

    public double Reputation { get; set; } = 70;
    public double RiskExposure { get; set; } = 35;
    public double InnovationVelocity { get; set; } = 50;
    public double CustomerRetention { get; set; } = 78;
    public double InvestorSentiment { get; set; } = 62;
    public double BoardConfidence { get; set; } = 65;
    public double TechnicalDebt { get; set; } = 35;

    public required FinanceState Finance { get; set; }
    public required WorkforceState Workforce { get; set; }
    public required List<RegionState> Regions { get; set; }
    public required List<BusinessUnitState> BusinessUnits { get; set; }

    public List<Dictionary<string, object>> EventLog { get; set; } = new();
    public List<Dictionary<string, object>> DecisionLog { get; set; } = new();
}

public sealed class Scenario
{
    public required string Name { get; init; }
    public required string Industry { get; init; }
    public required string Difficulty { get; init; }
    public required double EventChance { get; init; }
    public required FinanceState InitialFinance { get; init; }
    public required WorkforceState InitialWorkforce { get; init; }
    public required List<RegionState> Regions { get; init; }
    public required List<BusinessUnitState> BusinessUnits { get; init; }
    public required List<Initiative> Initiatives { get; init; }
    public required List<SimEvent> Events { get; init; }
}
