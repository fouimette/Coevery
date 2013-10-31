﻿using Coevery.Environment.Extensions;
using Coevery.Localization;
using Coevery.UI.Navigation;
using Coevery.Security;

namespace Coevery.Packaging {
    [CoeveryFeature("Gallery")]
    public class AdminMenu : INavigationProvider {
        public Localizer T { get; set; }

        public string MenuName {
            get { return "admin"; }
        }

        public void GetNavigation(NavigationBuilder builder) {
            builder
                .Add(T("Modules"), menu => menu
                    .Add(T("Gallery"), "3", item => Describe(item, "Modules", "Gallery", true)))
                .Add(T("Themes"), menu => menu
                    .Add(T("Gallery"), "3", item => Describe(item, "Themes", "Gallery", true)))
                .Add(T("Settings"), menu => menu
                    .Add(T("Gallery"), "1", item => Describe(item, "Sources", "Gallery", false)));
        }

        static NavigationItemBuilder Describe(NavigationItemBuilder item, string actionName, string controllerName, bool localNav) {
            item = item.Action(actionName, controllerName, new { area = "Coevery.Packaging" }).Permission(StandardPermissions.SiteOwner);
            if (localNav)
                item = item.LocalNav();
            return item;
        }
    }
}