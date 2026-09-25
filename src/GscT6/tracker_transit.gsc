// EETracker T6 TranZit observer.
// Place under scripts/zm/zm_transit so Plutonium loads it only on TranZit.
// Output is a tagged JSON line on the Plutonium console stream. The companion
// collector forwards those lines into scriptdata/ee-tracker-bo2.jsonl.

#include common_scripts\utility;
#include maps\mp\_utility;
#include maps\mp\zombies\_zm_stats;
#include quest_part_hooks;

main()
{
    level.ee_tracker_part_map = "zm_transit";
    level.ee_tracker_part_session = randomint(1000000000);
    level.ee_tracker_transit_session = level.ee_tracker_part_session;
    ee_tracker_install_quest_part_hooks();
    printf("[EETrackerT6] {\"schemaVersion\":1,\"type\":\"observer_loaded\",\"game\":\"bo2\",\"map\":\"zm_transit\",\"source\":\"gsc\"}\n");
}

init()
{
    level thread ee_tracker_transit_monitor();
}

ee_tracker_transit_monitor()
{
    level endon("end_game");
    level thread ee_tracker_transit_end_session();
    if (!isdefined(level.ee_tracker_transit_session)) level.ee_tracker_transit_session = randomint(1000000000);
    level.ee_tracker_transit_previous_global = "";
    level.ee_tracker_transit_player_signatures = [];
    level.ee_tracker_transit_progress_values = [];
    level.ee_tracker_transit_initialized_players = [];
    level.ee_tracker_transit_route_locked = 0;
    level.ee_tracker_transit_quest_started = 0;
    level.ee_tracker_transit_music_reported = 0;
    level.ee_tracker_transit_music_count = 0;
    level thread ee_tracker_transit_watch_inventory();

    printf("[EETrackerT6] {\"schemaVersion\":1,\"type\":\"session_started\",\"game\":\"bo2\",\"map\":\"zm_transit\",\"sessionId\":\"" + level.ee_tracker_transit_session + "\",\"source\":\"gsc\"}\n");

    while (true)
    {
        players = get_players();
        round_number = 0;
        if (isdefined(level.round_number))
            round_number = level.round_number;

        power_on = 0;
        power_json = "false";
        if (flag("power_on"))
        {
            power_on = 1;
            power_json = "true";
        }

        if (isdefined(level.sq_progress) && !level.ee_tracker_transit_quest_started)
        {
            level.ee_tracker_transit_quest_started = 1;
            ee_tracker_transit_emit_signal("bo2.transit.started");
        }

        global_signature = "" + round_number + ":" + players.size + ":" + power_on;
        if (global_signature != level.ee_tracker_transit_previous_global)
        {
            level.ee_tracker_transit_previous_global = global_signature;
            printf("[EETrackerT6] {\"schemaVersion\":1,\"type\":\"snapshot\",\"game\":\"bo2\",\"map\":\"zm_transit\",\"sessionId\":\"" + level.ee_tracker_transit_session + "\",\"round\":" + round_number + ",\"playerCount\":" + players.size + ",\"powerOn\":" + power_json + ",\"source\":\"gsc\"}\n");
        }

        for (i = 0; i < players.size; i++)
        {
            player = players[i];
            player thread ee_tracker_transit_emit_if_changed(i);
        }

        ee_tracker_transit_emit_progress();
        if (isdefined(level.meteor_counter) && level.meteor_counter > level.ee_tracker_transit_music_count)
        {
            level.ee_tracker_transit_music_count = level.meteor_counter;
            if (!level.ee_tracker_transit_music_reported && level.meteor_counter >= 3)
            {
                level.ee_tracker_transit_music_reported = 1;
                ee_tracker_transit_emit_side_egg_step("transit_music", 0);
                ee_tracker_transit_emit_side_egg_step("transit_music", 1);
                ee_tracker_transit_emit_side_egg_step("transit_music", 2);
                ee_tracker_transit_emit_signal("bo2.transit.music.complete");
            }
        }

        wait 2;
    }
}

