// EETracker Moon observer. Reads stock controller flags, live tank counters, and screen models.
#include common_scripts\utility;
#include maps\_utility;

main() { printf("EETracker: Moon observer loaded\n"); }

init()
{
	moon_tracker_begin_session();
	status = moon_tracker_read_game_status();
	moon_tracker_emit_snapshot(status[0], status[1], status[2]);
	level thread moon_tracker_watch_game_status(status[0], status[1], status[2]);
	level thread moon_tracker_watch_flags();
	level thread moon_tracker_watch_stage("sq_osc_over");
	level thread moon_tracker_watch_stage("sq_sc_over");
	level thread moon_tracker_watch_stage("sq_sc2_over");
	level thread moon_tracker_watch_stage("sq_ss2_over");
	level thread moon_tracker_watch_big_bang();
	level thread moon_tracker_watch_side_music_completion();
	level thread moon_tracker_watch_tanks();
	level thread moon_tracker_watch_samantha_colors();
	level thread moon_tracker_watch_richtofen_cue();
	level thread moon_tracker_watch_end_game();
	level thread moon_tracker_heartbeat();
	level thread moon_tracker_watch_player_inventory();
}

moon_tracker_watch_player_inventory()
{
	level endon("end_game"); level.ee_tracker_inventory_signatures = [];
	for (;;) { players = getPlayers(); for ( i = 0; i < players.size; i++ ) players[i] thread moon_tracker_inventory_if_changed(i); wait 1; }
}

moon_tracker_inventory_if_changed(slot)
{
	items = ""; weapons = self GetWeaponsListPrimaries();
	for ( i = 0; i < weapons.size; i++ ) if ( weapons[i] == "wavegun_zm" || weapons[i] == "wavegun_upgraded_zm" ) items = items + weapons[i] + "|";
	tactical = self get_player_tactical_grenade(); if ( isDefined(tactical) && (tactical == "qed_zm" || tactical == "zombie_black_hole_bomb") ) items = items + tactical + "|";
	if ( isDefined(level.ee_tracker_inventory_signatures[slot]) && level.ee_tracker_inventory_signatures[slot] == items ) return;
	level.ee_tracker_inventory_signatures[slot] = items;
	handle = fs_fopen("ee-tracker.jsonl", "append"); if ( !handle ) return;
	fs_writeline(handle, "{\"schemaVersion\":1,\"type\":\"player_inventory\",\"game\":\"bo1\",\"map\":\"zombie_moon\",\"playerSlot\":" + slot + ",\"inventoryItems\":\"" + items + "\",\"source\":\"gsc\"}"); fs_fclose(handle);
}

moon_tracker_watch_side_music_completion()
{
	last_count = 0;
	for (;;)
	{
		if ( isDefined(level.meteor_counter) && level.meteor_counter > last_count )
		{
			last_count = level.meteor_counter;
			if ( last_count >= 3 )
			{
				moon_tracker_emit_side_egg_step(0);
				moon_tracker_emit_side_egg_step(1);
				moon_tracker_emit_side_egg_step(2);
				moon_tracker_emit_signal("bo1.moon.music.complete");
				return;
			}
		}
		wait 0.2;
	}
}

moon_tracker_emit_side_egg_step(step_index)
{
	handle = fs_fopen("ee-tracker.jsonl", "append");
	if ( !handle ) return;
	line = "{\"schemaVersion\":1,\"type\":\"side_egg_step\",\"map\":\"Moon\",\"eggId\":\"coming_home\",\"stepIndex\":" + step_index + ",\"source\":\"gsc\"}";
	fs_writeline(handle, line);
	fs_fclose(handle);
}

moon_tracker_begin_session()
{
	handle = fs_fopen("ee-tracker.jsonl", "write");
	if ( !handle ) return;
	fs_writeline(handle, "{\"schemaVersion\":1,\"type\":\"session_started\",\"source\":\"gsc\"}");
	fs_fclose(handle);
}

moon_tracker_read_game_status()
{
	round = 0;
	if ( isDefined(level.round_number) ) round = level.round_number;
	players = getPlayers();
	power = 0;
	if ( flag("power_on") ) power = 1;
	return array(round, players.size, power);
}

moon_tracker_emit_snapshot(round, player_count, power)
{
	power_json = "false";
	if ( power ) power_json = "true";
	handle = fs_fopen("ee-tracker.jsonl", "append");
	if ( !handle ) return;
	line = "{\"schemaVersion\":1,\"type\":\"snapshot\",\"map\":\"zombie_moon\",\"round\":" + round + ",\"playerCount\":" + player_count + ",\"powerOn\":" + power_json + ",\"source\":\"gsc\"}";
	fs_writeline(handle, line);
	fs_fclose(handle);
}

moon_tracker_watch_game_status(last_round, last_player_count, last_power)
{
	for (;;)
	{
		wait 1;
		status = moon_tracker_read_game_status();
		if ( status[0] != last_round || status[1] != last_player_count || status[2] != last_power )
		{
			moon_tracker_emit_snapshot(status[0], status[1], status[2]);
			last_round = status[0]; last_player_count = status[1]; last_power = status[2];
		}
	}
}

