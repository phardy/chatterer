///////////////////////////////////////////////////////////////////////////////
//
//    Chatterer for Kerbal Space Program
//    Copyright (C) 2013 Iannic-ann-od
//
//    This program is free software: you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation, either version 3 of the License, or
//    (at your option) any later version.
//
//    This program is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//    GNU General Public License for more details.
//
//    You should have received a copy of the GNU General Public License
//    along with this program.  If not, see <http://www.gnu.org/licenses/>.
//
///////////////////////////////////////////////////////////////////////////////







///////////////////////////////////////////////////////////////////////////////


using UnityEngine;
using System;
using System.Collections.Generic;

namespace RBR
{
    public class rbr_chatterer : PartModule
    {
        private static System.Random rand = new System.Random();

        private List<AudioSource> all_beep_clips = new List<AudioSource>();
        private List<AudioSource> all_con_chatter = new List<AudioSource>();
        private List<AudioSource> all_pod_chatter = new List<AudioSource>();
        private List<AudioSource> initial_chatter_set = new List<AudioSource>();
        private int initial_chatter_index;
        private List<AudioSource> response_chatter_set = new List<AudioSource>();
        private int response_chatter_index;

        protected Rect window_0_pos = new Rect(Screen.width / 2, Screen.height / 2, 10, 10);
        protected Rect ui_icon_pos;
        private Texture2D ui_icon_off = new Texture2D(30, 30, TextureFormat.ARGB32, false);
        private Texture2D ui_icon_on = new Texture2D(30, 30, TextureFormat.ARGB32, false);
        private Texture2D ui_icon = new Texture2D(30, 30, TextureFormat.ARGB32, false);
        private bool ui_icons_loaded = false;

        private bool gui_styles_set = false;
        private GUIStyle label_txt_left;
        private GUIStyle label_txt_center;
        //private GUIStyle label_txt_center_bold;
        private GUIStyle label_txt_right;

        private bool active_chatterer = false;
        private bool gui_running = false;
        private bool main_gui_minimized = false;
        private float cfg_update_timer = 0f;

        private int total_beep_clips = 1;
        private int total_con_clips = 16;
        private int total_pod_clips = 17;
        private int current_beep_clip;
        private int current_con_clip;
        private int current_pod_clip;

        private float beep_freq_slider = 5f;
        private float chatter_freq_slider = 3f;
        private int beep_freq = 5;
        private int chatter_freq = 3;
        private float beep_vol_slider = 1f;
        private float chatter_vol_slider = 1f;
        private float prev_beep_vol_slider;
        private float prev_chatter_vol_slider;
        private int prev_beep_freq;
        private int prev_chatter_freq;
        private float beep_pitch_slider = 1f;
        private float prev_beep_pitch_slider;

        private float beep_timer = 0;
        private float secs_since_last_exchange = 0;
        private float secs_since_initial_chatter = 0;
        private float secs_between_exchanges = 0;
        private bool exchange_playing = false;
        private bool pod_begins_exchange = false;
        private int initial_chatter_source;
        private int response_delay_secs;

        private KeyCode insta_chatter_key = KeyCode.Slash;
        private bool set_insta_chatter_key = false;
        private bool key_just_changed = false;

        private Vessel.Situations vessel_prev_sit;
        private int vessel_prev_stage;

        private string this_version = "0.3.2";
        private string latest_version = "";
        private bool recvd_latest_version = false;


        ///////////////////////////////////////////////////////////////////////////////


        private void set_gui_styles()
        {
            label_txt_left = new GUIStyle(GUI.skin.label);
            label_txt_left.normal.textColor = Color.white;
            label_txt_left.alignment = TextAnchor.UpperLeft;

            label_txt_center = new GUIStyle(GUI.skin.label);
            label_txt_center.normal.textColor = Color.white;
            label_txt_center.alignment = TextAnchor.UpperCenter;

            //label_txt_center_bold = new GUIStyle(GUI.skin.label);
            //label_txt_center_bold.normal.textColor = Color.white;
            //label_txt_center_bold.alignment = TextAnchor.UpperCenter;
            //label_txt_center_bold.fontStyle = FontStyle.Bold;

            label_txt_right = new GUIStyle(GUI.skin.label);
            label_txt_right.normal.textColor = Color.white;
            label_txt_right.alignment = TextAnchor.UpperRight;

            gui_styles_set = true;
        }

