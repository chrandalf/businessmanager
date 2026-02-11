from __future__ import annotations

import argparse
import json
from pathlib import Path

from .sim import Simulation


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Business manager simulation")
    parser.add_argument("--scenario", default="data/mvp_scenario.json")
    parser.add_argument("--seed", type=int, default=42)
    parser.add_argument("--months", type=int, default=60)
    parser.add_argument("--save", type=str)
    parser.add_argument("--load", type=str)
    parser.add_argument("--json", action="store_true")
    return parser.parse_args()


def run() -> None:
    args = parse_args()
    sim = Simulation.load(args.load) if args.load else Simulation.from_json(args.scenario, seed=args.seed)

    initiatives = [i.name for i in sim.available_initiatives()[:2]]
    for _ in range(args.months):
        if sim.state.month % 3 == 1:
            sim.apply_initiatives(initiatives)
        sim.run_month()
        if sim.victory_status() != "ongoing":
            break

    output = {
        "status": sim.victory_status(),
        "dashboard": sim.dashboard(),
        "timeline": sim.replay_timeline()[-5:],
    }
    if args.save:
        sim.save(Path(args.save))

    if args.json:
        print(json.dumps(output, indent=2))
        return

    kpi = output["dashboard"]["overview"]
    print(f"Status: {output['status']}")
    print(f"Y{sim.state.year} Q{sim.state.quarter} M{sim.state.month}")
    print(f"Revenue growth: {kpi['revenue_growth']}% | Operating margin: {kpi['operating_margin']}%")
    print(f"FCF: {kpi['free_cash_flow']} | Debt ratio: {kpi['debt_ratio']} | Runway: {kpi['liquidity_runway']} months")
    print(f"Engagement: {kpi['employee_engagement']} | Attrition: {kpi['attrition']}")
    print(f"Innovation velocity: {kpi['innovation_velocity']} | Risk exposure: {kpi['risk_exposure']}")
    print(f"Enterprise Resilience Index: {kpi['enterprise_resilience_index']}")


if __name__ == "__main__":
    run()
