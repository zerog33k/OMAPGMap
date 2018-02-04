using Android.App;
using Android.Widget;
using Android.OS;
using Android.Support.V4.App;
using Android.Gms.Maps;
using Android.Gms.Maps.Model;
using Android.Support.V4.Content;
using Android;
using Android.Content.PM;
using Android.Locations;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Android.Views;
using Android.Support.V7.App;
using Android.Support.V7.Widget;
using System;
using System.Threading.Tasks;
using Android.Preferences;
using Android.Content;
using System.Linq;
using Android.Graphics;
using OMAPGMap.Models;
using Android.Views.InputMethods;
using System.Collections.Generic;
using MoreLinq;
using System.Threading;
using static Android.Gms.Maps.GoogleMap;

namespace OMAPGMap.Droid
{
    [Activity(Label = "OMA PGMap", MainLauncher = true, Icon = "@mipmap/ic_launcher", WindowSoftInputMode = SoftInput.AdjustResize)]
    public class MainActivity : AppCompatActivity, IOnMapReadyCallback, IInfoWindowAdapter
    {
        MapFragment _mapFragment = null;
        GoogleMap map = null;

        private LatLng mDefaultLocation = new LatLng(41.2524, -95.9980); //default location middle of omaha
        private int defaultZoom = 14;
        private int request_fine_location = 1;
        private bool mLocationPermissionGranted;
        private Location lastKnownLocation;
        private bool centeredMap = false;
        private EditText username;
        private EditText password;
        private CardView loginHolder;
        private  ISharedPreferences prefs;
        private CardView settingsHolder;
        private TextView loginMessage;

        public static int NumPokes = ServiceLayer.NumberPokemon;
        private int[] pokeResourceMap = new int[NumPokes+1];
        private List<Pokemon> PokesOnMap = new List<Pokemon>();
        private List<Pokemon> PokesVisible = new List<Pokemon>();

        private List<Gym> gymsOnMap = new List<Gym>();
        private List<Gym> gymssVisible = new List<Gym>();

        private List<Raid> raidsOnMap = new List<Raid>();
        private List<Raid> raidsVisible = new List<Raid>();

        private ListView settingsListview;

        private Timer secondTimer;
        private Timer minuteTimer;
        private UserSettings settings => ServiceLayer.SharedInstance.Settings;

        private Location userLocation;

        readonly string[] PermissionsLocation =
        {
            Manifest.Permission.AccessCoarseLocation,
            Manifest.Permission.AccessFineLocation
        };

        protected override async void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Window.RequestFeature(WindowFeatures.NoTitle);
            AppCenter.Start("7ac229ee-9940-46b8-becc-d2611c48b2ad", typeof(Analytics), typeof(Crashes));

            prefs = PreferenceManager.GetDefaultSharedPreferences(this);
            await ServiceLayer.SharedInstance.InitalizeSettings();

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            for (int i = 1; i <= NumPokes; i++)
            {
                try
                {
                    pokeResourceMap[i] = (int)typeof(Resource.Mipmap).GetField($"p{i.ToString("d3")}").GetValue(null);
                }catch(Exception e)
                {
                    Console.WriteLine($"poke {i} not found");
                }
            }
            _mapFragment = FragmentManager.FindFragmentByTag("map") as MapFragment;
            if (_mapFragment == null)
            {
                GoogleMapOptions mapOptions = new GoogleMapOptions()
                    .InvokeMapType(GoogleMap.MapTypeNormal)
                    .InvokeZoomControlsEnabled(false)
                    .InvokeCompassEnabled(true);    

                Android.App.FragmentTransaction fragTx = FragmentManager.BeginTransaction();
                _mapFragment = MapFragment.NewInstance(mapOptions);
                fragTx.Add(Resource.Id.map, _mapFragment, "map");
                fragTx.Commit();
            }

