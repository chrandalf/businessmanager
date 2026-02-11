using System.Text.Json;

namespace BusinessManager.Core;

public sealed class SimulationEngine
{
    private readonly Scenario _scenario;
    private readonly Random _rng;

    public CompanyState State { get; }

    public SimulationEngine(Scenario scenario, int seed)
    {
        _scenario = scenario;
        _rng = new Random(seed);
        State = new CompanyState
        {
            Finance = scenario.InitialFinance,
            Workforce = scenario.InitialWorkforce,
            Regions = scenario.Regions,
            BusinessUnits = scenario.BusinessUnits
        };
    }

    public static SimulationEngine FromJson(string path, int seed)
    {
        var scenario = JsonSerializer.Deserialize<Scenario>(File.ReadAllText(path), JsonOptions())
            ?? throw new InvalidOperationException("Invalid scenario JSON");
        return new SimulationEngine(scenario, seed);
    }

    public IReadOnlyList<Initiative> AvailableInitiatives() => _scenario.Initiatives;

    public void ApplyInitiatives(IEnumerable<string> names)
    {
        var lookup = _scenario.Initiatives.ToDictionary(i => i.Name, i => i);
        foreach (var name in names)
        {
            if (!lookup.TryGetValue(name, out var i))
                continue;

            State.Finance = State.Finance with { Capex = State.Finance.Capex + i.Budget };
            foreach (var kv in i.Effects)
            {
                Shift(kv.Key, kv.Value);
            }

            State.DecisionLog.Add(new Dictionary<string, object>
            {
                ["month"] = State.Month,
                ["initiative"] = i.Name,
                ["budget"] = i.Budget,
                ["confidence"] = i.Confidence,
                ["shortTermImpact"] = i.ShortTermImpact,
                ["delayedImpact"] = i.DelayedImpact
            });
        }
    }

    public Dictionary<string, object?> RunMonth()
    {
        var pre = Kpis();
        var monthlyEvent = TriggerEvent();
        ProcessOperations();
        ProcessFinance();
        ProcessPeople();
        var post = Kpis();

        if (State.Month % 3 == 0)
            QuarterlyBoardReview();

        AdvanceTime();

        return new Dictionary<string, object?>
        {
            ["month"] = State.Month,
            ["event"] = monthlyEvent,
            ["kpis"] = post,
            ["whyChanged"] = Explain(pre, post)
        };
    }

    public Dictionary<string, double> Kpis()
    {
        var revenueGrowth = ((State.Finance.Revenue / _scenario.InitialFinance.Revenue) - 1d) * 100d;
        var operatingMargin = ((State.Finance.Revenue - State.Finance.Cogs - State.Finance.Opex) / Math.Max(1, State.Finance.Revenue)) * 100d;
        var fcf = State.Finance.Revenue - State.Finance.Cogs - State.Finance.Opex - State.Finance.Capex;
        var debtRatio = State.Finance.Debt / Math.Max(1, State.Finance.Equity);
        var runway = fcf < 0 ? State.Finance.Cash / Math.Max(1, -fcf) : 24;

        return new Dictionary<string, double>
        {
            ["revenueGrowth"] = Math.Round(revenueGrowth, 2),
            ["operatingMargin"] = Math.Round(operatingMargin, 2),
            ["freeCashFlow"] = Math.Round(fcf, 2),
            ["debtRatio"] = Math.Round(debtRatio, 2),
            ["liquidityRunway"] = Math.Round(runway, 2),
            ["employeeEngagement"] = Math.Round(State.Workforce.Morale, 2),
            ["attrition"] = Math.Round(State.Workforce.Attrition, 2),
            ["innovationVelocity"] = Math.Round(State.InnovationVelocity, 2),
            ["customerRetention"] = Math.Round(State.CustomerRetention, 2),
            ["riskExposure"] = Math.Round(State.RiskExposure, 2),
            ["reputationScore"] = Math.Round(State.Reputation, 2),
            ["investorSentiment"] = Math.Round(State.InvestorSentiment, 2),
            ["boardConfidence"] = Math.Round(State.BoardConfidence, 2),
            ["enterpriseResilienceIndex"] = Math.Round(EnterpriseResilienceIndex(), 2)
        };
    }

