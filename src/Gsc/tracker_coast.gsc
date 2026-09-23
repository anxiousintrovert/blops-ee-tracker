// EETracker T5 Call of the Dead observer. Map scoped; does not replace quest scripts.
// Plutonium T5 confines file I/O to scriptdata/ee-tracker.jsonl.

#include common_scripts\utility;
#include maps\_utility;

main()
{
	printf("EETracker: Call of the Dead observer loaded\n");
}

init()
{
	coast_tracker_begin_session();
	status = coast_tracker_read_game_status();
	coast_tracker_emit_snapshot(status[0], status[1], status[2]);
	level thread coast_tracker_watch_game_status(status[0], status[1], status[2]);
	level thread coast_tracker_watch_quest_flags();
	level thread coast_tracker_watch_progress();
	level thread coast_tracker_watch_completion_event();
	level thread coast_tracker_watch_end_game();
	level thread coast_tracker_emit_heartbeat();
}

coast_tracker_begin_session()
{
	level.ee_tracker_session_id = randomInt(1000000000);
	handle = fs_fopen("ee-tracker.jsonl", "write");
	if ( !handle )
	{
		printf("EETracker: could not open Call of the Dead session file; scr_allowFileIo=" + getDvar("scr_allowFileIo") + "\n");
		return;
	}
	fs_writeline(handle, "{\"schemaVersion\":1,\"sessionId\":\"" + level.ee_tracker_session_id + "\",\"type\":\"session_started\",\"source\":\"gsc\"}");
	fs_fclose(handle);
	printf("EETracker: Call of the Dead session file opened\n");
}

coast_tracker_emit_event(signal)
{
	handle = fs_fopen("ee-tracker.jsonl", "append");
	if ( !handle )
		return;
	line = "{\"schemaVersion\":1,\"type\":\"quest_signal\",\"signal\":\"" + signal + "\",\"source\":\"gsc\"}";
	fs_writeline(handle, line);
	fs_fclose(handle);
}

coast_tracker_read_game_status()
{
	round = 0;
	if ( isDefined(level.round_number) )
		round = level.round_number;
	players = getPlayers();
	player_count = players.size;
	power = 0;
	if ( flag("power_on") )
		power = 1;
	return array(round, player_count, power);
}

coast_tracker_emit_snapshot(round, player_count, power)
{
	power_json = "false";
	if ( power )
		power_json = "true";
	handle = fs_fopen("ee-tracker.jsonl", "append");
	if ( !handle )
		return;
	line = "{\"schemaVersion\":1,\"type\":\"snapshot\",\"map\":\"zombie_coast\",\"round\":" + round + ",\"playerCount\":" + player_count + ",\"powerOn\":" + power_json + ",\"source\":\"gsc\"}";
	fs_writeline(handle, line);
	fs_fclose(handle);
}

coast_tracker_watch_game_status(last_round, last_player_count, last_power)
{
	for (;;)
	{
		wait 1;
		status = coast_tracker_read_game_status();
		round = status[0];
		player_count = status[1];
		power = status[2];
		if ( round != last_round || player_count != last_player_count || power != last_power )
		{
			coast_tracker_emit_snapshot(round, player_count, power);
			last_round = round;
			last_player_count = player_count;
			last_power = power;
		}
	}
}

coast_tracker_watch_quest_flags()
{
	// Let zombie_coast_eggs::init() declare the flags before reading them.
	wait 0.5;
	quest_flags = array("power_on", "ffs", "ffd", "hg0", "hg1", "hg2", "hg3", "hgd", "bd", "aca", "shs", "sr", "bp", "ss", "mcs", "mm", "re");
	reported = [];
	for ( i = 0; i < quest_flags.size; i++ )
		reported[i] = false;
	for (;;)
	{
		for ( i = 0; i < quest_flags.size; i++ )
		{
			if ( !reported[i] && flag(quest_flags[i]) )
			{
				coast_tracker_emit_event("stock.success." + quest_flags[i]);
				reported[i] = true;
			}
		}
		wait 0.2;
	}
}

coast_tracker_watch_progress()
{
	last_beacons = "";
	last_dials = "";
	for (;;)
	{
		if ( isDefined(level._serenade) && isDefined(level.mermaid) && level.mermaid.size > 0 )
		{
			progress = level._serenade.size;
			maximum = level.mermaid.size;
			if ( progress > maximum ) progress = maximum;
			key = progress + "/" + maximum;
			if ( key != last_beacons )
			{
				coast_tracker_emit_progress("coast.beacon_sequence", progress, maximum);
				last_beacons = key;
			}
		}
		if ( isDefined(level._dials) && isDefined(level.together_again) && level.together_again.size > 0 )
		{
			correct = 0;
			maximum = level.together_again.size;
			for ( i = 0; i < maximum; i++ )
			{
				if ( isDefined(level._dials[i]) && isDefined(level._dials[i].pos) && level._dials[i].pos == level.together_again[i] )
					correct++;
			}
			key = correct + "/" + maximum;
			if ( key != last_dials )
			{
				coast_tracker_emit_progress("coast.pure_harmony", correct, maximum);
				last_dials = key;
			}
		}
		wait 0.2;
	}
}

coast_tracker_emit_progress(signal, progress, maximum)
{
	handle = fs_fopen("ee-tracker.jsonl", "append");
	if ( !handle ) return;
	line = "{\"schemaVersion\":1,\"type\":\"quest_progress\",\"signal\":\"" + signal + "\",\"progress\":" + progress + ",\"progressMax\":" + maximum + ",\"source\":\"gsc\"}";
	fs_writeline(handle, line);
	fs_fclose(handle);
}

coast_tracker_watch_completion_event()
{
	level waittill("coast_easter_egg_achieved");
	coast_tracker_emit_event("stock.success.coast_easter_egg_achieved");
}

coast_tracker_emit_heartbeat()
{
	level endon("end_game");
	for (;;)
	{
		coast_tracker_emit_simple("heartbeat");
		wait 2;
	}
}

coast_tracker_watch_end_game()
{
	level waittill("end_game");
	coast_tracker_emit_simple("session_ended");
}

coast_tracker_emit_simple(event_type)
{
	handle = fs_fopen("ee-tracker.jsonl", "append");
	if ( !handle )
		return;
	fs_writeline(handle, "{\"schemaVersion\":1,\"type\":\"" + event_type + "\",\"source\":\"gsc\"}");
	fs_fclose(handle);
}
