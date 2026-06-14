using CommunityToolkit.Mvvm.ComponentModel;
using LedgerSyncModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LedgerSyncViewModel
{
    public partial class MenuViewModel : ObservableObject
    {
        public MenuViewModel()
        {
            menuModels = new MenuModel();
        }

        // FIX: removed unused SpotAccountTrade field (was declared but never initialized or used)
        [ObservableProperty]
        private MenuModel menuModels;
    }
}