        private void start_GUI()
        {
            load_settings();
            RenderingManager.AddToPostDrawQueue(3, new Callback(draw_GUI));	//start the GUI
            gui_running = true;
        }

        private void stop_GUI()
        {
            RenderingManager.RemoveFromPostDrawQueue(3, new Callback(draw_GUI));	//stop the GUI
            gui_running = false;
        }

        private void get_latest_version()
        {
            bool got_all_info = false;

            WWWForm form = new WWWForm();
            form.AddField("version", this_version);

            WWW version = new WWW("http://ksp.publicvm.com:8090/chatterer/get_latest_version.php", form.data);

            while (got_all_info == false)
            {
                if (version.isDone)
                {
                    latest_version = version.text;
                    //no analytics crap on my machine so cut this below?
                    //latest_version = latest_version.Substring(0, latest_version.IndexOf("<") - 1).Trim();  //substring cuts off the analytics crap from 000webhost
                    got_all_info = true;
                }
            }
            recvd_latest_version = true;
            //misc_checking_latest_version = false;
        }

        private void check_active_chatterer()
        {
            if (active_chatterer)
            {
                bool local_draw_it = true;
                foreach (Part p in vessel.Parts)
                {
                    foreach (PartModule pm in p.Modules)
                    {
                        if (pm.moduleName == moduleName)
                        {
                            if (part.flightID > p.flightID) local_draw_it = false;
                        }
                    }
                }
                if (local_draw_it == false)
                {
                    active_chatterer = false;
                    stop_GUI();
                }
            }
            else
            {
                bool local_draw_it = true;
                foreach (Part p in vessel.Parts)
                {
                    foreach (PartModule pm in p.Modules)
                    {
                        if (pm.moduleName == moduleName)
                        {
                            if (part.flightID > p.flightID) local_draw_it = false;
                        }
                    }
                }
                if (local_draw_it) active_chatterer = true;
            }
        }

        private void load_chatter_clip(string type, int counter)
        {
            string path = "";
            if (type == "con") path = "file://" + KSPUtil.ApplicationRootPath.Replace("\\", "/") + "PluginData/rbr_chatterer/ksp_chatter_con_" + counter.ToString("D2") + ".ogg";
            else path = "file://" + KSPUtil.ApplicationRootPath.Replace("\\", "/") + "PluginData/rbr_chatterer/ksp_chatter_pod_" + counter.ToString("D2") + ".ogg";

            AudioSource chatter = gameObject.AddComponent<AudioSource>();
            WWW www_chatter = new WWW(path);
            if (chatter != null && www_chatter != null)
            {
                chatter.clip = www_chatter.GetAudioClip(false);
                chatter.volume = chatter_vol_slider;
                chatter.Stop();
                if (type == "con") all_con_chatter.Add(chatter);
                else all_pod_chatter.Add(chatter);
            }
            else print("Failed to load sound " + www_chatter.url);
        }

        private void load_all_chatter()
        {
            int i;
            for (i = 1; i <= total_con_clips; i++)
            {
                load_chatter_clip("con", i);
            }
            for (i = 1; i <= total_pod_clips; i++)
            {
                load_chatter_clip("pod", i);
            }
            print("sounds loaded: " + (all_pod_chatter.Count + all_con_chatter.Count).ToString());
        }

        private void load_beep_clip(int counter)
        {
            AudioSource beep = gameObject.AddComponent<AudioSource>();
            string path = "file://" + KSPUtil.ApplicationRootPath.Replace("\\", "/") + "PluginData/rbr_chatterer/ksp_beep_" + counter.ToString("D2") + ".ogg";
            WWW www_beep = new WWW(path);

            if (beep != null && www_beep != null)
            {
                beep.clip = www_beep.GetAudioClip(false);
                beep.volume = beep_vol_slider;
                beep.Stop();
                all_beep_clips.Add(beep);
            }
            else print("Failed to load sound " + www_beep.url);
        }

