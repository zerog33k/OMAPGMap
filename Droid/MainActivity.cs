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
using Android.Support.V4.Widget;

namespace OMAPGMap.Droid
{
    [Activity(Label = "OMA PGMap", MainLauncher = true, Icon = "@mipmap/ic_launcher", WindowSoftInputMode = SoftInput.AdjustResize, LaunchMode = LaunchMode.SingleInstance)]
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
        private CardView settingsHolder;
        private CardView loginHolder;

        public static int NumPokes = ServiceLayer.NumberPokemon;
        private int[] pokeResourceMap = new int[NumPokes+1];
        private List<Pokemon> PokesVisible = new List<Pokemon>();

        private List<Gym> gymsVisible = new List<Gym>();

        private List<Raid> raidsVisible = new List<Raid>();

        private ListView settingsListview;

        private Timer secondTimer;
        private Timer minuteTimer;
        private UserSettings settings => ServiceLayer.SharedInstance.Settings;

        private Location userLocation;
        private SaveState currState = null;
        private ProgressBar progress = null;
        DateTime lastUpdate;
        bool currentlyUpdating = false;

        readonly string[] PermissionsLocation =
        {
            Manifest.Permission.AccessCoarseLocation,
            Manifest.Permission.AccessFineLocation
        };

        protected override async void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Console.WriteLine("OnCreate Called");
            Window.RequestFeature(WindowFeatures.NoTitle);
            AppCenter.Start("7ac229ee-9940-46b8-becc-d2611c48b2ad", typeof(Analytics), typeof(Crashes));

            currState = LastCustomNonConfigurationInstance as SaveState;
            SetContentView(Resource.Layout.Main);


