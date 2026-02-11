from __future__ import annotations

from dataclasses import dataclass, field, asdict
from typing import Any


@dataclass
class BusinessUnit:
    name: str
    revenue_share: float
    base_margin: float
    innovation_focus: float


@dataclass
class Region:
    name: str
    demand_index: float
    regulatory_risk: float


@dataclass
class Workforce:
    headcount: int
    morale: float
    productivity: float
    burnout: float
    attrition: float


@dataclass
class Finance:
    cash: float
    debt: float
    equity: float
    revenue: float
    cogs: float
    opex: float
    capex: float
    interest_rate: float


@dataclass
class State:
    month: int
    quarter: int
    year: int
    reputation: float
    risk_exposure: float
    innovation_velocity: float
    customer_retention: float
    investor_sentiment: float
    board_confidence: float
    technical_debt: float
    finance: Finance
    workforce: Workforce
    units: list[BusinessUnit] = field(default_factory=list)
    regions: list[Region] = field(default_factory=list)
    event_log: list[dict[str, Any]] = field(default_factory=list)
    decision_log: list[dict[str, Any]] = field(default_factory=list)

    def to_dict(self) -> dict[str, Any]:
        return asdict(self)

    @staticmethod
    def from_dict(payload: dict[str, Any]) -> "State":
        finance = Finance(**payload["finance"])
        workforce = Workforce(**payload["workforce"])
        units = [BusinessUnit(**x) for x in payload["units"]]
        regions = [Region(**x) for x in payload["regions"]]
        return State(
            month=payload["month"],
            quarter=payload["quarter"],
            year=payload["year"],
            reputation=payload["reputation"],
            risk_exposure=payload["risk_exposure"],
            innovation_velocity=payload["innovation_velocity"],
            customer_retention=payload["customer_retention"],
            investor_sentiment=payload["investor_sentiment"],
            board_confidence=payload["board_confidence"],
            technical_debt=payload["technical_debt"],
            finance=finance,
            workforce=workforce,
            units=units,
            regions=regions,
            event_log=payload.get("event_log", []),
            decision_log=payload.get("decision_log", []),
        )


@dataclass
class Initiative:
    name: str
    budget: float
    short_term_impact: str
    delayed_impact: str
    confidence: float
    effects: dict[str, float]