        private void load_all_beeps()
        {
            int i;
            for (i = 1; i <= total_beep_clips; i++)
            {
                load_beep_clip(i);
            }
            print("sounds loaded: " + (all_pod_chatter.Count + all_con_chatter.Count).ToString());
        }

        private void set_new_delay_between_exchanges()
        {
            if (chatter_freq == 1) secs_between_exchanges = rand.Next(180, 300);
            else if (chatter_freq == 2) secs_between_exchanges = rand.Next(90, 180);
            else if (chatter_freq == 3) secs_between_exchanges = rand.Next(60, 90);
            else if (chatter_freq == 4) secs_between_exchanges = rand.Next(30, 60);
            else if (chatter_freq == 5) secs_between_exchanges = rand.Next(10, 30);
            print("new delay between exchanges: " + secs_between_exchanges.ToString("F0"));
        }

        private void initialize_new_exchange()
        {
            set_new_delay_between_exchanges();
            secs_since_last_exchange = 0;
            secs_since_initial_chatter = 0;
            current_con_clip = rand.Next(0, all_con_chatter.Count); // select a new con clip to play
            current_pod_clip = rand.Next(0, all_pod_chatter.Count); // select a new pod clip to play
            response_delay_secs = rand.Next(2, 5);  // select another random int to set response delay time

            if (pod_begins_exchange) initial_chatter_source = 1;    //pod_begins_exchange set true OnUpdate when staging and on event change
            else initial_chatter_source = rand.Next(0, 2);   //if i_c_s == 0, con sends first message; if i_c_S == 1, pod sends first message

            if (initial_chatter_source == 0)
            {
                initial_chatter_set = all_con_chatter;
                response_chatter_set = all_pod_chatter;
                initial_chatter_index = current_con_clip;
                response_chatter_index = current_pod_clip;
            }
            else
            {
                initial_chatter_set = all_pod_chatter;
                response_chatter_set = all_con_chatter;
                initial_chatter_index = current_pod_clip;
                response_chatter_index = current_con_clip;
            }
        }

        private void begin_exchange(ulong delay)
        {
            exchange_playing = true;
            initialize_new_exchange();
            initial_chatter_set[initial_chatter_index].Play(delay);
            print("playing initial chatter, initial_chatter_source: " + initial_chatter_source.ToString());
        }

        private void load_settings()
        {
            if (KSP.IO.File.Exists<rbr_chatterer>("rbr_chatterer.cfg", null))
            {
                string[] data = KSP.IO.File.ReadAllLines<rbr_chatterer>("rbr_chatterer.cfg", null);
                if (data.Length == 2)
                {
                    string temp;
                    string[] data_split;

                    temp = data[0];
                    data_split = temp.Split(',');
                    if (data_split.Length == 6)
                    {
                        window_0_pos = new Rect(Convert.ToSingle(data_split[0]), Convert.ToSingle(data_split[1]), 10, 10);
                        main_gui_minimized = Convert.ToBoolean(data_split[2]);
                        chatter_freq = Convert.ToInt32(data_split[3]);
                        chatter_freq_slider = Convert.ToSingle(data_split[3]);
                        chatter_vol_slider = Convert.ToSingle(data_split[4]);
                        insta_chatter_key = (KeyCode)Enum.Parse(typeof(KeyCode), data_split[5]);
                    }

                    temp = data[1];
                    data_split = temp.Split(',');
                    if (data_split.Length == 4)
                    {
                        beep_freq = Convert.ToInt32(data_split[0]);
                        beep_freq_slider = Convert.ToSingle(data_split[0]);
                        beep_vol_slider = Convert.ToSingle(data_split[1]);
                        beep_pitch_slider = Convert.ToSingle(data_split[2]);
                        current_beep_clip = Convert.ToInt32(data_split[3]);
                    }
                }
            }

            bool icons_loaded = false;
            WWW ui_icon_off_img = new WWW("file://" + KSPUtil.ApplicationRootPath.Replace("\\", "/") + "PluginData/rbr_chatterer/chatterer_icon_off.png");
            WWW ui_icon_on_img = new WWW("file://" + KSPUtil.ApplicationRootPath.Replace("\\", "/") + "PluginData/rbr_chatterer/chatterer_icon_on.png");

            while (icons_loaded == false)
            {
                if (ui_icon_off_img.isDone && ui_icon_on_img.isDone)
                {
                    if (ui_icon_off_img.error == null && ui_icon_on_img.error == null)
                    {
                        //no errors, load local textures
                        print("ui icon textures loaded OK");
                        ui_icon_off_img.LoadImageIntoTexture(ui_icon_off);
                        ui_icon_on_img.LoadImageIntoTexture(ui_icon_on);
                        ui_icons_loaded = true;
                        ui_icon_pos = new Rect((Screen.width / 2) - 285, Screen.height - 32, 30, 30);
                        if (chatter_freq == 0) ui_icon = ui_icon_off;
                        else ui_icon = ui_icon_on;
                    }
                    else
                    {
                        //errors loading local textures
                        print("Errors loading local textures, using ugly button");
                        ui_icons_loaded = false;
                        ui_icon_pos = new Rect((Screen.width / 2) - 320, Screen.height - 22, 70, 20);
                    }
                    icons_loaded = true;
                }
            }
        }