ee_tracker_transit_emit_if_changed(player_slot)
{
    player = self;
    session_id = level.ee_tracker_transit_session;

    last_completed = player maps\mp\zombies\_zm_stats::get_global_stat("sq_transit_last_completed");
    rich_stage_1 = player maps\mp\zombies\_zm_stats::get_global_stat("sq_transit_rich_stage_1");
    rich_stage_2 = player maps\mp\zombies\_zm_stats::get_global_stat("sq_transit_rich_stage_2");
    rich_stage_3 = player maps\mp\zombies\_zm_stats::get_global_stat("sq_transit_rich_stage_3");
    rich_complete = player maps\mp\zombies\_zm_stats::get_global_stat("sq_transit_rich_complete");
    maxis_stage_1 = player maps\mp\zombies\_zm_stats::get_global_stat("sq_transit_maxis_stage_1");
    maxis_stage_2 = player maps\mp\zombies\_zm_stats::get_global_stat("sq_transit_maxis_stage_2");
    maxis_stage_3 = player maps\mp\zombies\_zm_stats::get_global_stat("sq_transit_maxis_stage_3");
    maxis_complete = player maps\mp\zombies\_zm_stats::get_global_stat("sq_transit_maxis_complete");
    navcard_applied = player maps\mp\zombies\_zm_stats::get_global_stat("navcard_applied_zm_transit");
    navcard_table_built = player maps\mp\zombies\_zm_stats::get_global_stat("sq_transit_started");

    // initpersstat hydrates the match-local counters from profile stats.
    // Emit branch progress only for increments observed after match start.
    if (!isdefined(player.ee_tracker_transit_counters))
    {
        player.ee_tracker_transit_counters = [];
        ee_tracker_transit_emit_counter_baseline(player, "sq_transit_maxis_stage_1", maxis_stage_1);
        ee_tracker_transit_emit_counter_baseline(player, "sq_transit_maxis_stage_2", maxis_stage_2);
        ee_tracker_transit_emit_counter_baseline(player, "sq_transit_maxis_stage_3", maxis_stage_3);
        ee_tracker_transit_emit_counter_baseline(player, "sq_transit_maxis_complete", maxis_complete);
        ee_tracker_transit_emit_counter_baseline(player, "sq_transit_rich_stage_1", rich_stage_1);
        ee_tracker_transit_emit_counter_baseline(player, "sq_transit_rich_stage_2", rich_stage_2);
        ee_tracker_transit_emit_counter_baseline(player, "sq_transit_rich_stage_3", rich_stage_3);
        ee_tracker_transit_emit_counter_baseline(player, "sq_transit_rich_complete", rich_complete);

    }

    ee_tracker_transit_watch_counter(player, "sq_transit_maxis_stage_1", maxis_stage_1, "bo2.transit.maxis.route_started", "maxis");
    ee_tracker_transit_watch_counter(player, "sq_transit_maxis_stage_2", maxis_stage_2, "bo2.transit.maxis.two_turbines_powered_at_pylon", "maxis");
    ee_tracker_transit_watch_counter(player, "sq_transit_maxis_stage_3", maxis_stage_3, "bo2.transit.maxis.avogadro_stunned", "maxis");
    ee_tracker_transit_watch_counter(player, "sq_transit_maxis_complete", maxis_complete, "bo2.transit.maxis.complete", "maxis");
    ee_tracker_transit_watch_counter(player, "sq_transit_rich_stage_1", rich_stage_1, "bo2.transit.richtofen.route_started", "richtofen");
    ee_tracker_transit_watch_counter(player, "sq_transit_rich_stage_2", rich_stage_2, "bo2.transit.richtofen.pylon_heated_confirmed", "richtofen");
    ee_tracker_transit_watch_counter(player, "sq_transit_rich_stage_3", rich_stage_3, "bo2.transit.richtofen.explosive_kills", "richtofen");
    ee_tracker_transit_watch_counter(player, "sq_transit_rich_complete", rich_complete, "bo2.transit.richtofen.complete", "richtofen");

    signature = "" + last_completed + ":" + rich_stage_1 + ":" + rich_stage_2 + ":" + rich_stage_3 + ":" + rich_complete + ":" + maxis_stage_1 + ":" + maxis_stage_2 + ":" + maxis_stage_3 + ":" + maxis_complete + ":" + navcard_applied + ":" + navcard_table_built;
    if (isdefined(level.ee_tracker_transit_player_signatures[player_slot]) && level.ee_tracker_transit_player_signatures[player_slot] == signature)
        return;

    level.ee_tracker_transit_player_signatures[player_slot] = signature;
    printf("[EETrackerT6] {\"schemaVersion\":1,\"type\":\"player_state\",\"game\":\"bo2\",\"map\":\"zm_transit\",\"sessionId\":\"" + session_id + "\",\"playerSlot\":" + player_slot + ",\"lastCompletedSide\":" + last_completed + ",\"richtofenStage1Count\":" + rich_stage_1 + ",\"richtofenStage2Count\":" + rich_stage_2 + ",\"richtofenStage3Count\":" + rich_stage_3 + ",\"richtofenCompletionCount\":" + rich_complete + ",\"maxisStage1Count\":" + maxis_stage_1 + ",\"maxisStage2Count\":" + maxis_stage_2 + ",\"maxisStage3Count\":" + maxis_stage_3 + ",\"maxisCompletionCount\":" + maxis_complete + ",\"navcardAppliedCount\":" + navcard_applied + ",\"navcardTableBuiltCount\":" + navcard_table_built + ",\"source\":\"gsc\"}\n");
}

