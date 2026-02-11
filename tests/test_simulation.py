from businessmanager.sim import Simulation


def test_seeded_runs_are_deterministic():
    sim_a = Simulation.from_json("data/mvp_scenario.json", seed=99)
    sim_b = Simulation.from_json("data/mvp_scenario.json", seed=99)

    for _ in range(12):
        sim_a.apply_initiatives(["Automation Upgrade", "Leadership Development"])
        sim_b.apply_initiatives(["Automation Upgrade", "Leadership Development"])
        sim_a.run_month()
        sim_b.run_month()

    assert sim_a.kpis() == sim_b.kpis()


def test_save_and_load_round_trip(tmp_path):
    sim = Simulation.from_json("data/mvp_scenario.json", seed=7)
    sim.apply_initiatives(["Tech Debt Cleanup"])
    sim.run_month()

    save_path = tmp_path / "save.json"
    sim.save(save_path)

    loaded = Simulation.load(save_path)
    assert loaded.kpis() == sim.kpis()
    assert loaded.state.event_log == sim.state.event_log


def test_dashboard_contains_required_layers():
    sim = Simulation.from_json("data/mvp_scenario.json")
    sim.apply_initiatives(["Automation Upgrade", "Leadership Development"])
    sim.run_month()
    dashboard = sim.dashboard()

    assert "overview" in dashboard
    assert "business_units" in dashboard
    assert "initiatives" in dashboard
    assert "board_feedback" in dashboard
    assert "forecast" in dashboard


def test_5_year_playthrough_runs():
    sim = Simulation.from_json("data/mvp_scenario.json", seed=3)
    for _ in range(60):
        if sim.state.month % 3 == 1:
            sim.apply_initiatives(["Automation Upgrade", "Tech Debt Cleanup"])
        sim.run_month()
        if sim.victory_status() != "ongoing":
            break

    assert sim.state.year >= 5 or sim.victory_status() != "ongoing"