moon_tracker_watch_flags()
{
	wait 0.5;
	quest_flags = array("ss1", "security_1_done", "security_2_done", "security_3_done", "security_4_done", "complete_be_1", "c_built", "w_placed", "vg_placed", "vg_charged", "first_tanks_drained", "second_tanks_drained", "soul_swap_done", "be2");
	reported = [];
	for ( i = 0; i < quest_flags.size; i++ ) reported[i] = false;
	for (;;)
	{
		for ( i = 0; i < quest_flags.size; i++ )
		{
			if ( !reported[i] && flag(quest_flags[i]) )
			{
				moon_tracker_emit_signal(quest_flags[i]);
				if ( quest_flags[i] == "soul_swap_done" )
					moon_tracker_emit_value("richtofen_cue", "The soul-swap dialogue begins; Richtofen and Samantha exchange lines.");
				reported[i] = true;
			}
		}
		wait 0.2;
	}
}

moon_tracker_watch_stage(stage_event)
{
	level waittill(stage_event);
	moon_tracker_emit_signal(stage_event);
}

moon_tracker_watch_big_bang()
{
	level waittill("moon_sidequest_big_bang_achieved");
	moon_tracker_emit_signal("moon_sidequest_big_bang_achieved");
}

moon_tracker_watch_tanks()
{
	last_key = "";
	for (;;)
	{
		wait 0.2;
		if ( !isDefined(level._active_tanks) || level._active_tanks.size == 0 ) continue;
		fills = "";
		max_fills = "";
		key = "";
		for ( i = 0; i < level._active_tanks.size; i++ )
		{
			tank = level._active_tanks[i];
			if ( !isDefined(tank) || !isDefined(tank.fill) || !isDefined(tank.max_fill) ) continue;
			if ( fills != "" ) { fills += ","; max_fills += ","; key += ","; }
			fills += tank.fill;
			max_fills += tank.max_fill;
			key += tank.fill + "/" + tank.max_fill;
		}
		if ( key != "" && key != last_key )
		{
			handle = fs_fopen("ee-tracker.jsonl", "append");
			if ( handle )
			{
				line = "{\"schemaVersion\":1,\"type\":\"soul_tank_progress\",\"soulTankFill\":[" + fills + "],\"soulTankMaxFill\":[" + max_fills + "],\"source\":\"gsc\"}";
				fs_writeline(handle, line);
				fs_fclose(handle);
			}
			last_key = key;
		}
	}
}

moon_tracker_read_display_color()
{
	if ( !isDefined(level._ss_buttons) || level._ss_buttons.size != 4 || !flag("displays_active") ) return "";
	color = "";
	for ( i = 0; i < level._ss_buttons.size; i++ )
	{
		button = level._ss_buttons[i];
		if ( !isDefined(button.terminal_model) || !isDefined(button.terminal_model.model) ) return "";
		model = button.terminal_model.model;
		current = "";
		if ( model == "p_zom_moon_magic_box_com_red" ) current = "RED";
		if ( model == "p_zom_moon_magic_box_com_green" ) current = "GREEN";
		if ( model == "p_zom_moon_magic_box_com_blue" ) current = "BLUE";
		if ( model == "p_zom_moon_magic_box_com_yellow" ) current = "YELLOW";
		if ( current == "" ) return "";
		if ( color != "" && color != current ) return "";
		color = current;
	}
	return color;
}

moon_tracker_watch_samantha_colors()
{
	active_color = "";
	stable_ticks = 0;
	for (;;)
	{
		wait 0.05;
		color = moon_tracker_read_display_color();
		if ( color == active_color && color != "" ) { stable_ticks++; continue; }
		if ( active_color != "" && stable_ticks >= 6 ) moon_tracker_emit_value("samantha_color", active_color);
		active_color = color;
		stable_ticks = 0;
	}
}

moon_tracker_watch_richtofen_cue()
{
	reported = false;
	for (;;)
	{
		wait 0.2;
		if ( reported || !isDefined(level._cur_stage_name) || level._cur_stage_name != "ctt2" ) continue;
		s = getstruct("sq_vg_final", "targetname");
		players = get_players();
		for ( i = 0; i < players.size; i++ )
		{
			character = players[i] GetEntityNumber();
			if ( isDefined(players[i].zm_random_char) ) character = players[i].zm_random_char;
			if ( character == 3 && DistanceSquared(players[i].origin, s.origin) < 240 * 240 )
			{
				moon_tracker_emit_value("richtofen_cue", "Richtofen approaches the Vril Device; his tank-stage dialogue begins.");
				reported = true;
				break;
			}
		}
	}
}

moon_tracker_watch_end_game()
{
	level waittill("end_game");
	moon_tracker_emit_simple("session_ended");
}

moon_tracker_heartbeat()
{
	level endon("end_game");
	for (;;) { moon_tracker_emit_simple("heartbeat"); wait 2; }
}

moon_tracker_emit_signal(signal)
{
	handle = fs_fopen("ee-tracker.jsonl", "append");
	if ( !handle ) return;
	fs_writeline(handle, "{\"schemaVersion\":1,\"type\":\"quest_signal\",\"signal\":\"stock.success." + signal + "\",\"source\":\"gsc\"}");
	fs_fclose(handle);
}

moon_tracker_emit_value(event_type, value)
{
	handle = fs_fopen("ee-tracker.jsonl", "append");
	if ( !handle ) return;
	line = "{\"schemaVersion\":1,\"type\":\"" + event_type + "\",\"signalValue\":\"" + value + "\",\"source\":\"gsc\"}";
	fs_writeline(handle, line);
	fs_fclose(handle);
}

moon_tracker_emit_simple(event_type)
{
	handle = fs_fopen("ee-tracker.jsonl", "append");
	if ( !handle ) return;
	fs_writeline(handle, "{\"schemaVersion\":1,\"type\":\"" + event_type + "\",\"source\":\"gsc\"}");
	fs_fclose(handle);
}