        private void write_settings()
        {
            string settings = "";
            settings += window_0_pos.x + "," + window_0_pos.y + "," + main_gui_minimized + "," + chatter_freq + "," + chatter_vol_slider.ToString("F2") + "," + insta_chatter_key + "\n";
            settings += beep_freq + "," + beep_vol_slider.ToString("F2") + "," + beep_pitch_slider.ToString("F2") + "," + current_beep_clip + "\n";
            KSP.IO.File.WriteAllText<rbr_chatterer>(settings, "rbr_chatterer.cfg", null);
        }

        private void main_gui(int window_id)
        {
            GUILayout.BeginVertical();

            //GUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
            //FlightState fs = new FlightState();
            //GUILayout.Label("Universal Time:" + fs.universalTime.ToString());
            //GUILayout.EndHorizontal();

            if (vessel.GetCrewCapacity() == 0)
            {
                GUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
                beep_freq_slider = GUILayout.HorizontalSlider(beep_freq_slider, 1, 61f, GUILayout.ExpandWidth(true));
                GUILayout.EndHorizontal();

                beep_freq = Convert.ToInt32(Math.Round(beep_freq_slider));
                string beep_freq_str = "";
                if (beep_freq == 61)
                {
                    ui_icon = ui_icon_off;
                    beep_freq_str = "No beeps";
                }
                else
                {
                    ui_icon = ui_icon_on;
                    beep_freq_str = "Every " + beep_freq.ToString() + "s";
                }

                GUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
                GUILayout.Label("Beep frequency: " + beep_freq_str, label_txt_left, GUILayout.ExpandWidth(true));
                GUILayout.EndHorizontal();

                if (beep_freq != prev_beep_freq)
                {
                    print("beep_freq has changed, resetting beep_timer...");
                    beep_timer = 0;
                    prev_beep_freq = beep_freq;
                }

                GUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
                beep_vol_slider = GUILayout.HorizontalSlider(beep_vol_slider, 0, 1f, GUILayout.ExpandWidth(true));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
                GUILayout.Label("Beep volume: " + (beep_vol_slider * 100).ToString("F0") + "%", label_txt_left, GUILayout.ExpandWidth(true));
                GUILayout.EndHorizontal();

                if (beep_vol_slider != prev_beep_vol_slider)
                {
                    print("Beep volume has been changed, changing volume for all beeps...");
                    foreach (AudioSource aud in all_beep_clips) aud.volume = beep_vol_slider;
                    prev_beep_vol_slider = beep_vol_slider;
                }

                GUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
                beep_pitch_slider = GUILayout.HorizontalSlider(beep_pitch_slider, 0.1f, 5f, GUILayout.ExpandWidth(true));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
                GUILayout.Label("Beep pitch: " + beep_pitch_slider.ToString("F2"), label_txt_left, GUILayout.ExpandWidth(true));
                GUILayout.EndHorizontal();

                if (beep_pitch_slider != prev_beep_pitch_slider)
                {
                    print("Beep pitch has been changed, changing pitch for all beeps...");
                    foreach (AudioSource aud in all_beep_clips) aud.pitch = beep_pitch_slider;
                    prev_beep_pitch_slider = beep_pitch_slider;
                }

                //GUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
                //if (GUILayout.Button("Change", GUILayout.ExpandWidth(false)))
                //{
                //    current_beep_clip++;
                //    if (current_beep_clip >= total_beep_clips) current_beep_clip = 0;
                //}
                //GUILayout.Label("Current beep: ", label_txt_left);
                //GUILayout.Label(current_beep_clip.ToString(), label_txt_right);
                //GUILayout.EndHorizontal();

            }
            else if (vessel.GetCrewCapacity() > 0)
            {
                GUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
                chatter_freq_slider = GUILayout.HorizontalSlider(chatter_freq_slider, 0, 5f, GUILayout.ExpandWidth(true));
                GUILayout.EndHorizontal();

                chatter_freq = Convert.ToInt32(Math.Round(chatter_freq_slider));
                string chatter_freq_str = "";
                if (chatter_freq == 0)
                {
                    ui_icon = ui_icon_off;
                    chatter_freq_str = "No chatter";
                }
                else
                {
                    ui_icon = ui_icon_on;
                    if (chatter_freq == 1) chatter_freq_str = "Minimal (180-300s)";
                    else if (chatter_freq == 2) chatter_freq_str = "Occasional (90-180)";
                    else if (chatter_freq == 3) chatter_freq_str = "Average (60-90s)";
                    else if (chatter_freq == 4) chatter_freq_str = "Often (30-60s)";
                    else if (chatter_freq == 5) chatter_freq_str = "Excessive (10-30s)";
                }

                GUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
                GUILayout.Label("Chatter frequency: " + chatter_freq_str, label_txt_left, GUILayout.ExpandWidth(true));
                GUILayout.EndHorizontal();

                if (chatter_freq != prev_chatter_freq)
                {
                    print("chatter_freq has changed, setting new delay between exchanges...");
                    secs_since_last_exchange = 0;
                    set_new_delay_between_exchanges();
                    prev_chatter_freq = chatter_freq;
                }

                GUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
                chatter_vol_slider = GUILayout.HorizontalSlider(chatter_vol_slider, 0, 1f, GUILayout.ExpandWidth(true));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
                GUILayout.Label("Chatter volume: " + (chatter_vol_slider * 100).ToString("F0") + "%", label_txt_left, GUILayout.ExpandWidth(true));
                GUILayout.EndHorizontal();

                if (chatter_vol_slider != prev_chatter_vol_slider)
                {
                    print("Chatter volume has been changed, changing volume for all sounds...");
                    foreach (AudioSource aud in all_con_chatter) aud.volume = chatter_vol_slider;
                    foreach (AudioSource aud in all_pod_chatter) aud.volume = chatter_vol_slider;
                    prev_chatter_vol_slider = chatter_vol_slider;
                }

                GUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
                if (set_insta_chatter_key == false)
                {
                    if (GUILayout.Button("Change", GUILayout.ExpandWidth(false))) set_insta_chatter_key = true;
                }
                GUILayout.Label("Insta-chatter key: ", label_txt_left);
                GUILayout.Label(insta_chatter_key.ToString(), label_txt_right);
                GUILayout.EndHorizontal();

                if (set_insta_chatter_key)
                {
                    GUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
                    GUILayout.Label("Press new Insta-chatter key...", label_txt_left);
                    GUILayout.EndHorizontal();
                }

                if (set_insta_chatter_key && Event.current.isKey)
                {
                    insta_chatter_key = Event.current.keyCode;
                    set_insta_chatter_key = false;
                    key_just_changed = true;
                }
            }

            if (recvd_latest_version && latest_version != "")
            {
                GUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
                GUILayout.Label(latest_version, label_txt_left);
                GUILayout.EndHorizontal();
            }
            
            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        protected void draw_GUI()
        {
            if (vessel.isActiveVessel)
            {
                GUI.skin = HighLogic.Skin;
                if (gui_styles_set == false) set_gui_styles();

                if (ui_icons_loaded == true)
                {
                    if (GUI.Button(new Rect(ui_icon_pos), ui_icon, new GUIStyle())) main_gui_minimized = !main_gui_minimized;
                }
                else
                {
                    if (GUI.Button(new Rect(ui_icon_pos), "Chatterer", GUI.skin.button)) main_gui_minimized = !main_gui_minimized;
                }

                if (main_gui_minimized == false) window_0_pos = GUILayout.Window(-526925713, window_0_pos, main_gui, "Chatterer " + this_version, GUILayout.Width(260), GUILayout.Height(50));
            }
        }

        public override void OnStart(PartModule.StartState state)
        {
            if (state != StartState.Editor && vessel == FlightGlobals.ActiveVessel)
            {
                part.force_activate();
                check_active_chatterer();
                get_latest_version();
                load_settings();    // must run once before loading chatter to get volume level
                vessel_prev_sit = vessel.situation;
                vessel_prev_stage = vessel.currentStage;
                if (vessel.GetCrewCapacity() == 0) load_all_beeps();
                else
                {
                    load_all_chatter();
                    initialize_new_exchange();
                }
            }
        }

        public override void OnUpdate()
        {
            if (vessel == FlightGlobals.ActiveVessel)
            {
                check_active_chatterer();
                if (active_chatterer)
                {
                    cfg_update_timer += Time.deltaTime;
                    if (cfg_update_timer >= 5f)
                    {
                        write_settings();
                        cfg_update_timer = 0f;
                    }

                    if (gui_running == false) start_GUI();

                    if (vessel.GetCrewCapacity() == 0 && beep_freq != 61)   //unmanned craft and beep freq is not OFF (61)
                    {
                        beep_timer += Time.deltaTime;
                        if (beep_timer > beep_freq)
                        {
                            beep_timer = 0;
                            all_beep_clips[current_beep_clip].Play();
                        }
                    }
                    else if (vessel.GetCrewCapacity() > 0)
                    {
                        if (key_just_changed && Input.GetKeyUp(insta_chatter_key)) key_just_changed = false;

                        if (key_just_changed == false && Input.GetKeyDown(insta_chatter_key) && exchange_playing == false)
                        {
                            print("beginning exchange,insta-chatter");
                            begin_exchange(0);
                        }

                        if (vessel.GetCrewCount() > 0 && chatter_freq > 0)
                        {
                            if (exchange_playing == false)
                            {
                                secs_since_last_exchange += Time.deltaTime;
                                if (secs_since_last_exchange > secs_between_exchanges)
                                {
                                    print("beginning exchange,auto");
                                    begin_exchange(0);
                                }
                            }
                        }

                        if (vessel.currentStage != vessel_prev_stage && exchange_playing == false)
                        {
                            print("beginning exchange,staging");
                            pod_begins_exchange = true;
                            begin_exchange(Convert.ToUInt64(44100 * rand.Next(0, 3)));  //delay Play for 0-2 seconds for randonmess
                        }

                        if (vessel.situation != vessel_prev_sit && exchange_playing == false)
                        {
                            //print("vessel.situation: " + vessel.situation.ToString() + " ::: vessel_prev_sit: " + vessel_prev_sit.ToString());
                            print("beginning exchange,event");
                            pod_begins_exchange = true;
                            begin_exchange(Convert.ToUInt64(44100 * rand.Next(0, 3)));  //delay Play for 0-2 seconds for randonmess
                        }

                        if (exchange_playing)
                        {
                            if (initial_chatter_set[initial_chatter_index].isPlaying == false)
                            {
                                secs_since_initial_chatter += Time.deltaTime;
                                if (secs_since_initial_chatter > response_delay_secs)
                                {
                                    print("response delay has elapsed...");
                                    if (response_chatter_set[response_chatter_index].isPlaying == false)
                                    {
                                        //play response clip if not already playing
                                        response_chatter_set[response_chatter_index].Play();
                                        print("playing response chatter...");
                                        exchange_playing = false;
                                    }
                                }
                            }
                        }
                    }
                }
                vessel_prev_sit = vessel.situation;
                vessel_prev_stage = vessel.currentStage;
            }
        }
    }
}
