using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xania.ObjectMapper.Tests
{
    public class Person
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public Person Parent { get; set; }
    }
}
