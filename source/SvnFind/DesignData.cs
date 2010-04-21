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
#if DEBUG
            MainViewModel.QueryText = "bla";
            MainViewModel.Query();
#endif
        }

        public static MainViewModel MainViewModel { get; private set;}
    }
}