    public string VictoryStatus()
    {
        var kpis = Kpis();
        if (State.Finance.Cash <= 0 && kpis["freeCashFlow"] < 0) return "loss_insolvency";
        if (State.BoardConfidence < 25) return "loss_board_removal";
        if (State.Reputation < 20) return "loss_reputation_collapse";
        if (State.Year >= 5 && kpis["enterpriseResilienceIndex"] > 65 && kpis["operatingMargin"] > 10) return "win";
        return "ongoing";
    }

    public Dictionary<string, object> Dashboard()
    {
        return new Dictionary<string, object>
        {
            ["time"] = new Dictionary<string, int> { ["year"] = State.Year, ["quarter"] = State.Quarter, ["month"] = State.Month },
            ["overview"] = Kpis(),
            ["businessUnits"] = State.BusinessUnits,
            ["initiatives"] = _scenario.Initiatives,
            ["events"] = State.EventLog.TakeLast(5).ToList()
        };
    }

    private Dictionary<string, object>? TriggerEvent()
    {
        if (_rng.NextDouble() > _scenario.EventChance)
            return null;

        var e = _scenario.Events[_rng.Next(0, _scenario.Events.Count)];
        foreach (var kv in e.Effects)
            Shift(kv.Key, kv.Value * (1 + ((_rng.NextDouble() * 0.3) - 0.15)));

        var record = new Dictionary<string, object>
        {
            ["month"] = State.Month,
            ["name"] = e.Name,
            ["category"] = e.Category,
            ["severity"] = e.Severity
        };
        State.EventLog.Add(record);
        return record;
    }

    private void ProcessOperations()
    {
        var demand = State.Regions.Average(r => r.DemandIndex);
        var capacityPressure = Math.Max(0, State.RiskExposure - 40) / 100d;
        var throughput = Math.Max(0.6, State.Workforce.Productivity / 100d - capacityPressure);
        var qualityPenalty = Math.Max(0, (State.TechnicalDebt - 40) / 300d);

        var revenue = State.Finance.Revenue * (demand / 100d) * throughput;
        revenue *= 1 + ((State.Reputation - 50) / 500d);
        revenue *= 1 - qualityPenalty;
        var cogs = revenue * Math.Min(0.9, 0.52 + capacityPressure + (State.Workforce.Burnout / 500d));

        State.Finance = State.Finance with { Revenue = Math.Max(0, revenue), Cogs = cogs };
    }

    private void ProcessFinance()
    {
        var interest = State.Finance.Debt * (State.Finance.InterestRate / 12d);
        var opex = State.Finance.Revenue * 0.22 + State.Workforce.Headcount * 120;
        var fcf = State.Finance.Revenue - State.Finance.Cogs - opex - State.Finance.Capex - interest;

        var cash = State.Finance.Cash + fcf;
        var debt = State.Finance.Debt;
        if (cash < 0)
        {
            debt += Math.Abs(cash) * 1.2;
            cash = 0;
        }

        State.Finance = State.Finance with { Cash = cash, Debt = debt, Opex = opex, Capex = 0 };
        State.InvestorSentiment = Clamp(State.InvestorSentiment + Math.Clamp(fcf / 50000d, -15, 15));
    }

    private void ProcessPeople()
    {
        var pressure = (State.RiskExposure / 200d) + (State.TechnicalDebt / 300d);
        var burnout = Clamp(State.Workforce.Burnout + pressure * 4);
        var morale = Clamp(State.Workforce.Morale + 1.8 - pressure * 6 + (State.Reputation - 50) / 100d);
        var attrition = Clamp(6 + (100 - morale) / 8 + burnout / 10);
        var productivity = Clamp(65 + morale / 3 - burnout / 4 + State.InnovationVelocity / 8);
        var headcount = Math.Max(50, State.Workforce.Headcount + (int)((70 - attrition) / 8));

        State.Workforce = new WorkforceState(headcount, morale, productivity, burnout, attrition);
    }

