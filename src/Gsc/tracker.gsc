// EETracker T5 Ascension observer. Scoped to this map; does not replace quest scripts.
// Plutonium T5 confines file I/O to scriptdata/ee-tracker.jsonl.

#include common_scripts\utility;
#include maps\_utility;

main()
{
	printf("EETracker: map script loaded\n");
}

init()
{
	tracker_begin_session();
	tracker_detect_variant();
	status = tracker_read_game_status();
	if ( tracker_emit_snapshot(status[0], status[1], status[2]) )
		printf("EETracker: initial status written to scriptdata/ee-tracker.jsonl\n");
	else
		printf("EETracker: failed to write initial status to scriptdata/ee-tracker.jsonl\n");
	level thread tracker_watch_game_status(status[0], status[1], status[2]);
	level thread tracker_watch_ascension_success_flags();
	level thread tracker_watch_ascension_monkey_button_interaction();
	level thread tracker_watch_end_game();
	level thread tracker_emit_heartbeat();
	level thread tracker_watch_pressure_pad_estimate();
	level thread tracker_watch_luna_progress();
	level thread tracker_watch_side_music_completion();
	level thread tracker_watch_player_inventory();
}

tracker_watch_player_inventory()
{
	level endon("end_game");
	for (;;)
	{
		players = getPlayers();
		for ( i = 0; i < players.size; i++ )
			players[i] thread tracker_emit_inventory_if_changed(i);
		wait 1;
	}
}

tracker_emit_inventory_if_changed(player_slot)
{
	player = self;
	items = "";
	weapons = player GetWeaponsListPrimaries();
	for ( i = 0; i < weapons.size; i++ )
		if ( tracker_is_quest_item(weapons[i]) ) items = items + weapons[i] + "|";
	tactical = player get_player_tactical_grenade();
	if ( isDefined(tactical) && tracker_is_quest_item(tactical) ) items = items + tactical + "|";
	if ( !isDefined(level.ee_tracker_inventory_signatures) ) level.ee_tracker_inventory_signatures = [];
	if ( isDefined(level.ee_tracker_inventory_signatures[player_slot]) && level.ee_tracker_inventory_signatures[player_slot] == items ) return;
	level.ee_tracker_inventory_signatures[player_slot] = items;
	tracker_emit_inventory(player_slot, items);
}

tracker_is_quest_item(weapon)
{
	return weapon == "zombie_black_hole_bomb" || weapon == "zombie_nesting_dolls" || weapon == "ray_gun_upgraded_zm" || weapon == "thundergun_upgraded_zm";
}

tracker_emit_inventory(player_slot, items)
{
	handle = fs_fopen("ee-tracker.jsonl", "append");
	if ( !handle ) return;
	line = "{\"schemaVersion\":1,\"type\":\"player_inventory\",\"game\":\"bo1\",\"map\":\"zombie_cosmodrome\",\"playerSlot\":" + player_slot + ",\"inventoryItems\":\"" + items + "\",\"source\":\"gsc\"}";
	fs_writeline(handle, line);
	fs_fclose(handle);
}

tracker_watch_side_music_completion()
{
	last_count = 0;
	for (;;)
	{
		if ( isDefined(level.teddybear_counter) && level.teddybear_counter > last_count )
		{
			last_count = level.teddybear_counter;
			if ( last_count >= 3 )
			{
				tracker_emit_side_egg_step("Ascension", "abracadavre_song", 0);
				tracker_emit_side_egg_step("Ascension", "abracadavre_song", 1);
				tracker_emit_side_egg_step("Ascension", "abracadavre_song", 2);
				tracker_emit_event("bo1.ascension.music.complete");
				return;
			}
		}
		wait 0.2;
	}
}

tracker_emit_side_egg_step(map_name, egg_id, step_index)
{
	handle = fs_fopen("ee-tracker.jsonl", "append");
	if ( !handle ) return;
	line = "{\"schemaVersion\":1,\"type\":\"side_egg_step\",\"map\":\"" + map_name + "\",\"eggId\":\"" + egg_id + "\",\"stepIndex\":" + step_index + ",\"source\":\"gsc\"}";
	fs_writeline(handle, line);
	fs_fclose(handle);
}

