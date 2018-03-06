using System;
using Android.App;
using Android.Views;
using Android.Widget;
using Java.Lang;
using OMAPGMap.Models;

namespace OMAPGMap.Droid
{
    public class SettingsAdaptor : BaseAdapter<int>
    {
        Activity context;
        int[] imgMap;
        public SettingsAdaptor(Activity context, int[] resourceMap) : base() {
            this.context = context;
            imgMap = resourceMap;
        }

        private int currentGen = 1;

        private int numExtraRows = 20;

        public override int this[int position] => position;

        public override int Count {
            get {
                switch (currentGen)
                {
                    case 1:
                        return 151 + numExtraRows;
                    case 2:
                        return 100 + numExtraRows;
                    case 3:
                        return 132 + numExtraRows;
                }
                return 0;
            }
        } 

        public override long GetItemId(int position)
        {
            return position;
        }

        private RadioGroup genGroup = null;

        UserSettings settings = ServiceLayer.SharedInstance.Settings;

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            View view = convertView;
            var t = view?.Tag?.ToString();

            if (position == 0)
            {
                view = context.LayoutInflater.Inflate(Resource.Layout.settings_header, null);
                var title = view.FindViewById(Resource.Id.settingsHeader) as TextView;
                title.Text = "Layer Layers";
                return view;
            }
            else if (position < 5)
            {
                Button b;
                if (view == null || t != "buttonItem")
                {
                    view = context.LayoutInflater.Inflate(Resource.Layout.settings_button_item, null);
                    b = view.FindViewById(Resource.Id.settings_Item_button) as Button;
                    b.Click += ButtonClick;
                }
                else
                {
                    b = view.FindViewById(Resource.Id.settings_Item_button) as Button;
                }
                b.Tag = position.ToString();
                switch (position)
                {
                    case 1:
                        b.Text = "Hide Everything";
                        break;
                    case 2:
                        b.Text = "Reset Default Hidden";
                        break;
                    case 3:
                        b.Text = "Save Current Hidden";
                        break;
                    case 4:
                        b.Text = "Recall Saved Hidden";
                        break;
                }
                return view;
            }
            else if (position == 5)
            {
                view = context.LayoutInflater.Inflate(Resource.Layout.settings_header, null);
                var title = view.FindViewById(Resource.Id.settingsHeader) as TextView;
                title.Text = "Notification Layers";
                return view;
            }
            else if (position < 9)
            {
                Switch s;
                if (view == null || t != "switchItem")
                {
                    view = context.LayoutInflater.Inflate(Resource.Layout.settings_switch_item, null);
                    s = view.FindViewById(Resource.Id.settingSwitch) as Switch;
                    s.CheckedChange += S_CheckedChange;
                }
                else
                {
                    s = view.FindViewById(Resource.Id.settingSwitch) as Switch;
                }
                s.Tag = position.ToString();
                switch(position)
                {
                    case 6:
                        s.Text = "All Notifications";
                        s.Checked = settings.NotifyEnabled;
                        break;
                    case 7:
                        s.Text = "> 90% IV Notify";
                        s.Checked = settings.Notify90Enabled;
                        break;
                    case 8:
                        s.Text = "100% IV Notify";
                        s.Checked = settings.Notify100Enabled;
                        break;
                }
                return view;
            }
            else if (position < 12)
            {
                EditText e;
                view = context.LayoutInflater.Inflate(Resource.Layout.settings_input_item, null);
                if (view == null || t != "editItuem")
                {
                    view = context.LayoutInflater.Inflate(Resource.Layout.settings_input_item, null);
                    e = view.FindViewById(Resource.Id.listInputValue) as EditText;
                    e.TextChanged += E_TextChanged;
                }
                else
                {
                    e = view.FindViewById(Resource.Id.listInputValue) as EditText;
                }
                var title = view.FindViewById(Resource.Id.listInputTitle) as TextView;
                var edit = view.FindViewById(Resource.Id.listInputValue) as EditText;
                edit.Tag = position.ToString();
                switch(position)
                {
                    case 9 :
                        title.Text = "Distance (miles)";
                        edit.Text = settings.NotifyDistance.ToString();
                        break;
                    case 10:
                        title.Text = "Maximum Distance (miles)";
                        edit.Text = settings.NotifyMaxDistance.ToString();
                        break;
                    case 11:
                        title.Text = "Minimum Level";
                        edit.Text = settings.NotifyLevel.ToString();
                        break;
                }
                return view;
            }
            else if (position == 12)
            {
                view = context.LayoutInflater.Inflate(Resource.Layout.settings_header, null);
                var title = view.FindViewById(Resource.Id.settingsHeader) as TextView;
                title.Text = "Raid Settings";
                return view;
            }
            else if (position < 18)
            {
                Switch s;
                if (view == null || t != "switchItem")
                {
                    view = context.LayoutInflater.Inflate(Resource.Layout.settings_switch_item, null);
                    s = view.FindViewById(Resource.Id.settingSwitch) as Switch;
                    s.CheckedChange += S_CheckedChange;
                }
                else
                {
                    s = view.FindViewById(Resource.Id.settingSwitch) as Switch;
                }
                s.Tag = position.ToString();
                if(position != 17)
                {
                    s.Text = $"Level {position - 12} Raids:";
                } else 
                {
                    s.Text = "Legendary Raids:";
                }
                switch (position)
                {
                    case 13:
                        s.Checked = settings.Level1Raids;
                        break;
                    case 14:
                        s.Checked = settings.Level2Raids;
                        break;
                    case 15:
                        s.Checked = settings.Level3Raids;
                        break;
                    case 16:
                        s.Checked = settings.Level4Raids;
                        break;
                    case 17:
                        s.Checked = settings.LegondaryRaids;
                        break;
                }
                return view;
            }