    private void QuarterlyBoardReview()
    {
        var k = Kpis();
        var delta = 0;
        if (k["freeCashFlow"] > 0) delta += 2;
        if (k["riskExposure"] > 70) delta -= 3;
        if (k["innovationVelocity"] > 60) delta += 1;
        if (k["employeeEngagement"] < 45) delta -= 2;
        State.BoardConfidence = Clamp(State.BoardConfidence + delta);
    }

    private void AdvanceTime()
    {
        State.Month++;
        State.Quarter = ((State.Month - 1) / 3) % 4 + 1;
        State.Year = ((State.Month - 1) / 12) + 1;

        if (State.Month % 12 == 1 && State.Month > 1)
        {
            State.TechnicalDebt = Clamp(State.TechnicalDebt + (_rng.NextDouble() * 10 - 3));
        }
    }

    private void Shift(string field, double delta)
    {
        switch (field)
        {
            case "riskExposure": State.RiskExposure = Clamp(State.RiskExposure + delta); break;
            case "reputation": State.Reputation = Clamp(State.Reputation + delta); break;
            case "innovationVelocity": State.InnovationVelocity = Clamp(State.InnovationVelocity + delta); break;
            case "customerRetention": State.CustomerRetention = Clamp(State.CustomerRetention + delta); break;
            case "investorSentiment": State.InvestorSentiment = Clamp(State.InvestorSentiment + delta); break;
            case "boardConfidence": State.BoardConfidence = Clamp(State.BoardConfidence + delta); break;
            case "technicalDebt": State.TechnicalDebt = Clamp(State.TechnicalDebt + delta); break;
            case "finance.cash": State.Finance = State.Finance with { Cash = Math.Max(0, State.Finance.Cash + delta) }; break;
            case "finance.debt": State.Finance = State.Finance with { Debt = Math.Max(0, State.Finance.Debt + delta) }; break;
            case "finance.revenue": State.Finance = State.Finance with { Revenue = Math.Max(0, State.Finance.Revenue + delta) }; break;
            case "finance.opex": State.Finance = State.Finance with { Opex = Math.Max(0, State.Finance.Opex + delta) }; break;
            case "workforce.morale": State.Workforce = State.Workforce with { Morale = Clamp(State.Workforce.Morale + delta) }; break;
            case "workforce.productivity": State.Workforce = State.Workforce with { Productivity = Clamp(State.Workforce.Productivity + delta) }; break;
            case "workforce.burnout": State.Workforce = State.Workforce with { Burnout = Clamp(State.Workforce.Burnout + delta) }; break;
            case "workforce.attrition": State.Workforce = State.Workforce with { Attrition = Clamp(State.Workforce.Attrition + delta) }; break;
        }
    }

    private Dictionary<string, string> Explain(Dictionary<string, double> pre, Dictionary<string, double> post)
    {
        var res = new Dictionary<string, string>();
        foreach (var kv in post)
        {
            var delta = kv.Value - pre[kv.Key];
            if (Math.Abs(delta) < 0.3) continue;
            res[kv.Key] = $"{kv.Key} {(delta > 0 ? "increased" : "decreased")} by {Math.Abs(delta):F2} from operations, decisions, and events.";
        }
        return res;
    }

    private double EnterpriseResilienceIndex()
    {
        var debtPenalty = Math.Min(80, State.Finance.Debt / Math.Max(1, State.Finance.Equity) * 25);
        var financeStrength = Math.Max(0, 100 - debtPenalty + State.InvestorSentiment / 2);
        var adaptability = (State.InnovationVelocity + State.Workforce.Productivity) / 2;
        var trust = (State.Reputation + State.CustomerRetention + State.BoardConfidence) / 3;
        return 0.35 * financeStrength + 0.25 * adaptability + 0.2 * trust + 0.2 * (100 - State.RiskExposure);
    }

    private static JsonSerializerOptions JsonOptions() => new() { PropertyNameCaseInsensitive = true };
    private static double Clamp(double x, double min = 0, double max = 100) => Math.Min(max, Math.Max(min, x));
}
