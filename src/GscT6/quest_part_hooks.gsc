// T6 stock callback adapters. Included by each map-scoped observer.
#include maps\mp\zombies\_zm_buildables;
#include maps\mp\zombies\_zm_craftables;


ee_tracker_install_quest_part_hooks()
{
    replaceFunc(maps/mp/zombies/_zm_buildables::track_buildable_piece_pickedup, ::ee_tracker_track_buildable_piece_pickedup);
    replaceFunc(maps/mp/zombies/_zm_buildables::player_drop_piece, ::ee_tracker_buildable_player_drop_piece);
    replaceFunc(maps/mp/zombies/_zm_buildables::player_drop_piece_on_death, ::ee_tracker_buildable_player_drop_piece_on_death);
    replaceFunc(maps/mp/zombies/_zm_buildables::track_buildable_pieces_built, ::ee_tracker_track_buildable_pieces_built);
    replaceFunc(maps/mp/zombies/_zm_craftables::track_craftable_piece_pickedup, ::ee_tracker_track_craftable_piece_pickedup);
    replaceFunc(maps/mp/zombies/_zm_craftables::player_drop_piece, ::ee_tracker_craftable_player_drop_piece);
    replaceFunc(maps/mp/zombies/_zm_craftables::player_drop_piece_on_death, ::ee_tracker_craftable_player_drop_piece_on_death);
    replaceFunc(maps/mp/zombies/_zm_craftables::onplayerlaststand, ::ee_tracker_craftable_onplayerlaststand);
    replaceFunc(maps/mp/zombies/_zm_craftables::track_craftable_pieces_crafted, ::ee_tracker_track_craftable_pieces_crafted);
}

ee_tracker_emit_part_event(part_id, label, state, player)
{
    if (!isdefined(level.ee_tracker_part_session))
        return;
    slot = -1;
    if (isdefined(player))
    {
        players = get_players();
        for (i = 0; i < players.size; i++)
        {
            if (players[i] == player)
            {
                slot = i;
                break;
            }
        }
    }
    slot_json = "";
    origin_json = "";
    if (slot >= 0)
        slot_json = ",\"playerSlot\":" + slot;
    if (isdefined(player) && isdefined(player.origin))
        origin_json = ",\"partOrigin\":\"player current location approximate\"";
    printf("[EETrackerT6] {\"schemaVersion\":1,\"type\":\"quest_part\",\"game\":\"bo2\",\"map\":\"" + level.ee_tracker_part_map + "\",\"sessionId\":\"" + level.ee_tracker_part_session + "\",\"partId\":\"" + part_id + "\",\"partLabel\":\"" + label + "\",\"partState\":\"" + state + "\"" + slot_json + origin_json + ",\"source\":\"stock_callback\"}\n");
}

ee_tracker_track_buildable_piece_pickedup(piece)
{
    if (!isdefined(piece) || !isdefined(piece.buildablename))
        return;
    self add_map_buildable_stat(piece.buildablename, "pieces_pickedup", 1);
    if (isdefined(piece.modelname))
        ee_tracker_emit_part_event(piece.buildablename + ":" + piece.modelname, piece.buildablename + " · " + piece.modelname, "carried", self);
    buildable_struct = level.zombie_include_buildables[piece.buildablename];
    if (isdefined(buildable_struct.piece_vox_id))
    {
        if (isdefined(self.a_buildable_piece_pickedup_vox_cooldown) && isinarray(self.a_buildable_piece_pickedup_vox_cooldown, buildable_struct.piece_vox_id))
            return;
        self thread do_player_general_vox("general", buildable_struct.piece_vox_id + "_pickup");
        if (isdefined(buildable_struct.piece_vox_timer))
            self thread buildable_piece_pickedup_vox_cooldown(buildable_struct.piece_vox_id, buildable_struct.piece_vox_timer);
    }
    else
        self thread do_player_general_vox("general", "build_pickup");
}

ee_tracker_buildable_player_drop_piece(piece, slot)
{
    if (!isdefined(slot))
        slot = 0;
    if (!isdefined(piece))
        piece = self player_get_buildable_piece(slot);
    else
        slot = piece.buildable_slot;
    if (isdefined(piece))
    {
        origin = self.origin;
        origintrace = groundtrace(origin + vectorscale((0, 0, 1), 5.0), origin - vectorscale((0, 0, 1), 999999.0), 0, self);
        if (isdefined(origintrace["entity"]))
            origintrace = groundtrace(origintrace["entity"].origin, origintrace["entity"].origin - vectorscale((0, 0, 1), 999999.0), 0, origintrace["entity"]);
        if (isdefined(origintrace["position"]))
            origin = origintrace["position"];
        piece.damage = 0;
        piece piece_spawn_at(origin, self.angles);
        if (isplayer(self))
            self clear_buildable_clientfield(slot);
        if (isdefined(piece.ondrop))
            piece [[ piece.ondrop ]](self);
        ee_tracker_emit_part_event(piece.buildablename + ":" + piece.modelname, piece.buildablename + " · " + piece.modelname, "dropped", self);
    }
    self player_set_buildable_piece(undefined, slot);
    self notify("piece_released" + slot);
}

