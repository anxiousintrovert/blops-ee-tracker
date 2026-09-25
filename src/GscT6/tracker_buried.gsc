// EETracker observer for Buried / Mined Games.
#include common_scripts\utility;
#include maps\mp\_utility;
#include maps\mp\zombies\_zm_stats;
#include quest_part_hooks;

main() { level.ee_tracker_part_map = "zm_buried"; level.ee_tracker_part_session = randomint(1000000000); level.ee_tracker_buried_session = level.ee_tracker_part_session; ee_tracker_install_quest_part_hooks(); printf("[EETrackerT6] {\"schemaVersion\":1,\"type\":\"observer_loaded\",\"game\":\"bo2\",\"map\":\"zm_buried\",\"source\":\"gsc\"}\n"); }
init() { level thread ee_tracker_buried_monitor(); level thread ee_tracker_buried_watch_inventory(); }

ee_tracker_buried_watch_inventory()
{
    level endon("end_game"); level.ee_tracker_inventory_signatures = [];
    while (true) { players = get_players(); for (i = 0; i < players.size; i++) players[i] thread ee_tracker_buried_inventory_if_changed(i); wait 1; }
}

ee_tracker_buried_inventory_if_changed(slot)
{
    items = ""; weapons = self getweaponslist();
    for (i = 0; i < weapons.size; i++) if (weapons[i] == "slowgun_zm" || weapons[i] == "slowgun_upgraded_zm" || weapons[i] == "time_bomb_zm" || weapons[i] == "galvaknuckles_zm") items = items + weapons[i] + "|";
    if (isdefined(self.navcard_grabbed) && self.navcard_grabbed == "navcard_held_zm_transit") items = items + "navcard_held_zm_transit|";
    if (isdefined(self.navcard_grabbed) && self.navcard_grabbed == "navcard_held_zm_highrise") items = items + "navcard_held_zm_highrise|";
    if (isdefined(self.navcard_grabbed) && self.navcard_grabbed == "navcard_held_zm_buried") items = items + "navcard_held_zm_buried|";
    if (isdefined(level.ee_tracker_inventory_signatures[slot]) && level.ee_tracker_inventory_signatures[slot] == items) return;
    level.ee_tracker_inventory_signatures[slot] = items;
    printf("[EETrackerT6] {\"schemaVersion\":1,\"type\":\"player_inventory\",\"game\":\"bo2\",\"map\":\"zm_buried\",\"sessionId\":\"" + level.ee_tracker_buried_session + "\",\"playerSlot\":" + slot + ",\"inventoryItems\":\"" + items + "\",\"source\":\"gsc\"}\n");
}

