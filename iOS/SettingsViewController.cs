// This file has been autogenerated from a class added in the UI designer.

using System;
using System.Collections.Generic;
using System.Linq;
using CoreGraphics;
using Foundation;
using OMAPGMap.Models;
using UIKit;

namespace OMAPGMap.iOS
{
	public partial class SettingsViewController : UITableViewController
	{
        public SettingsViewController(IntPtr ptr) : base(ptr)
        {
            
        }

        private List<int> TrashAdded = new List<int>();
        private List<int> TrashRemoved = new List<int>();

        public ViewController ParentVC { get; set; }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            Title = "Pokemon Filters";
            NavigationItem.LeftBarButtonItem = new UIBarButtonItem("Done", UIBarButtonItemStyle.Done, (sender, e) =>
             {
                if(TrashAdded.Count > 0)
                {
                    ParentVC.TrashAdded(TrashAdded);
                }
                if(TrashRemoved.Count > 0)
                {
                    ParentVC.TrashRemoved( TrashRemoved);
                }
                DismissViewController(true, null);
             });
            //TableView.AllowsSelection = false;
        }

        public override nint NumberOfSections(UITableView tableView)
        {
            return 2;
        }

        public override string TitleForHeader(UITableView tableView, nint section)
        {
            return section == 0 ? "Settings" : "Pokemon Settings";
        }

        public override UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath)
        {
            
            if (indexPath.Section == 0)
            {
                var cell = tableView.DequeueReusableCell("ResetTrashCell", indexPath);
                var label = cell.ViewWithTag(1) as UILabel;
				switch (indexPath.Row)
				{
                    case 0:
                        label.Text = "Hide Everything";
                        break;
                    case 1:
                        label.Text = "Reset Default Trash";
                        break;
                    case 2:
                        label.Text = "Save Current Trash";
                        break;
                    case 3:
                        label.Text = "Recall Saved Trash";
                        break;
				}
                return cell;
            }
            else
            {
                var cell = tableView.DequeueReusableCell("FilterCell", indexPath);
                var img = cell.ViewWithTag(1) as UIImageView;
                var notifyLbl = cell.ViewWithTag(2) as UILabel;
                var notifySwitch = cell.ViewWithTag(3) as UISwitch;
                var trashLbl = cell.ViewWithTag(4) as UILabel;
                var trashSwitch = cell.ViewWithTag(5) as UISwitch;
                var pokemonid = indexPath.Row + 1;
                img.Image = UIImage.FromBundle(pokemonid.ToString("D3"));
                if (!ServiceLayer.SharedInstance.PokemonHidden.Contains(pokemonid))
                {
                    img.Alpha = 1.0f;
                    notifyLbl.TextColor = UIColor.Black;
                    trashLbl.TextColor = UIColor.Black;
                    trashSwitch.Enabled = true;
                    trashSwitch.On = ServiceLayer.SharedInstance.PokemonTrash.Contains(pokemonid);
                    notifySwitch.Enabled = false;
                    notifySwitch.On = false;
                }
                else
                {
                    img.Alpha = 0.7f;
                    notifyLbl.TextColor = UIColor.LightGray;
                    trashLbl.TextColor = UIColor.LightGray;
                    trashSwitch.Enabled = false;
                    trashSwitch.On = false;
                    notifySwitch.Enabled = false;
                    notifySwitch.On = false;
                }
                cell.SelectionStyle = UITableViewCellSelectionStyle.None;
                return cell;
            }
        }

        public override nint RowsInSection(UITableView tableView, nint section)
        {
            return section == 0 ? 4 : ServiceLayer.NumberPokemon;
        }

        partial void TrashToggled(NSObject sender)
        {
            var trashSwitch = sender as UISwitch;
            var cell = trashSwitch.Superview.Superview as UITableViewCell;
            if (cell != null)
            {
                var path = TableView.IndexPathForCell(cell);
                var pokemonid = path.Row + 1;
                if(trashSwitch.On)
                {
                    TrashAdded.Add(pokemonid);
                    TrashRemoved.Remove(pokemonid);
                    ServiceLayer.SharedInstance.PokemonTrash.Add(pokemonid);
                } else 
                {
                    TrashRemoved.Add(pokemonid);
                    TrashAdded.Remove(pokemonid);
                    ServiceLayer.SharedInstance.PokemonTrash.Remove(pokemonid);
                }
            }
        }

        public override void RowSelected(UITableView tableView, NSIndexPath indexPath)
        {
            switch(indexPath.Row)
            {
                case 0:
					for (var i = 0; i < ServiceLayer.NumberPokemon; i++)
					{
                        TrashRemoved.Clear();
                        if (!ServiceLayer.SharedInstance.PokemonTrash.Contains(i) && !TrashAdded.Contains(i))
						{
							TrashAdded.Add(i);
                            ServiceLayer.SharedInstance.PokemonTrash.Add(i);
						}
					}
					
                    TableView.ReloadData();
                    break;
                case 1:
                    for (var i = 0; i < ServiceLayer.NumberPokemon; i++)
					{
						if (!ServiceLayer.SharedInstance.PokemonTrash.Contains(i) && ServiceLayer.DefaultTrash.Contains(i))
						{
							TrashAdded.Add(i);
                            TrashRemoved.Remove(i);
						}
						else if (ServiceLayer.SharedInstance.PokemonTrash.Contains(i) && !ServiceLayer.DefaultTrash.Contains(i))
						{
							TrashRemoved.Add(i);
                            TrashAdded.Remove(i);
						}
					}
					ServiceLayer.SharedInstance.PokemonTrash.Clear();
					ServiceLayer.SharedInstance.PokemonTrash.AddRange(ServiceLayer.DefaultTrash);
					TableView.ReloadData();
                    break;
                case 2:
					var trashStrings = ServiceLayer.SharedInstance.PokemonTrash.Select(t => t.ToString()).ToArray();
					var tosave = NSArray.FromStrings(trashStrings);
					NSUserDefaults.StandardUserDefaults.SetValueForKey(tosave, new NSString("trashSaved"));
                    break;
                case 3:
					var trash = NSUserDefaults.StandardUserDefaults.StringArrayForKey("trashSaved");
					if (trash != null)
					{
						var trashInt = trash.Select(l => int.Parse(l));
                        for (var i = 0; i < ServiceLayer.NumberPokemon; i++)
                        {
                            if (!ServiceLayer.SharedInstance.PokemonTrash.Contains(i) && trashInt.Contains(i))
							{
								TrashAdded.Add(i);
                                TrashRemoved.Remove(i);
							}
							else if (ServiceLayer.SharedInstance.PokemonTrash.Contains(i) && !trashInt.Contains(i))
							{
								TrashRemoved.Add(i);
                                TrashAdded.Remove(i);
							}
                        }
						ServiceLayer.SharedInstance.PokemonTrash.Clear();
                        ServiceLayer.SharedInstance.PokemonTrash.AddRange(trashInt);
						TableView.ReloadData();
					}
                    break;
            }
            tableView.DeselectRow(indexPath, true);
        }
	}
}