ee_tracker_buildable_player_drop_piece_on_death(slot)
{
    self notify("piece_released" + slot);
    self endon("piece_released" + slot);
    origin = self.origin;
    angles = self.angles;
    piece = self player_get_buildable_piece(slot);
    self waittill("death_or_disconnect");
    if (isdefined(piece))
    {
        piece piece_spawn_at(origin, angles);
        if (isdefined(self))
            self clear_buildable_clientfield(slot);
        if (isdefined(piece.ondrop))
            piece [[ piece.ondrop ]](self);
        ee_tracker_emit_part_event(piece.buildablename + ":" + piece.modelname, piece.buildablename + " · " + piece.modelname, "dropped", self);
    }
}

ee_tracker_track_buildable_pieces_built(buildable)
{
    if (!isdefined(buildable) || !isdefined(buildable.buildable_name))
        return;
    bname = buildable.buildable_name;
    if (isdefined(buildable.stat_name))
        bname = buildable.stat_name;
    self add_map_buildable_stat(bname, "pieces_built", 1);
    if (!buildable buildable_all_built())
    {
        if (isdefined(level.zombie_include_buildables[buildable.buildable_name]) && isdefined(level.zombie_include_buildables[buildable.buildable_name].snd_build_add_vo_override))
            self thread [[ level.zombie_include_buildables[buildable.buildable_name].snd_build_add_vo_override ]]();
        else
            self thread do_player_general_vox("general", "build_add");
    }
}

ee_tracker_track_craftable_piece_pickedup(piece)
{
    if (!isdefined(piece) || !isdefined(piece.craftablename))
        return;
    self add_map_craftable_stat(piece.craftablename, "pieces_pickedup", 1);
    if (isdefined(piece.piecename))
        ee_tracker_emit_part_event(piece.craftablename + ":" + piece.piecename, piece.craftablename + " · " + piece.piecename, "carried", self);
    if (isdefined(piece.piecestub.vox_id))
    {
        if (isdefined(piece.piecestub.b_one_time_vo) && piece.piecestub.b_one_time_vo)
        {
            if (!isdefined(self.a_one_time_piece_pickup_vo))
                self.a_one_time_piece_pickup_vo = [];
            if (isdefined(self.dontspeak) && self.dontspeak)
                return;
            if (isinarray(self.a_one_time_piece_pickup_vo, piece.piecestub.vox_id))
                return;
            self.a_one_time_piece_pickup_vo[self.a_one_time_piece_pickup_vo.size] = piece.piecestub.vox_id;
        }
        self thread do_player_general_vox("general", piece.piecestub.vox_id + "_pickup");
    }
    else
        self thread do_player_general_vox("general", "build_pickup");
}

ee_tracker_craftable_player_drop_piece(piece)
{
    if (!isdefined(piece))
        piece = self.current_craftable_piece;
    if (isdefined(piece))
    {
        piece.damage = 0;
        piece piece_spawn_at(self.origin, self.angles);
        self setclientfieldtoplayer("craftable", 0);
        if (isdefined(piece.ondrop))
            piece [[ piece.ondrop ]](self);
        ee_tracker_emit_part_event(piece.craftablename + ":" + piece.piecename, piece.craftablename + " · " + piece.piecename, "dropped", self);
    }
    self.current_craftable_piece = undefined;
    self notify("craftable_piece_released");
}

ee_tracker_craftable_player_drop_piece_on_death()
{
    self notify("craftable_piece_released");
    self endon("craftable_piece_released");
    self thread player_drop_piece_on_downed();
    origin = self.origin;
    angles = self.angles;
    piece = self.current_craftable_piece;
    self waittill("disconnect");
    if (isdefined(piece))
    {
        piece piece_spawn_at(origin, angles);
        if (isdefined(self))
            self setclientfieldtoplayer("craftable", 0);
        ee_tracker_emit_part_event(piece.craftablename + ":" + piece.piecename, piece.craftablename + " · " + piece.piecename, "dropped", self);
    }
}

ee_tracker_craftable_onplayerlaststand()
{
    piece = self.current_craftable_piece;
    if (isdefined(piece))
    {
        return_to_start_pos = 0;
        if (isdefined(level.safe_place_for_craftable_piece) && !self [[ level.safe_place_for_craftable_piece ]](piece))
            return_to_start_pos = 1;
        if (return_to_start_pos)
            piece piece_spawn_at();
        else
            piece piece_spawn_at(self.origin + vectorscale((1, 1, 0), 5.0), self.angles);
        if (isdefined(piece.ondrop))
            piece [[ piece.ondrop ]](self);
        self setclientfieldtoplayer("craftable", 0);
        ee_tracker_emit_part_event(piece.craftablename + ":" + piece.piecename, piece.craftablename + " · " + piece.piecename, "dropped", self);
    }
}

ee_tracker_track_craftable_pieces_crafted(craftable)
{
    if (!isdefined(craftable) || !isdefined(craftable.craftable_name))
        return;
    bname = craftable.craftable_name;
    if (isdefined(craftable.stat_name))
        bname = craftable.stat_name;
    self add_map_craftable_stat(bname, "pieces_built", 1);
    if (!craftable craftable_all_crafted())
        self thread do_player_general_vox("general", "build_add");
}
