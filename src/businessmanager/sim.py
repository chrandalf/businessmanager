from __future__ import annotations

import ast
import json
import random
from dataclasses import asdict
from pathlib import Path
from typing import Any

from .models import BusinessUnit, Finance, Initiative, Region, State, Workforce


class Simulation:
    def __init__(self, scenario: dict[str, Any], seed: int = 42):
        self.scenario = scenario
        self.rng = random.Random(seed)
        self.state = self._bootstrap_state(scenario)

    @classmethod
    def from_json(cls, path: str | Path, seed: int = 42) -> "Simulation":
        scenario = json.loads(Path(path).read_text())
        return cls(scenario, seed=seed)

    def save(self, path: str | Path) -> None:
        payload = {
            "scenario": self.scenario,
            "state": self.state.to_dict(),
            "rng": repr(self.rng.getstate()),
        }
        Path(path).write_text(json.dumps(payload, indent=2))

    @classmethod
    def load(cls, path: str | Path) -> "Simulation":
        payload = json.loads(Path(path).read_text())
        sim = cls(payload["scenario"], seed=1)
        sim.state = State.from_dict(payload["state"])
        sim.rng.setstate(ast.literal_eval(payload["rng"]))
        return sim

    def _bootstrap_state(self, scenario: dict[str, Any]) -> State:
        finance = Finance(**scenario["initial_finance"])
        workforce = Workforce(**scenario["initial_workforce"])
        units = [BusinessUnit(**u) for u in scenario["business_units"]]
        regions = [Region(**r) for r in scenario["regions"]]
        return State(
            month=1,
            quarter=1,
            year=1,
            reputation=70,
            risk_exposure=35,
            innovation_velocity=50,
            customer_retention=78,
            investor_sentiment=62,
            board_confidence=65,
            technical_debt=35,
            finance=finance,
            workforce=workforce,
            units=units,
            regions=regions,
        )

    def available_initiatives(self) -> list[Initiative]:
        return [Initiative(**i) for i in self.scenario["initiatives"]]

    def apply_initiatives(self, names: list[str]) -> None:
        initiatives = {i.name: i for i in self.available_initiatives()}
        for name in names:
            initiative = initiatives[name]
            self.state.finance.capex += initiative.budget
            for field, delta in initiative.effects.items():
                self._shift(field, delta)
            self.state.decision_log.append(
                {
                    "month": self.state.month,
                    "initiative": name,
                    "budget": initiative.budget,
                    "confidence": initiative.confidence,
                    "short_term_impact": initiative.short_term_impact,
                    "delayed_impact": initiative.delayed_impact,
                }
            )

    def _shift(self, field: str, delta: float) -> None:
        if field.startswith("finance."):
            finance_field = field.split(".", maxsplit=1)[1]
            setattr(self.state.finance, finance_field, getattr(self.state.finance, finance_field) + delta)
            return
        if field.startswith("workforce."):
            wf_field = field.split(".", maxsplit=1)[1]
            setattr(self.state.workforce, wf_field, getattr(self.state.workforce, wf_field) + delta)
            return
        setattr(self.state, field, getattr(self.state, field) + delta)

    def run_month(self) -> dict[str, Any]:
        pre = self.kpis()
        event = self._trigger_event()
        self._process_operations()
        self._process_finance()
        self._process_people()
        post = self.kpis()
        explain = self._explain_change(pre, post)
        if self.state.month % 3 == 0:
            self._quarterly_board_review()
        self._advance_time()
        return {
            "month": self.state.month,
            "event": event,
            "kpis": post,
            "explain": explain,
        }

    def _trigger_event(self) -> dict[str, Any] | None:
        events = self.scenario["events"]
        if self.rng.random() > self.scenario.get("event_chance", 0.6):
            return None
        event = self.rng.choice(events)
        for field, delta in event["effects"].items():
            shock = delta * (1 + self.rng.uniform(-0.15, 0.15))
            self._shift(field, shock)
        record = {
            "month": self.state.month,
            "name": event["name"],
            "category": event["category"],
            "severity": event["severity"],
        }
        self.state.event_log.append(record)
        return record

    def _process_operations(self) -> None:
        demand = sum(r.demand_index for r in self.state.regions) / len(self.state.regions)
        capacity_pressure = max(0, self.state.risk_exposure - 40) / 100
        throughput = max(0.6, self.state.workforce.productivity / 100 - capacity_pressure)
        quality_penalty = max(0, (self.state.technical_debt - 40) / 300)

        gross_revenue = self.state.finance.revenue * (demand / 100) * throughput
        gross_revenue *= 1 + (self.state.reputation - 50) / 500
        gross_revenue *= 1 - quality_penalty
        self.state.finance.revenue = max(0.0, gross_revenue)

        cogs_ratio = 0.52 + capacity_pressure + (self.state.workforce.burnout / 500)
        self.state.finance.cogs = self.state.finance.revenue * min(0.9, cogs_ratio)

    def _process_finance(self) -> None:
        interest = self.state.finance.debt * (self.state.finance.interest_rate / 12)
        self.state.finance.opex = self.state.finance.revenue * 0.22 + self.state.workforce.headcount * 120
        fcf = self.state.finance.revenue - self.state.finance.cogs - self.state.finance.opex - self.state.finance.capex - interest
        self.state.finance.cash += fcf
        if self.state.finance.cash < 0:
            self.state.finance.debt += abs(self.state.finance.cash) * 1.2
            self.state.finance.cash = 0
        consistency = max(-15, min(15, fcf / 50000))
        self.state.investor_sentiment = self._bounded(self.state.investor_sentiment + consistency)
        self.state.finance.capex = 0

    def _process_people(self) -> None:
        pressure = self.state.risk_exposure / 200 + self.state.technical_debt / 300
        self.state.workforce.burnout = self._bounded(self.state.workforce.burnout + pressure * 4)
        morale_shift = 1.8 - pressure * 6 + (self.state.reputation - 50) / 100
        self.state.workforce.morale = self._bounded(self.state.workforce.morale + morale_shift)
        self.state.workforce.attrition = self._bounded(6 + (100 - self.state.workforce.morale) / 8 + self.state.workforce.burnout / 10)
        self.state.workforce.productivity = self._bounded(
            65 + self.state.workforce.morale / 3 - self.state.workforce.burnout / 4 + self.state.innovation_velocity / 8
        )
        net_hires = int((70 - self.state.workforce.attrition) / 8)
        self.state.workforce.headcount = max(50, self.state.workforce.headcount + net_hires)

    def _quarterly_board_review(self) -> None:
        kpi = self.kpis()
        confidence_delta = 0
        if kpi["free_cash_flow"] > 0:
            confidence_delta += 2
        if kpi["risk_exposure"] > 70:
            confidence_delta -= 3
        if kpi["innovation_velocity"] > 60:
            confidence_delta += 1
        if kpi["employee_engagement"] < 45:
            confidence_delta -= 2
        self.state.board_confidence = self._bounded(self.state.board_confidence + confidence_delta)

    def _advance_time(self) -> None:
        self.state.month += 1
        self.state.quarter = ((self.state.month - 1) // 3) % 4 + 1
        self.state.year = ((self.state.month - 1) // 12) + 1
        if self.state.month % 12 == 1 and self.state.month > 1:
            self._annual_reset()

    def _annual_reset(self) -> None:
        self.state.technical_debt = self._bounded(self.state.technical_debt + self.rng.uniform(-3, 7))
        for region in self.state.regions:
            region.demand_index = self._bounded(region.demand_index + self.rng.uniform(-5, 6))
            region.regulatory_risk = self._bounded(region.regulatory_risk + self.rng.uniform(-4, 5))

    def kpis(self) -> dict[str, float]:
        revenue_growth = (self.state.finance.revenue / self.scenario["initial_finance"]["revenue"] - 1) * 100
        operating_margin = ((self.state.finance.revenue - self.state.finance.cogs - self.state.finance.opex) / max(1, self.state.finance.revenue)) * 100
        fcf = self.state.finance.revenue - self.state.finance.cogs - self.state.finance.opex - self.state.finance.capex
        debt_ratio = self.state.finance.debt / max(1, self.state.finance.equity)
        runway = self.state.finance.cash / max(1, -fcf) if fcf < 0 else 24.0
        resilience = self.enterprise_resilience_index()
        return {
            "revenue_growth": round(revenue_growth, 2),
            "operating_margin": round(operating_margin, 2),
            "free_cash_flow": round(fcf, 2),
            "debt_ratio": round(debt_ratio, 2),
            "liquidity_runway": round(runway, 2),
            "employee_engagement": round(self.state.workforce.morale, 2),
            "attrition": round(self.state.workforce.attrition, 2),
            "innovation_velocity": round(self.state.innovation_velocity, 2),
            "customer_retention": round(self.state.customer_retention, 2),
            "risk_exposure": round(self.state.risk_exposure, 2),
            "reputation_score": round(self.state.reputation, 2),
            "investor_sentiment": round(self.state.investor_sentiment, 2),
            "board_confidence": round(self.state.board_confidence, 2),
            "enterprise_resilience_index": round(resilience, 2),
        }

    def _explain_change(self, pre: dict[str, float], post: dict[str, float]) -> dict[str, str]:
        explain: dict[str, str] = {}
        for k, v in post.items():
            delta = v - pre.get(k, 0)
            if abs(delta) < 0.3:
                continue
            direction = "increased" if delta > 0 else "decreased"
            explain[k] = f"{k.replace('_', ' ').title()} {direction} by {abs(delta):.2f} due to this month\'s operating and event mix."
        return explain

    def enterprise_resilience_index(self) -> float:
        finance_strength = max(0.0, 100 - self.kpi_debt_penalty() + self.state.investor_sentiment / 2)
        adaptability = (self.state.innovation_velocity + self.state.workforce.productivity) / 2
        trust = (self.state.reputation + self.state.customer_retention + self.state.board_confidence) / 3
        return 0.35 * finance_strength + 0.25 * adaptability + 0.2 * trust + 0.2 * (100 - self.state.risk_exposure)

    def kpi_debt_penalty(self) -> float:
        return min(80, self.state.finance.debt / max(1, self.state.finance.equity) * 25)

    @staticmethod
    def _bounded(value: float, floor: float = 0, ceiling: float = 100) -> float:
        return min(ceiling, max(floor, value))

    def victory_status(self) -> str:
        kpis = self.kpis()
        if self.state.finance.cash <= 0 and kpis["free_cash_flow"] < 0:
            return "loss_insolvency"
        if self.state.board_confidence < 25:
            return "loss_board_removal"
        if self.state.reputation < 20:
            return "loss_reputation_collapse"
        if self.state.year >= 5 and kpis["enterprise_resilience_index"] > 65 and kpis["operating_margin"] > 10:
            return "win"
        return "ongoing"

    def replay_timeline(self) -> list[dict[str, Any]]:
        return [
            {
                "decision": d,
                "events": [e for e in self.state.event_log if e["month"] == d["month"]],
            }
            for d in self.state.decision_log
        ]

    def dashboard(self) -> dict[str, Any]:
        return {
            "time": {"year": self.state.year, "quarter": self.state.quarter, "month": self.state.month},
            "overview": self.kpis(),
            "business_units": [asdict(u) for u in self.state.units],
            "initiatives": [asdict(i) for i in self.available_initiatives()],
            "recent_events": self.state.event_log[-5:],
            "board_feedback": {
                "confidence": self.state.board_confidence,
                "sentiment": "supportive" if self.state.board_confidence >= 60 else "pressured",
            },
            "forecast": self._forecast(),
        }

    def _forecast(self) -> dict[str, float]:
        kpis = self.kpis()
        multiplier = 1 + (kpis["innovation_velocity"] - kpis["risk_exposure"]) / 400
        return {
            "next_quarter_revenue": round(self.state.finance.revenue * multiplier * 3, 2),
            "next_quarter_fcf": round(kpis["free_cash_flow"] * multiplier * 3, 2),
        }
