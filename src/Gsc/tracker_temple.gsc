// EETracker observer for Shangri-La; reads stock quest state without replacing it.
#include common_scripts\utility;
#include maps\_utility;

main()
{
	printf("EETracker: Shangri-La observer loaded\n");
}

init()
{
	temple_tracker_begin_session();
	status = temple_tracker_read_game_status();
	temple_tracker_emit_snapshot(status[0], status[1], status[2]);
	level thread temple_tracker_watch_game_status(status[0], status[1], status[2]);
	level thread temple_tracker_watch_quest_flags();
	level thread temple_tracker_watch_progress();
	level thread temple_tracker_watch_eclipse();
	level thread temple_tracker_watch_stage("sq_LGS_over");
	level thread temple_tracker_watch_stage("sq_bttp2_over");
	level thread temple_tracker_watch_stage("sq_BaG_over");
	level thread temple_tracker_watch_completion();
	level thread temple_tracker_watch_end_game();
	level thread temple_tracker_heartbeat();
}

temple_tracker_begin_session()
{
	handle = fs_fopen("ee-tracker.jsonl", "write");
	if ( !handle ) return;
	fs_writeline(handle, "{\"schemaVersion\":1,\"type\":\"session_started\",\"source\":\"gsc\"}");
	fs_fclose(handle);
}

temple_tracker_read_game_status()
{
	round = 0;
	if ( isDefined(level.round_number) ) round = level.round_number;
	players = getPlayers();
	power = 0;
	if ( flag("power_on") ) power = 1;
	return array(round, players.size, power);
}

temple_tracker_emit_snapshot(round, player_count, power)
{
	power_json = "false";
	if ( power ) power_json = "true";
	handle = fs_fopen("ee-tracker.jsonl", "append");
	if ( !handle ) return;
	line = "{\"schemaVersion\":1,\"type\":\"snapshot\",\"map\":\"zombie_temple\",\"round\":" + round + ",\"playerCount\":" + player_count + ",\"powerOn\":" + power_json + ",\"source\":\"gsc\"}";
	fs_writeline(handle, line);
	fs_fclose(handle);
}

temple_tracker_watch_game_status(last_round, last_player_count, last_power)
{
	for (;;)
	{
		wait 1;
		status = temple_tracker_read_game_status();
		if ( status[0] != last_round || status[1] != last_player_count || status[2] != last_power )
		{
			temple_tracker_emit_snapshot(status[0], status[1], status[2]);
			last_round = status[0]; last_player_count = status[1]; last_power = status[2];
		}
	}
}

temple_tracker_watch_quest_flags()
{
	wait 0.5;
	quest_flags = array("oafc_switch_pressed", "oafc_plot_vo_done", "dgcwf_sw1_pressed", "dgcwf_plot_vo_done", "sq_ptt_dial_dialed", "sq_ptt_level_pulled", "ptt_plot_vo_done", "std_target_1", "std_target_2", "std_target_3", "std_target_4", "std_plot_vo_done", "given_dynamite", "meteorite_shrunk");
	reported = [];
	for ( i = 0; i < quest_flags.size; i++ ) reported[i] = false;
	for (;;)
	{
		for ( i = 0; i < quest_flags.size; i++ )
		{
			if ( !reported[i] && flag(quest_flags[i]) )
			{
				temple_tracker_emit_signal(quest_flags[i]);
				reported[i] = true;
			}
		}
		wait 0.2;
	}
}

