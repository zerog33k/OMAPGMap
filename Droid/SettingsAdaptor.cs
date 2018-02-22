using System;
using Android.App;
using Android.Views;
using Android.Widget;
using Java.Lang;

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

        private int numExtraRows = 10;

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

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            View view = convertView;
            var t = view?.Tag?.ToString();

            if (position < 5)
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
                if(position != 4)
                {
                    s.Text = $"Level {position + 1} Raids:";
                } else 
                {
                    s.Text = "Legendary Raids:";
                }
                switch (position)
                {
                    case 0:
                        s.Checked = ServiceLayer.SharedInstance.Settings.Level1Raids;
                        break;
                    case 1:
                        s.Checked = ServiceLayer.SharedInstance.Settings.Level2Raids;
                        break;
                    case 2:
                        s.Checked = ServiceLayer.SharedInstance.Settings.Level3Raids;
                        break;
                    case 3:
                        s.Checked = ServiceLayer.SharedInstance.Settings.Level4Raids;
                        break;
                    case 4:
                        s.Checked = ServiceLayer.SharedInstance.Settings.LegondaryRaids;
                        break;
                }
                return view;
            }
            else if(position < 9)
            {
                Button b;
                if(view == null || t != "buttonItem")
                {
                    view = context.LayoutInflater.Inflate(Resource.Layout.settings_button_item, null);
                    b = view.FindViewById(Resource.Id.settings_Item_button) as Button;
                    b.Click += ButtonClick;
                } else
                {
                    b = view.FindViewById(Resource.Id.settings_Item_button) as Button;
                }
                b.Tag = position.ToString();
                switch (position)
                {
                    case 5:
                        b.Text = "Hide Everything";
                        break;
                    case 6:
                        b.Text = "Reset Default Hidden";
                        break;
                    case 7:
                        b.Text = "Save Current Hidden";
                        break;
                    case 8:
                        b.Text = "Recall Saved Hidden";
                        break;
                }
                return view;
            }
            if (position == 9)
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
                ns.CheckedChange += NotifySwitch_CheckedChange;
                hs.Click += HideSwitch_CheckedChange;
                view.Tag = "pokeItem";
            }
            var img = view.FindViewById(Resource.Id.pokeImgId) as ImageView;
            var notifySwitch = view.FindViewById(Resource.Id.notifySwitch) as Switch;
            var hideSwitch = view.FindViewById(Resource.Id.hiddenSwitch) as Switch;
            img.SetImageResource(imgMap[pId]);
            notifySwitch.Enabled = !ServiceLayer.SharedInstance.Settings.PokemonHidden.Contains(pId);
            notifySwitch.Checked = ServiceLayer.SharedInstance.Settings.NotifyPokemon.Contains(pId);
            notifySwitch.Tag = pId;
            notifySwitch.Enabled = false;
            hideSwitch.Enabled = !ServiceLayer.SharedInstance.Settings.PokemonHidden.Contains(pId);
            hideSwitch.Checked = ServiceLayer.SharedInstance.Settings.PokemonTrash.Contains(pId);
            hideSwitch.Tag = pId;
            return view;
        }

        void NotifySwitch_CheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
        {
            var sw = sender as Switch;
            var pId = (int)sw.Tag;
            if (ServiceLayer.SharedInstance.Settings.NotifyPokemon.Contains(pId))
            {
                ServiceLayer.SharedInstance.Settings.NotifyPokemon.Remove(pId);
            }
            else
            {
                ServiceLayer.SharedInstance.Settings.NotifyPokemon.Add(pId);
            }
        }

        void HideSwitch_CheckedChange(object sender, EventArgs e)
        {
            var sw = sender as Switch;
            var pId = (int)sw.Tag;
            if(ServiceLayer.SharedInstance.Settings.PokemonTrash.Contains(pId))
            {
                ServiceLayer.SharedInstance.Settings.PokemonTrash.Remove(pId);
            } else 
            {
                ServiceLayer.SharedInstance.Settings.PokemonTrash.Add(pId);
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
                case 5: //hide everything
                    for (var i = 1; i <= MainActivity.NumPokes; i++)
                    {
                        if(!ServiceLayer.SharedInstance.Settings.PokemonTrash.Contains(i))
                        {
                            ServiceLayer.SharedInstance.Settings.PokemonTrash.Add(i);
                        }
                    }
                    break;
                case 6: //reset hidden to default
                    ServiceLayer.SharedInstance.Settings.ResetTrash();
                    break;
                case 7: //save hidden
                    ServiceLayer.SharedInstance.Settings.SavedHiddenPokemon.Clear();
                    ServiceLayer.SharedInstance.Settings.SavedHiddenPokemon.AddRange(ServiceLayer.SharedInstance.Settings.PokemonTrash);
                    break;
                case 8: //recall saved hidden
                    ServiceLayer.SharedInstance.Settings.PokemonTrash.Clear();
                    ServiceLayer.SharedInstance.Settings.PokemonTrash.AddRange(ServiceLayer.SharedInstance.Settings.SavedHiddenPokemon);
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
                case 0:
                    ServiceLayer.SharedInstance.Settings.Level1Raids = sw.Checked;
                    break;
                case 1:
                    ServiceLayer.SharedInstance.Settings.Level2Raids = sw.Checked;
                    break;
                case 2:
                    ServiceLayer.SharedInstance.Settings.Level3Raids = sw.Checked;
                    break;
                case 3:
                    ServiceLayer.SharedInstance.Settings.Level4Raids = sw.Checked;
                    break;
                case 4:
                    ServiceLayer.SharedInstance.Settings.LegondaryRaids = sw.Checked;
                    break;
            }
        }
    }
}