tracker_begin_session()
{
	level.ee_tracker_session_id = randomInt(1000000000);
	handle = fs_fopen("ee-tracker.jsonl", "write");
	if ( !handle )
	{
		printf("EETracker: could not open session file; scr_allowFileIo=" + getDvar("scr_allowFileIo") + "\n");
		return;
	}

	fs_writeline(handle, "{\"schemaVersion\":1,\"sessionId\":\"" + level.ee_tracker_session_id + "\",\"type\":\"session_started\",\"source\":\"gsc\"}");
	fs_fclose(handle);
	printf("EETracker: session file opened\n");
}

tracker_emit_event(signal)
{
	handle = fs_fopen("ee-tracker.jsonl", "append");
	if ( !handle )
		return;

	line = "{\"schemaVersion\":1,\"type\":\"quest_signal\",\"signal\":\"" + signal + "\",\"source\":\"gsc\"}";
	fs_writeline(handle, line);
	fs_fclose(handle);
}

tracker_read_game_status()
{
	round = 0;
	if ( isDefined( level.round_number ) )
		round = level.round_number;

	players = getPlayers();
	player_count = players.size;
	power = 0;
	if ( flag( "power_on" ) )
		power = 1;

	return array(round, player_count, power);
}

tracker_emit_snapshot(round, player_count, power)
{
	power_json = "false";
	if ( power )
		power_json = "true";

	handle = fs_fopen("ee-tracker.jsonl", "append");
	if ( !handle )
		return false;

	line = "{\"schemaVersion\":1,\"type\":\"snapshot\",\"map\":\"zombie_cosmodrome\",\"round\":" + round + ",\"playerCount\":" + player_count + ",\"powerOn\":" + power_json + ",\"source\":\"gsc\"}";
	written = fs_writeline(handle, line);
	fs_fclose(handle);
	return written;
}

tracker_emit_variant( variant )
{
	handle = fs_fopen("ee-tracker.jsonl", "append");
	if ( !handle )
		return;

	line = "{\"schemaVersion\":1,\"type\":\"variant\",\"variantEvidence\":{\"kind\":\"ModLoaded\",\"value\":\"" + variant + "\"},\"source\":\"gsc\"}";
	fs_writeline(handle, line);
	fs_fclose(handle);
}

tracker_effective_profile_value( dvar_name, fallback )
{
	if ( getDvar( dvar_name ) == "" )
		return fallback;

	return getDvarInt( dvar_name );
}

tracker_detect_variant()
{
	// The Any Player EE hook function is the positive mod identity. Then match
	// the complete Ascension profile, including its distinctive SR timeout.
	mod_override = getFunction( "scripts/sp/any_player_ee", "override" );
	if ( !isDefined( mod_override ) )
		return;

	buttons = tracker_effective_profile_value( "any_player_ee_cosmodrome_buttons", -1 );
	timeout = tracker_effective_profile_value( "any_player_ee_cosmodrome_buttons_timeout", 0 );
	lander = tracker_effective_profile_value( "any_player_ee_cosmodrome_lander_1p", 1 );
	doll = tracker_effective_profile_value( "any_player_ee_cosmodrome_combo_doll_1p", 1 );

	if ( buttons == 4 && timeout == 100000 && lander == 1 && doll == 1 )
		tracker_emit_variant( "any_player_ee_sr" );
	else if ( buttons == -1 && timeout == 0 && lander == 1 && doll == 1 )
		tracker_emit_variant( "any_player_ee" );
}

tracker_watch_game_status(last_round, last_player_count, last_power)
{
	for (;;)
	{
		wait 1;
		status = tracker_read_game_status();
		round = status[0];
		player_count = status[1];
		power = status[2];

		if ( round != last_round || player_count != last_player_count || power != last_power )
		{
			tracker_emit_snapshot(round, player_count, power);
			last_round = round;
			last_player_count = player_count;
			last_power = power;
		}
	}
}

