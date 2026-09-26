// EETracker observer for Die Rise / High Maintenance.
#include common_scripts\utility;
#include maps\mp\_utility;
#include maps\mp\zombies\_zm_stats;
#include quest_part_hooks;

main()
{
    level.ee_tracker_part_map = "zm_highrise";
    level.ee_tracker_part_session = randomint(1000000000);
    level.ee_tracker_highrise_session = level.ee_tracker_part_session;
    ee_tracker_install_quest_part_hooks();
    printf("[EETrackerT6] {\"schemaVersion\":1,\"type\":\"observer_loaded\",\"game\":\"bo2\",\"map\":\"zm_highrise\",\"source\":\"gsc\"}\n");
}

init() { level thread ee_tracker_highrise_monitor(); level thread ee_tracker_highrise_watch_inventory(); level thread ee_tracker_highrise_watch_tile_progress(); }

ee_tracker_highrise_watch_tile_progress()
{
    level endon("end_game");
    last_tower_progress = -1;
    last_floor_progress = -1;
    while (true)
    {
        if (isdefined(level.n_cur_leg) && level.n_cur_leg != last_tower_progress)
        {
            last_tower_progress = level.n_cur_leg;
            ee_tracker_highrise_progress("bo2.highrise.tower_tile_progress", level.n_cur_leg, 4);
        }
        if (isdefined(level.sq_atd_cur_drg) && level.sq_atd_cur_drg != last_floor_progress)
        {
            last_floor_progress = level.sq_atd_cur_drg;
            ee_tracker_highrise_progress("bo2.highrise.floor_symbol_progress", level.sq_atd_cur_drg, 4);
        }
        wait 0.1;
    }
}

ee_tracker_highrise_watch_inventory()
{
    level endon("end_game"); level.ee_tracker_inventory_signatures = [];
    while (true) { players = get_players(); for (i = 0; i < players.size; i++) players[i] thread ee_tracker_highrise_inventory_if_changed(i); wait 1; }
}

ee_tracker_highrise_inventory_if_changed(slot)
{
    items = ""; weapons = self getweaponslist();
    for (i = 0; i < weapons.size; i++) if (weapons[i] == "slipgun_zm" || weapons[i] == "slipgun_upgraded_zm" || weapons[i] == "galvaknuckles_zm" || weapons[i] == "knife_ballistic_zm") items = items + weapons[i] + "|";
    if (isdefined(self.navcard_grabbed) && self.navcard_grabbed == "navcard_held_zm_transit") items = items + "navcard_held_zm_transit|";
    if (isdefined(level.ee_tracker_inventory_signatures[slot]) && level.ee_tracker_inventory_signatures[slot] == items) return;
    level.ee_tracker_inventory_signatures[slot] = items;
    printf("[EETrackerT6] {\"schemaVersion\":1,\"type\":\"player_inventory\",\"game\":\"bo2\",\"map\":\"zm_highrise\",\"sessionId\":\"" + level.ee_tracker_highrise_session + "\",\"playerSlot\":" + slot + ",\"inventoryItems\":\"" + items + "\",\"source\":\"gsc\"}\n");
}