temple_tracker_watch_progress()
{
	last_tiles = "";
	last_plate = "";
	last_gas = "";
	last_dials = "";
	last_gongs = "";
	gongs_reported = false;
	for (;;)
	{
		if ( isDefined(level._num_matched_tiles) && isDefined(level._num_tiles_to_match) && level._num_tiles_to_match > 0 )
		{
			key = level._num_matched_tiles + "/" + level._num_tiles_to_match;
			if ( key != last_tiles )
			{
				temple_tracker_emit_progress("temple.tiles", level._num_matched_tiles, level._num_tiles_to_match);
				last_tiles = key;
			}
		}
		if ( isDefined(level._on_plate) || flag("dgcwf_on_plate") )
		{
			players = getPlayers();
			maximum = 3;
			progress = 0;
			if ( players.size == 1 )
			{
				maximum = 1;
				if ( flag("dgcwf_on_plate") ) progress = 1;
			}
			else if ( isDefined(level._on_plate) )
				progress = level._on_plate;
			if ( progress > maximum ) progress = maximum;
			key = progress + "/" + maximum;
			if ( key != last_plate )
			{
				temple_tracker_emit_progress("temple.slide_plate", progress, maximum);
				last_plate = key;
			}
		}
		if ( isDefined(level._ptt_num_lit) && isDefined(level._ptt_jets) && level._ptt_jets > 0 )
		{
			key = level._ptt_num_lit + "/" + level._ptt_jets;
			if ( key != last_gas )
			{
				temple_tracker_emit_progress("temple.gas_pipes", level._ptt_num_lit, level._ptt_jets);
				last_gas = key;
			}
		}
		if ( isDefined(level._num_dials_correct) && isDefined(level._num_dials_to_match) && level._num_dials_to_match > 0 )
		{
			key = level._num_dials_correct + "/" + level._num_dials_to_match;
			if ( key != last_dials )
			{
				temple_tracker_emit_progress("temple.dials", level._num_dials_correct, level._num_dials_to_match);
				last_dials = key;
			}
		}
		if ( isDefined(level._num_gongs) )
		{
			gongs = level._num_gongs;
			if ( gongs < 0 ) gongs = 0;
			if ( gongs > 4 ) gongs = 4;
			key = gongs + "/4";
			if ( key != last_gongs )
			{
				temple_tracker_emit_progress("temple.gongs", gongs, 4);
				last_gongs = key;
			}
			if ( !gongs_reported && gongs == 4 )
			{
				temple_tracker_emit_signal("temple_gongs_complete");
				gongs_reported = true;
			}
		}
		wait 0.2;
	}
}

temple_tracker_emit_progress(signal, progress, maximum)
{
	handle = fs_fopen("ee-tracker.jsonl", "append");
	if ( !handle ) return;
	line = "{\"schemaVersion\":1,\"type\":\"quest_progress\",\"signal\":\"" + signal + "\",\"progress\":" + progress + ",\"progressMax\":" + maximum + ",\"source\":\"gsc\"}";
	fs_writeline(handle, line);
	fs_fclose(handle);
}

temple_tracker_watch_eclipse()
{
	reported = false;
	for (;;)
	{
		if ( !reported && isDefined(level._stage_active) && level._stage_active )
		{
			temple_tracker_emit_signal("eclipse_started");
			reported = true;
		}
		wait 0.2;
	}
}

temple_tracker_watch_stage(stage_event)
{
	level waittill(stage_event);
	temple_tracker_emit_signal(stage_event);
}

temple_tracker_watch_completion()
{
	level waittill("temple_sidequest_achieved");
	temple_tracker_emit_signal("temple_sidequest_achieved");
}

temple_tracker_watch_end_game()
{
	level waittill("end_game");
	temple_tracker_emit_simple("session_ended");
}

temple_tracker_heartbeat()
{
	level endon("end_game");
	for (;;) { temple_tracker_emit_simple("heartbeat"); wait 2; }
}

temple_tracker_emit_signal(signal)
{
	handle = fs_fopen("ee-tracker.jsonl", "append");
	if ( !handle ) return;
	fs_writeline(handle, "{\"schemaVersion\":1,\"type\":\"quest_signal\",\"signal\":\"stock.success." + signal + "\",\"source\":\"gsc\"}");
	fs_fclose(handle);
}

temple_tracker_emit_simple(event_type)
{
	handle = fs_fopen("ee-tracker.jsonl", "append");
	if ( !handle ) return;
	fs_writeline(handle, "{\"schemaVersion\":1,\"type\":\"" + event_type + "\",\"source\":\"gsc\"}");
	fs_fclose(handle);
}