            loginHolder = FindViewById(Resource.Id.loginHolder) as CardView;
            username = FindViewById(Resource.Id.username) as EditText;
            if(settings.LoggedIn)
            {
                loginHolder.Visibility = ViewStates.Gone;
                _mapFragment.GetMapAsync(this);
                var imm = GetSystemService(Context.InputMethodService) as InputMethodManager;
                imm.HideSoftInputFromWindow(username.WindowToken, 0);
                await LoadData();
                secondTimer = new Timer(HandleTimerCallback, null, 5000, 5000);
                minuteTimer = new Timer(refreshMap, null, 60000, 60000);
            } else 
            {
                
                password = FindViewById(Resource.Id.password) as EditText;
                username.RequestFocus();
                _mapFragment.GetMapAsync(this);
            }

            var loginButton = FindViewById(Resource.Id.signInButton) as Button;
            loginButton.Click += LoginButton_Click;
            var settingButton = FindViewById(Resource.Id.settingsButton);
            settingButton.Click += SettingButton_Click;
            var layerButton = FindViewById(Resource.Id.layerssButton);
            layerButton.Click += LayerButton_Click;
            settingsHolder = FindViewById(Resource.Id.settingsHolder) as CardView;
            var settingsDone = settingsHolder.FindViewById(Resource.Id.settingsDoneButton);
            settingsDone.Click += SettingsDone_Click;

            settingsListview = FindViewById(Resource.Id.settingsListView) as ListView;
            settingsListview.Adapter = new SettingsAdaptor(this, pokeResourceMap);
        }

        private void refreshMap(object state)
        {
            RunOnUiThread(async () =>
            {
                await ServiceLayer.SharedInstance.LoadData();
                UpdateMapPokemon(false);
                UpdateMapGyms(true);
                updateMapRaids(true);
            });
        }

        private void HandleTimerCallback(object state)
        {
            RunOnUiThread(() =>
            {
                UpdateMapPokemon(false);
            });

        }

        private async Task LoadData()
        {
            var progress = new ProgressDialog(this);  
            progress.Indeterminate = true;  
            progress.SetProgressStyle(ProgressDialogStyle.Spinner);  
            progress.SetMessage("Loading...");  
            progress.SetCancelable(false);  
            progress.Show();

            await ServiceLayer.SharedInstance.LoadData();
            progress.Dismiss();

            UpdateMapPokemon(true);
            UpdateMapGyms(true);
            updateMapRaids(true);
        }

        private void updateMapRaids(bool reload)
        {
            var bounds = map.Projection.VisibleRegion.LatLngBounds;
            List<Raid> toRemove = new List<Raid>();
            if (reload)
            {
                toRemove.AddRange(raidsOnMap);
            }
            else
            {
                toRemove.AddRange(raidsOnMap.Where((Raid rd) =>
                {
                    if(!bounds.Contains(rd.Location))
                    {
                        return true;
                    }
                    if(rd.TimeEnd > DateTime.UtcNow)
                    {
                        return true;
                    }
                    if(rd.TimeBattle > DateTime.Now && rd.pokemon_id == 0)
                    {
                        return true;
                    }
                    return false;
                }));
                foreach (var r in toRemove)
                {
                    r.RaidMarker.Remove();
                    r.RaidMarker = null;
                    raidsOnMap.Remove(r);
                }
                List<Raid> toAdd = new List<Raid>();
                toAdd.AddRange(ServiceLayer.SharedInstance.Raids.Where(r => bounds.Contains(r.Location)).Except(raidsOnMap));
                foreach (var r in toAdd)
                {
                    AddRaidMarker(r);
                }
                Console.WriteLine($"Adding {toAdd.Count()} raids to the map");
            }
        }