ee_tracker_transit_watch_counter(player, stat_name, current_value, signal, route)
{
    if (!isdefined(player.ee_tracker_transit_counters))
        player.ee_tracker_transit_counters = [];

    previous_value = player.ee_tracker_transit_counters[stat_name];
    player.ee_tracker_transit_counters[stat_name] = current_value;
    if (!isdefined(previous_value) || current_value <= previous_value)
        return;

    if (signal == "bo2.transit.richtofen.route_started")
        ee_tracker_transit_select_path("richtofen");

    if (signal != "bo2.transit.maxis.route_started" && signal != "bo2.transit.richtofen.route_started")
        ee_tracker_transit_emit_signal(signal);
}

ee_tracker_transit_emit_counter_baseline(player, stat_name, current_value)
{
    player.ee_tracker_transit_counters[stat_name] = current_value;
}

ee_tracker_transit_select_path(route)
{
    if (level.ee_tracker_transit_route_locked)
        return;

    level.ee_tracker_transit_route_locked = 1;
    printf("[EETrackerT6] {\"schemaVersion\":1,\"type\":\"quest_path_selected\",\"game\":\"bo2\",\"map\":\"zm_transit\",\"sessionId\":\"" + level.ee_tracker_transit_session + "\",\"signalValue\":\"" + route + "\",\"source\":\"gsc\"}\n");
}

ee_tracker_transit_emit_signal(signal)
{
    printf("[EETrackerT6] {\"schemaVersion\":1,\"type\":\"quest_signal\",\"game\":\"bo2\",\"map\":\"zm_transit\",\"sessionId\":\"" + level.ee_tracker_transit_session + "\",\"signal\":\"" + signal + "\",\"source\":\"gsc\"}\n");
}

ee_tracker_transit_end_session() { level waittill("end_game"); printf("[EETrackerT6] {\"schemaVersion\":1,\"type\":\"session_ended\",\"game\":\"bo2\",\"map\":\"zm_transit\",\"sessionId\":\"" + level.ee_tracker_transit_session + "\",\"source\":\"gsc\"}\n"); }

ee_tracker_transit_watch_inventory()
{
    level endon("end_game"); level.ee_tracker_inventory_signatures = [];
    while (true)
    {
        players = get_players();
        for (i = 0; i < players.size; i++) players[i] thread ee_tracker_transit_inventory_if_changed(i);
        wait 1;
    }
}

