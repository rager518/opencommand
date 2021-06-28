using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace OpenCommandService
{
    public class LocalServiceWrapper : ILocalService
    {
        public double Add(double n1, double n2)
        {
            double result = n1 + n2;
            return result;
        }
    }
}