        private void UpdateMapGyms(bool reload)
        {
            var bounds = map.Projection.VisibleRegion.LatLngBounds;
            List<Gym> toRemove = new List<Gym>();
            if (reload)
            {
                toRemove.AddRange(gymsOnMap);
            }
            else
            {
                toRemove.AddRange(gymsOnMap.Where(p => !bounds.Contains(p.Location)));
            }
            foreach (var g in toRemove)
            {
                g.GymMarker.Remove();
                g.GymMarker = null;
                gymsOnMap.Remove(g);
            }
            List<Gym> toAdd = new List<Gym>();
            toAdd.AddRange(ServiceLayer.SharedInstance.Gyms.Values.Where(g => bounds.Contains(g.Location)).Except(gymsOnMap));
            foreach(var g in toAdd)
            {
                AddGymMarker(g);
            }
            Console.WriteLine($"Adding {toAdd.Count()} gyms to the map");
        }

        private async void RefreshData()
        {
            try
            {
                await ServiceLayer.SharedInstance.LoadData();
            }
            catch (Exception)
            {
                //swallow exception because it tastes good
            }
            UpdateMapPokemon(false);
            updateMapRaids(false);
            UpdateMapGyms(false);
        }

        private void UpdateMapPokemon(bool reload)
        {
            var bounds = map.Projection.VisibleRegion.LatLngBounds;
            List<Pokemon> toRemove = new List<Pokemon>();
            if(reload)
            {
                toRemove.AddRange(PokesOnMap);
            } else 
            {
                toRemove.AddRange(PokesOnMap.Where(p => !bounds.Contains(p.Location) || p.ExpiresDate < DateTime.UtcNow));
            }
            foreach (var p in toRemove)
            {
                if (p != null) // shouldn't have to do this
                {
                    p.PokeMarker.Remove();
                    p.PokeMarker = null;
                    PokesOnMap.Remove(p);
                }
            }
            List<Pokemon> toAdd = new List<Pokemon>();
            if (settings.NinetyOnlyEnabled)
            {
                toAdd.AddRange(ServiceLayer.SharedInstance.Pokemon.Where(p => p.iv > 0.9).Except(PokesOnMap));
            }
            if(settings.PokemonEnabled)
            {
                toAdd.AddRange(ServiceLayer.SharedInstance.Pokemon.Where(p => !settings.PokemonTrash.Contains(p.pokemon_id)).Except(PokesOnMap));
            }

            var visible = toAdd.Where(p => bounds.Contains(p.Location));
            foreach (var p in visible)
            {
                AddPokemonMarker(p);
            }
            PokesVisible.AddRange(toAdd.Except(visible));
            Console.WriteLine($"Adding {visible.Count()} mons to the map");
            var now = DateTime.UtcNow;
            foreach(var p in PokesOnMap)
            {
                var diff = p.ExpiresDate - now;
                if (diff.Minutes < 1)
                {
                    p.PokeMarker.Alpha = diff.Seconds / 60.0f;
                }
            }
        }


        private void AddPokemonMarker(Pokemon p)
        {
            var mOps = new MarkerOptions();
            mOps.SetPosition(p.Location);

            mOps.SetIcon(BitmapDescriptorFactory.FromBitmap(GetPokemonMarker(p)));
            mOps.Anchor(0.5f, 0.5f);
            var marker = map.AddMarker(mOps);
            marker.Tag = $"poke:{p.id}";
            p.PokeMarker = marker;
            PokesOnMap.Add(p);
        }

        private void AddGymMarker(Gym g)
        {
            var mOps = new MarkerOptions();
            mOps.SetPosition(g.Location);
            int img = 0;
            switch(g.team)
            {
                case Team.Mystic:
                    img = Resource.Mipmap.mystic;
                    break;
                case Team.Valor:
                    img = Resource.Mipmap.valor;
                    break;
                case Team.Instinct:
                    img = Resource.Mipmap.instinct;
                    break;
                case Team.None:
                    img = Resource.Mipmap.empty;
                    break;
            }

            mOps.SetIcon(BitmapDescriptorFactory.FromResource(img));
            mOps.Anchor(0.5f, 0.5f);
            var marker = map.AddMarker(mOps);
            marker.Tag = $"gym:{g.id}";
            g.GymMarker = marker;
            gymsOnMap.Add(g);
        }