ee_tracker_highrise_monitor()
{
    level endon("end_game");
    level thread ee_tracker_highrise_end_session();
    if (!isdefined(level.ee_tracker_highrise_session)) level.ee_tracker_highrise_session = randomint(1000000000);
    level.ee_tracker_highrise_stage = "";
    level.ee_tracker_highrise_started = 0;
    level.ee_tracker_highrise_route_locked = 0;
    level.ee_tracker_highrise_elevator_progress = -1;
    level.ee_tracker_highrise_ball_progress = -1;
    level.ee_tracker_highrise_ball_stage_seen = 0;
    level thread ee_tracker_highrise_watch_complete();
    printf("[EETrackerT6] {\"schemaVersion\":1,\"type\":\"session_started\",\"game\":\"bo2\",\"map\":\"zm_highrise\",\"sessionId\":\"" + level.ee_tracker_highrise_session + "\",\"source\":\"gsc\"}\n");
    while (true)
    {
        stage = "";
        if (isdefined(level._last_stage_started)) stage = level._last_stage_started;
        if (stage != level.ee_tracker_highrise_stage)
        {
            previous = level.ee_tracker_highrise_stage;
            level.ee_tracker_highrise_stage = stage;
            if (stage == "atd" && !level.ee_tracker_highrise_started)
            {
                level.ee_tracker_highrise_started = 1;
                ee_tracker_highrise_signal("bo2.highrise.started");
            }
            if (previous == "slb")
                ee_tracker_highrise_signal("bo2.highrise.shared.dragon_balls");
            if (previous != "")
                ee_tracker_highrise_stage_complete(previous);
        }
        if (flag("sq_atd_elevator_activated"))
            ee_tracker_highrise_signal_once("elevators", "bo2.highrise.shared.elevators");
        if (flag("sq_atd_drg_puzzle_complete"))
            ee_tracker_highrise_signal_once("floor_symbols", "bo2.highrise.shared.floor_symbols");
        if (!isdefined(level.ee_tracker_highrise_tile_sequence_sent) && isdefined(level.a_wind_order) && level.a_wind_order.size == 4)
            ee_tracker_highrise_emit_tile_sequence();
        if (stage == "slb")
            ee_tracker_highrise_emit_ball_progress();
        elevator_count = int(flag("sq_atd_elevator0")) + int(flag("sq_atd_elevator1")) + int(flag("sq_atd_elevator2")) + int(flag("sq_atd_elevator3"));
        if (elevator_count != level.ee_tracker_highrise_elevator_progress)
        {
            level.ee_tracker_highrise_elevator_progress = elevator_count;
            ee_tracker_highrise_progress("bo2.highrise.elevator_progress", elevator_count, 4);
        }
        players = get_players();
        for (i = 0; i < players.size; i++)
            players[i] thread ee_tracker_highrise_profile(i);
        if (!level.ee_tracker_highrise_route_locked && flag("sq_ric_tower_complete"))
            ee_tracker_highrise_select_route("richtofen");
        else if (!level.ee_tracker_highrise_route_locked && flag("sq_max_tower_complete"))
            ee_tracker_highrise_select_route("maxis");
        wait 1;
    }
}

ee_tracker_highrise_end_session() { level waittill("end_game"); printf("[EETrackerT6] {\"schemaVersion\":1,\"type\":\"session_ended\",\"game\":\"bo2\",\"map\":\"zm_highrise\",\"sessionId\":\"" + level.ee_tracker_highrise_session + "\",\"source\":\"gsc\"}\n"); }

ee_tracker_highrise_profile(player_slot)
{
    player = self;
    last_completed = player maps\mp\zombies\_zm_stats::get_global_stat("sq_highrise_last_completed");
    rich_complete = player maps\mp\zombies\_zm_stats::get_global_stat("sq_highrise_rich_complete");
    maxis_complete = player maps\mp\zombies\_zm_stats::get_global_stat("sq_highrise_maxis_complete");
    navcard = player maps\mp\zombies\_zm_stats::get_global_stat("navcard_applied_zm_highrise");
    navcard_table_built = player maps\mp\zombies\_zm_stats::get_global_stat("sq_highrise_started");
    incoming_card = 0;
    if (isdefined(player.navcard_grabbed) && player.navcard_grabbed == "navcard_held_zm_transit") incoming_card = 1;
    signature = "" + last_completed + ":" + rich_complete + ":" + maxis_complete + ":" + navcard + ":" + navcard_table_built + ":" + incoming_card;
    if (isdefined(player.ee_tracker_highrise_profile_signature) && player.ee_tracker_highrise_profile_signature == signature) return;
    player.ee_tracker_highrise_profile_signature = signature;
    printf("[EETrackerT6] {\"schemaVersion\":1,\"type\":\"player_state\",\"game\":\"bo2\",\"map\":\"zm_highrise\",\"sessionId\":\"" + level.ee_tracker_highrise_session + "\",\"playerSlot\":" + player_slot + ",\"lastCompletedSide\":" + last_completed + ",\"richtofenCompletionCount\":" + rich_complete + ",\"maxisCompletionCount\":" + maxis_complete + ",\"navcardAppliedCount\":" + navcard + ",\"navcardTableBuiltCount\":" + navcard_table_built + ",\"navcardHeld\":" + incoming_card + ",\"source\":\"gsc\"}\n");
}

