using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace TextToLinq
{
    public class QueryBuilder
    {

         private static readonly string[] _relationalOperators = { "eq", "ne", "lt", "gt", "le", "ge", "like" };
        private static readonly string[] _logicalOperators = { "and", "or" };
        private static ParameterExpression _paramExp;


        public static string[] Split(string query)
        {
            var queryParts = new List<string>();
            var splitQuery = query.Split(' ');


            var queryPart = string.Empty;
            var i = 0;
            foreach (var part in splitQuery)
            {
                if (IsLogicalOperator(part))
                {
                    if (string.IsNullOrEmpty(queryPart)) throw new Exception("Invoiced Query");
                    queryParts.Add(queryPart.Trim());
                    queryParts.Add(part);
                    queryPart = string.Empty;
                }
                else
                {
                    queryPart += $"{part} ";
                }

                if (i == splitQuery.Length - 1)
                    queryParts.Add(queryPart.Trim());

                i++;
            }

            return queryParts.ToArray();

        }

        public static bool IsRelationalOperator(string key)
        {
            foreach (var relationalOperator in _relationalOperators)
            {
                if (relationalOperator == key) return true;
            }

            return false;
        }

        public static bool IsLogicalOperator(string key)
        {
            foreach (var logicalOperator in _logicalOperators)
            {
                if (logicalOperator == key) return true;
            }

            return false;
        }

        public static PropertyInfo GetProperty<T>(string propName) where T : class
        {
            return GetProperty(propName, typeof(T));
        }

        public static PropertyInfo GetProperty(string propName, Type type)
        {
            try
            {
                return type.GetProperty(propName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            }
            catch (ArgumentNullException)
            {
                return null;
            }

        }

        public static Expression<Func<T, bool>> GetQuery<T>(string query) where T : class
        {
            var paramType = typeof(T);
            _paramExp = Expression.Parameter(paramType);
            var queryParts = Split(query);

            var expression = BuildExpression(queryParts, paramType);

            return Expression.Lambda<Func<T, bool>>(expression, new[] { _paramExp });

        }

        public static Expression GetQuery(string query, Type type)
        {
            _paramExp = Expression.Parameter(type);
            var queryParts = Split(query);

            var expression = BuildExpression(queryParts, type);

            return Expression.Lambda(expression, new[] { _paramExp });
        }

        public static Expression GetExpression<T>(string queryPart) where T : class
        {
            return GetExpression(queryPart, typeof(T));
        }

        public static Expression GetExpression(string queryPart, Type type)
        {

            var array = queryPart.Split(' ');

            if (array.Length != 3)
                throw new Exception($"Not valid query part,{queryPart}");


            var property = GetProperty(array[0], type);

            if (property == null)
                throw new Exception($"cannot find a property named ${array[0]}");

            Expression leftExp = Expression.Property(_paramExp, property);

            object rightValue;
            if (property.PropertyType == typeof(string))
            {
                array[2] = array[2].Trim('\'');

                leftExp = Expression.Call(leftExp, typeof(string).GetMethod("ToLower",new Type[] { }));               
            }

            if (property.PropertyType == typeof(Guid) || property.PropertyType == typeof(Guid?))
                rightValue = Guid.Parse(array[2]);
            else
                rightValue = Convert.ChangeType(array[2], property.PropertyType);

            Expression rightExp = Expression.Constant(rightValue, property.PropertyType);


            if (property.PropertyType == typeof(string))
            {              
                rightExp = Expression.Call(rightExp, typeof(string).GetMethod("ToLower", new Type[] { }));
            }

            Expression finalExp;
            switch (array[1])
            {
                case "eq": finalExp = Expression.Equal(leftExp, rightExp); break;
                case "neq": finalExp = Expression.NotEqual(leftExp, rightExp); break;
                case "lt": finalExp = Expression.LessThan(leftExp, rightExp); break;
                case "lte": finalExp = Expression.LessThanOrEqual(leftExp, rightExp); break;
                case "gt": finalExp = Expression.GreaterThan(leftExp, rightExp); break;
                case "gte": finalExp = Expression.GreaterThanOrEqual(leftExp, rightExp); break;
                case "like":
                    {
                        if (property.PropertyType != typeof(string))
                        {
                            throw new Exception($"Cannot use 'like' for {array[0]}. 'like' can only use for System.String type");
                        }

                        var containMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) });
                        finalExp = Expression.Call(leftExp, containMethod, rightExp);
                        break;
                    }

                default: throw new Exception($"Invalid operator {array[1]}");
            }

            return finalExp;

        }


        public static Expression BuildExpression<T>(string[] queryParts) where T : class
        {
            return BuildExpression(queryParts, typeof(T));
        }

        public static Expression BuildExpression(string[] queryParts, Type type)
        {
            var expressionQueue = new Queue<object>();
            foreach (var queryPart in queryParts)
            {
                if (IsLogicalOperator(queryPart))
                {
                    expressionQueue.Enqueue(queryPart);
                }
                else
                {
                    var expressionPart = GetExpression(queryPart, type);
                    expressionQueue.Enqueue(expressionPart);
                }
            }

            Expression expression1 = null, expression2 = null;
            string logicalOp = string.Empty;
            bool run = expressionQueue.Any();
            while (run)
            {


                if (expression1 != null && expression2 != null && !string.IsNullOrEmpty(logicalOp))
                {
                    switch (logicalOp)
                    {
                        case "and": expression1 = Expression.AndAlso(expression1, expression2); break;
                        case "or": expression1 = Expression.OrElse(expression1, expression2); break;
                        default: throw new Exception($"Invoice logical operator {logicalOp}");
                    }

                    expression2 = null;
                    logicalOp = string.Empty;
                }

                run = expressionQueue.Any();

                if (!run)
                    break;


                if (expression1 == null)
                {
                    expression1 = expressionQueue.Dequeue() as Expression;
                }
                else if (string.IsNullOrEmpty(logicalOp))
                {
                    logicalOp = expressionQueue.Dequeue() as string;
                }
                else if (expression2 == null)
                {
                    expression2 = expressionQueue.Dequeue() as Expression;
                }

            }

            return expression1;
        }

    }
}