            else if (position == 18)
            {
                view = context.LayoutInflater.Inflate(Resource.Layout.settings_header, null);
                var title = view.FindViewById(Resource.Id.settingsHeader) as TextView;
                title.Text = "Pokémon Settings";
                return view;
            }
            else if (position == 19)
            {
                if (view == null || t != "radioItem")
                {
                    view = context.LayoutInflater.Inflate(Resource.Layout.gen_switcher_list_item, null);
                    genGroup = view.FindViewById(Resource.Id.radioGenGroup) as RadioGroup;
                    genGroup.CheckedChange += GenGroup_CheckedChange;
                }
                var checkedId = Resource.Id.radioGen1;
                switch (currentGen)
                {
                    case 2:
                        checkedId = Resource.Id.radioGen2;
                        break;
                    case 3:
                        checkedId = Resource.Id.radioGen3;
                        break;
                }
                genGroup.Check(checkedId);
                view.Tag = "radioItem";
                return view;
            }
            var pId = position - numExtraRows + 1;
            switch (currentGen)
            {
                case 2:
                    pId += 151;
                    break;
                case 3:
                    pId += 251;
                    break;
            }

            if (view == null || t != "pokeItem")
            {
                view = context.LayoutInflater.Inflate(Resource.Layout.pokemon_list_item, null);
                var ns = view.FindViewById(Resource.Id.notifySwitch) as Switch;
                var hs = view.FindViewById(Resource.Id.hiddenSwitch) as Switch;
                var igs = view.FindViewById(Resource.Id.ignoreSwitch) as Switch;
                ns.Click += NotifySwitch_CheckedChange;
                hs.Click += HideSwitch_CheckedChange;
                igs.Click += Igs_Click;
                view.Tag = "pokeItem";
            }
            var img = view.FindViewById(Resource.Id.pokeImgId) as ImageView;
            var notifySwitch = view.FindViewById(Resource.Id.notifySwitch) as Switch;
            var hideSwitch = view.FindViewById(Resource.Id.hiddenSwitch) as Switch;
            var ignoreSwitch = view.FindViewById(Resource.Id.ignoreSwitch) as Switch;
            img.SetImageResource(imgMap[pId]);
            notifySwitch.Enabled = !settings.PokemonHidden.Contains(pId);
            notifySwitch.Checked = settings.NotifyPokemon.Contains(pId);
            notifySwitch.Tag = pId;
            hideSwitch.Enabled = !settings.PokemonHidden.Contains(pId);
            hideSwitch.Checked = settings.PokemonTrash.Contains(pId);
            hideSwitch.Tag = pId;
            ignoreSwitch.Enabled = !settings.PokemonHidden.Contains(pId);
            ignoreSwitch.Checked = settings.IgnorePokemon.Contains(pId);
            ignoreSwitch.Tag = pId;
            return view;
        }