ee_tracker_buried_monitor()
{
    level endon("end_game"); level thread ee_tracker_buried_end_session();
    if (!isdefined(level.ee_tracker_buried_session)) level.ee_tracker_buried_session = randomint(1000000000);
    level.ee_tracker_buried_stage = "";
    level.ee_tracker_buried_started = 0;
    level.ee_tracker_buried_route = "";
    level.ee_tracker_buried_done = 0;
    level.ee_tracker_buried_wisp_done = 0;
    printf("[EETrackerT6] {\"schemaVersion\":1,\"type\":\"session_started\",\"game\":\"bo2\",\"map\":\"zm_buried\",\"sessionId\":\"" + level.ee_tracker_buried_session + "\",\"source\":\"gsc\"}\n");
    while (true)
    {
        stage = "";
        if (isdefined(level._last_stage_started)) stage = level._last_stage_started;
        if (stage != level.ee_tracker_buried_stage)
        {
            previous = level.ee_tracker_buried_stage;
            level.ee_tracker_buried_stage = stage;
            if (stage == "bt" && !level.ee_tracker_buried_started)
            {
                level.ee_tracker_buried_started = 1;
                ee_tracker_buried_signal("bo2.buried.started");
            }
            if (previous != "") ee_tracker_buried_stage_complete(previous);
        }
        if (level.ee_tracker_buried_route == "" && flag("sq_is_max_tower_built"))
        {
            ee_tracker_buried_path("maxis");
            ee_tracker_buried_signal("bo2.buried.maxis.tower_built");
        }
        else if (level.ee_tracker_buried_route == "" && flag("sq_is_ric_tower_built"))
        {
            ee_tracker_buried_path("richtofen");
            ee_tracker_buried_signal("bo2.buried.richtofen.tower_built");
        }
        if (level.ee_tracker_buried_route == "maxis" && flag("sq_amplifiers_broken"))
            ee_tracker_buried_signal_once("maxis_amplifiers", "bo2.buried.maxis.amplifiers");
        else if (level.ee_tracker_buried_route == "richtofen" && flag("sq_amplifiers_on"))
            ee_tracker_buried_signal_once("richtofen_amplifiers", "bo2.buried.richtofen.amplifiers");
        if (!level.ee_tracker_buried_done && flag("sq_is_max_tower_built") && isdefined(level.buried_sq_maxis_complete) && level.buried_sq_maxis_complete)
        {
            level.ee_tracker_buried_done = 1;
            ee_tracker_buried_signal("bo2.buried.maxis.complete");
        }
        else if (!level.ee_tracker_buried_done && flag("sq_is_ric_tower_built") && isdefined(level.buried_sq_richtofen_complete) && level.buried_sq_richtofen_complete)
        {
            level.ee_tracker_buried_done = 1;
            ee_tracker_buried_signal("bo2.buried.richtofen.complete");
        }
        if (!level.ee_tracker_buried_wisp_done && flag("sq_wisp_success"))
        {
            level.ee_tracker_buried_wisp_done = 1;
            ee_tracker_buried_signal("bo2.buried.shared.wisp");
        }
        if (flag("sq_ows_success"))
            ee_tracker_buried_signal_once("ows", "bo2.buried.shared.final");
        players = get_players();
        for (i = 0; i < players.size; i++)
            players[i] thread ee_tracker_buried_profile(i);
        wait 1;
    }
}

ee_tracker_buried_end_session() { level waittill("end_game"); printf("[EETrackerT6] {\"schemaVersion\":1,\"type\":\"session_ended\",\"game\":\"bo2\",\"map\":\"zm_buried\",\"sessionId\":\"" + level.ee_tracker_buried_session + "\",\"source\":\"gsc\"}\n"); }

ee_tracker_buried_profile(player_slot)
{
    player = self;
    if (!isdefined(player.ee_tracker_buried_profile_signatures)) player.ee_tracker_buried_profile_signatures = [];
    player ee_tracker_buried_emit_profile_map(player_slot, 0, "zm_transit", "sq_transit_last_completed", "sq_transit_rich_complete", "sq_transit_maxis_complete", "navcard_applied_zm_transit", "sq_transit_started");
    player ee_tracker_buried_emit_profile_map(player_slot, 1, "zm_highrise", "sq_highrise_last_completed", "sq_highrise_rich_complete", "sq_highrise_maxis_complete", "navcard_applied_zm_highrise", "sq_highrise_started");
    player ee_tracker_buried_emit_profile_map(player_slot, 2, "zm_buried", "sq_buried_last_completed", "sq_buried_rich_complete", "sq_buried_maxis_complete", "navcard_applied_zm_buried", "sq_buried_started");
}

