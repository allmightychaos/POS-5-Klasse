using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.RightsManagement;
using System.Text;
using System.Threading.Tasks;

namespace SchiffeGUI
{
    public class GesetztesSchiff
    {
        public int Leben { get; set; }
        public List<Feld> Felder { get; set; }

        public GesetztesSchiff(int Leben)
        {
            this.Leben = Leben;
        }
    }
}
