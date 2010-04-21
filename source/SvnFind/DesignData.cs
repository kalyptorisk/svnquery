using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SvnFind
{
    static class DesignData
    {

        static DesignData()
        {
            MainViewModel = new MainViewModel();
            MainViewModel.QueryText = "bla";
            MainViewModel.Query();
        }

        public static MainViewModel MainViewModel { get; private set;}
    }
}
