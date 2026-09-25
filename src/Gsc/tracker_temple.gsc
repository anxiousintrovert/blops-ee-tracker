// EETracker observer for Shangri-La; reads stock quest state without replacing it.
#include common_scripts\utility;
#include maps\_utility;

main()
{
	printf("EETracker: Shangri-La observer loaded\n");
}

init()
{
	level.ee_tracker_glyph_models = array("p_ztem_glyphs_01_unlit", "p_ztem_glyphs_02_unlit", "p_ztem_glyphs_03_unlit", "p_ztem_glyphs_04_unlit", "p_ztem_glyphs_05_unlit", "p_ztem_glyphs_06_unlit", "p_ztem_glyphs_07_unlit", "p_ztem_glyphs_08_unlit", "p_ztem_glyphs_09_unlit", "p_ztem_glyphs_10_unlit", "p_ztem_glyphs_11_unlit", "p_ztem_glyphs_12_unlit");
	temple_tracker_begin_session();
	status = temple_tracker_read_game_status();
	temple_tracker_emit_snapshot(status[0], status[1], status[2]);
	level thread temple_tracker_watch_game_status(status[0], status[1], status[2]);
	level thread temple_tracker_watch_quest_flags();
	level thread temple_tracker_watch_progress();
	level thread temple_tracker_watch_tile_banks();
	level thread temple_tracker_watch_eclipse();
	level thread temple_tracker_watch_stage("sq_LGS_over");
	level thread temple_tracker_watch_stage("sq_bttp2_over");
	level thread temple_tracker_watch_stage("sq_BaG_over");
	level thread temple_tracker_watch_completion();
	level thread temple_tracker_watch_side_music_completion();
	level thread temple_tracker_watch_end_game();
	level thread temple_tracker_heartbeat();
	level thread temple_tracker_watch_player_inventory();
}

temple_tracker_watch_player_inventory()
{
	level endon("end_game"); level.ee_tracker_inventory_signatures = [];
	for (;;) { players = getPlayers(); for ( i = 0; i < players.size; i++ ) players[i] thread temple_tracker_inventory_if_changed(i); wait 1; }
}

temple_tracker_inventory_if_changed(slot)
{
	items = ""; weapons = self GetWeaponsListPrimaries();
	for ( i = 0; i < weapons.size; i++ ) if ( weapons[i] == "shrink_ray_zm" || weapons[i] == "shrink_ray_upgraded_zm" || weapons[i] == "spikemore_zm" ) items = items + weapons[i] + "|";
	tactical = self get_player_tactical_grenade(); if ( isDefined(tactical) && tactical == "spikemore_zm" ) items = items + tactical + "|";
	if ( isDefined(level.ee_tracker_inventory_signatures[slot]) && level.ee_tracker_inventory_signatures[slot] == items ) return;
	level.ee_tracker_inventory_signatures[slot] = items;
	handle = fs_fopen("ee-tracker.jsonl", "append"); if ( !handle ) return;
	fs_writeline(handle, "{\"schemaVersion\":1,\"type\":\"player_inventory\",\"game\":\"bo1\",\"map\":\"zombie_temple\",\"playerSlot\":" + slot + ",\"inventoryItems\":\"" + items + "\",\"source\":\"gsc\"}"); fs_fclose(handle);
}

temple_tracker_watch_side_music_completion()
{
	last_count = 0;
	for (;;)
	{
		if ( isDefined(level.meteor_counter) && level.meteor_counter > last_count )
		{
			last_count = level.meteor_counter;
			if ( last_count >= 3 )
			{
				temple_tracker_emit_side_egg_step("pareidolia_song", 0);
				temple_tracker_emit_side_egg_step("pareidolia_song", 1);
				temple_tracker_emit_side_egg_step("pareidolia_song", 2);
				temple_tracker_emit_signal("bo1.temple.music.complete");
				return;
			}
		}
		wait 0.2;
	}
}