        private void AddRaidMarker(Raid r)
        {
            var mOps = new MarkerOptions();
            mOps.SetPosition(r.Location);

            mOps.SetIcon(BitmapDescriptorFactory.FromBitmap(GetRaidMarker(r)));
            mOps.Anchor(0.5f, 0.5f);
            var marker = map.AddMarker(mOps);
            marker.Tag = $"raid:{r.id}";
            r.RaidMarker = marker;
            raidsOnMap.Add(r);
        }
        public void OnMapReady(GoogleMap mapp)
        {
            map = mapp;
            var camUpdate = CameraUpdateFactory.NewLatLngZoom(mDefaultLocation, defaultZoom);
            map.MoveCamera(camUpdate);
            DisplayUserLocation();

            map.MyLocationChange += (sender, e) => 
            {
                if(!centeredMap)
                {
                    centeredMap = true;
                    var cam = CameraUpdateFactory.NewLatLngZoom(new LatLng(e.Location.Latitude, e.Location.Longitude), defaultZoom);
                    map.MoveCamera(cam);
                }
                userLocation = e.Location;
            };
            map.SetInfoWindowAdapter(this);
            map.CameraIdle += Map_CameraIdle;
        }

        void Map_CameraIdle(object sender, EventArgs e)
        {
            UpdateMapPokemon(false);
            updateMapRaids(false);
            UpdateMapGyms(false);
        }

        private void RequestLocation()
        {
            if(ContextCompat.CheckSelfPermission(ApplicationContext, Android.Manifest.Permission.AccessFineLocation) == Permission.Granted)
            {
                mLocationPermissionGranted = true;
                DisplayUserLocation();
            } else 
            {
                RequestPermissions(PermissionsLocation, 0);
            }
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            if (requestCode == 0 && grantResults.Length > 0 && grantResults[0] == Permission.Granted)
            {
                mLocationPermissionGranted = true;
                DisplayUserLocation();
            }
        }

        public void DisplayUserLocation()
        {
            if(map == null)
            {
                return;
            }
            if(mLocationPermissionGranted)
            {
                map.MyLocationEnabled = true;
                map.UiSettings.MyLocationButtonEnabled = true;

            } else
            {
                map.MyLocationEnabled = false;
                map.UiSettings.MyLocationButtonEnabled = true;

                RequestLocation();
            }

        }

        Bitmap GetPokemonMarker(Pokemon poke)
        {
            var mapMarker = ((LayoutInflater)GetSystemService(Context.LayoutInflaterService)).Inflate(Resource.Layout.map_marker, null);
            var img = mapMarker.FindViewById(Resource.Id.marker_img) as ImageView;
            try
            {
                img.SetImageResource(pokeResourceMap[poke.pokemon_id]);
            }catch(Exception){}
            var imgTitle = mapMarker.FindViewById(Resource.Id.marker_text) as TextView;
            imgTitle.Text = poke.ExpiresDate.AddHours(-6.0).ToShortTimeString();
            if(poke.iv > 0.99)
            {
                var purple = Resources.GetDrawable(Resource.Drawable.rounded_corner_purple);
                imgTitle.Background = purple;
            } else if(poke.iv > 0.9)
            {
                var green = Resources.GetDrawable(Resource.Drawable.rounded_corner_green);
                imgTitle.Background = green;
            }
            mapMarker.Measure(0, 0);
            mapMarker.Layout(0, 0, mapMarker.MeasuredWidth, mapMarker.MeasuredHeight);
            mapMarker.BuildDrawingCache();
            var rval = Bitmap.CreateBitmap(mapMarker.MeasuredWidth, mapMarker.MeasuredHeight, Bitmap.Config.Argb8888);
            var canvas = new Canvas(rval);
            canvas.DrawColor(Color.White, PorterDuff.Mode.SrcIn);
            mapMarker.Draw(canvas);
            return rval;
        }

