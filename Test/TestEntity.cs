using System;
using System.Collections.Generic;
using System.Text;

namespace Test
{
    public class Student
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int Age { get; set; }
    }

    public class Class
    {
        public int Id { get; set; }
        public List<Student> Students { get; set; }

        public Teacher Teacher { get; set; }
    }

    public class Teacher
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }



}
