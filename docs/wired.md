# Wired

Every wired box the client can configure, with the client dialog code the server answers with
(TriggerConfCodes, ActionTypeCodes, ConditionCodes, SelectorCodes, AddonCodes, VariableCodes in the
client) and the hotel feature it needs beyond the room itself. Descriptions are Habbo's own (furnidata).

## Triggers

| Box | Code | What it does | Needs |
|---|---|---|---|
| `wf_trg_says_something` | 0 | Effects are triggered when a user says a certain word or phrase. |  |
| `wf_trg_walks_on_furni` | 1 | Actions are triggered when a user steps or sits on a Furni. |  |
| `wf_trg_walks_off_furni` | 2 | Effects are triggered when a user steps off a Furni. |  |
| `wf_trg_at_given_time` | 3 | Effects are triggered at a given time after the timer is reset. |  |
| `wf_trg_at_time_long` | 3 | wf_trg_at_time_long desc |  |
| `wf_trg_state_changed` | 4 | Effects are triggered when the state of a Furni is changed by double clicking it. |  |
| `wf_trg_periodically` | 6 | Effects are triggered every x seconds. |  |
| `wf_trg_enter_room` | 7 | Effects are triggered when a user enters the room. |  |
| `wf_trg_game_starts` | 8 | Effects are triggered when a game starts in the room | teams & games |
| `wf_trg_game_ends` | 9 | Effects are triggered when a game ends in the room. | teams & games |
| `wf_trg_score_achieved` | 10 | Effects are triggered when a team has achieved x points in a game. | teams & games |
| `wf_trg_collision` | 11 | Effects are triggered when there a user stuff collision detected by one of the move effects (move furni to direction, mo |  |
| `wf_trg_period_long` | 12 | Longer games! Effects are triggered each time a given time has elapsed. Like Trigger Periodically but longer! |  |
| `wf_trg_bot_reached_stf` | 13 | Effects are triggered when the selected Bot reaches one of the selected Furni | bots |
| `wf_trg_bot_reached_avtr` | 14 | Effects are triggered when the selected Bot reaches a user | bots |
| `wf_trg_clock_counter` | 15 | wf_trg_clock_counter desc | counters |
| `wf_trg_user_performs_action` | 16 | wf_trg_user_performs_action desc | user actions |
| `wf_trg_recv_signal` | 17 | wf_trg_recv_signal desc | signals |
| `wf_trg_click_furni` | 18 | wf_trg_click_furni desc | click events |
| `wf_trg_period_short` | 19 | wf_trg_period_short desc |  |
| `wf_trg_stuff_state` | 20 | wf_trg_stuff_state desc |  |
| `wf_trg_click_tile` | 21 | wf_trg_click_tile desc | click events |
| `wf_trg_var_changed` | 22 | wf_trg_var_changed desc | variables |
| `wf_trg_leave_room` | 23 | wf_trg_leave_room desc |  |
| `wf_trg_click_user` | 24 | wf_trg_click_user desc | click events |
| `wf_trg_transaction_complete` | 25 | wf_trg_transaction_complete desc | chests, currency & contracts |
| `wf_trg_transaction_fail` | 26 | wf_trg_transaction_fail desc | chests, currency & contracts |

## Effects

| Box | Code | What it does | Needs |
|---|---|---|---|
| `wf_act_toggle_state` | 0 | This Effect changes the state of the chosen Furni as if the Furni has just been double clicked. |  |
| `wf_act_reset_timers` | 1 | This Effect resets triggers that use timers. |  |
| `wf_act_match_to_sshot` | 3 | This Effect returns the selected Furni to its set conditions. |  |
| `wf_act_move_rotate` | 4 | This effect moves and rotates the selected Furni |  |
| `wf_act_give_score` | 6 | This Effect gives the set amount of points to the team that triggers the Effect. You can also choose how many times in a | teams & games |
| `wf_act_show_message` | 7 | This effect shows a secret message only to the triggering users. |  |
| `wf_act_teleport_to` | 8 | This Effect teleports the triggering user to a Furni. If you select more than one Furni, the user is randomly teleported |  |
| `wf_act_join_team` | 9 | This Effect makes the triggering user join a team of your choosing. | teams & games |
| `wf_act_leave_team` | 10 | This Effect makes the triggering user leave their current team. | teams & games |
| `wf_act_chase` | 11 | This Effect moves the Furnis towards nearest user in the same row or column. If there is a user next to this furni, WIRE |  |
| `wf_act_flee` | 12 | This Effect moves the selected Furnis away from the nearest user in the same row or column. If there is user next to thi |  |
| `wf_act_move_to_dir` | 13 | This Effect changes the Furnis direction. When the move is blocked, Furni will change direction based on setting. If the |  |
| `wf_act_give_score_tm` | 14 | This Effect gives the set amount of points to a team of your choosing. You can also choose how many times in a game the  | teams & games |
| `wf_act_toggle_to_rnd` | 15 | This Effect changes the state of the chosen furni randomly. |  |
| `wf_act_move_furni_to` | 16 | This Effect moves a Furni defined by the Trigger to the vicinity of one of the selected Furnis. |  |
| `wf_act_give_reward` | 17 | wf_act_give_reward desc | chests, currency & contracts |
| `wf_act_call_stacks` | 18 | This Wired allows you to select 5 wired stacks to execute from a single trigger, regardless of existing conditions or tr |  |
| `wf_act_kick_user` | 19 | This effect kicks a user depending on the triggering criteria | kicking |
| `wf_act_mute_triggerer` | 20 | This Effect mutes the triggering user. | room mutes |
| `wf_act_bot_teleport` | 21 | This effect teleports the selected Bot to one of the selected Furni | bots |
| `wf_act_bot_move` | 22 | This effect makes the selected Bot move to one of the selected Furni | bots |
| `wf_act_bot_talk` | 23 | This effect makes the selected Bot talk to all users | bots |
| `wf_act_bot_give_handitem` | 24 | This effect makes the selected Bot give the selected handitem | bots |
| `wf_act_bot_follow_avatar` | 25 | This effect makes the selected Bot follow the triggering users | bots |
| `wf_act_bot_clothes` | 26 | This effect changes the clothes of the selected Bot | bots |
| `wf_act_bot_talk_to_avatar` | 27 | This effect makes the selected Bot talk or whisper to the triggering user | bots |
| `wf_act_control_clock` | 28 | wf_act_control_clock desc | counters |
| `wf_act_set_altitude` | 29 | wf_act_set_altitude desc |  |
| `wf_act_send_signal` | 30 | wf_act_send_signal desc | signals |
| `wf_act_freeze` | 31 | wf_act_freeze desc | avatar effects |
| `wf_act_unfreeze` | 32 | wf_act_unfreeze desc | avatar effects |
| `wf_act_rel_mov` | 33 | wf_act_rel_mov desc |  |
| `wf_act_furni_to_furni` | 34 | wf_act_furni_to_furni desc |  |
| `wf_act_furni_to_user` | 35 | wf_act_furni_to_user desc |  |
| `wf_act_neg_call_stacks` | 36 | wf_act_neg_call_stacks desc |  |
| `wf_act_neg_send_signal` | 37 | wf_act_neg_send_signal desc | signals |
| `wf_act_adjust_clock` | 38 | wf_act_adjust_clock desc | counters |
| `wf_act_give_var` | 39 | wf_act_give_var desc | variables |
| `wf_act_remove_var` | 40 | wf_act_remove_var desc | variables |
| `wf_act_change_var_val` | 41 | wf_act_change_var_val desc | variables |
| `wf_act_move_rotate_user` | 42 | wf_act_move_rotate_user desc |  |
| `wf_act_user_to_furni` | 43 | wf_act_user_to_furni desc |  |
| `wf_act_teleport_to_room` | 44 | wf_act_teleport_to_room desc | room forwarding |
| `wf_act_give_currency` | 45 | wf_act_give_currency desc | chests, currency & contracts |
| `wf_act_give_furni` | 46 | wf_act_give_furni desc | chests, currency & contracts |
| `wf_act_init_transaction` | 47 | wf_act_init_transaction desc | chests, currency & contracts |
| `wf_act_cancel_transaction` | 48 | wf_act_cancel_transaction desc | chests, currency & contracts |
| `wf_act_log` | 49 | wf_act_log desc | room logs |
| `wf_act_neg_log` | 50 | wf_act_neg_log desc | room logs |

## Conditions

| Box | Code | What it does | Needs |
|---|---|---|---|
| `wf_cnd_match_snapshot` | 0 | Triggering is allowed if the states and positions of the selected Furni match. |  |
| `wf_cnd_furnis_hv_avtrs` | 1 | Triggering is allowed if all the selected Furni have users on them. |  |
| `wf_cnd_trggrer_on_frn` | 2 | Triggering is allowed if the triggering user is on one of the selected Furni. |  |
| `wf_cnd_time_more_than` | 3 | Triggering is allowed if more than x seconds have elapsed since timer reset. |  |
| `wf_cnd_time_less_than` | 4 | Triggering is allowed if less than x seconds have elapsed since timer reset. |  |
| `wf_cnd_user_count_in` | 5 | Triggering is allowed if the user count in room is within the defined limits |  |
| `wf_cnd_actor_in_team` | 6 | Triggering is allowed if the triggerering user belongs to the selected team. | teams & games |
| `wf_cnd_has_furni_on` | 7 | Triggering is allowed if one of the chosen Furni has another Furni on it. The Furni on top can then be acted upon with E |  |
| `wf_cnd_stuff_is` | 8 | Triggering is allowed if the Furni defined by Trigger is one of the chosen Furnis or is of the same type |  |
| `wf_cnd_actor_in_group` | 10 | Condition passes when this is a group room and the avatar is a member of that group | groups |
| `wf_cnd_wearing_badge` | 11 | Wired Condition: Wearing Badge | badges |
| `wf_cnd_wearing_effect` | 12 | Wired Condition: Wearing Avatar Effect | avatar effects |
| `wf_cnd_not_match_snap` | 13 | Triggering is allowed if the states of the picked furnis DOESN'T match. |  |
| `wf_cnd_not_hv_avtrs` | 14 | Triggering is allowed if the picked furnis have NO avatars on them. |  |
| `wf_cnd_not_trggrer_on` | 15 | Triggering is allowed if the triggerer is NOT on one of the picked furnis. |  |
| `wf_cnd_not_user_count` | 16 | Triggering is allowed if the user count in room is NOT within the defined limits |  |
| `wf_cnd_not_in_team` | 17 | Condition passes when avatar is NOT a member of the pre-set team | teams & games |
| `wf_cnd_not_furni_on` | 18 | Triggering is allowed if one of the chosen Furni has NO other Furni on it. |  |
| `wf_cnd_not_stuff_is` | 19 | Triggering is allowed if the Furni defined by Trigger is NOT one of the chosen Furnis or is NOT of the same type |  |
| `wf_cnd_not_in_group` | 21 | Condition passes when this is a group room and the avatar is NOT a member of that group | groups |
| `wf_cnd_not_wearing_b` | 22 | wf_cnd_not_wearing_b desc | badges |
| `wf_cnd_not_wearing_fx` | 23 | wf_cnd_not_wearing_fx desc | avatar effects |
| `wf_cnd_date_rng_active` | 24 | Allows you to set a specific date/time range for the wired effect to work. |  |
| `wf_cnd_has_handitem` | 25 | Triggering is allowed if the user is holding the selected handitem | hand items |
| `wf_cnd_triggerer_match` | 26 | wf_cnd_triggerer_match desc |  |
| `wf_cnd_not_triggerer_match` | 27 | wf_cnd_not_triggerer_match desc |  |
| `wf_cnd_match_time` | 28 | wf_cnd_match_time desc |  |
| `wf_cnd_match_date` | 29 | wf_cnd_match_date desc |  |
| `wf_cnd_not_has_handitem` | 30 | wf_cnd_not_has_handitem desc | hand items |
| `wf_cnd_team_has_rank` | 31 | wf_cnd_team_has_rank desc | teams & games |
| `wf_cnd_user_performs_action` | 32 | wf_cnd_user_performs_action desc | user actions |
| `wf_cnd_not_user_performs_action` | 33 | wf_cnd_not_user_performs_action desc | user actions |
| `wf_cnd_team_has_score` | 34 | wf_cnd_team_has_score desc | teams & games |
| `wf_cnd_counter_time_matches` | 35 | wf_cnd_counter_time_matches desc | counters |
| `wf_cnd_has_altitude` | 36 | wf_cnd_has_altitude desc |  |
| `wf_cnd_actor_dir` | 37 | wf_cnd_actor_dir desc |  |
| `wf_cnd_slc_quantity` | 38 | wf_cnd_slc_quantity desc |  |
| `wf_cnd_valid_moves` | 39 | wf_cnd_valid_moves desc |  |
| `wf_cnd_has_var` | 40 | wf_cnd_has_var desc | variables |
| `wf_cnd_neg_has_var` | 41 | wf_cnd_neg_has_var desc | variables |
| `wf_cnd_var_val_match` | 42 | wf_cnd_var_val_match desc | variables |
| `wf_cnd_var_age_match` | 43 | wf_cnd_var_age_match desc | variables |
| `wf_cnd_chest_has_items` | 45 | wf_cnd_chest_has_items desc | chests, currency & contracts |
| `wf_cnd_chest_has_item_type` | 46 | wf_cnd_chest_has_item_type desc | chests, currency & contracts |

## Selectors

| Box | Code | What it does | Needs |
|---|---|---|---|
| `wf_slc_furni_bytype` | 0 | wf_slc_furni_bytype desc |  |
| `wf_slc_furni_picks` | 1 | wf_slc_furni_picks desc |  |
| `wf_slc_users_bytype` | 2 | wf_slc_users_bytype desc |  |
| `wf_slc_users_team` | 3 | wf_slc_users_team desc | teams & games |
| `wf_slc_furni_onfurni` | 4 | wf_slc_furni_onfurni desc |  |
| `wf_slc_furni_signal` | 5 | wf_slc_furni_signal desc | signals |
| `wf_slc_furni_neighborhood` | 6 | wf_slc_furni_neighborhood desc |  |
| `wf_slc_furni_area` | 7 | wf_slc_furni_area desc |  |
| `wf_slc_users_onfurni` | 8 | wf_slc_users_onfurni desc |  |
| `wf_slc_users_byaction` | 9 | wf_slc_users_byaction desc | user actions |
| `wf_slc_users_signal` | 10 | wf_slc_users_signal desc | signals |
| `wf_slc_users_byname` | 11 | wf_slc_users_byname desc |  |
| `wf_slc_users_neighborhood` | 12 | wf_slc_users_neighborhood desc |  |
| `wf_slc_users_area` | 13 | wf_slc_users_area desc |  |
| `wf_slc_users_handitem` | 14 | wf_slc_users_handitem desc | hand items |
| `wf_slc_users_group` | 15 | wf_slc_users_group desc | groups |
| `wf_slc_furni_altitude` | 16 | wf_slc_furni_altitude desc |  |
| `wf_slc_furni_with_var` | 17 | wf_slc_furni_with_var desc | variables |
| `wf_slc_users_with_var` | 18 | wf_slc_users_with_var desc | variables |
| `wf_slc_remote` | 19 | wf_slc_remote desc | signals |

## Add-ons

| Box | Code | What it does | Needs |
|---|---|---|---|
| `wf_xtra_or_eval` | 0 | wf_xtra_or_eval desc |  |
| `wf_xtra_random` | 1 | Only one random Effect in this stack is triggered at a time. |  |
| `wf_xtra_unseen` | 2 | Only one previously unused Effect in this stack is triggered at a time. |  |
| `wf_xtra_execution_limit` | 5 | wf_xtra_execution_limit desc |  |
| `wf_xtra_mov_no_animation` | 6 | wf_xtra_mov_no_animation desc |  |
| `wf_xtra_mov_physics` | 7 | wf_xtra_mov_physics desc |  |
| `wf_xtra_mov_carry_users` | 8 | wf_xtra_mov_carry_users desc |  |
| `wf_xtra_anim_time` | 9 | wf_xtra_anim_time desc |  |
| `wf_xtra_filter_furni` | 10 | wf_xtra_filter_furni desc |  |
| `wf_xtra_filter_users` | 11 | wf_xtra_filter_users desc |  |
| `wf_xtra_filter_furni_by_var` | 12 | wf_xtra_filter_furni_by_var desc | variables |
| `wf_xtra_filter_users_by_var` | 13 | wf_xtra_filter_users_by_var desc | variables |
| `wf_xtra_text_output_username` | 14 | wf_xtra_text_output_username desc |  |
| `wf_xtra_text_output_variable` | 15 | wf_xtra_text_output_variable desc | variables |
| `wf_xtra_text_input_variable` | 16 | wf_xtra_text_input_variable desc | variables |
| `wf_xtra_exec_in_order` | 17 | wf_xtra_exec_in_order desc |  |
| `wf_xtra_scan_chest_furni_by_type` | 18 | wf_xtra_scan_chest_furni_by_type desc | chests, currency & contracts |
| `wf_xtra_text_output_furni_name` | 19 | wf_xtra_text_output_furni_name desc |  |
| `wf_xtra_custom_contract` | 20 | wf_xtra_custom_contract desc | chests, currency & contracts |
| `wf_xtra_var_text_connector` | 1000 | wf_xtra_var_text_connector desc | variables |
| `wf_xtra_var_lvlup_system` | 1001 | wf_xtra_var_lvlup_system desc | variables |
| `wf_xtra_var_time_util` | 1002 | wf_xtra_var_time_util desc | variables |

## Variables

| Box | Code | What it does | Needs |
|---|---|---|---|
| `wf_var_furni` | 0 | wf_var_furni desc |  |
| `wf_var_user` | 1 | wf_var_user desc |  |
| `wf_var_room` | 2 | wf_var_room desc |  |
| `wf_var_context` | 3 | wf_var_context desc |  |
| `wf_var_reference` | 4 | wf_var_reference desc |  |
| `wf_var_quest` | 5 | wf_var_quest desc | quests |
| `wf_var_quest_chain` | 6 | wf_var_quest_chain desc | quests |
| `wf_var_echo` | 7 | wf_var_echo desc |  |

## Not configurable on the March 2026 client

These boxes exist in furnidata, but the March 2026 client has no dialog for them. The September 2026 client and later
have the dialogs, and the server implements them:

`wf_act_click_conf`, `wf_act_move_furni_as_group`, `wf_act_place_furni`, `wf_act_remove_furni`, `wf_xtra_mov_curve`, `wf_xtra_rotate_to_dir`, `wf_xtra_varfx_boss`, `wf_xtra_varfx_hp`, `wf_xtra_varfx_levelling`, `wf_xtra_varfx_number`, `wf_xtra_varfx_prog`, `wf_xtra_varfx_status`

## Not implemented

`wf_xtra_var_web_api` (add-on code 2002): the September 2026 client and later have its dialog (API keys, the API
documentation link), but the server does not implement Habbo's HTTP API for room variables.