        Bitmap GetRaidMarker(Raid raid)
        {
            var mapMarker = ((LayoutInflater)GetSystemService(Context.LayoutInflaterService)).Inflate(Resource.Layout.map_marker_raid, null);
            var img = mapMarker.FindViewById(Resource.Id.marker_egg_img) as ImageView;
            switch(raid.level)
            {
                case 1:
                    img.SetImageResource(Resource.Mipmap.egg1);
                    break;
                case 2:
                    img.SetImageResource(Resource.Mipmap.egg2);
                    break;
                case 3:
                    img.SetImageResource(Resource.Mipmap.egg3);
                    break;
                case 4:
                    img.SetImageResource(Resource.Mipmap.egg4);
                    break;
                case 5:
                    img.SetImageResource(Resource.Mipmap.egg5);
                    break;

            }
            var now = DateTime.UtcNow;
            DateTime displayTime = raid.TimeBattle;
            var pokeImg = mapMarker.FindViewById(Resource.Id.marker_raid_poke) as ImageView;
            if (raid.pokemon_id != 0 || now < raid.TimeEnd)
            {
                displayTime = raid.TimeEnd;
                try
                {
                    pokeImg.SetImageResource(pokeResourceMap[raid.pokemon_id]);
                }
                catch (Exception) { pokeImg.Visibility = ViewStates.Gone; }
            } else {
                pokeImg.Visibility = ViewStates.Gone;
            }
            var imgTitle = mapMarker.FindViewById(Resource.Id.marker_text) as TextView;

            imgTitle.Text = displayTime.AddHours(-6.0).ToShortTimeString();
            mapMarker.Measure(0, 0);
            mapMarker.Layout(0, 0, mapMarker.MeasuredWidth, mapMarker.MeasuredHeight);
            mapMarker.BuildDrawingCache();
            var rval = Bitmap.CreateBitmap(mapMarker.MeasuredWidth, mapMarker.MeasuredHeight, Bitmap.Config.Argb8888);
            var canvas = new Canvas(rval);
            canvas.DrawColor(Color.White, PorterDuff.Mode.SrcIn);
            mapMarker.Draw(canvas);
            return rval;
        }

        async void LoginButton_Click(object sender, EventArgs e)
        {
            settings.Username = username.Text;
            settings.Password = password.Text;
            var loggedIn = await ServiceLayer.SharedInstance.VerifyCredentials();
            if(loggedIn)
            {
                loginHolder.Visibility = ViewStates.Gone;
                await ServiceLayer.SharedInstance.SaveSettings();
                await LoadData();
            } else 
            {
                Android.Support.V7.App.AlertDialog.Builder alert = new Android.Support.V7.App.AlertDialog.Builder(this);
                alert.SetTitle("Error Logging In");
                alert.SetMessage("Username or password incorrect");
                alert.SetPositiveButton("Ok", (senderAlert, args) => { });

                Dialog dialog = alert.Create();
                dialog.Show();
            }
        }

        private View pokemonInfo = null;
        private View gymInfo = null;
        private View raidInfo = null;

        private Marker currentMarker;

