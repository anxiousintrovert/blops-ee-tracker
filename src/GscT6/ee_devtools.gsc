// Opt-in private-match development shortcuts. Does not emit quest telemetry.
// Console dvars: ee_dev_points <0..50000>, ee_dev_weapon <weapon-name>.
#include common_scripts\utility;
#include maps\mp\_utility;

main()
{
    printf("[EETrackerT6Dev] Development shortcuts loaded.\n");
}

init()
{
    setdvar("ee_dev_points", "0");
    setdvar("ee_dev_weapon", "none");
    level thread ee_devtools_monitor();
}

ee_devtools_monitor()
{
    level endon("end_game");
    while (true)
    {
        points = int(getdvar( #"ee_dev_points" ));
        if (points > 0)
        {
            points = int(min(points, 50000));
            players = get_players();
            foreach (player in players)
            {
                player.score_total += points;
                player.score = player.score_total;
                player iprintlnbold("EE dev: added " + points + " points");
            }
            setdvar("ee_dev_points", "0");
        }

        weapon = getdvar( #"ee_dev_weapon" );
        if (isdefined(weapon) && weapon != "" && weapon != "none")
        {
            players = get_players();
            foreach (player in players)
            {
                player giveweapon(weapon);
                player switchtoweapon(weapon);
                player iprintlnbold("EE dev: gave " + weapon);
            }
            setdvar("ee_dev_weapon", "none");
        }
        wait 1;
    }
}
