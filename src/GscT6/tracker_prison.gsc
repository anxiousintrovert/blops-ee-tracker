// EETracker observer for Mob of the Dead / Pop Goes the Weasel.
#include common_scripts\utility;
#include maps\mp\_utility;
#include quest_part_hooks;

main() { level.ee_tracker_part_map = "zm_prison"; level.ee_tracker_part_session = randomint(1000000000); level.ee_tracker_prison_session = level.ee_tracker_part_session; ee_tracker_install_quest_part_hooks(); printf("[EETrackerT6] {\"schemaVersion\":1,\"type\":\"observer_loaded\",\"game\":\"bo2\",\"map\":\"zm_prison\",\"source\":\"gsc\"}\n"); }
init() {level thread ee_tracker_prison_monitor(); level thread ee_tracker_prison_watch_inventory(); level thread ee_tracker_prison_watch_parts(); }

ee_tracker_prison_watch_parts()
{
    level endon("end_game"); level.ee_tracker_prison_parts_seen = [];
    part_ids = array("plane_key", "plane_cloth", "plane_fueltanks", "plane_engine", "plane_steering", "plane_rigging", "plane_assembled");
        part_flags = array("key_found", "cloth_found", "fueltanks_found", "engine_found", "steering_found", "rigging_found", "plane_built");
    while (true)
    {
        for (i = 0; i < part_flags.size; i++)
        {
            if (flag(part_flags[i]) && !isdefined(level.ee_tracker_prison_parts_seen[i]))
            {
                level.ee_tracker_prison_parts_seen[i] = 1;
                state = "collected";
                if (part_ids[i] == "plane_assembled") state = "built";
                ee_tracker_prison_part(part_ids[i], state);
            }
        }
        wait 0.5;
    }
}

ee_tracker_prison_part(part_id, state)
{
    printf("[EETrackerT6] {\"schemaVersion\":1,\"type\":\"quest_part\",\"game\":\"bo2\",\"map\":\"zm_prison\",\"sessionId\":\"" + level.ee_tracker_part_session + "\",\"partId\":\"" + part_id + "\",\"partState\":\"" + state + "\",\"source\":\"stock_map_flag\"}\n");
}

ee_tracker_prison_watch_inventory()
{
    level endon("end_game"); level.ee_tracker_inventory_signatures = [];
    while (true) { players = get_players(); for (i = 0; i < players.size; i++) players[i] thread ee_tracker_prison_inventory_if_changed(i); wait 1; }
}

ee_tracker_prison_inventory_if_changed(slot)
{
    items = ""; weapons = self getweaponslist();
    for (i = 0; i < weapons.size; i++) if (weapons[i] == "blundergat_zm" || weapons[i] == "blundergat_upgraded_zm" || weapons[i] == "acidgat_zm" || weapons[i] == "bouncing_tomahawk_zm" || weapons[i] == "galvaknuckles_zm") items = items + weapons[i] + "|";
    if (isdefined(level.ee_tracker_inventory_signatures[slot]) && level.ee_tracker_inventory_signatures[slot] == items) return;
    level.ee_tracker_inventory_signatures[slot] = items;
    printf("[EETrackerT6] {\"schemaVersion\":1,\"type\":\"player_inventory\",\"game\":\"bo2\",\"map\":\"zm_prison\",\"sessionId\":\"" + level.ee_tracker_prison_session + "\",\"playerSlot\":" + slot + ",\"inventoryItems\":\"" + items + "\",\"source\":\"gsc\"}\n");
}