        public View GetInfoContents(Marker marker)
        {
            View infoView = null;
            var markerTag = marker.Tag.ToString().Split(':');
            var t = markerTag[0];
            var id = markerTag[1];
            var l = new Location("");
            l.Latitude = marker.Position.Latitude;
            l.Longitude = marker.Position.Longitude;

            var distText = "";
            if (userLocation != null)
            {
                var dist = (userLocation?.DistanceTo(l) ?? 0) * 0.000621371;
                distText = $"{dist.ToString("F1")} miles away";
            }
            else
            {
                distText = "Dist unknown";
            }
            if (t == "poke") 
            {
                Button notifyButton;
                if (pokemonInfo == null)
                {
                    pokemonInfo = ((LayoutInflater)GetSystemService(Context.LayoutInflaterService)).Inflate(Resource.Layout.pokemon_info, null);
                    var hideButton = pokemonInfo.FindViewById(Resource.Id.hide_button) as Button;
                    notifyButton = pokemonInfo.FindViewById(Resource.Id.notify_button) as Button;
                    hideButton.Touch += HideButton_Touch;
                }
                infoView = pokemonInfo;
                var title = pokemonInfo.FindViewById(Resource.Id.info_title) as TextView;
                var distLabel = pokemonInfo.FindViewById(Resource.Id.info_distance) as TextView;
                var move1Label = pokemonInfo.FindViewById(Resource.Id.info_move1) as TextView;
                var move2Label = pokemonInfo.FindViewById(Resource.Id.info_move2) as TextView;
                var ivLabel = pokemonInfo.FindViewById(Resource.Id.info_iv) as TextView;
                var cpLabel = pokemonInfo.FindViewById(Resource.Id.info_cp_level) as TextView;
                var poke = ServiceLayer.SharedInstance.Pokemon.Where(p => p.id == id).FirstOrDefault();
                var infoTitle = $"{poke.name} ({poke.gender}) - #{poke.pokemon_id}";
                if (!string.IsNullOrEmpty(poke.move1))
                {
                    var iv = poke.iv * 100;
                    infoTitle= $"{infoTitle} - {iv.ToString("F1")}%";
                }
                title.Text = infoTitle;
                distLabel.Text = distText;
                if(!string.IsNullOrEmpty(poke.move1))
                {
                    move1Label.Visibility = ViewStates.Visible;
                    move2Label.Visibility = ViewStates.Visible;
                    ivLabel.Visibility = ViewStates.Visible;
                    cpLabel.Visibility = ViewStates.Visible;
                    move1Label.Text = $"Move 1: {poke.move1} ({poke.damage1} dps)";
                    move2Label.Text = $"Move 1: {poke.move2} ({poke.damage2} dps)";
                    ivLabel.Text = $"IV: {poke.atk}atk {poke.def}def {poke.sta}sta";
                    cpLabel.Text = $"CP: {poke.cp} Level: {poke.level}";
                } else 
                {
                    move1Label.Visibility = ViewStates.Gone;
                    move2Label.Visibility = ViewStates.Gone;
                    ivLabel.Visibility = ViewStates.Gone;
                    cpLabel.Visibility = ViewStates.Gone;
                }

            } else if(t == "raid")
            {
                if (raidInfo == null)
                {
                    raidInfo = ((LayoutInflater)GetSystemService(Context.LayoutInflaterService)).Inflate(Resource.Layout.raid_info, null);
                }
                var title = raidInfo.FindViewById(Resource.Id.raid_info_title) as TextView;
                var distLabel = raidInfo.FindViewById(Resource.Id.raid_info_distance) as TextView;
                var cpLabel = raidInfo.FindViewById(Resource.Id.raid_info_cp) as TextView;
                var move1 = raidInfo.FindViewById(Resource.Id.raid_info_move1) as TextView;
                var move2 = raidInfo.FindViewById(Resource.Id.raid_info_move2) as TextView;
                var gymName = raidInfo.FindViewById(Resource.Id.raid_info_gym_name) as TextView;
                var gymCtl = raidInfo.FindViewById(Resource.Id.raid_info_gym_control) as TextView;
                var raid = ServiceLayer.SharedInstance.Raids.Where(r => r.id == id).FirstOrDefault();
                title.Text = (raid.pokemon_id == 0) ? $"Upcoming raid level {raid.level}" : $"{raid.pokemon_name} (#{raid.pokemon_id}) Raid - Level {raid.level}";
                distLabel.Text = distText;
                cpLabel.Text = $"CP: {raid.cp}";
                move1.Text = $"Move 1: {raid.move_1}";
                move2.Text = $"Move 1: {raid.move_2}";
                gymName.Text = $"Gym Name: {raid.name}";
                gymCtl.Text = $"Gym Control: {Enum.GetName(typeof(Team), raid.team)}";
                infoView = raidInfo;

            } else if (t == "gym")
            {
                if(gymInfo == null)
                {
                    gymInfo = ((LayoutInflater)GetSystemService(Context.LayoutInflaterService)).Inflate(Resource.Layout.gym_info, null);
                }
                var title = gymInfo.FindViewById(Resource.Id.gym_info_title) as TextView;
                var distLabel = gymInfo.FindViewById(Resource.Id.gym_info_distance) as TextView;
                var modified = gymInfo.FindViewById(Resource.Id.gym_info_mod) as TextView;
                var guarding = gymInfo.FindViewById(Resource.Id.gym_info_guarding) as TextView;
                var slots = gymInfo.FindViewById(Resource.Id.gym_info_slots) as TextView;
                var gym = ServiceLayer.SharedInstance.Gyms[id];
                title.Text = gym.name;
                distLabel.Text = distText;
                modified.Text = $"Last modified {Utility.TimeAgo(gym.LastModifedDate)}";
                slots.Text = $"Slots Available: {gym.slots_available}";
                guarding.Text = $"Guarding Pokemon: {gym.pokemon_name}({gym.pokemon_id})";
                infoView = gymInfo;
            }

            return infoView;
        }

