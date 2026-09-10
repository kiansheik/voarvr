using System;
using System.IO;
namespace VoarVR.Telemetry
{
    // Version 1: fixed-width little-endian IEEE754 doubles. Main thread snapshots only;
    // the worker reads scalar fields and never calls Unity APIs. NaN means unavailable.
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
        public static readonly string[] Fields = { "timestamp", "frame", "simulation_time", "dt", "render_dt", "unscaled_dt", "raw_left_tracked", "raw_left_position_x", "raw_left_position_y", "raw_left_position_z", "raw_left_velocity_x", "raw_left_velocity_y", "raw_left_velocity_z", "raw_left_rotation_x", "raw_left_rotation_y", "raw_left_rotation_z", "raw_left_rotation_w", "raw_right_tracked", "raw_right_position_x", "raw_right_position_y", "raw_right_position_z", "raw_right_velocity_x", "raw_right_velocity_y", "raw_right_velocity_z", "raw_right_rotation_x", "raw_right_rotation_y", "raw_right_rotation_z", "raw_right_rotation_w", "raw_head_tracked", "raw_head_position_x", "raw_head_position_y", "raw_head_position_z", "raw_head_rotation_x", "raw_head_rotation_y", "raw_head_rotation_z", "raw_head_rotation_w", "raw_look_x", "raw_look_y", "raw_look_z", "raw_body_rotation_x", "raw_body_rotation_y", "raw_body_rotation_z", "raw_body_rotation_w", "raw_body_tracked", "raw_bank", "raw_tuck", "raw_flare", "raw_ground_move_x", "raw_ground_move_y", "raw_reset", "raw_recalibrate", "raw_characterselect", "raw_pause", "raw_viewtoggle", "raw_windmode", "raw_hudtoggle", "raw_marker", "raw_buttons", "mapped_left_tracked", "mapped_left_position_x", "mapped_left_position_y", "mapped_left_position_z", "mapped_left_velocity_x", "mapped_left_velocity_y", "mapped_left_velocity_z", "mapped_left_rotation_x", "mapped_left_rotation_y", "mapped_left_rotation_z", "mapped_left_rotation_w", "mapped_right_tracked", "mapped_right_position_x", "mapped_right_position_y", "mapped_right_position_z", "mapped_right_velocity_x", "mapped_right_velocity_y", "mapped_right_velocity_z", "mapped_right_rotation_x", "mapped_right_rotation_y", "mapped_right_rotation_z", "mapped_right_rotation_w", "mapped_head_tracked", "mapped_head_position_x", "mapped_head_position_y", "mapped_head_position_z", "mapped_head_rotation_x", "mapped_head_rotation_y", "mapped_head_rotation_z", "mapped_head_rotation_w", "mapped_look_x", "mapped_look_y", "mapped_look_z", "mapped_body_rotation_x", "mapped_body_rotation_y", "mapped_body_rotation_z", "mapped_body_rotation_w", "mapped_body_tracked", "mapped_bank", "mapped_tuck", "mapped_flare", "mapped_ground_move_x", "mapped_ground_move_y", "mapped_reset", "mapped_recalibrate", "mapped_characterselect", "mapped_pause", "mapped_viewtoggle", "mapped_windmode", "mapped_hudtoggle", "mapped_marker", "mapped_buttons", "native_left_velocity_available", "native_left_velocity_x", "native_left_velocity_y", "native_left_velocity_z", "native_left_angular_available", "native_left_angular_x", "native_left_angular_y", "native_left_angular_z", "native_right_velocity_available", "native_right_velocity_x", "native_right_velocity_y", "native_right_velocity_z", "native_right_angular_available", "native_right_angular_x", "native_right_angular_y", "native_right_angular_z", "calibrated", "calibration_sequence", "input_wings_enabled", "human_span", "bird_half_span", "motion_scale", "calibration_head_origin_x", "calibration_head_origin_y", "calibration_head_origin_z", "calibration_heading_x", "calibration_heading_y", "calibration_heading_z", "calibration_heading_w", "calibration_pitch_x", "calibration_pitch_y", "calibration_pitch_z", "calibration_pitch_w", "calibration_left_neutral_x", "calibration_left_neutral_y", "calibration_left_neutral_z", "calibration_left_rotation_x", "calibration_left_rotation_y", "calibration_left_rotation_z", "calibration_left_rotation_w", "target_left_x", "target_left_y", "target_left_z", "reach_left", "joint_left_upper_x", "joint_left_upper_y", "joint_left_upper_z", "joint_left_upper_w", "joint_left_forearm_x", "joint_left_forearm_y", "joint_left_forearm_z", "joint_left_forearm_w", "joint_left_hand_x", "joint_left_hand_y", "joint_left_hand_z", "joint_left_hand_w", "calibration_right_neutral_x", "calibration_right_neutral_y", "calibration_right_neutral_z", "calibration_right_rotation_x", "calibration_right_rotation_y", "calibration_right_rotation_z", "calibration_right_rotation_w", "target_right_x", "target_right_y", "target_right_z", "reach_right", "joint_right_upper_x", "joint_right_upper_y", "joint_right_upper_z", "joint_right_upper_w", "joint_right_forearm_x", "joint_right_forearm_y", "joint_right_forearm_z", "joint_right_forearm_w", "joint_right_hand_x", "joint_right_hand_y", "joint_right_hand_z", "joint_right_hand_w", "position_x", "position_y", "position_z", "velocity_x", "velocity_y", "velocity_z", "rotation_x", "rotation_y", "rotation_z", "rotation_w", "logical_x", "logical_y", "logical_z", "wind_x", "wind_y", "wind_z", "lift_x", "lift_y", "lift_z", "drag_x", "drag_y", "drag_z", "stroke_x", "stroke_y", "stroke_z", "phase", "airspeed", "groundspeed", "aoa", "head_pitch", "brake", "energy", "landing_approach", "collision_count", "landing_count", "impact_speed", "streaming_blocked", "chunks", "chunk_queue", "chunk_x", "chunk_z", "generation_ms", "view", "weather", "marker", "dropped", "capture_cpu_ms", "stalled", "takeoff_count", "cpu_ms", "gpu_ms", "timing_available", "refresh_hz", "refresh_available", "clearance", "clearance_available", "avian_leftfan", "avian_rightfan", "avian_leftfold", "avian_rightfold", "avian_alula", "avian_tailspread", "avian_tailpitch", "avian_tailyaw" };
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
        }
        public static TelemetrySample Read(BinaryReader r) => new TelemetrySample
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
        };
    }
}
