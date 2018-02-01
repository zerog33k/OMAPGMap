// WARNING
//
// This file has been generated automatically by Visual Studio from the outlets and
// actions declared in your storyboard file.
// Manual changes to this file will not be maintained.
//
using Foundation;
using System;
using System.CodeDom.Compiler;

namespace OMAPGMap.iOS
{
    [Register ("SettingsViewController")]
    partial class SettingsViewController
    {
        [Action ("DistanceChanged:")]
        partial void DistanceChanged (Foundation.NSObject sender);


        [Action ("GenerationSelected:")]
        partial void GenerationSelected (Foundation.NSObject sender);


        [Action ("IgnoreToggled:")]
        partial void IgnoreToggled (UIKit.UISwitch sender);


        [Action ("NotifyToggled:")]
        partial void NotifyToggled (Foundation.NSObject sender);


        [Action ("SettingButtonPressed:")]
        partial void SettingButtonPressed (Foundation.NSObject sender);


        [Action ("SettingToggled:")]
        partial void SettingToggled (Foundation.NSObject sender);


        [Action ("TrashToggled:")]
        partial void TrashToggled (Foundation.NSObject sender);

        void ReleaseDesignerOutlets ()
        {
        }
    }
}