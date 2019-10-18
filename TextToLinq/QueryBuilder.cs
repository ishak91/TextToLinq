﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace TextToLinq
{
    public class QueryBuilder
    {

        private readonly string[] _relationalOperators = { "eq", "neq", "lt", "gt", "lte", "gte" };
        private readonly string[] _logicalOperators = { "and", "or" };


        public string[] Split(string query)
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



        public bool IsRelationalOperator(string key)
        {
            foreach (var relationalOperator in _relationalOperators)
            {
                if (relationalOperator == key) return true;
            }

            return false;
        }

        public bool IsLogicalOperator(string key)
        {
            foreach (var logicalOperator in _logicalOperators)
            {
                if (logicalOperator == key) return true;
            }

            return false;
        }

        public PropertyInfo GetProperty<T>(string propName) where T : class
        {
            try
            {
                return typeof(T).GetProperty(propName);
            }
            catch (ArgumentNullException)
            {
                return null;
            }
        }


        public Expression<Func<T, bool>> GetQuery<T>(string query) where T : class
        {
            var paramType = typeof(T);
            var paramExp = Expression.Parameter(paramType);
            var queryParts = Split(query);

           var expression= BuildExpression<T>(queryParts);

           return Expression.Lambda<Func<T, bool>>(expression, new[] {paramExp});

        }

        public Expression GetExpression<T>(string queryPart) where T : class
        {
            var array = queryPart.Split(' ');

            if (array.Length != 3)
                throw new Exception($"Not valid query part,{queryPart}");

            var paramType = typeof(T);
            var paramExp = Expression.Parameter(paramType);

            var property = GetProperty<T>(array[0]);

            if (property == null)
                throw new Exception($"cannot find a property named ${array[0]}");

            var leftExp = Expression.Property(paramExp, property);

            if (property.PropertyType == typeof(string))
            {
                array[2] = array[2].Trim('\'');
            }
            var rightValue = Convert.ChangeType(array[2], property.PropertyType);



            var rightExp = Expression.Constant(rightValue, property.PropertyType);

            Expression finalExp;
            switch (array[1])
            {
                case "eq": finalExp = Expression.Equal(leftExp, rightExp); break;
                case "neq": finalExp = Expression.NotEqual(leftExp, rightExp); break;
                case "lt": finalExp = Expression.LessThan(leftExp, rightExp); break;
                case "lte": finalExp = Expression.LessThanOrEqual(leftExp, rightExp); break;
                case "gt": finalExp = Expression.GreaterThan(leftExp, rightExp); break;
                case "gte": finalExp = Expression.GreaterThanOrEqual(leftExp, rightExp); break;

                default: throw new Exception($"Invalid operator {array[1]}");
            }

            return finalExp;

        }



        public Expression BuildExpression<T>(string [] queryParts) where T:class
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
                    var expressionPart = GetExpression<T>(queryPart);
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