        async void HideButton_Touch(object sender, View.TouchEventArgs e)
        {
            if (e.Event.Action == MotionEventActions.Up)
            {
                var markerTag = currentMarker.Tag.ToString().Split(':');
                var t = markerTag[0];
                var id = markerTag[1];
                var poke = ServiceLayer.SharedInstance.Pokemon.Where(p => p.id == id).FirstOrDefault();
                settings.PokemonTrash.Add(poke.pokemon_id);
                currentMarker.HideInfoWindow();
                await ServiceLayer.SharedInstance.SaveSettings();
                UpdateMapPokemon(true);
            }
        }



        void NotifyButton_Click(object sender, EventArgs e)
        {
            var markerTag = currentMarker.Tag.ToString().Split(':');
            var t = markerTag[0];
            var id = markerTag[1];
            currentMarker.HideInfoWindow();
        }

        public View GetInfoWindow(Marker marker)
        {
            return null;
        }

        void SettingButton_Click(object sender, EventArgs e)
        {
            settingsHolder.Visibility = ViewStates.Visible;
        }

        void LayerButton_Click(object sender, EventArgs e)
        {
            var button = sender as ImageButton;
            var popup = new Android.Support.V7.Widget.PopupMenu(this, button);
            popup.Inflate(Resource.Menu.layers);
            var menu = popup.Menu;
            var item = menu.GetItem(0);
            item.SetChecked(settings.PokemonEnabled);
            item = menu.GetItem(1);
            item.SetChecked(settings.GymsEnabled);
            item = menu.GetItem(2);
            item.SetChecked(settings.RaidsEnabled);
            item = menu.GetItem(3);
            item.SetChecked(settings.NinetyOnlyEnabled);
            popup.MenuItemClick += Popup_MenuItemClick;
            popup.Show();
        }

        async void Popup_MenuItemClick(object sender, Android.Support.V7.Widget.PopupMenu.MenuItemClickEventArgs e)
        {
            var id = e.Item.ItemId;
            if(id == Resource.Id.menu_pokemon)
            {
                settings.PokemonEnabled = !settings.PokemonEnabled;
            } else if(id == Resource.Id.menu_gyms)
            {
                settings.GymsEnabled = !settings.GymsEnabled;
            } else if(id == Resource.Id.menu_raids)
            {
                settings.RaidsEnabled = !settings.RaidsEnabled;
            } else if(id == Resource.Id.menu_90plus)
            {
                settings.NinetyOnlyEnabled = !settings.NinetyOnlyEnabled;
            }
            await ServiceLayer.SharedInstance.SaveSettings();
            UpdateMapPokemon(true);
        }

        async void SettingsDone_Click(object sender, EventArgs e)
        {
            settingsHolder.Visibility = ViewStates.Gone;
            await ServiceLayer.SharedInstance.SaveSettings();
            UpdateMapPokemon(true);
        }
    }
}

    