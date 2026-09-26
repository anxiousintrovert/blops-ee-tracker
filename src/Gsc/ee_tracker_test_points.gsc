// Opt-in points helper for the ten round-based Black Ops 1 Zombies maps.
// Console: ee_tracker_test_points 1

#include common_scripts\utility;
#include maps\_utility;

init()
{
	setDvar( "ee_tracker_test_points", "0" );
	level thread ee_points_poll();
}

ee_points_poll()
{
	level endon( "end_game" );
	for (;;)
	{
		wait 0.25;
		if ( getDvarInt( "ee_tracker_test_points" ) != 1 )
			continue;

		setDvar( "ee_tracker_test_points", "0" );
		players = getPlayers();
		for ( i = 0; i < players.size; i++ )
		{
			players[i] maps\_zombiemode_score::add_to_player_score( 100000 );
			players[i] iPrintLnBold( "EETracker: +100000 points" );
		}
	}
}