        void NotifySwitch_CheckedChange(object sender, EventArgs e)
        {
            var sw = sender as Switch;
            var pId = (int)sw.Tag;
            if (settings.NotifyPokemon.Contains(pId))
            {
                settings.NotifyPokemon.Remove(pId);
            }
            else
            {
                settings.NotifyPokemon.Add(pId);
            }
        }

        void HideSwitch_CheckedChange(object sender, EventArgs e)
        {
            var sw = sender as Switch;
            var pId = (int)sw.Tag;
            if(settings.PokemonTrash.Contains(pId))
            {
                settings.PokemonTrash.Remove(pId);
            } else 
            {
                settings.PokemonTrash.Add(pId);
            }
        }

        void GenGroup_CheckedChange(object sender, RadioGroup.CheckedChangeEventArgs e)
        {
            if(genGroup.CheckedRadioButtonId == Resource.Id.radioGen1)
            {
                currentGen = 1;
            } else if(genGroup.CheckedRadioButtonId == Resource.Id.radioGen2)
            {
                currentGen = 2;
            } else if(genGroup.CheckedRadioButtonId == Resource.Id.radioGen3)
            {
                currentGen = 3;
            }
            NotifyDataSetChanged();
        }

        void ButtonClick(object sender, EventArgs e)
        {
            var button = sender as Button;
            var row = int.Parse(button.Tag.ToString());
            switch(row)
            {
                case 1: //hide everything
                    for (var i = 1; i <= MainActivity.NumPokes; i++)
                    {
                        if(!settings.PokemonTrash.Contains(i))
                        {
                            settings.PokemonTrash.Add(i);
                        }
                    }
                    break;
                case 2: //reset hidden to default
                    settings.ResetTrash();
                    break;
                case 3: //save hidden
                    settings.SavedHiddenPokemon.Clear();
                    settings.SavedHiddenPokemon.AddRange(settings.PokemonTrash);
                    break;
                case 4: //recall saved hidden
                    settings.PokemonTrash.Clear();
                    settings.PokemonTrash.AddRange(settings.SavedHiddenPokemon);
                    break;
            }
            NotifyDataSetChanged();
        }

        void S_CheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
        {
            var sw = sender as Switch;
            var row = int.Parse(sw.Tag.ToString());
            switch (row)
            {
                case 6:
                    settings.NotifyEnabled = sw.Checked;
                    break;
                case 7:
                    settings.Notify90Enabled = sw.Checked;
                    break;
                case 8:
                    settings.Notify100Enabled = sw.Checked;
                    break;
                case 13:
                    settings.Level1Raids = sw.Checked;
                    break;
                case 14:
                    settings.Level2Raids = sw.Checked;
                    break;
                case 15:
                    settings.Level3Raids = sw.Checked;
                    break;
                case 16:
                    settings.Level4Raids = sw.Checked;
                    break;
                case 17:
                    settings.LegondaryRaids = sw.Checked;
                    break;
            }
        }

        void Igs_Click(object sender, EventArgs e)
        {
            var sw = sender as Switch;
            var pId = (int)sw.Tag;
            if (settings.IgnorePokemon.Contains(pId))
            {
                settings.IgnorePokemon.Remove(pId);
            }
            else
            {
                settings.IgnorePokemon.Add(pId);
            }
        }

        void E_TextChanged(object sender, Android.Text.TextChangedEventArgs e)
        {
            var txt = sender as EditText;
            var row = int.Parse(txt.Tag.ToString());
            int value = 0;
            if(!int.TryParse(txt.Text, out value))
            {
                return;
            }
            switch (row)
            {
                case 9:
                    settings.NotifyDistance = value;
                    break;
                case 10:
                    settings.NotifyMaxDistance = value;
                    break;
                case 11:
                    settings.NotifyLevel = value;
                    break;
            }
        }
    }
}