ee_tracker_buried_emit_profile_map(player_slot, map_index, profile_map, last_stat, rich_stat, maxis_stat, nav_stat, started_stat)
{
    last_completed = self maps\mp\zombies\_zm_stats::get_global_stat(last_stat);
    rich_complete = self maps\mp\zombies\_zm_stats::get_global_stat(rich_stat);
    maxis_complete = self maps\mp\zombies\_zm_stats::get_global_stat(maxis_stat);
    navcard = self maps\mp\zombies\_zm_stats::get_global_stat(nav_stat);
    table_built = self maps\mp\zombies\_zm_stats::get_global_stat(started_stat);
    incoming_card = "";
    if (profile_map == "zm_transit") incoming_card = "navcard_held_zm_buried";
    else if (profile_map == "zm_highrise") incoming_card = "navcard_held_zm_transit";
    else incoming_card = "navcard_held_zm_highrise";
    held_card = 0;
    if (isdefined(self.navcard_grabbed) && self.navcard_grabbed == incoming_card) held_card = 1;
    signature = "" + last_completed + ":" + rich_complete + ":" + maxis_complete + ":" + navcard + ":" + table_built + ":" + held_card;
    if (isdefined(self.ee_tracker_buried_profile_signatures[map_index]) && self.ee_tracker_buried_profile_signatures[map_index] == signature) return;
    self.ee_tracker_buried_profile_signatures[map_index] = signature;
    printf("[EETrackerT6] {\"schemaVersion\":1,\"type\":\"player_state\",\"game\":\"bo2\",\"map\":\"zm_buried\",\"profileMap\":\"" + profile_map + "\",\"sessionId\":\"" + level.ee_tracker_buried_session + "\",\"playerSlot\":" + player_slot + ",\"lastCompletedSide\":" + last_completed + ",\"richtofenCompletionCount\":" + rich_complete + ",\"maxisCompletionCount\":" + maxis_complete + ",\"navcardAppliedCount\":" + navcard + ",\"navcardTableBuiltCount\":" + table_built + ",\"navcardHeld\":" + held_card + ",\"source\":\"gsc\"}\n");
}

ee_tracker_buried_stage_complete(stage)
{
    if (stage == "mta") ee_tracker_buried_signal("bo2.buried.shared.mta");
    else if (stage == "gl") ee_tracker_buried_signal("bo2.buried.shared.gl");
    else if (stage == "ftl") ee_tracker_buried_signal("bo2.buried.shared.ftl");
    else if (stage == "ll") ee_tracker_buried_signal("bo2.buried.shared.ll");
    else if (stage == "ts") ee_tracker_buried_signal("bo2.buried.shared.ts");
    else if (stage == "ctw") return;
    else if (stage == "tpo") ee_tracker_buried_signal("bo2.buried.shared.tpo");
    else if (stage == "ip") ee_tracker_buried_signal("bo2.buried.shared.ip");
    else if (stage == "ows") ee_tracker_buried_signal("bo2.buried.shared.ows");
}

ee_tracker_buried_signal_once(key, signal)
{
    if (key == "maxis_amplifiers")
    {
        if (isdefined(level.ee_tracker_buried_maxis_amp_seen)) return;
        level.ee_tracker_buried_maxis_amp_seen = 1;
    }
    else if (key == "richtofen_amplifiers")
    {
        if (isdefined(level.ee_tracker_buried_ric_amp_seen)) return;
        level.ee_tracker_buried_ric_amp_seen = 1;
    }
    else
    {
        if (isdefined(level.ee_tracker_buried_ows_seen)) return;
        level.ee_tracker_buried_ows_seen = 1;
    }
    ee_tracker_buried_signal(signal);
}

ee_tracker_buried_path(route)
{
    level.ee_tracker_buried_route = route;
    printf("[EETrackerT6] {\"schemaVersion\":1,\"type\":\"quest_path_selected\",\"game\":\"bo2\",\"map\":\"zm_buried\",\"sessionId\":\"" + level.ee_tracker_buried_session + "\",\"signalValue\":\"" + route + "\",\"source\":\"gsc\"}\n");
}

ee_tracker_buried_signal(signal)
{
    printf("[EETrackerT6] {\"schemaVersion\":1,\"type\":\"quest_signal\",\"game\":\"bo2\",\"map\":\"zm_buried\",\"sessionId\":\"" + level.ee_tracker_buried_session + "\",\"signal\":\"" + signal + "\",\"source\":\"gsc\"}\n");
}