ee_tracker_transit_inventory_if_changed(slot)
{
    items = ""; weapons = self getweaponslist();
    for (i = 0; i < weapons.size; i++) if (weapons[i] == "jetgun_zm") items = items + weapons[i] + "|";
    tactical = self get_player_tactical_grenade(); if (isdefined(tactical) && tactical == "emp_grenade_zm") items = items + tactical + "|";
    if (isdefined(self.navcard_grabbed) && self.navcard_grabbed == "navcard_held_zm_buried") items = items + "navcard_held_zm_buried|";
    if (isdefined(level.ee_tracker_inventory_signatures[slot]) && level.ee_tracker_inventory_signatures[slot] == items) return;
    level.ee_tracker_inventory_signatures[slot] = items;
    printf("[EETrackerT6] {\"schemaVersion\":1,\"type\":\"player_inventory\",\"game\":\"bo2\",\"map\":\"zm_transit\",\"sessionId\":\"" + level.ee_tracker_transit_session + "\",\"playerSlot\":" + slot + ",\"inventoryItems\":\"" + items + "\",\"source\":\"gsc\"}\n");
}

ee_tracker_transit_emit_side_egg_step(egg_id, step_index)
{
    printf("[EETrackerT6] {\"schemaVersion\":1,\"type\":\"side_egg_step\",\"game\":\"bo2\",\"map\":\"zm_transit\",\"eggId\":\"" + egg_id + "\",\"stepIndex\":" + step_index + ",\"source\":\"gsc\"}\n");
}

ee_tracker_transit_emit_progress()
{
    if (!isdefined(level.sq_progress))
        return;

    maxis_a = level.sq_progress["maxis"]["A_turbine_1"];
    maxis_b = level.sq_progress["maxis"]["A_turbine_2"];
    maxis_turbines = 0;
    if (isdefined(maxis_a) && !isint(maxis_a)) maxis_turbines++;
    if (isdefined(maxis_b) && !isint(maxis_b)) maxis_turbines++;
    // Maxis stage 1 starts automatically while power is off. Wait for the
    // player's first Turbine at the pylon before treating that as a route choice.
    if (maxis_turbines > 0 && !level.ee_tracker_transit_route_locked)
        ee_tracker_transit_select_path("maxis");
    ee_tracker_transit_emit_progress_value("bo2.transit.maxis.turbines_at_pylon", maxis_turbines, 2);

    maxis_lamp_a = level.sq_progress["maxis"]["C_turbine_1"];
    maxis_lamp_b = level.sq_progress["maxis"]["C_turbine_2"];
    maxis_lamps = 0;
    if (isdefined(maxis_lamp_a) && !isint(maxis_lamp_a)) maxis_lamps++;
    if (isdefined(maxis_lamp_b) && !isint(maxis_lamp_b)) maxis_lamps++;
    ee_tracker_transit_emit_progress_value("bo2.transit.maxis.screecher_lights", maxis_lamps, 2);

    remaining = level.sq_progress["rich"]["B_zombies_tower"];
    explosive_kills = 0;
    if (isdefined(remaining)) explosive_kills = 25 - remaining;
    explosive_kills = int(max(0, min(25, explosive_kills)));
    ee_tracker_transit_emit_progress_value("bo2.transit.richtofen.explosive_kills_progress", explosive_kills, 25);

    emp_lights = level.sq_progress["rich"]["C_screecher_light"];
    if (!isdefined(emp_lights)) emp_lights = 0;
    emp_lights = int(max(0, min(4, emp_lights)));
    ee_tracker_transit_emit_progress_value("bo2.transit.richtofen.emp_lights", emp_lights, 4);
}

ee_tracker_transit_emit_progress_value(signal, progress, maximum)
{
    if (isdefined(level.ee_tracker_transit_progress_values[signal]) && level.ee_tracker_transit_progress_values[signal] == progress)
        return;

    level.ee_tracker_transit_progress_values[signal] = progress;
    printf("[EETrackerT6] {\"schemaVersion\":1,\"type\":\"quest_progress\",\"game\":\"bo2\",\"map\":\"zm_transit\",\"sessionId\":\"" + level.ee_tracker_transit_session + "\",\"signal\":\"" + signal + "\",\"progress\":" + progress + ",\"progressMax\":" + maximum + ",\"source\":\"gsc\"}\n");
}