temple_tracker_emit_side_egg_step(egg_id, step_index)
{
	handle = fs_fopen("ee-tracker.jsonl", "append");
	if ( !handle ) return;
	line = "{\"schemaVersion\":1,\"type\":\"side_egg_step\",\"map\":\"Shangri-La\",\"eggId\":\"" + egg_id + "\",\"stepIndex\":" + step_index + ",\"source\":\"gsc\"}";
	fs_writeline(handle, line);
	fs_fclose(handle);
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

// Observe the stock Shangri-La tile-pair state. The quest itself assigns one
// of twelve glyph models to each bank and compares the two picked model names.
// Keep this informational: only stock quest flags advance the main flow.
temple_tracker_watch_tile_banks()
{
	last_bank1 = 0;
	last_bank2 = 0;
	last_pair_state = "";
	last_pair_signature = "";
	for (;;)
	{
		bank1 = 0;
		bank2 = 0;
		if ( isDefined(level._picked_tile1) ) bank1 = temple_tracker_glyph_id(level._picked_tile1.tile);
		if ( isDefined(level._picked_tile2) ) bank2 = temple_tracker_glyph_id(level._picked_tile2.tile);

		if ( bank1 > 0 && bank1 != last_bank1 )
			temple_tracker_emit_tile_state(1, bank1, 0, 0, "selected");
		if ( bank2 > 0 && bank2 != last_bank2 )
			temple_tracker_emit_tile_state(2, bank2, 0, 0, "selected");

		if ( bank1 > 0 && bank2 > 0 )
		{
			signature = bank1 + ":" + bank2;
			if ( isDefined(level._picked_tile1.matched) && level._picked_tile1.matched && isDefined(level._picked_tile2.matched) && level._picked_tile2.matched )
			{
				if ( last_pair_state != "matched" || signature != last_pair_signature )
					temple_tracker_emit_tile_state(1, bank1, 2, bank2, "matched");
				last_pair_state = "matched";
				last_pair_signature = signature;
			}
			else if ( bank1 != bank2 && ( last_pair_state != "mismatch" || signature != last_pair_signature ) )
			{
				temple_tracker_emit_tile_state(1, bank1, 2, bank2, "mismatch");
				last_pair_state = "mismatch";
				last_pair_signature = signature;
			}
		}
		else if ( bank1 == 0 && bank2 == 0 )
		{
			if ( last_bank1 > 0 && last_pair_state != "matched" ) temple_tracker_emit_tile_state(1, last_bank1, 0, 0, "cleared");
			if ( last_bank2 > 0 && last_pair_state != "matched" ) temple_tracker_emit_tile_state(2, last_bank2, 0, 0, "cleared");
			last_pair_state = "";
			last_pair_signature = "";
		}
		else
		{
			if ( last_bank1 > 0 && bank1 == 0 && last_pair_state != "matched" ) temple_tracker_emit_tile_state(1, last_bank1, 0, 0, "cleared");
			if ( last_bank2 > 0 && bank2 == 0 && last_pair_state != "matched" ) temple_tracker_emit_tile_state(2, last_bank2, 0, 0, "cleared");
			if ( last_pair_state == "mismatch" ) { last_pair_state = ""; last_pair_signature = ""; }
		}

		last_bank1 = bank1;
		last_bank2 = bank2;
		wait 0.05;
	}
}

temple_tracker_glyph_id(model_name)
{
	for ( i = 0; i < level.ee_tracker_glyph_models.size; i++ )
		if ( level.ee_tracker_glyph_models[i] == model_name ) return i + 1;
	return 0;
}

temple_tracker_emit_tile_state(bank, tile_id, peer_bank, peer_tile_id, state)
{
	handle = fs_fopen("ee-tracker.jsonl", "append");
	if ( !handle ) return;
	line = "{\"schemaVersion\":1,\"type\":\"quest_tile_state\",\"game\":\"bo1\",\"map\":\"Shangri-La\",\"signal\":\"temple.tiles\",\"bank\":" + bank + ",\"tileId\":" + tile_id + ",\"peerBank\":" + peer_bank + ",\"peerTileId\":" + peer_tile_id + ",\"tileState\":\"" + state + "\",\"source\":\"gsc\"}";
	fs_writeline(handle, line);
	fs_fclose(handle);
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