            if (currState == null)
            {
                await ServiceLayer.SharedInstance.InitalizeSettings();
                for (int i = 1; i <= NumPokes; i++)
                {
                    try
                    {
                        pokeResourceMap[i] = (int)typeof(Resource.Mipmap).GetField($"p{i.ToString("d3")}").GetValue(null);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"poke {i} not found");
                    }
                }
                Console.WriteLine("No saved state");
            } else 
            {
                pokeResourceMap = currState.ResourceMap;
                Console.WriteLine("saved state recalled");
            }

            lastUpdate = DateTime.UtcNow;
            if(savedInstanceState != null && savedInstanceState.ContainsKey("centerLat") && currState == null)
            {
                var cLat = savedInstanceState.GetDouble("centerLat");
                var cLon = savedInstanceState.GetDouble("centerLon");
                var zoom = savedInstanceState.GetFloat("centerZoom");
                var update = savedInstanceState.GetLong("lastUpdate");
                lastUpdate = Utility.FromUnixTime(update);
                currState = new SaveState()
                {
                    CurrentCenter = new LatLng(cLat, cLon),
                    CurrentZoom = zoom
                };
            }

            _mapFragment = FragmentManager.FindFragmentByTag("map") as MapFragment;
            progress = FindViewById(Resource.Id.progressBar) as ProgressBar;
            if (_mapFragment == null)
            {
                CameraPosition startCam;
                if (currState == null)
                {
                    startCam = new CameraPosition(mDefaultLocation, defaultZoom, 0.0f, 0.0f); //CameraUpdateFactory.NewLatLngZoom(mDefaultLocation, defaultZoom);
                } else 
                {
                    startCam = new CameraPosition(currState.CurrentCenter, currState.CurrentZoom, 0.0f, 0.0f);
                }
                GoogleMapOptions mapOptions = new GoogleMapOptions()
                    .InvokeMapType(GoogleMap.MapTypeNormal)
                    .InvokeZoomControlsEnabled(false)
                    .InvokeCompassEnabled(true)
                    .InvokeRotateGesturesEnabled(false)
                    .InvokeCamera(startCam);

                Android.App.FragmentTransaction fragTx = FragmentManager.BeginTransaction();
                _mapFragment = MapFragment.NewInstance(mapOptions);
                fragTx.Add(Resource.Id.map, _mapFragment, "map");
                fragTx.Commit();
            }

            loginHolder = FindViewById(Resource.Id.loginHolder) as CardView;
            username = FindViewById(Resource.Id.username) as EditText;
            if (settings.LoggedIn)
            {
                loginHolder.Visibility = ViewStates.Gone;
                _mapFragment.GetMapAsync(this);
                var imm = GetSystemService(Context.InputMethodService) as InputMethodManager;
                imm.HideSoftInputFromWindow(username.WindowToken, 0);
                if (currState == null)
                {
                    await LoadData();
                } else if(lastUpdate < DateTime.UtcNow.AddSeconds(-20))
                {
                    RefreshMapData(null);
                }

            }
            else
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


            progress.Indeterminate = true;
            settingsListview = FindViewById(Resource.Id.settingsListView) as ListView;
            settingsListview.Adapter = new SettingsAdaptor(this, pokeResourceMap);

        }

        protected override async void OnResume()
        {
            base.OnResume();
            Console.WriteLine("OnResume Called");
            await ServiceLayer.SharedInstance.InitalizeSettings();
            if(settings.LoggedIn)
            {
                secondTimer = new Timer(HandleTimerCallback, null, 5000, 5000);
                minuteTimer = new Timer(RefreshMapData, null, 60000, 60000);
                if (lastUpdate < DateTime.UtcNow.AddSeconds(-20))
                {
                    RefreshMapData(null);
                }
            }
        }


        protected override void OnPause()
        {
            base.OnPause();
            Console.WriteLine("activity paused");
            if (minuteTimer != null)
            {
                minuteTimer.Dispose();
                minuteTimer = null;
                secondTimer.Dispose();
                secondTimer = null;
            }
        }

        public override Java.Lang.Object OnRetainCustomNonConfigurationInstance()
        {
            Console.WriteLine("state saved");
            var currentSate = new SaveState()
            {
                Service = ServiceLayer.SharedInstance,
                CurrentBounds = map.Projection.VisibleRegion.LatLngBounds,
                CurrentCenter = map.CameraPosition.Target,
                CurrentZoom = map.CameraPosition.Zoom,
                ResourceMap = pokeResourceMap
            };
            return currentSate;
        }

        protected override void OnSaveInstanceState(Bundle outState)
        {
            base.OnSaveInstanceState(outState);
            Console.WriteLine("instance state saved");
            outState.PutDouble("centerLat", map.CameraPosition.Target.Latitude);
            outState.PutDouble("centerLon", map.CameraPosition.Target.Longitude);
            outState.PutFloat("centerZoom", map.CameraPosition.Zoom);
            outState.PutLong("lastUpdate", Utility.ToUnixTime(lastUpdate));
        }

        protected override void OnRestoreInstanceState(Bundle savedInstanceState)
        {
            base.OnRestoreInstanceState(savedInstanceState);

        }

        private void RefreshMapData(object state)
        {
            RunOnUiThread(async () =>
            {
                if(currentlyUpdating)
                {
                    return;
                }
                currentlyUpdating = true;
                progress.Visibility = ViewStates.Visible;
                await ServiceLayer.SharedInstance.LoadData();
                UpdateMapPokemon(false);
                UpdateMapGyms(true);
                UpdateMapRaids(true);
                progress.Visibility = ViewStates.Gone;
                lastUpdate = DateTime.UtcNow;
                currentlyUpdating = false;
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
            currentlyUpdating = true;
            await ServiceLayer.SharedInstance.LoadData();
            progress.Dismiss();
            lastUpdate = DateTime.UtcNow;
            RefreshMapMarkers(true);
            currentlyUpdating = false;
        }

        private void UpdateMapRaids(bool reload)
        {
            var bounds = map.Projection.VisibleRegion.LatLngBounds;
            List<Raid> toRemove = new List<Raid>();
            if (reload)
            {
                toRemove.AddRange(raidsVisible);
            }
            else
            {
                toRemove.AddRange(raidsVisible.Where((Raid rd) =>
                {
                    if (!bounds.Contains(rd.Location))
                    {
                        return true;
                    }
                    if (rd.TimeEnd < DateTime.UtcNow)
                    {
                        return true;
                    }
                    if (rd.TimeBattle < DateTime.UtcNow && rd.pokemon_id == 0)
                    {
                        return true;
                    }
                    return false;
                }));
            }
            Console.WriteLine($"Removing {toRemove.Count()} raids to the map");
            foreach (var r in toRemove)
            {
                r.RaidMarker.Remove();
                r.RaidMarker = null;
                raidsVisible.Remove(r);
            }
            List<Raid> toAdd = new List<Raid>();
            if (settings.RaidsEnabled)
            {
                toAdd.AddRange(ServiceLayer.SharedInstance.Raids.Where(r => bounds.Contains(r.Location)).Except(raidsVisible));
            }
            if(!settings.Level1Raids)
            {
                toAdd = toAdd.Where(r => r.level != 1).ToList();
            }
            if (!settings.Level2Raids)
            {
                toAdd = toAdd.Where(r => r.level != 2).ToList();
            }
            if (!settings.Level3Raids)
            {
                toAdd = toAdd.Where(r => r.level != 3).ToList();
            }
            if (!settings.Level4Raids)
            {
                toAdd = toAdd.Where(r => r.level != 4).ToList();
            }
            if (!settings.LegondaryRaids)
            {
                toAdd = toAdd.Where(r => r.level != 5).ToList();
            }
            foreach (var r in toAdd)
            {
                AddRaidMarker(r);
            }
            Console.WriteLine($"Adding {toAdd.Count()} raids to the map");

        }

        private void UpdateMapGyms(bool reload)
        {
            var bounds = map.Projection.VisibleRegion.LatLngBounds;
            List<Gym> toRemove = new List<Gym>();
            if (reload)
            {
                toRemove.AddRange(gymsVisible);
            }
            else
            {
                toRemove.AddRange(gymsVisible.Where(p => !bounds.Contains(p.Location)));
            }
            foreach (var g in toRemove)
            {
                g.GymMarker.Remove();
                g.GymMarker = null;
                gymsVisible.Remove(g);
            }
            List<Gym> toAdd = new List<Gym>();
            if (settings.GymsEnabled)
            {
                toAdd.AddRange(ServiceLayer.SharedInstance.Gyms.Values.Where(g => bounds.Contains(g.Location)).Except(gymsVisible));
            }
            foreach(var g in toAdd)
            {
                AddGymMarker(g);
            }
            Console.WriteLine($"Adding {toAdd.Count()} gyms to the map");
        }

        private void UpdateMapPokemon(bool reload)
        {
            try
            {
                var bounds = map.Projection.VisibleRegion.LatLngBounds;
                List<Pokemon> toRemove = new List<Pokemon>();
                if (reload)
                {
                    toRemove.AddRange(PokesVisible);
                }
                else
                {
                    toRemove.AddRange(PokesVisible.Where(p => !bounds.Contains(p.Location) || p.ExpiresDate < DateTime.UtcNow));
                }
                foreach (var p in toRemove)
                {
                    if (p.PokeMarker != null)
                    {
                        p.PokeMarker.Remove();
                        p.PokeMarker = null;
                    }
                    PokesVisible.Remove(p);
                }
                List<Pokemon> toAdd = new List<Pokemon>();
                if (settings.NinetyOnlyEnabled)
                {
                    toAdd.AddRange(ServiceLayer.SharedInstance.Pokemon.Values.Where(p => p.iv > 0.9).Except(PokesVisible));
                }
                if (settings.PokemonEnabled)
                {
                    toAdd.AddRange(ServiceLayer.SharedInstance.Pokemon.Values.Where(p => !settings.PokemonTrash.Contains(p.pokemon_id)).Except(PokesVisible));
                }

                var visible = toAdd.Where(p => bounds.Contains(p.Location)).Where(p => p.ExpiresDate > DateTime.UtcNow);
                foreach (var p in visible)
                {
                    AddPokemonMarker(p);
                }
                Console.WriteLine($"Adding {visible.Count()} mons to the map - total of {PokesVisible.Count()} on map");
                var now = DateTime.UtcNow;
                foreach (var p in PokesVisible)
                {
                    var diff = p.ExpiresDate - now;
                    if (diff.Minutes < 1)
                    {
                        p.PokeMarker.Alpha = diff.Seconds / 60.0f;
                    }
                }
            } catch(Exception e)
            {
                Console.WriteLine($"Exception: {e.ToString()}");
            }

        }


        private void AddPokemonMarker(Pokemon p)
        {
            var mOps = new MarkerOptions();
            mOps.SetPosition(p.Location);

            if (PokesVisible.Count() < 100)
            {
                mOps.SetIcon(BitmapDescriptorFactory.FromBitmap(GetPokemonMarker(p)));
            } else 
            {
                var img = BitmapDescriptorFactory.FromResource(pokeResourceMap[p.pokemon_id]);
                mOps.SetIcon(img);
            }
            mOps.Anchor(0.5f, 0.5f);
            var marker = map.AddMarker(mOps);
            marker.Tag = $"poke:{p.id}";
            if (marker != null)
            {
                p.PokeMarker = marker;
                PokesVisible.Add(p);
            }
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
            gymsVisible.Add(g);
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
            raidsVisible.Add(r);
        }
        public void OnMapReady(GoogleMap mapp)
        {
            map = mapp;
            if (currState != null) 
            {
                var camUpdate = CameraUpdateFactory.NewLatLngZoom(currState.CurrentCenter, currState.CurrentZoom);
                map.MoveCamera(camUpdate);
                centeredMap = true;
                RefreshMapMarkers(true);
            }
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
            map.InfoWindowLongClick += Map_InfoWindowLongClick;
        }

        void Map_CameraIdle(object sender, EventArgs e)
        {
            RefreshMapMarkers(false);
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
            DateTime displayTime = raid.TimeBattle;
            var pokeImg = mapMarker.FindViewById(Resource.Id.marker_raid_poke) as ImageView;
            if (raid.pokemon_id != 0 && DateTime.UtcNow < raid.TimeEnd)
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
            var imgTitle = mapMarker.FindViewById(Resource.Id.marker_raid_text) as TextView;

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
                if (pokemonInfo == null)
                {
                    pokemonInfo = ((LayoutInflater)GetSystemService(Context.LayoutInflaterService)).Inflate(Resource.Layout.pokemon_info, null);
                }
                infoView = pokemonInfo;
                var title = pokemonInfo.FindViewById(Resource.Id.info_title) as TextView;
                var distLabel = pokemonInfo.FindViewById(Resource.Id.info_distance) as TextView;
                var move1Label = pokemonInfo.FindViewById(Resource.Id.info_move1) as TextView;
                var move2Label = pokemonInfo.FindViewById(Resource.Id.info_move2) as TextView;
                var ivLabel = pokemonInfo.FindViewById(Resource.Id.info_iv) as TextView;
                var cpLabel = pokemonInfo.FindViewById(Resource.Id.info_cp_level) as TextView;
                var poke = ServiceLayer.SharedInstance.Pokemon[id];
                if(poke == null)
                {
                    return null;
                }
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
                var poke = ServiceLayer.SharedInstance.Pokemon[id];
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
            if (id == Resource.Id.menu_pokemon)
            {
                settings.PokemonEnabled = !settings.PokemonEnabled;
            }
            else if (id == Resource.Id.menu_gyms)
            {
                settings.GymsEnabled = !settings.GymsEnabled;
            }
            else if (id == Resource.Id.menu_raids)
            {
                settings.RaidsEnabled = !settings.RaidsEnabled;
            }
            else if (id == Resource.Id.menu_90plus)
            {
                settings.NinetyOnlyEnabled = !settings.NinetyOnlyEnabled;
            }
            await ServiceLayer.SharedInstance.SaveSettings();
            RefreshMapMarkers(true);
            RefreshMapData(null);
        }

        private void RefreshMapMarkers(bool force)
        {
            try
            {
                UpdateMapPokemon(force);
                UpdateMapGyms(force);
                UpdateMapRaids(force);
            } catch(Exception e)
            {
                Console.WriteLine($"Exception: {e.ToString()}");
            }
        }

        async void SettingsDone_Click(object sender, EventArgs e)
        {
            settingsHolder.Visibility = ViewStates.Gone;
            await ServiceLayer.SharedInstance.SaveSettings();
            UpdateMapPokemon(true);
            UpdateMapRaids(true);
        }

        async void Map_InfoWindowLongClick(object sender, InfoWindowLongClickEventArgs e)
        {
            var marker = e.Marker;
            var markerTag = marker.Tag.ToString().Split(':');
            var t = markerTag[0];
            var id = markerTag[1];
            if (t == "poke")
            {
                var poke = ServiceLayer.SharedInstance.Pokemon[id];
                marker.HideInfoWindow();
                if (settings.PokemonTrash.Where(p => p == poke.pokemon_id).Count() == 0)
                {
                    settings.PokemonTrash.Add(poke.pokemon_id);
                }
                UpdateMapPokemon(true);
                await ServiceLayer.SharedInstance.SaveSettings();
            }
        }
    }

    class SaveState : Java.Lang.Object
    {
        public ServiceLayer Service { get; set; }
        public LatLngBounds CurrentBounds { get; set; }
        public LatLng CurrentCenter { get; set; }
        public float CurrentZoom { get; set; }
        public int[] ResourceMap { get; set; }
    }
}

    