tracker_watch_ascension_success_flags()
{
	quest_flags = array("target_teleported", "rerouted_power", "switches_synced", "pressure_sustained", "passkey_confirmed", "weapons_combined");
	reported = [];
	for ( i = 0; i < quest_flags.size; i++ )
		reported[i] = false;

	for (;;)
	{
		for ( i = 0; i < quest_flags.size; i++ )
		{
			if ( !reported[i] && flag( quest_flags[i] ) )
			{
				tracker_emit_event("stock.success." + quest_flags[i]);
				reported[i] = true;
			}
		}
		wait 0.25;
	}
}

tracker_watch_ascension_monkey_button_interaction()
{
	// Confirmed in installed Any Player EE v2.3.1 source. This is an interaction,
	// not a success/completion signal and contains no switch identity.
	for (;;)
	{
		level waittill("sync_button_pressed");
		tracker_emit_event("ascension.monkey_button_interaction");
	}
}

tracker_watch_end_game()
{
	level waittill("end_game");
	tracker_emit_simple("session_ended");
}

tracker_emit_heartbeat()
{
	level endon("end_game");
	for (;;)
	{
		tracker_emit_simple("heartbeat");
		wait 2;
	}
}

tracker_emit_simple(event_type)
{
	handle = fs_fopen("ee-tracker.jsonl", "append");
	if ( !handle )
		return;
	fs_writeline(handle, "{\"schemaVersion\":1,\"type\":\"" + event_type + "\",\"source\":\"gsc\"}");
	fs_fclose(handle);
}

tracker_emit_pressure_timer(seconds_remaining, timer_state)
{
	handle = fs_fopen("ee-tracker.jsonl", "append");
	if ( !handle )
		return;
	fs_writeline(handle, "{\"schemaVersion\":1,\"type\":\"pressure_timer\",\"secondsRemaining\":" + seconds_remaining + ",\"signalValue\":\"" + timer_state + "\",\"source\":\"gsc\"}");
	fs_fclose(handle);
}

tracker_emit_luna_progress(letters_collected)
{
	handle = fs_fopen("ee-tracker.jsonl", "append");
	if ( !handle )
		return;
	fs_writeline(handle, "{\"schemaVersion\":1,\"type\":\"luna_progress\",\"lettersCollected\":" + letters_collected + ",\"source\":\"gsc\"}");
	fs_fclose(handle);
}

tracker_watch_luna_progress()
{
	level endon("end_game");
	// Both stock Ascension and the reviewed Any Player EE script update this
	// quest-owned counter only after a letter is accepted, or when it resets.
	last_progress = -1;
	for (;;)
	{
		if ( isDefined(level.passkey_progress) )
		{
			progress = level.passkey_progress;
			if ( progress >= 0 && progress <= 4 && progress != last_progress )
			{
				tracker_emit_luna_progress(progress);
				last_progress = progress;
			}
		}
		wait 0.05;
	}
}

tracker_watch_pressure_pad_estimate()
{
	level endon("end_game");
	// Mirrors the reviewed 120-second stock trigger. The stock script keeps its
	// own counter local, so this is explicitly an estimate for the companion.
	wait 1;
	area = GetStruct("pressure_pad", "targetname");
	if ( !isDefined(area) )
		return;
	trig = Spawn("trigger_radius", area.origin, 0, 300, 100);
	active = false;
	remaining = 120;
	while ( !flag("pressure_sustained") )
	{
		players = getPlayers();
		all_inside = players.size > 0;
		for ( i = 0; i < players.size; i++ )
			if ( !players[i] IsTouching(trig) )
				all_inside = false;
		if ( !all_inside )
		{
			if ( active )
			{
				active = false;
				remaining = 120;
				tracker_emit_pressure_timer(remaining, "waiting");
			}
			wait 0.25;
			continue;
		}
		if ( !active )
		{
			active = true;
			remaining = 120;
			tracker_emit_pressure_timer(remaining, "running");
		}
		wait 1;
		remaining--;
		tracker_emit_pressure_timer(remaining, "running");
	}
	tracker_emit_pressure_timer(0, "complete");
	trig Delete();
}
