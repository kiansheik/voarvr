using System.IO;
namespace VoarVR.Telemetry
{
    // v3 uses float32 for native float/int signals; time and logical coordinates retain float64.
    public struct TelemetrySample
    {
        public double timestamp;
        public double frame;
        public double simulation_time;
        public double dt;
        public double render_dt;
        public double unscaled_dt;
        public double raw_left_tracked;
        public double raw_left_position_x;
        public double raw_left_position_y;
        public double raw_left_position_z;
        public double raw_left_velocity_x;
        public double raw_left_velocity_y;
        public double raw_left_velocity_z;
        public double raw_left_rotation_x;
        public double raw_left_rotation_y;
        public double raw_left_rotation_z;
        public double raw_left_rotation_w;
        public double raw_right_tracked;
        public double raw_right_position_x;
        public double raw_right_position_y;
        public double raw_right_position_z;
        public double raw_right_velocity_x;
        public double raw_right_velocity_y;
        public double raw_right_velocity_z;
        public double raw_right_rotation_x;
        public double raw_right_rotation_y;
        public double raw_right_rotation_z;
        public double raw_right_rotation_w;
        public double raw_head_tracked;
        public double raw_head_position_x;
        public double raw_head_position_y;
        public double raw_head_position_z;
        public double raw_head_rotation_x;
        public double raw_head_rotation_y;
        public double raw_head_rotation_z;
        public double raw_head_rotation_w;
        public double raw_look_x;
        public double raw_look_y;
        public double raw_look_z;
        public double raw_body_rotation_x;
        public double raw_body_rotation_y;
        public double raw_body_rotation_z;
        public double raw_body_rotation_w;
        public double raw_body_tracked;
        public double raw_bank;
        public double raw_tuck;
        public double raw_flare;
        public double raw_ground_move_x;
        public double raw_ground_move_y;
        public double raw_reset;
        public double raw_recalibrate;
        public double raw_characterselect;
        public double raw_pause;
        public double raw_viewtoggle;
        public double raw_windmode;
        public double raw_hudtoggle;
        public double raw_marker;
        public double raw_buttons;
        public double mapped_left_tracked;
        public double mapped_left_position_x;
        public double mapped_left_position_y;
        public double mapped_left_position_z;
        public double mapped_left_velocity_x;
        public double mapped_left_velocity_y;
        public double mapped_left_velocity_z;
        public double mapped_left_rotation_x;
        public double mapped_left_rotation_y;
        public double mapped_left_rotation_z;
        public double mapped_left_rotation_w;
        public double mapped_right_tracked;
        public double mapped_right_position_x;
        public double mapped_right_position_y;
        public double mapped_right_position_z;
        public double mapped_right_velocity_x;
        public double mapped_right_velocity_y;
        public double mapped_right_velocity_z;
        public double mapped_right_rotation_x;
        public double mapped_right_rotation_y;
        public double mapped_right_rotation_z;
        public double mapped_right_rotation_w;
        public double mapped_head_tracked;
        public double mapped_head_position_x;
        public double mapped_head_position_y;
        public double mapped_head_position_z;
        public double mapped_head_rotation_x;
        public double mapped_head_rotation_y;
        public double mapped_head_rotation_z;
        public double mapped_head_rotation_w;
        public double mapped_look_x;
        public double mapped_look_y;
        public double mapped_look_z;
        public double mapped_body_rotation_x;
        public double mapped_body_rotation_y;
        public double mapped_body_rotation_z;
        public double mapped_body_rotation_w;
        public double mapped_body_tracked;
        public double mapped_bank;
        public double mapped_tuck;
        public double mapped_flare;
        public double mapped_ground_move_x;
        public double mapped_ground_move_y;
        public double mapped_reset;
        public double mapped_recalibrate;
        public double mapped_characterselect;
        public double mapped_pause;
        public double mapped_viewtoggle;
        public double mapped_windmode;
        public double mapped_hudtoggle;
        public double mapped_marker;
        public double mapped_buttons;
        public double native_left_velocity_available;
        public double native_left_velocity_x;
        public double native_left_velocity_y;
        public double native_left_velocity_z;
        public double native_left_angular_available;
        public double native_left_angular_x;
        public double native_left_angular_y;
        public double native_left_angular_z;
        public double native_right_velocity_available;
        public double native_right_velocity_x;
        public double native_right_velocity_y;
        public double native_right_velocity_z;
        public double native_right_angular_available;
        public double native_right_angular_x;
        public double native_right_angular_y;
        public double native_right_angular_z;
        public double calibrated;
        public double calibration_sequence;
        public double input_wings_enabled;
        public double human_span;
        public double bird_half_span;
        public double motion_scale;
        public double calibration_head_origin_x;
        public double calibration_head_origin_y;
        public double calibration_head_origin_z;
        public double calibration_heading_x;
        public double calibration_heading_y;
        public double calibration_heading_z;
        public double calibration_heading_w;
        public double calibration_pitch_x;
        public double calibration_pitch_y;
        public double calibration_pitch_z;
        public double calibration_pitch_w;
        public double calibration_left_neutral_x;
        public double calibration_left_neutral_y;
        public double calibration_left_neutral_z;
        public double calibration_left_rotation_x;
        public double calibration_left_rotation_y;
        public double calibration_left_rotation_z;
        public double calibration_left_rotation_w;
        public double target_left_x;
        public double target_left_y;
        public double target_left_z;
        public double reach_left;
        public double joint_left_upper_x;
        public double joint_left_upper_y;
        public double joint_left_upper_z;
        public double joint_left_upper_w;
        public double joint_left_forearm_x;
        public double joint_left_forearm_y;
        public double joint_left_forearm_z;
        public double joint_left_forearm_w;
        public double joint_left_hand_x;
        public double joint_left_hand_y;
        public double joint_left_hand_z;
        public double joint_left_hand_w;
        public double calibration_right_neutral_x;
        public double calibration_right_neutral_y;
        public double calibration_right_neutral_z;
        public double calibration_right_rotation_x;
        public double calibration_right_rotation_y;
        public double calibration_right_rotation_z;
        public double calibration_right_rotation_w;
        public double target_right_x;
        public double target_right_y;
        public double target_right_z;
        public double reach_right;
        public double joint_right_upper_x;
        public double joint_right_upper_y;
        public double joint_right_upper_z;
        public double joint_right_upper_w;
        public double joint_right_forearm_x;
        public double joint_right_forearm_y;
        public double joint_right_forearm_z;
        public double joint_right_forearm_w;
        public double joint_right_hand_x;
        public double joint_right_hand_y;
        public double joint_right_hand_z;
        public double joint_right_hand_w;
        public double position_x;
        public double position_y;
        public double position_z;
        public double velocity_x;
        public double velocity_y;
        public double velocity_z;
        public double rotation_x;
        public double rotation_y;
        public double rotation_z;
        public double rotation_w;
        public double logical_x;
        public double logical_y;
        public double logical_z;
        public double wind_x;
        public double wind_y;
        public double wind_z;
        public double lift_x;
        public double lift_y;
        public double lift_z;
        public double drag_x;
        public double drag_y;
        public double drag_z;
        public double stroke_x;
        public double stroke_y;
        public double stroke_z;
        public double phase;
        public double airspeed;
        public double groundspeed;
        public double aoa;
        public double head_pitch;
        public double brake;
        public double energy;
        public double landing_approach;
        public double collision_count;
        public double landing_count;
        public double impact_speed;
        public double streaming_blocked;
        public double chunks;
        public double chunk_queue;
        public double chunk_x;
        public double chunk_z;
        public double generation_ms;
        public double view;
        public double weather;
        public double marker;
        public double dropped;
        public double capture_cpu_ms;
        public double stalled;
        public double takeoff_count;
        public double cpu_ms;
        public double gpu_ms;
        public double timing_available;
        public double refresh_hz;
        public double refresh_available;
        public double clearance;
        public double clearance_available;
        public double avian_leftfan;
        public double avian_rightfan;
        public double avian_leftfold;
        public double avian_rightfold;
        public double avian_alula;
        public double avian_tailspread;
        public double avian_tailpitch;
        public double avian_tailyaw;
        public double raw_control_mode_pressed;
        public double control_mode;
        public double angular_velocity_x;
        public double angular_velocity_y;
        public double angular_velocity_z;
        public double control_torque_x;
        public double control_torque_y;
        public double control_torque_z;
        public double trick_count;
        public double trick_score;
        public double last_trick;
        public double mission_id;
        public double objective_stage;
        public double objective_progress;
        public double objective_status;
        public double mission_score;
        public double thermal_gradient_x;
        public double thermal_gradient_z;
        public double thermal_assist_bank;
        public double altitude_biome;
        public double weather_region;
        public double island_distance;
        public double span_ratio;
        public double inferred_tuck;
        public double feather_support;
        public double feather_degrees;
        public double aero_torque_x;
        public double aero_torque_y;
        public double aero_torque_z;
        public double effort_active_seconds;
        public double effort_rest_seconds;
        public double effort_hand_travel_m;
        public double effort_speed_ema;
        public double effort_strokes;
        public double effort_continuous_seconds;
        public double effort_resting;
        public double food_caught;
        public double food_points;
        public double food_combo;
        public double objective_quiet_gain;
        public double objective_highest_altitude;
        public double clearance_age_seconds;
        public static readonly string[] Fields = {
            "timestamp",
            "frame",
            "simulation_time",
            "dt",
            "render_dt",
            "unscaled_dt",
            "raw_left_tracked",
            "raw_left_position_x",
            "raw_left_position_y",
            "raw_left_position_z",
            "raw_left_velocity_x",
            "raw_left_velocity_y",
            "raw_left_velocity_z",
            "raw_left_rotation_x",
            "raw_left_rotation_y",
            "raw_left_rotation_z",
            "raw_left_rotation_w",
            "raw_right_tracked",
            "raw_right_position_x",
            "raw_right_position_y",
            "raw_right_position_z",
            "raw_right_velocity_x",
            "raw_right_velocity_y",
            "raw_right_velocity_z",
            "raw_right_rotation_x",
            "raw_right_rotation_y",
            "raw_right_rotation_z",
            "raw_right_rotation_w",
            "raw_head_tracked",
            "raw_head_position_x",
            "raw_head_position_y",
            "raw_head_position_z",
            "raw_head_rotation_x",
            "raw_head_rotation_y",
            "raw_head_rotation_z",
            "raw_head_rotation_w",
            "raw_look_x",
            "raw_look_y",
            "raw_look_z",
            "raw_body_rotation_x",
            "raw_body_rotation_y",
            "raw_body_rotation_z",
            "raw_body_rotation_w",
            "raw_body_tracked",
            "raw_bank",
            "raw_tuck",
            "raw_flare",
            "raw_ground_move_x",
            "raw_ground_move_y",
            "raw_reset",
            "raw_recalibrate",
            "raw_characterselect",
            "raw_pause",
            "raw_viewtoggle",
            "raw_windmode",
            "raw_hudtoggle",
            "raw_marker",
            "raw_buttons",
            "mapped_left_tracked",
            "mapped_left_position_x",
            "mapped_left_position_y",
            "mapped_left_position_z",
            "mapped_left_velocity_x",
            "mapped_left_velocity_y",
            "mapped_left_velocity_z",
            "mapped_left_rotation_x",
            "mapped_left_rotation_y",
            "mapped_left_rotation_z",
            "mapped_left_rotation_w",
            "mapped_right_tracked",
            "mapped_right_position_x",
            "mapped_right_position_y",
            "mapped_right_position_z",
            "mapped_right_velocity_x",
            "mapped_right_velocity_y",
            "mapped_right_velocity_z",
            "mapped_right_rotation_x",
            "mapped_right_rotation_y",
            "mapped_right_rotation_z",
            "mapped_right_rotation_w",
            "mapped_head_tracked",
            "mapped_head_position_x",
            "mapped_head_position_y",
            "mapped_head_position_z",
            "mapped_head_rotation_x",
            "mapped_head_rotation_y",
            "mapped_head_rotation_z",
            "mapped_head_rotation_w",
            "mapped_look_x",
            "mapped_look_y",
            "mapped_look_z",
            "mapped_body_rotation_x",
            "mapped_body_rotation_y",
            "mapped_body_rotation_z",
            "mapped_body_rotation_w",
            "mapped_body_tracked",
            "mapped_bank",
            "mapped_tuck",
            "mapped_flare",
            "mapped_ground_move_x",
            "mapped_ground_move_y",
            "mapped_reset",
            "mapped_recalibrate",
            "mapped_characterselect",
            "mapped_pause",
            "mapped_viewtoggle",
            "mapped_windmode",
            "mapped_hudtoggle",
            "mapped_marker",
            "mapped_buttons",
            "native_left_velocity_available",
            "native_left_velocity_x",
            "native_left_velocity_y",
            "native_left_velocity_z",
            "native_left_angular_available",
            "native_left_angular_x",
            "native_left_angular_y",
            "native_left_angular_z",
            "native_right_velocity_available",
            "native_right_velocity_x",
            "native_right_velocity_y",
            "native_right_velocity_z",
            "native_right_angular_available",
            "native_right_angular_x",
            "native_right_angular_y",
            "native_right_angular_z",
            "calibrated",
            "calibration_sequence",
            "input_wings_enabled",
            "human_span",
            "bird_half_span",
            "motion_scale",
            "calibration_head_origin_x",
            "calibration_head_origin_y",
            "calibration_head_origin_z",
            "calibration_heading_x",
            "calibration_heading_y",
            "calibration_heading_z",
            "calibration_heading_w",
            "calibration_pitch_x",
            "calibration_pitch_y",
            "calibration_pitch_z",
            "calibration_pitch_w",
            "calibration_left_neutral_x",
            "calibration_left_neutral_y",
            "calibration_left_neutral_z",
            "calibration_left_rotation_x",
            "calibration_left_rotation_y",
            "calibration_left_rotation_z",
            "calibration_left_rotation_w",
            "target_left_x",
            "target_left_y",
            "target_left_z",
            "reach_left",
            "joint_left_upper_x",
            "joint_left_upper_y",
            "joint_left_upper_z",
            "joint_left_upper_w",
            "joint_left_forearm_x",
            "joint_left_forearm_y",
            "joint_left_forearm_z",
            "joint_left_forearm_w",
            "joint_left_hand_x",
            "joint_left_hand_y",
            "joint_left_hand_z",
            "joint_left_hand_w",
            "calibration_right_neutral_x",
            "calibration_right_neutral_y",
            "calibration_right_neutral_z",
            "calibration_right_rotation_x",
            "calibration_right_rotation_y",
            "calibration_right_rotation_z",
            "calibration_right_rotation_w",
            "target_right_x",
            "target_right_y",
            "target_right_z",
            "reach_right",
            "joint_right_upper_x",
            "joint_right_upper_y",
            "joint_right_upper_z",
            "joint_right_upper_w",
            "joint_right_forearm_x",
            "joint_right_forearm_y",
            "joint_right_forearm_z",
            "joint_right_forearm_w",
            "joint_right_hand_x",
            "joint_right_hand_y",
            "joint_right_hand_z",
            "joint_right_hand_w",
            "position_x",
            "position_y",
            "position_z",
            "velocity_x",
            "velocity_y",
            "velocity_z",
            "rotation_x",
            "rotation_y",
            "rotation_z",
            "rotation_w",
            "logical_x",
            "logical_y",
            "logical_z",
            "wind_x",
            "wind_y",
            "wind_z",
            "lift_x",
            "lift_y",
            "lift_z",
            "drag_x",
            "drag_y",
            "drag_z",
            "stroke_x",
            "stroke_y",
            "stroke_z",
            "phase",
            "airspeed",
            "groundspeed",
            "aoa",
            "head_pitch",
            "brake",
            "energy",
            "landing_approach",
            "collision_count",
            "landing_count",
            "impact_speed",
            "streaming_blocked",
            "chunks",
            "chunk_queue",
            "chunk_x",
            "chunk_z",
            "generation_ms",
            "view",
            "weather",
            "marker",
            "dropped",
            "capture_cpu_ms",
            "stalled",
            "takeoff_count",
            "cpu_ms",
            "gpu_ms",
            "timing_available",
            "refresh_hz",
            "refresh_available",
            "clearance",
            "clearance_available",
            "avian_leftfan",
            "avian_rightfan",
            "avian_leftfold",
            "avian_rightfold",
            "avian_alula",
            "avian_tailspread",
            "avian_tailpitch",
            "avian_tailyaw",
            "raw_control_mode_pressed",
            "control_mode",
            "angular_velocity_x",
            "angular_velocity_y",
            "angular_velocity_z",
            "control_torque_x",
            "control_torque_y",
            "control_torque_z",
            "trick_count",
            "trick_score",
            "last_trick",
            "mission_id",
            "objective_stage",
            "objective_progress",
            "objective_status",
            "mission_score",
            "thermal_gradient_x",
            "thermal_gradient_z",
            "thermal_assist_bank",
            "altitude_biome",
            "weather_region",
            "island_distance",
            "span_ratio",
            "inferred_tuck",
            "feather_support",
            "feather_degrees",
            "aero_torque_x",
            "aero_torque_y",
            "aero_torque_z",
            "effort_active_seconds",
            "effort_rest_seconds",
            "effort_hand_travel_m",
            "effort_speed_ema",
            "effort_strokes",
            "effort_continuous_seconds",
            "effort_resting",
            "food_caught",
            "food_points",
            "food_combo",
            "objective_quiet_gain",
            "objective_highest_altitude",
            "clearance_age_seconds",
        };
        public static readonly string[] WideFields = { "timestamp","simulation_time","logical_x","logical_y","logical_z" };
        public const int CompactSize = 1200;
        public void Write(BinaryWriter w)
        {
            w.Write(timestamp);
            w.Write(frame);
            w.Write(simulation_time);
            w.Write(dt);
            w.Write(render_dt);
            w.Write(unscaled_dt);
            w.Write(raw_left_tracked);
            w.Write(raw_left_position_x);
            w.Write(raw_left_position_y);
            w.Write(raw_left_position_z);
            w.Write(raw_left_velocity_x);
            w.Write(raw_left_velocity_y);
            w.Write(raw_left_velocity_z);
            w.Write(raw_left_rotation_x);
            w.Write(raw_left_rotation_y);
            w.Write(raw_left_rotation_z);
            w.Write(raw_left_rotation_w);
            w.Write(raw_right_tracked);
            w.Write(raw_right_position_x);
            w.Write(raw_right_position_y);
            w.Write(raw_right_position_z);
            w.Write(raw_right_velocity_x);
            w.Write(raw_right_velocity_y);
            w.Write(raw_right_velocity_z);
            w.Write(raw_right_rotation_x);
            w.Write(raw_right_rotation_y);
            w.Write(raw_right_rotation_z);
            w.Write(raw_right_rotation_w);
            w.Write(raw_head_tracked);
            w.Write(raw_head_position_x);
            w.Write(raw_head_position_y);
            w.Write(raw_head_position_z);
            w.Write(raw_head_rotation_x);
            w.Write(raw_head_rotation_y);
            w.Write(raw_head_rotation_z);
            w.Write(raw_head_rotation_w);
            w.Write(raw_look_x);
            w.Write(raw_look_y);
            w.Write(raw_look_z);
            w.Write(raw_body_rotation_x);
            w.Write(raw_body_rotation_y);
            w.Write(raw_body_rotation_z);
            w.Write(raw_body_rotation_w);
            w.Write(raw_body_tracked);
            w.Write(raw_bank);
            w.Write(raw_tuck);
            w.Write(raw_flare);
            w.Write(raw_ground_move_x);
            w.Write(raw_ground_move_y);
            w.Write(raw_reset);
            w.Write(raw_recalibrate);
            w.Write(raw_characterselect);
            w.Write(raw_pause);
            w.Write(raw_viewtoggle);
            w.Write(raw_windmode);
            w.Write(raw_hudtoggle);
            w.Write(raw_marker);
            w.Write(raw_buttons);
            w.Write(mapped_left_tracked);
            w.Write(mapped_left_position_x);
            w.Write(mapped_left_position_y);
            w.Write(mapped_left_position_z);
            w.Write(mapped_left_velocity_x);
            w.Write(mapped_left_velocity_y);
            w.Write(mapped_left_velocity_z);
            w.Write(mapped_left_rotation_x);
            w.Write(mapped_left_rotation_y);
            w.Write(mapped_left_rotation_z);
            w.Write(mapped_left_rotation_w);
            w.Write(mapped_right_tracked);
            w.Write(mapped_right_position_x);
            w.Write(mapped_right_position_y);
            w.Write(mapped_right_position_z);
            w.Write(mapped_right_velocity_x);
            w.Write(mapped_right_velocity_y);
            w.Write(mapped_right_velocity_z);
            w.Write(mapped_right_rotation_x);
            w.Write(mapped_right_rotation_y);
            w.Write(mapped_right_rotation_z);
            w.Write(mapped_right_rotation_w);
            w.Write(mapped_head_tracked);
            w.Write(mapped_head_position_x);
            w.Write(mapped_head_position_y);
            w.Write(mapped_head_position_z);
            w.Write(mapped_head_rotation_x);
            w.Write(mapped_head_rotation_y);
            w.Write(mapped_head_rotation_z);
            w.Write(mapped_head_rotation_w);
            w.Write(mapped_look_x);
            w.Write(mapped_look_y);
            w.Write(mapped_look_z);
            w.Write(mapped_body_rotation_x);
            w.Write(mapped_body_rotation_y);
            w.Write(mapped_body_rotation_z);
            w.Write(mapped_body_rotation_w);
            w.Write(mapped_body_tracked);
            w.Write(mapped_bank);
            w.Write(mapped_tuck);
            w.Write(mapped_flare);
            w.Write(mapped_ground_move_x);
            w.Write(mapped_ground_move_y);
            w.Write(mapped_reset);
            w.Write(mapped_recalibrate);
            w.Write(mapped_characterselect);
            w.Write(mapped_pause);
            w.Write(mapped_viewtoggle);
            w.Write(mapped_windmode);
            w.Write(mapped_hudtoggle);
            w.Write(mapped_marker);
            w.Write(mapped_buttons);
            w.Write(native_left_velocity_available);
            w.Write(native_left_velocity_x);
            w.Write(native_left_velocity_y);
            w.Write(native_left_velocity_z);
            w.Write(native_left_angular_available);
            w.Write(native_left_angular_x);
            w.Write(native_left_angular_y);
            w.Write(native_left_angular_z);
            w.Write(native_right_velocity_available);
            w.Write(native_right_velocity_x);
            w.Write(native_right_velocity_y);
            w.Write(native_right_velocity_z);
            w.Write(native_right_angular_available);
            w.Write(native_right_angular_x);
            w.Write(native_right_angular_y);
            w.Write(native_right_angular_z);
            w.Write(calibrated);
            w.Write(calibration_sequence);
            w.Write(input_wings_enabled);
            w.Write(human_span);
            w.Write(bird_half_span);
            w.Write(motion_scale);
            w.Write(calibration_head_origin_x);
            w.Write(calibration_head_origin_y);
            w.Write(calibration_head_origin_z);
            w.Write(calibration_heading_x);
            w.Write(calibration_heading_y);
            w.Write(calibration_heading_z);
            w.Write(calibration_heading_w);
            w.Write(calibration_pitch_x);
            w.Write(calibration_pitch_y);
            w.Write(calibration_pitch_z);
            w.Write(calibration_pitch_w);
            w.Write(calibration_left_neutral_x);
            w.Write(calibration_left_neutral_y);
            w.Write(calibration_left_neutral_z);
            w.Write(calibration_left_rotation_x);
            w.Write(calibration_left_rotation_y);
            w.Write(calibration_left_rotation_z);
            w.Write(calibration_left_rotation_w);
            w.Write(target_left_x);
            w.Write(target_left_y);
            w.Write(target_left_z);
            w.Write(reach_left);
            w.Write(joint_left_upper_x);
            w.Write(joint_left_upper_y);
            w.Write(joint_left_upper_z);
            w.Write(joint_left_upper_w);
            w.Write(joint_left_forearm_x);
            w.Write(joint_left_forearm_y);
            w.Write(joint_left_forearm_z);
            w.Write(joint_left_forearm_w);
            w.Write(joint_left_hand_x);
            w.Write(joint_left_hand_y);
            w.Write(joint_left_hand_z);
            w.Write(joint_left_hand_w);
            w.Write(calibration_right_neutral_x);
            w.Write(calibration_right_neutral_y);
            w.Write(calibration_right_neutral_z);
            w.Write(calibration_right_rotation_x);
            w.Write(calibration_right_rotation_y);
            w.Write(calibration_right_rotation_z);
            w.Write(calibration_right_rotation_w);
            w.Write(target_right_x);
            w.Write(target_right_y);
            w.Write(target_right_z);
            w.Write(reach_right);
            w.Write(joint_right_upper_x);
            w.Write(joint_right_upper_y);
            w.Write(joint_right_upper_z);
            w.Write(joint_right_upper_w);
            w.Write(joint_right_forearm_x);
            w.Write(joint_right_forearm_y);
            w.Write(joint_right_forearm_z);
            w.Write(joint_right_forearm_w);
            w.Write(joint_right_hand_x);
            w.Write(joint_right_hand_y);
            w.Write(joint_right_hand_z);
            w.Write(joint_right_hand_w);
            w.Write(position_x);
            w.Write(position_y);
            w.Write(position_z);
            w.Write(velocity_x);
            w.Write(velocity_y);
            w.Write(velocity_z);
            w.Write(rotation_x);
            w.Write(rotation_y);
            w.Write(rotation_z);
            w.Write(rotation_w);
            w.Write(logical_x);
            w.Write(logical_y);
            w.Write(logical_z);
            w.Write(wind_x);
            w.Write(wind_y);
            w.Write(wind_z);
            w.Write(lift_x);
            w.Write(lift_y);
            w.Write(lift_z);
            w.Write(drag_x);
            w.Write(drag_y);
            w.Write(drag_z);
            w.Write(stroke_x);
            w.Write(stroke_y);
            w.Write(stroke_z);
            w.Write(phase);
            w.Write(airspeed);
            w.Write(groundspeed);
            w.Write(aoa);
            w.Write(head_pitch);
            w.Write(brake);
            w.Write(energy);
            w.Write(landing_approach);
            w.Write(collision_count);
            w.Write(landing_count);
            w.Write(impact_speed);
            w.Write(streaming_blocked);
            w.Write(chunks);
            w.Write(chunk_queue);
            w.Write(chunk_x);
            w.Write(chunk_z);
            w.Write(generation_ms);
            w.Write(view);
            w.Write(weather);
            w.Write(marker);
            w.Write(dropped);
            w.Write(capture_cpu_ms);
            w.Write(stalled);
            w.Write(takeoff_count);
            w.Write(cpu_ms);
            w.Write(gpu_ms);
            w.Write(timing_available);
            w.Write(refresh_hz);
            w.Write(refresh_available);
            w.Write(clearance);
            w.Write(clearance_available);
            w.Write(avian_leftfan);
            w.Write(avian_rightfan);
            w.Write(avian_leftfold);
            w.Write(avian_rightfold);
            w.Write(avian_alula);
            w.Write(avian_tailspread);
            w.Write(avian_tailpitch);
            w.Write(avian_tailyaw);
            w.Write(raw_control_mode_pressed);
            w.Write(control_mode);
            w.Write(angular_velocity_x);
            w.Write(angular_velocity_y);
            w.Write(angular_velocity_z);
            w.Write(control_torque_x);
            w.Write(control_torque_y);
            w.Write(control_torque_z);
            w.Write(trick_count);
            w.Write(trick_score);
            w.Write(last_trick);
            w.Write(mission_id);
            w.Write(objective_stage);
            w.Write(objective_progress);
            w.Write(objective_status);
            w.Write(mission_score);
            w.Write(thermal_gradient_x);
            w.Write(thermal_gradient_z);
            w.Write(thermal_assist_bank);
            w.Write(altitude_biome);
            w.Write(weather_region);
            w.Write(island_distance);
            w.Write(span_ratio);
            w.Write(inferred_tuck);
            w.Write(feather_support);
            w.Write(feather_degrees);
            w.Write(aero_torque_x);
            w.Write(aero_torque_y);
            w.Write(aero_torque_z);
            w.Write(effort_active_seconds);
            w.Write(effort_rest_seconds);
            w.Write(effort_hand_travel_m);
            w.Write(effort_speed_ema);
            w.Write(effort_strokes);
            w.Write(effort_continuous_seconds);
            w.Write(effort_resting);
            w.Write(food_caught);
            w.Write(food_points);
            w.Write(food_combo);
            w.Write(objective_quiet_gain);
            w.Write(objective_highest_altitude);
            w.Write(clearance_age_seconds);
        }
        public void WriteCompact(BinaryWriter w)
        {
            w.Write(timestamp);
            w.Write((float)frame);
            w.Write(simulation_time);
            w.Write((float)dt);
            w.Write((float)render_dt);
            w.Write((float)unscaled_dt);
            w.Write((float)raw_left_tracked);
            w.Write((float)raw_left_position_x);
            w.Write((float)raw_left_position_y);
            w.Write((float)raw_left_position_z);
            w.Write((float)raw_left_velocity_x);
            w.Write((float)raw_left_velocity_y);
            w.Write((float)raw_left_velocity_z);
            w.Write((float)raw_left_rotation_x);
            w.Write((float)raw_left_rotation_y);
            w.Write((float)raw_left_rotation_z);
            w.Write((float)raw_left_rotation_w);
            w.Write((float)raw_right_tracked);
            w.Write((float)raw_right_position_x);
            w.Write((float)raw_right_position_y);
            w.Write((float)raw_right_position_z);
            w.Write((float)raw_right_velocity_x);
            w.Write((float)raw_right_velocity_y);
            w.Write((float)raw_right_velocity_z);
            w.Write((float)raw_right_rotation_x);
            w.Write((float)raw_right_rotation_y);
            w.Write((float)raw_right_rotation_z);
            w.Write((float)raw_right_rotation_w);
            w.Write((float)raw_head_tracked);
            w.Write((float)raw_head_position_x);
            w.Write((float)raw_head_position_y);
            w.Write((float)raw_head_position_z);
            w.Write((float)raw_head_rotation_x);
            w.Write((float)raw_head_rotation_y);
            w.Write((float)raw_head_rotation_z);
            w.Write((float)raw_head_rotation_w);
            w.Write((float)raw_look_x);
            w.Write((float)raw_look_y);
            w.Write((float)raw_look_z);
            w.Write((float)raw_body_rotation_x);
            w.Write((float)raw_body_rotation_y);
            w.Write((float)raw_body_rotation_z);
            w.Write((float)raw_body_rotation_w);
            w.Write((float)raw_body_tracked);
            w.Write((float)raw_bank);
            w.Write((float)raw_tuck);
            w.Write((float)raw_flare);
            w.Write((float)raw_ground_move_x);
            w.Write((float)raw_ground_move_y);
            w.Write((float)raw_reset);
            w.Write((float)raw_recalibrate);
            w.Write((float)raw_characterselect);
            w.Write((float)raw_pause);
            w.Write((float)raw_viewtoggle);
            w.Write((float)raw_windmode);
            w.Write((float)raw_hudtoggle);
            w.Write((float)raw_marker);
            w.Write((float)raw_buttons);
            w.Write((float)mapped_left_tracked);
            w.Write((float)mapped_left_position_x);
            w.Write((float)mapped_left_position_y);
            w.Write((float)mapped_left_position_z);
            w.Write((float)mapped_left_velocity_x);
            w.Write((float)mapped_left_velocity_y);
            w.Write((float)mapped_left_velocity_z);
            w.Write((float)mapped_left_rotation_x);
            w.Write((float)mapped_left_rotation_y);
            w.Write((float)mapped_left_rotation_z);
            w.Write((float)mapped_left_rotation_w);
            w.Write((float)mapped_right_tracked);
            w.Write((float)mapped_right_position_x);
            w.Write((float)mapped_right_position_y);
            w.Write((float)mapped_right_position_z);
            w.Write((float)mapped_right_velocity_x);
            w.Write((float)mapped_right_velocity_y);
            w.Write((float)mapped_right_velocity_z);
            w.Write((float)mapped_right_rotation_x);
            w.Write((float)mapped_right_rotation_y);
            w.Write((float)mapped_right_rotation_z);
            w.Write((float)mapped_right_rotation_w);
            w.Write((float)mapped_head_tracked);
            w.Write((float)mapped_head_position_x);
            w.Write((float)mapped_head_position_y);
            w.Write((float)mapped_head_position_z);
            w.Write((float)mapped_head_rotation_x);
            w.Write((float)mapped_head_rotation_y);
            w.Write((float)mapped_head_rotation_z);
            w.Write((float)mapped_head_rotation_w);
            w.Write((float)mapped_look_x);
            w.Write((float)mapped_look_y);
            w.Write((float)mapped_look_z);
            w.Write((float)mapped_body_rotation_x);
            w.Write((float)mapped_body_rotation_y);
            w.Write((float)mapped_body_rotation_z);
            w.Write((float)mapped_body_rotation_w);
            w.Write((float)mapped_body_tracked);
            w.Write((float)mapped_bank);
            w.Write((float)mapped_tuck);
            w.Write((float)mapped_flare);
            w.Write((float)mapped_ground_move_x);
            w.Write((float)mapped_ground_move_y);
            w.Write((float)mapped_reset);
            w.Write((float)mapped_recalibrate);
            w.Write((float)mapped_characterselect);
            w.Write((float)mapped_pause);
            w.Write((float)mapped_viewtoggle);
            w.Write((float)mapped_windmode);
            w.Write((float)mapped_hudtoggle);
            w.Write((float)mapped_marker);
            w.Write((float)mapped_buttons);
            w.Write((float)native_left_velocity_available);
            w.Write((float)native_left_velocity_x);
            w.Write((float)native_left_velocity_y);
            w.Write((float)native_left_velocity_z);
            w.Write((float)native_left_angular_available);
            w.Write((float)native_left_angular_x);
            w.Write((float)native_left_angular_y);
            w.Write((float)native_left_angular_z);
            w.Write((float)native_right_velocity_available);
            w.Write((float)native_right_velocity_x);
            w.Write((float)native_right_velocity_y);
            w.Write((float)native_right_velocity_z);
            w.Write((float)native_right_angular_available);
            w.Write((float)native_right_angular_x);
            w.Write((float)native_right_angular_y);
            w.Write((float)native_right_angular_z);
            w.Write((float)calibrated);
            w.Write((float)calibration_sequence);
            w.Write((float)input_wings_enabled);
            w.Write((float)human_span);
            w.Write((float)bird_half_span);
            w.Write((float)motion_scale);
            w.Write((float)calibration_head_origin_x);
            w.Write((float)calibration_head_origin_y);
            w.Write((float)calibration_head_origin_z);
            w.Write((float)calibration_heading_x);
            w.Write((float)calibration_heading_y);
            w.Write((float)calibration_heading_z);
            w.Write((float)calibration_heading_w);
            w.Write((float)calibration_pitch_x);
            w.Write((float)calibration_pitch_y);
            w.Write((float)calibration_pitch_z);
            w.Write((float)calibration_pitch_w);
            w.Write((float)calibration_left_neutral_x);
            w.Write((float)calibration_left_neutral_y);
            w.Write((float)calibration_left_neutral_z);
            w.Write((float)calibration_left_rotation_x);
            w.Write((float)calibration_left_rotation_y);
            w.Write((float)calibration_left_rotation_z);
            w.Write((float)calibration_left_rotation_w);
            w.Write((float)target_left_x);
            w.Write((float)target_left_y);
            w.Write((float)target_left_z);
            w.Write((float)reach_left);
            w.Write((float)joint_left_upper_x);
            w.Write((float)joint_left_upper_y);
            w.Write((float)joint_left_upper_z);
            w.Write((float)joint_left_upper_w);
            w.Write((float)joint_left_forearm_x);
            w.Write((float)joint_left_forearm_y);
            w.Write((float)joint_left_forearm_z);
            w.Write((float)joint_left_forearm_w);
            w.Write((float)joint_left_hand_x);
            w.Write((float)joint_left_hand_y);
            w.Write((float)joint_left_hand_z);
            w.Write((float)joint_left_hand_w);
            w.Write((float)calibration_right_neutral_x);
            w.Write((float)calibration_right_neutral_y);
            w.Write((float)calibration_right_neutral_z);
            w.Write((float)calibration_right_rotation_x);
            w.Write((float)calibration_right_rotation_y);
            w.Write((float)calibration_right_rotation_z);
            w.Write((float)calibration_right_rotation_w);
            w.Write((float)target_right_x);
            w.Write((float)target_right_y);
            w.Write((float)target_right_z);
            w.Write((float)reach_right);
            w.Write((float)joint_right_upper_x);
            w.Write((float)joint_right_upper_y);
            w.Write((float)joint_right_upper_z);
            w.Write((float)joint_right_upper_w);
            w.Write((float)joint_right_forearm_x);
            w.Write((float)joint_right_forearm_y);
            w.Write((float)joint_right_forearm_z);
            w.Write((float)joint_right_forearm_w);
            w.Write((float)joint_right_hand_x);
            w.Write((float)joint_right_hand_y);
            w.Write((float)joint_right_hand_z);
            w.Write((float)joint_right_hand_w);
            w.Write((float)position_x);
            w.Write((float)position_y);
            w.Write((float)position_z);
            w.Write((float)velocity_x);
            w.Write((float)velocity_y);
            w.Write((float)velocity_z);
            w.Write((float)rotation_x);
            w.Write((float)rotation_y);
            w.Write((float)rotation_z);
            w.Write((float)rotation_w);
            w.Write(logical_x);
            w.Write(logical_y);
            w.Write(logical_z);
            w.Write((float)wind_x);
            w.Write((float)wind_y);
            w.Write((float)wind_z);
            w.Write((float)lift_x);
            w.Write((float)lift_y);
            w.Write((float)lift_z);
            w.Write((float)drag_x);
            w.Write((float)drag_y);
            w.Write((float)drag_z);
            w.Write((float)stroke_x);
            w.Write((float)stroke_y);
            w.Write((float)stroke_z);
            w.Write((float)phase);
            w.Write((float)airspeed);
            w.Write((float)groundspeed);
            w.Write((float)aoa);
            w.Write((float)head_pitch);
            w.Write((float)brake);
            w.Write((float)energy);
            w.Write((float)landing_approach);
            w.Write((float)collision_count);
            w.Write((float)landing_count);
            w.Write((float)impact_speed);
            w.Write((float)streaming_blocked);
            w.Write((float)chunks);
            w.Write((float)chunk_queue);
            w.Write((float)chunk_x);
            w.Write((float)chunk_z);
            w.Write((float)generation_ms);
            w.Write((float)view);
            w.Write((float)weather);
            w.Write((float)marker);
            w.Write((float)dropped);
            w.Write((float)capture_cpu_ms);
            w.Write((float)stalled);
            w.Write((float)takeoff_count);
            w.Write((float)cpu_ms);
            w.Write((float)gpu_ms);
            w.Write((float)timing_available);
            w.Write((float)refresh_hz);
            w.Write((float)refresh_available);
            w.Write((float)clearance);
            w.Write((float)clearance_available);
            w.Write((float)avian_leftfan);
            w.Write((float)avian_rightfan);
            w.Write((float)avian_leftfold);
            w.Write((float)avian_rightfold);
            w.Write((float)avian_alula);
            w.Write((float)avian_tailspread);
            w.Write((float)avian_tailpitch);
            w.Write((float)avian_tailyaw);
            w.Write((float)raw_control_mode_pressed);
            w.Write((float)control_mode);
            w.Write((float)angular_velocity_x);
            w.Write((float)angular_velocity_y);
            w.Write((float)angular_velocity_z);
            w.Write((float)control_torque_x);
            w.Write((float)control_torque_y);
            w.Write((float)control_torque_z);
            w.Write((float)trick_count);
            w.Write((float)trick_score);
            w.Write((float)last_trick);
            w.Write((float)mission_id);
            w.Write((float)objective_stage);
            w.Write((float)objective_progress);
            w.Write((float)objective_status);
            w.Write((float)mission_score);
            w.Write((float)thermal_gradient_x);
            w.Write((float)thermal_gradient_z);
            w.Write((float)thermal_assist_bank);
            w.Write((float)altitude_biome);
            w.Write((float)weather_region);
            w.Write((float)island_distance);
            w.Write((float)span_ratio);
            w.Write((float)inferred_tuck);
            w.Write((float)feather_support);
            w.Write((float)feather_degrees);
            w.Write((float)aero_torque_x);
            w.Write((float)aero_torque_y);
            w.Write((float)aero_torque_z);
            w.Write((float)effort_active_seconds);
            w.Write((float)effort_rest_seconds);
            w.Write((float)effort_hand_travel_m);
            w.Write((float)effort_speed_ema);
            w.Write((float)effort_strokes);
            w.Write((float)effort_continuous_seconds);
            w.Write((float)effort_resting);
            w.Write((float)food_caught);
            w.Write((float)food_points);
            w.Write((float)food_combo);
            w.Write((float)objective_quiet_gain);
            w.Write((float)objective_highest_altitude);
            w.Write((float)clearance_age_seconds);
        }
        public static TelemetrySample Read(BinaryReader r)=>new TelemetrySample
        {
            timestamp = r.ReadDouble(),
            frame = r.ReadDouble(),
            simulation_time = r.ReadDouble(),
            dt = r.ReadDouble(),
            render_dt = r.ReadDouble(),
            unscaled_dt = r.ReadDouble(),
            raw_left_tracked = r.ReadDouble(),
            raw_left_position_x = r.ReadDouble(),
            raw_left_position_y = r.ReadDouble(),
            raw_left_position_z = r.ReadDouble(),
            raw_left_velocity_x = r.ReadDouble(),
            raw_left_velocity_y = r.ReadDouble(),
            raw_left_velocity_z = r.ReadDouble(),
            raw_left_rotation_x = r.ReadDouble(),
            raw_left_rotation_y = r.ReadDouble(),
            raw_left_rotation_z = r.ReadDouble(),
            raw_left_rotation_w = r.ReadDouble(),
            raw_right_tracked = r.ReadDouble(),
            raw_right_position_x = r.ReadDouble(),
            raw_right_position_y = r.ReadDouble(),
            raw_right_position_z = r.ReadDouble(),
            raw_right_velocity_x = r.ReadDouble(),
            raw_right_velocity_y = r.ReadDouble(),
            raw_right_velocity_z = r.ReadDouble(),
            raw_right_rotation_x = r.ReadDouble(),
            raw_right_rotation_y = r.ReadDouble(),
            raw_right_rotation_z = r.ReadDouble(),
            raw_right_rotation_w = r.ReadDouble(),
            raw_head_tracked = r.ReadDouble(),
            raw_head_position_x = r.ReadDouble(),
            raw_head_position_y = r.ReadDouble(),
            raw_head_position_z = r.ReadDouble(),
            raw_head_rotation_x = r.ReadDouble(),
            raw_head_rotation_y = r.ReadDouble(),
            raw_head_rotation_z = r.ReadDouble(),
            raw_head_rotation_w = r.ReadDouble(),
            raw_look_x = r.ReadDouble(),
            raw_look_y = r.ReadDouble(),
            raw_look_z = r.ReadDouble(),
            raw_body_rotation_x = r.ReadDouble(),
            raw_body_rotation_y = r.ReadDouble(),
            raw_body_rotation_z = r.ReadDouble(),
            raw_body_rotation_w = r.ReadDouble(),
            raw_body_tracked = r.ReadDouble(),
            raw_bank = r.ReadDouble(),
            raw_tuck = r.ReadDouble(),
            raw_flare = r.ReadDouble(),
            raw_ground_move_x = r.ReadDouble(),
            raw_ground_move_y = r.ReadDouble(),
            raw_reset = r.ReadDouble(),
            raw_recalibrate = r.ReadDouble(),
            raw_characterselect = r.ReadDouble(),
            raw_pause = r.ReadDouble(),
            raw_viewtoggle = r.ReadDouble(),
            raw_windmode = r.ReadDouble(),
            raw_hudtoggle = r.ReadDouble(),
            raw_marker = r.ReadDouble(),
            raw_buttons = r.ReadDouble(),
            mapped_left_tracked = r.ReadDouble(),
            mapped_left_position_x = r.ReadDouble(),
            mapped_left_position_y = r.ReadDouble(),
            mapped_left_position_z = r.ReadDouble(),
            mapped_left_velocity_x = r.ReadDouble(),
            mapped_left_velocity_y = r.ReadDouble(),
            mapped_left_velocity_z = r.ReadDouble(),
            mapped_left_rotation_x = r.ReadDouble(),
            mapped_left_rotation_y = r.ReadDouble(),
            mapped_left_rotation_z = r.ReadDouble(),
            mapped_left_rotation_w = r.ReadDouble(),
            mapped_right_tracked = r.ReadDouble(),
            mapped_right_position_x = r.ReadDouble(),
            mapped_right_position_y = r.ReadDouble(),
            mapped_right_position_z = r.ReadDouble(),
            mapped_right_velocity_x = r.ReadDouble(),
            mapped_right_velocity_y = r.ReadDouble(),
            mapped_right_velocity_z = r.ReadDouble(),
            mapped_right_rotation_x = r.ReadDouble(),
            mapped_right_rotation_y = r.ReadDouble(),
            mapped_right_rotation_z = r.ReadDouble(),
            mapped_right_rotation_w = r.ReadDouble(),
            mapped_head_tracked = r.ReadDouble(),
            mapped_head_position_x = r.ReadDouble(),
            mapped_head_position_y = r.ReadDouble(),
            mapped_head_position_z = r.ReadDouble(),
            mapped_head_rotation_x = r.ReadDouble(),
            mapped_head_rotation_y = r.ReadDouble(),
            mapped_head_rotation_z = r.ReadDouble(),
            mapped_head_rotation_w = r.ReadDouble(),
            mapped_look_x = r.ReadDouble(),
            mapped_look_y = r.ReadDouble(),
            mapped_look_z = r.ReadDouble(),
            mapped_body_rotation_x = r.ReadDouble(),
            mapped_body_rotation_y = r.ReadDouble(),
            mapped_body_rotation_z = r.ReadDouble(),
            mapped_body_rotation_w = r.ReadDouble(),
            mapped_body_tracked = r.ReadDouble(),
            mapped_bank = r.ReadDouble(),
            mapped_tuck = r.ReadDouble(),
            mapped_flare = r.ReadDouble(),
            mapped_ground_move_x = r.ReadDouble(),
            mapped_ground_move_y = r.ReadDouble(),
            mapped_reset = r.ReadDouble(),
            mapped_recalibrate = r.ReadDouble(),
            mapped_characterselect = r.ReadDouble(),
            mapped_pause = r.ReadDouble(),
            mapped_viewtoggle = r.ReadDouble(),
            mapped_windmode = r.ReadDouble(),
            mapped_hudtoggle = r.ReadDouble(),
            mapped_marker = r.ReadDouble(),
            mapped_buttons = r.ReadDouble(),
            native_left_velocity_available = r.ReadDouble(),
            native_left_velocity_x = r.ReadDouble(),
            native_left_velocity_y = r.ReadDouble(),
            native_left_velocity_z = r.ReadDouble(),
            native_left_angular_available = r.ReadDouble(),
            native_left_angular_x = r.ReadDouble(),
            native_left_angular_y = r.ReadDouble(),
            native_left_angular_z = r.ReadDouble(),
            native_right_velocity_available = r.ReadDouble(),
            native_right_velocity_x = r.ReadDouble(),
            native_right_velocity_y = r.ReadDouble(),
            native_right_velocity_z = r.ReadDouble(),
            native_right_angular_available = r.ReadDouble(),
            native_right_angular_x = r.ReadDouble(),
            native_right_angular_y = r.ReadDouble(),
            native_right_angular_z = r.ReadDouble(),
            calibrated = r.ReadDouble(),
            calibration_sequence = r.ReadDouble(),
            input_wings_enabled = r.ReadDouble(),
            human_span = r.ReadDouble(),
            bird_half_span = r.ReadDouble(),
            motion_scale = r.ReadDouble(),
            calibration_head_origin_x = r.ReadDouble(),
            calibration_head_origin_y = r.ReadDouble(),
            calibration_head_origin_z = r.ReadDouble(),
            calibration_heading_x = r.ReadDouble(),
            calibration_heading_y = r.ReadDouble(),
            calibration_heading_z = r.ReadDouble(),
            calibration_heading_w = r.ReadDouble(),
            calibration_pitch_x = r.ReadDouble(),
            calibration_pitch_y = r.ReadDouble(),
            calibration_pitch_z = r.ReadDouble(),
            calibration_pitch_w = r.ReadDouble(),
            calibration_left_neutral_x = r.ReadDouble(),
            calibration_left_neutral_y = r.ReadDouble(),
            calibration_left_neutral_z = r.ReadDouble(),
            calibration_left_rotation_x = r.ReadDouble(),
            calibration_left_rotation_y = r.ReadDouble(),
            calibration_left_rotation_z = r.ReadDouble(),
            calibration_left_rotation_w = r.ReadDouble(),
            target_left_x = r.ReadDouble(),
            target_left_y = r.ReadDouble(),
            target_left_z = r.ReadDouble(),
            reach_left = r.ReadDouble(),
            joint_left_upper_x = r.ReadDouble(),
            joint_left_upper_y = r.ReadDouble(),
            joint_left_upper_z = r.ReadDouble(),
            joint_left_upper_w = r.ReadDouble(),
            joint_left_forearm_x = r.ReadDouble(),
            joint_left_forearm_y = r.ReadDouble(),
            joint_left_forearm_z = r.ReadDouble(),
            joint_left_forearm_w = r.ReadDouble(),
            joint_left_hand_x = r.ReadDouble(),
            joint_left_hand_y = r.ReadDouble(),
            joint_left_hand_z = r.ReadDouble(),
            joint_left_hand_w = r.ReadDouble(),
            calibration_right_neutral_x = r.ReadDouble(),
            calibration_right_neutral_y = r.ReadDouble(),
            calibration_right_neutral_z = r.ReadDouble(),
            calibration_right_rotation_x = r.ReadDouble(),
            calibration_right_rotation_y = r.ReadDouble(),
            calibration_right_rotation_z = r.ReadDouble(),
            calibration_right_rotation_w = r.ReadDouble(),
            target_right_x = r.ReadDouble(),
            target_right_y = r.ReadDouble(),
            target_right_z = r.ReadDouble(),
            reach_right = r.ReadDouble(),
            joint_right_upper_x = r.ReadDouble(),
            joint_right_upper_y = r.ReadDouble(),
            joint_right_upper_z = r.ReadDouble(),
            joint_right_upper_w = r.ReadDouble(),
            joint_right_forearm_x = r.ReadDouble(),
            joint_right_forearm_y = r.ReadDouble(),
            joint_right_forearm_z = r.ReadDouble(),
            joint_right_forearm_w = r.ReadDouble(),
            joint_right_hand_x = r.ReadDouble(),
            joint_right_hand_y = r.ReadDouble(),
            joint_right_hand_z = r.ReadDouble(),
            joint_right_hand_w = r.ReadDouble(),
            position_x = r.ReadDouble(),
            position_y = r.ReadDouble(),
            position_z = r.ReadDouble(),
            velocity_x = r.ReadDouble(),
            velocity_y = r.ReadDouble(),
            velocity_z = r.ReadDouble(),
            rotation_x = r.ReadDouble(),
            rotation_y = r.ReadDouble(),
            rotation_z = r.ReadDouble(),
            rotation_w = r.ReadDouble(),
            logical_x = r.ReadDouble(),
            logical_y = r.ReadDouble(),
            logical_z = r.ReadDouble(),
            wind_x = r.ReadDouble(),
            wind_y = r.ReadDouble(),
            wind_z = r.ReadDouble(),
            lift_x = r.ReadDouble(),
            lift_y = r.ReadDouble(),
            lift_z = r.ReadDouble(),
            drag_x = r.ReadDouble(),
            drag_y = r.ReadDouble(),
            drag_z = r.ReadDouble(),
            stroke_x = r.ReadDouble(),
            stroke_y = r.ReadDouble(),
            stroke_z = r.ReadDouble(),
            phase = r.ReadDouble(),
            airspeed = r.ReadDouble(),
            groundspeed = r.ReadDouble(),
            aoa = r.ReadDouble(),
            head_pitch = r.ReadDouble(),
            brake = r.ReadDouble(),
            energy = r.ReadDouble(),
            landing_approach = r.ReadDouble(),
            collision_count = r.ReadDouble(),
            landing_count = r.ReadDouble(),
            impact_speed = r.ReadDouble(),
            streaming_blocked = r.ReadDouble(),
            chunks = r.ReadDouble(),
            chunk_queue = r.ReadDouble(),
            chunk_x = r.ReadDouble(),
            chunk_z = r.ReadDouble(),
            generation_ms = r.ReadDouble(),
            view = r.ReadDouble(),
            weather = r.ReadDouble(),
            marker = r.ReadDouble(),
            dropped = r.ReadDouble(),
            capture_cpu_ms = r.ReadDouble(),
            stalled = r.ReadDouble(),
            takeoff_count = r.ReadDouble(),
            cpu_ms = r.ReadDouble(),
            gpu_ms = r.ReadDouble(),
            timing_available = r.ReadDouble(),
            refresh_hz = r.ReadDouble(),
            refresh_available = r.ReadDouble(),
            clearance = r.ReadDouble(),
            clearance_available = r.ReadDouble(),
            avian_leftfan = r.ReadDouble(),
            avian_rightfan = r.ReadDouble(),
            avian_leftfold = r.ReadDouble(),
            avian_rightfold = r.ReadDouble(),
            avian_alula = r.ReadDouble(),
            avian_tailspread = r.ReadDouble(),
            avian_tailpitch = r.ReadDouble(),
            avian_tailyaw = r.ReadDouble(),
            raw_control_mode_pressed = r.ReadDouble(),
            control_mode = r.ReadDouble(),
            angular_velocity_x = r.ReadDouble(),
            angular_velocity_y = r.ReadDouble(),
            angular_velocity_z = r.ReadDouble(),
            control_torque_x = r.ReadDouble(),
            control_torque_y = r.ReadDouble(),
            control_torque_z = r.ReadDouble(),
            trick_count = r.ReadDouble(),
            trick_score = r.ReadDouble(),
            last_trick = r.ReadDouble(),
            mission_id = r.ReadDouble(),
            objective_stage = r.ReadDouble(),
            objective_progress = r.ReadDouble(),
            objective_status = r.ReadDouble(),
            mission_score = r.ReadDouble(),
            thermal_gradient_x = r.ReadDouble(),
            thermal_gradient_z = r.ReadDouble(),
            thermal_assist_bank = r.ReadDouble(),
            altitude_biome = r.ReadDouble(),
            weather_region = r.ReadDouble(),
            island_distance = r.ReadDouble(),
            span_ratio = r.ReadDouble(),
            inferred_tuck = r.ReadDouble(),
            feather_support = r.ReadDouble(),
            feather_degrees = r.ReadDouble(),
            aero_torque_x = r.ReadDouble(),
            aero_torque_y = r.ReadDouble(),
            aero_torque_z = r.ReadDouble(),
            effort_active_seconds = r.ReadDouble(),
            effort_rest_seconds = r.ReadDouble(),
            effort_hand_travel_m = r.ReadDouble(),
            effort_speed_ema = r.ReadDouble(),
            effort_strokes = r.ReadDouble(),
            effort_continuous_seconds = r.ReadDouble(),
            effort_resting = r.ReadDouble(),
            food_caught = r.ReadDouble(),
            food_points = r.ReadDouble(),
            food_combo = r.ReadDouble(),
            objective_quiet_gain = r.ReadDouble(),
            objective_highest_altitude = r.ReadDouble(),
            clearance_age_seconds = r.ReadDouble(),
        };
        public static TelemetrySample ReadCompact(BinaryReader r)=>new TelemetrySample
        {
            timestamp = r.ReadDouble(),
            frame = r.ReadSingle(),
            simulation_time = r.ReadDouble(),
            dt = r.ReadSingle(),
            render_dt = r.ReadSingle(),
            unscaled_dt = r.ReadSingle(),
            raw_left_tracked = r.ReadSingle(),
            raw_left_position_x = r.ReadSingle(),
            raw_left_position_y = r.ReadSingle(),
            raw_left_position_z = r.ReadSingle(),
            raw_left_velocity_x = r.ReadSingle(),
            raw_left_velocity_y = r.ReadSingle(),
            raw_left_velocity_z = r.ReadSingle(),
            raw_left_rotation_x = r.ReadSingle(),
            raw_left_rotation_y = r.ReadSingle(),
            raw_left_rotation_z = r.ReadSingle(),
            raw_left_rotation_w = r.ReadSingle(),
            raw_right_tracked = r.ReadSingle(),
            raw_right_position_x = r.ReadSingle(),
            raw_right_position_y = r.ReadSingle(),
            raw_right_position_z = r.ReadSingle(),
            raw_right_velocity_x = r.ReadSingle(),
            raw_right_velocity_y = r.ReadSingle(),
            raw_right_velocity_z = r.ReadSingle(),
            raw_right_rotation_x = r.ReadSingle(),
            raw_right_rotation_y = r.ReadSingle(),
            raw_right_rotation_z = r.ReadSingle(),
            raw_right_rotation_w = r.ReadSingle(),
            raw_head_tracked = r.ReadSingle(),
            raw_head_position_x = r.ReadSingle(),
            raw_head_position_y = r.ReadSingle(),
            raw_head_position_z = r.ReadSingle(),
            raw_head_rotation_x = r.ReadSingle(),
            raw_head_rotation_y = r.ReadSingle(),
            raw_head_rotation_z = r.ReadSingle(),
            raw_head_rotation_w = r.ReadSingle(),
            raw_look_x = r.ReadSingle(),
            raw_look_y = r.ReadSingle(),
            raw_look_z = r.ReadSingle(),
            raw_body_rotation_x = r.ReadSingle(),
            raw_body_rotation_y = r.ReadSingle(),
            raw_body_rotation_z = r.ReadSingle(),
            raw_body_rotation_w = r.ReadSingle(),
            raw_body_tracked = r.ReadSingle(),
            raw_bank = r.ReadSingle(),
            raw_tuck = r.ReadSingle(),
            raw_flare = r.ReadSingle(),
            raw_ground_move_x = r.ReadSingle(),
            raw_ground_move_y = r.ReadSingle(),
            raw_reset = r.ReadSingle(),
            raw_recalibrate = r.ReadSingle(),
            raw_characterselect = r.ReadSingle(),
            raw_pause = r.ReadSingle(),
            raw_viewtoggle = r.ReadSingle(),
            raw_windmode = r.ReadSingle(),
            raw_hudtoggle = r.ReadSingle(),
            raw_marker = r.ReadSingle(),
            raw_buttons = r.ReadSingle(),
            mapped_left_tracked = r.ReadSingle(),
            mapped_left_position_x = r.ReadSingle(),
            mapped_left_position_y = r.ReadSingle(),
            mapped_left_position_z = r.ReadSingle(),
            mapped_left_velocity_x = r.ReadSingle(),
            mapped_left_velocity_y = r.ReadSingle(),
            mapped_left_velocity_z = r.ReadSingle(),
            mapped_left_rotation_x = r.ReadSingle(),
            mapped_left_rotation_y = r.ReadSingle(),
            mapped_left_rotation_z = r.ReadSingle(),
            mapped_left_rotation_w = r.ReadSingle(),
            mapped_right_tracked = r.ReadSingle(),
            mapped_right_position_x = r.ReadSingle(),
            mapped_right_position_y = r.ReadSingle(),
            mapped_right_position_z = r.ReadSingle(),
            mapped_right_velocity_x = r.ReadSingle(),
            mapped_right_velocity_y = r.ReadSingle(),
            mapped_right_velocity_z = r.ReadSingle(),
            mapped_right_rotation_x = r.ReadSingle(),
            mapped_right_rotation_y = r.ReadSingle(),
            mapped_right_rotation_z = r.ReadSingle(),
            mapped_right_rotation_w = r.ReadSingle(),
            mapped_head_tracked = r.ReadSingle(),
            mapped_head_position_x = r.ReadSingle(),
            mapped_head_position_y = r.ReadSingle(),
            mapped_head_position_z = r.ReadSingle(),
            mapped_head_rotation_x = r.ReadSingle(),
            mapped_head_rotation_y = r.ReadSingle(),
            mapped_head_rotation_z = r.ReadSingle(),
            mapped_head_rotation_w = r.ReadSingle(),
            mapped_look_x = r.ReadSingle(),
            mapped_look_y = r.ReadSingle(),
            mapped_look_z = r.ReadSingle(),
            mapped_body_rotation_x = r.ReadSingle(),
            mapped_body_rotation_y = r.ReadSingle(),
            mapped_body_rotation_z = r.ReadSingle(),
            mapped_body_rotation_w = r.ReadSingle(),
            mapped_body_tracked = r.ReadSingle(),
            mapped_bank = r.ReadSingle(),
            mapped_tuck = r.ReadSingle(),
            mapped_flare = r.ReadSingle(),
            mapped_ground_move_x = r.ReadSingle(),
            mapped_ground_move_y = r.ReadSingle(),
            mapped_reset = r.ReadSingle(),
            mapped_recalibrate = r.ReadSingle(),
            mapped_characterselect = r.ReadSingle(),
            mapped_pause = r.ReadSingle(),
            mapped_viewtoggle = r.ReadSingle(),
            mapped_windmode = r.ReadSingle(),
            mapped_hudtoggle = r.ReadSingle(),
            mapped_marker = r.ReadSingle(),
            mapped_buttons = r.ReadSingle(),
            native_left_velocity_available = r.ReadSingle(),
            native_left_velocity_x = r.ReadSingle(),
            native_left_velocity_y = r.ReadSingle(),
            native_left_velocity_z = r.ReadSingle(),
            native_left_angular_available = r.ReadSingle(),
            native_left_angular_x = r.ReadSingle(),
            native_left_angular_y = r.ReadSingle(),
            native_left_angular_z = r.ReadSingle(),
            native_right_velocity_available = r.ReadSingle(),
            native_right_velocity_x = r.ReadSingle(),
            native_right_velocity_y = r.ReadSingle(),
            native_right_velocity_z = r.ReadSingle(),
            native_right_angular_available = r.ReadSingle(),
            native_right_angular_x = r.ReadSingle(),
            native_right_angular_y = r.ReadSingle(),
            native_right_angular_z = r.ReadSingle(),
            calibrated = r.ReadSingle(),
            calibration_sequence = r.ReadSingle(),
            input_wings_enabled = r.ReadSingle(),
            human_span = r.ReadSingle(),
            bird_half_span = r.ReadSingle(),
            motion_scale = r.ReadSingle(),
            calibration_head_origin_x = r.ReadSingle(),
            calibration_head_origin_y = r.ReadSingle(),
            calibration_head_origin_z = r.ReadSingle(),
            calibration_heading_x = r.ReadSingle(),
            calibration_heading_y = r.ReadSingle(),
            calibration_heading_z = r.ReadSingle(),
            calibration_heading_w = r.ReadSingle(),
            calibration_pitch_x = r.ReadSingle(),
            calibration_pitch_y = r.ReadSingle(),
            calibration_pitch_z = r.ReadSingle(),
            calibration_pitch_w = r.ReadSingle(),
            calibration_left_neutral_x = r.ReadSingle(),
            calibration_left_neutral_y = r.ReadSingle(),
            calibration_left_neutral_z = r.ReadSingle(),
            calibration_left_rotation_x = r.ReadSingle(),
            calibration_left_rotation_y = r.ReadSingle(),
            calibration_left_rotation_z = r.ReadSingle(),
            calibration_left_rotation_w = r.ReadSingle(),
            target_left_x = r.ReadSingle(),
            target_left_y = r.ReadSingle(),
            target_left_z = r.ReadSingle(),
            reach_left = r.ReadSingle(),
            joint_left_upper_x = r.ReadSingle(),
            joint_left_upper_y = r.ReadSingle(),
            joint_left_upper_z = r.ReadSingle(),
            joint_left_upper_w = r.ReadSingle(),
            joint_left_forearm_x = r.ReadSingle(),
            joint_left_forearm_y = r.ReadSingle(),
            joint_left_forearm_z = r.ReadSingle(),
            joint_left_forearm_w = r.ReadSingle(),
            joint_left_hand_x = r.ReadSingle(),
            joint_left_hand_y = r.ReadSingle(),
            joint_left_hand_z = r.ReadSingle(),
            joint_left_hand_w = r.ReadSingle(),
            calibration_right_neutral_x = r.ReadSingle(),
            calibration_right_neutral_y = r.ReadSingle(),
            calibration_right_neutral_z = r.ReadSingle(),
            calibration_right_rotation_x = r.ReadSingle(),
            calibration_right_rotation_y = r.ReadSingle(),
            calibration_right_rotation_z = r.ReadSingle(),
            calibration_right_rotation_w = r.ReadSingle(),
            target_right_x = r.ReadSingle(),
            target_right_y = r.ReadSingle(),
            target_right_z = r.ReadSingle(),
            reach_right = r.ReadSingle(),
            joint_right_upper_x = r.ReadSingle(),
            joint_right_upper_y = r.ReadSingle(),
            joint_right_upper_z = r.ReadSingle(),
            joint_right_upper_w = r.ReadSingle(),
            joint_right_forearm_x = r.ReadSingle(),
            joint_right_forearm_y = r.ReadSingle(),
            joint_right_forearm_z = r.ReadSingle(),
            joint_right_forearm_w = r.ReadSingle(),
            joint_right_hand_x = r.ReadSingle(),
            joint_right_hand_y = r.ReadSingle(),
            joint_right_hand_z = r.ReadSingle(),
            joint_right_hand_w = r.ReadSingle(),
            position_x = r.ReadSingle(),
            position_y = r.ReadSingle(),
            position_z = r.ReadSingle(),
            velocity_x = r.ReadSingle(),
            velocity_y = r.ReadSingle(),
            velocity_z = r.ReadSingle(),
            rotation_x = r.ReadSingle(),
            rotation_y = r.ReadSingle(),
            rotation_z = r.ReadSingle(),
            rotation_w = r.ReadSingle(),
            logical_x = r.ReadDouble(),
            logical_y = r.ReadDouble(),
            logical_z = r.ReadDouble(),
            wind_x = r.ReadSingle(),
            wind_y = r.ReadSingle(),
            wind_z = r.ReadSingle(),
            lift_x = r.ReadSingle(),
            lift_y = r.ReadSingle(),
            lift_z = r.ReadSingle(),
            drag_x = r.ReadSingle(),
            drag_y = r.ReadSingle(),
            drag_z = r.ReadSingle(),
            stroke_x = r.ReadSingle(),
            stroke_y = r.ReadSingle(),
            stroke_z = r.ReadSingle(),
            phase = r.ReadSingle(),
            airspeed = r.ReadSingle(),
            groundspeed = r.ReadSingle(),
            aoa = r.ReadSingle(),
            head_pitch = r.ReadSingle(),
            brake = r.ReadSingle(),
            energy = r.ReadSingle(),
            landing_approach = r.ReadSingle(),
            collision_count = r.ReadSingle(),
            landing_count = r.ReadSingle(),
            impact_speed = r.ReadSingle(),
            streaming_blocked = r.ReadSingle(),
            chunks = r.ReadSingle(),
            chunk_queue = r.ReadSingle(),
            chunk_x = r.ReadSingle(),
            chunk_z = r.ReadSingle(),
            generation_ms = r.ReadSingle(),
            view = r.ReadSingle(),
            weather = r.ReadSingle(),
            marker = r.ReadSingle(),
            dropped = r.ReadSingle(),
            capture_cpu_ms = r.ReadSingle(),
            stalled = r.ReadSingle(),
            takeoff_count = r.ReadSingle(),
            cpu_ms = r.ReadSingle(),
            gpu_ms = r.ReadSingle(),
            timing_available = r.ReadSingle(),
            refresh_hz = r.ReadSingle(),
            refresh_available = r.ReadSingle(),
            clearance = r.ReadSingle(),
            clearance_available = r.ReadSingle(),
            avian_leftfan = r.ReadSingle(),
            avian_rightfan = r.ReadSingle(),
            avian_leftfold = r.ReadSingle(),
            avian_rightfold = r.ReadSingle(),
            avian_alula = r.ReadSingle(),
            avian_tailspread = r.ReadSingle(),
            avian_tailpitch = r.ReadSingle(),
            avian_tailyaw = r.ReadSingle(),
            raw_control_mode_pressed = r.ReadSingle(),
            control_mode = r.ReadSingle(),
            angular_velocity_x = r.ReadSingle(),
            angular_velocity_y = r.ReadSingle(),
            angular_velocity_z = r.ReadSingle(),
            control_torque_x = r.ReadSingle(),
            control_torque_y = r.ReadSingle(),
            control_torque_z = r.ReadSingle(),
            trick_count = r.ReadSingle(),
            trick_score = r.ReadSingle(),
            last_trick = r.ReadSingle(),
            mission_id = r.ReadSingle(),
            objective_stage = r.ReadSingle(),
            objective_progress = r.ReadSingle(),
            objective_status = r.ReadSingle(),
            mission_score = r.ReadSingle(),
            thermal_gradient_x = r.ReadSingle(),
            thermal_gradient_z = r.ReadSingle(),
            thermal_assist_bank = r.ReadSingle(),
            altitude_biome = r.ReadSingle(),
            weather_region = r.ReadSingle(),
            island_distance = r.ReadSingle(),
            span_ratio = r.ReadSingle(),
            inferred_tuck = r.ReadSingle(),
            feather_support = r.ReadSingle(),
            feather_degrees = r.ReadSingle(),
            aero_torque_x = r.ReadSingle(),
            aero_torque_y = r.ReadSingle(),
            aero_torque_z = r.ReadSingle(),
            effort_active_seconds = r.ReadSingle(),
            effort_rest_seconds = r.ReadSingle(),
            effort_hand_travel_m = r.ReadSingle(),
            effort_speed_ema = r.ReadSingle(),
            effort_strokes = r.ReadSingle(),
            effort_continuous_seconds = r.ReadSingle(),
            effort_resting = r.ReadSingle(),
            food_caught = r.ReadSingle(),
            food_points = r.ReadSingle(),
            food_combo = r.ReadSingle(),
            objective_quiet_gain = r.ReadSingle(),
            objective_highest_altitude = r.ReadSingle(),
            clearance_age_seconds = r.ReadSingle(),
        };
    }
}
