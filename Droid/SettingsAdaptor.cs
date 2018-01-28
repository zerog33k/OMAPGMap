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

        public override int this[int position] => position;

        public override int Count => MainActivity.NumPokes;

        public override long GetItemId(int position)
        {
            return position;
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            var pId = position+1;
            View view = convertView;
            if (view == null)
            {
                view = context.LayoutInflater.Inflate(Resource.Layout.pokemon_list_item, null);
                var ns = view.FindViewById(Resource.Id.notifySwitch) as Switch;
                var hs = view.FindViewById(Resource.Id.hiddenSwitch) as Switch;
                ns.CheckedChange += NotifySwitch_CheckedChange;
                hs.Click += HideSwitch_CheckedChange;

            }
            var img = view.FindViewById(Resource.Id.pokeImgId) as ImageView;
            var notifySwitch = view.FindViewById(Resource.Id.notifySwitch) as Switch;
            var hideSwitch = view.FindViewById(Resource.Id.hiddenSwitch) as Switch;
            img.SetImageResource(imgMap[pId]);
            notifySwitch.Enabled = !ServiceLayer.SharedInstance.Settings.PokemonHidden.Contains(pId);
            notifySwitch.Checked = ServiceLayer.SharedInstance.Settings.NotifyPokemon.Contains(pId);
            notifySwitch.Tag = pId;
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
    }
}