ee_tracker_highrise_watch_complete()
{
    level endon("end_game");
    level waittill("highrise_sidequest_achieved");
    if (!level.ee_tracker_highrise_route_locked)
    {
        if (flag("sq_ric_tower_complete")) ee_tracker_highrise_select_route("richtofen");
        else if (flag("sq_max_tower_complete")) ee_tracker_highrise_select_route("maxis");
    }
    ee_tracker_highrise_signal("bo2.highrise.final.complete");
}

ee_tracker_highrise_select_route(route)
{
    if (level.ee_tracker_highrise_route_locked) return;
    level.ee_tracker_highrise_route_locked = 1;
    ee_tracker_highrise_path(route);
    if (route == "richtofen")
    {
        ee_tracker_highrise_signal("bo2.highrise.richtofen.ssp");
        ee_tracker_highrise_signal("bo2.highrise.richtofen.pts");
    }
    else
    {
        ee_tracker_highrise_signal("bo2.highrise.maxis.ssp");
        ee_tracker_highrise_signal("bo2.highrise.maxis.pts");
    }
}

ee_tracker_highrise_stage_complete(stage)
{
    // Elevator, floor-symbol, and dragon-ball steps have their own stock signals above.
}

ee_tracker_highrise_signal_once(key, signal)
{
    if (key == "elevators")
    {
        if (isdefined(level.ee_tracker_highrise_elevators_seen)) return;
        level.ee_tracker_highrise_elevators_seen = 1;
    }
    else
    {
        if (isdefined(level.ee_tracker_highrise_floor_symbols_seen)) return;
        level.ee_tracker_highrise_floor_symbols_seen = 1;
    }
    ee_tracker_highrise_signal(signal);
}

ee_tracker_highrise_progress(signal, progress, maximum)
{
    printf("[EETrackerT6] {\"schemaVersion\":1,\"type\":\"quest_progress\",\"game\":\"bo2\",\"map\":\"zm_highrise\",\"sessionId\":\"" + level.ee_tracker_highrise_session + "\",\"signal\":\"" + signal + "\",\"progress\":" + progress + ",\"progressMax\":" + maximum + ",\"source\":\"gsc\"}\n");
}

ee_tracker_highrise_emit_tile_sequence()
{
    level.ee_tracker_highrise_tile_sequence_sent = 1;
    sequence = level.a_wind_order[0] + "," + level.a_wind_order[1] + "," + level.a_wind_order[2] + "," + level.a_wind_order[3];
    printf("[EETrackerT6] {\"schemaVersion\":1,\"type\":\"quest_tile_sequence\",\"game\":\"bo2\",\"map\":\"zm_highrise\",\"sessionId\":\"" + level.ee_tracker_highrise_session + "\",\"signal\":\"bo2.highrise.tower_tiles\",\"signalValue\":\"" + sequence + "\",\"source\":\"gsc\"}\n");
}

ee_tracker_highrise_emit_ball_progress()
{
    balls = getentarray("sq_dragon_lion_ball", "targetname");
    remaining = 0;
    for (i = 0; i < balls.size; i++)
        if (isdefined(balls[i])) remaining++;
    level.ee_tracker_highrise_ball_stage_seen = 1;
    destroyed = 2 - remaining;
    destroyed = int(max(0, min(2, destroyed)));
    if (destroyed != level.ee_tracker_highrise_ball_progress)
    {
        level.ee_tracker_highrise_ball_progress = destroyed;
        ee_tracker_highrise_progress("bo2.highrise.dragon_balls_progress", destroyed, 2);
    }
    if (remaining == 0 && level.ee_tracker_highrise_ball_stage_seen)
        ee_tracker_highrise_signal_once("dragon_balls", "bo2.highrise.shared.dragon_balls");
}

ee_tracker_highrise_path(route)
{
    printf("[EETrackerT6] {\"schemaVersion\":1,\"type\":\"quest_path_selected\",\"game\":\"bo2\",\"map\":\"zm_highrise\",\"sessionId\":\"" + level.ee_tracker_highrise_session + "\",\"signalValue\":\"" + route + "\",\"source\":\"gsc\"}\n");
}

ee_tracker_highrise_signal(signal)
{
    printf("[EETrackerT6] {\"schemaVersion\":1,\"type\":\"quest_signal\",\"game\":\"bo2\",\"map\":\"zm_highrise\",\"sessionId\":\"" + level.ee_tracker_highrise_session + "\",\"signal\":\"" + signal + "\",\"source\":\"gsc\"}\n");
}


