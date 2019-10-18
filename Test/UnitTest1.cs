using System;
using System.Collections.Generic;
using System.Linq;
using TextToLinq;
using Xunit;

namespace Test
{
    public class UnitTest1
    {
        private List<Student> studets = new List<Student>
        {
            new Student
            {
                FirstName = "Ahamed",
                LastName = "Ishak",
                Age = 30
            },
            new Student
            {
                FirstName = "Ahamed",
                LastName = "Ishak",
                Age = 19
            }, new Student
            {
                FirstName = "Ahamed",
                LastName = "Ishak",
                Age = 20
            },new Student
            {
                FirstName = "Fazik",
                LastName = "Imran",
                Age = 20
            }





        };


        [Fact]
        public void GetQueryPart()
        {
            var query = "FirstName eq 'Ahamed' and LastName eq 'Ishak' or Age gt 20";

            var queryBuilder = new QueryBuilder();

            var splitQuery = queryBuilder.Split(query);

        }


        [Fact]
        public void GetExpresstionPart()
        {
            var query = "FirstName eq 'Ahamed'";

            var queryBuilder = new QueryBuilder();

            var expresstion = queryBuilder.GetExpression<Student>(query);

        }

        [Fact]
        public void GetExpression()
        {
            var query = "FirstName eq 'Ahamed' and LastName eq 'Ishak' or Age lte 20 or LastName like 'ran'";

            var queryBuilder = new QueryBuilder();

            var expression = queryBuilder.GetQuery<Student>(query);

            var queryList = studets;
            var func= expression.Compile();

           var result= queryList.Where(func);


        }
    }



    
}