ee_tracker_prison_monitor()
{
    level endon("end_game");
    level thread ee_tracker_prison_end_session();
    if (!isdefined(level.ee_tracker_prison_session)) level.ee_tracker_prison_session = randomint(1000000000);
    level.ee_tracker_prison_last_flag = "";
    level.ee_tracker_prison_ending = 0;
    level.ee_tracker_prison_final_flight = 0;
    level.ee_tracker_prison_seen = [];
    level.ee_tracker_prison_music_reported = 0;
    level.ee_tracker_prison_music_count = 0;
    level.ee_tracker_prison_935_reported = 0;
    level.ee_tracker_prison_blundergat_reported = 0;
    level thread ee_tracker_prison_watch_935_song();
    printf("[EETrackerT6] {\"schemaVersion\":1,\"type\":\"session_started\",\"game\":\"bo2\",\"map\":\"zm_prison\",\"sessionId\":\"" + level.ee_tracker_prison_session + "\",\"source\":\"gsc\"}\n");
    while (true)
    {
        ee_tracker_prison_watch("key_found", "bo2.mob.key");
        ee_tracker_prison_watch("cloth_found", "bo2.mob.cloth");
        ee_tracker_prison_watch("fueltanks_found", "bo2.mob.fuel");
        ee_tracker_prison_watch("engine_found", "bo2.mob.engine");
        ee_tracker_prison_watch("steering_found", "bo2.mob.steering");
        ee_tracker_prison_watch("rigging_found", "bo2.mob.rigging");
        if (flag("key_found") && flag("cloth_found") && flag("fueltanks_found") && flag("engine_found") && flag("steering_found") && flag("rigging_found"))
            ee_tracker_prison_watch("all_plane_parts", "bo2.mob.parts_complete");
        ee_tracker_prison_watch("plane_built", "bo2.mob.plane_built");
        ee_tracker_prison_watch("plane_trip_to_nml_successful", "bo2.mob.bridge_trip");
        ee_tracker_prison_watch("quest_completed_thrice", "bo2.mob.three_trips");
        ee_tracker_prison_watch("generator_challenge_completed", "bo2.mob.generators");
        ee_tracker_prison_watch("warden_blundergat_obtained", "bo2.mob.blundergat");
        if (!level.ee_tracker_prison_blundergat_reported && flag("warden_blundergat_obtained"))
        {
            level.ee_tracker_prison_blundergat_reported = 1;
            ee_tracker_prison_emit_side_egg_step("blue_skulls", 0);
            ee_tracker_prison_emit_side_egg_step("blue_skulls", 1);
            ee_tracker_prison_emit_side_egg_step("blue_skulls", 2);
            ee_tracker_prison_emit_side_egg_step("blue_skulls", 3);
            ee_tracker_prison_emit_side_egg_step("blue_skulls", 4);
            ee_tracker_prison_emit_side_egg_step("blue_skulls", 5);
        }
        ee_tracker_prison_watch("spoon_obtained", "bo2.mob.spoon");
        if (isdefined(level.meteor_counter) && level.meteor_counter > level.ee_tracker_prison_music_count)
        {
            level.ee_tracker_prison_music_count = level.meteor_counter;
            if (!level.ee_tracker_prison_music_reported && level.meteor_counter >= 3)
            {
                level.ee_tracker_prison_music_reported = 1;
                ee_tracker_prison_emit_side_egg_step("rusty_cage", 0);
                ee_tracker_prison_emit_side_egg_step("rusty_cage", 1);
                ee_tracker_prison_emit_side_egg_step("rusty_cage", 2);
                ee_tracker_prison_signal("bo2.mob.music.complete");
            }
        }
        if (!level.ee_tracker_prison_final_flight && isdefined(level.final_flight_activated) && level.final_flight_activated)
        {
            level.ee_tracker_prison_final_flight = 1;
            ee_tracker_prison_signal("bo2.mob.final_flight");
        }
        if (!level.ee_tracker_prison_ending && isdefined(level.winner))
        {
            level.ee_tracker_prison_ending = 1;
            if (level.winner == "weasel")
            {
                ee_tracker_prison_path("break_cycle");
                ee_tracker_prison_signal("bo2.mob.break_cycle.complete");
            }
            else
            {
                ee_tracker_prison_path("continue_cycle");
                ee_tracker_prison_signal("bo2.mob.continue_cycle.complete");
            }
        }
        wait 1;
    }
}

ee_tracker_prison_end_session() { level waittill("end_game"); printf("[EETrackerT6] {\"schemaVersion\":1,\"type\":\"session_ended\",\"game\":\"bo2\",\"map\":\"zm_prison\",\"sessionId\":\"" + level.ee_tracker_prison_session + "\",\"source\":\"gsc\"}\n"); }

ee_tracker_prison_watch_935_song()
{
    level endon("end_game");
    level waittill("nixie_935");
    if (level.ee_tracker_prison_935_reported) return;
    level.ee_tracker_prison_935_reported = 1;
    ee_tracker_prison_emit_side_egg_step("where_are_we_going", 0);
    ee_tracker_prison_emit_side_egg_step("where_are_we_going", 1);
    ee_tracker_prison_signal("bo2.mob.music_935.complete");
}

ee_tracker_prison_watch(flag_name, signal)
{
    if (flag(flag_name) && !isdefined(level.ee_tracker_prison_seen[flag_name]))
    {
        level.ee_tracker_prison_seen[flag_name] = 1;
        ee_tracker_prison_signal(signal);
    }
}

ee_tracker_prison_path(route)
{
    printf("[EETrackerT6] {\"schemaVersion\":1,\"type\":\"quest_path_selected\",\"game\":\"bo2\",\"map\":\"zm_prison\",\"sessionId\":\"" + level.ee_tracker_prison_session + "\",\"signalValue\":\"" + route + "\",\"source\":\"gsc\"}\n");
}

ee_tracker_prison_signal(signal)
{
    printf("[EETrackerT6] {\"schemaVersion\":1,\"type\":\"quest_signal\",\"game\":\"bo2\",\"map\":\"zm_prison\",\"sessionId\":\"" + level.ee_tracker_prison_session + "\",\"signal\":\"" + signal + "\",\"source\":\"gsc\"}\n");
}

ee_tracker_prison_emit_side_egg_step(egg_id, step_index)
{
    printf("[EETrackerT6] {\"schemaVersion\":1,\"type\":\"side_egg_step\",\"game\":\"bo2\",\"map\":\"zm_prison\",\"eggId\":\"" + egg_id + "\",\"stepIndex\":" + step_index + ",\"source\":\"gsc\"}\n");
}


