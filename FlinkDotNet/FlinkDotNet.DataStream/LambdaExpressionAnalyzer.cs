//  Licensed to the Apache Software Foundation (ASF) under one
//  or more contributor license agreements.  See the NOTICE file
//  distributed with this work for additional information
//  regarding copyright ownership.  The ASF licenses this file
//  to you under the Apache License, Version 2.0 (the
//  "License"); you may not use this file except in compliance
//  with the License.  You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.

using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq.Expressions;
using FlinkDotNet.Common.Logging;

namespace FlinkDotNet.DataStream
{
    /// <summary>
    /// Analyzes lambda expressions and translates them to Flink IR expressions.
    /// Supports .NET string methods, arithmetic operations, and method chaining.
    /// </summary>
    internal static class LambdaExpressionAnalyzer
    {
        private static readonly IFileSystem _fileSystem = new FileSystem();
        private static readonly Serilog.Core.Logger _logger = LoggerFactory.CreateLogger(_fileSystem);

        /// <summary>
        /// Mapping of .NET method names to Flink IR expressions
        /// </summary>
        private static readonly Dictionary<string, string> StringMethodMappings = new()
        {
            { "ToUpper", "upper" },
            { "ToUpperInvariant", "upper" },
            { "ToLower", "lower" },
            { "ToLowerInvariant", "lower" },
            { "Trim", "trim" },
            { "TrimStart", "ltrim" },
            { "TrimEnd", "rtrim" }
        };

        /// <summary>
        /// Analyzes a lambda expression and returns the corresponding Flink IR expression.
        /// Returns null if the lambda cannot be translated.
        /// </summary>
        public static string? AnalyzeLambda<TIn, TOut>(Expression<Func<TIn, TOut>> lambda)
        {
            if (lambda == null)
            {
                return null;
            }

            try
            {
                return AnalyzeExpression(lambda.Body);
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "[LambdaExpressionAnalyzer] Failed to analyze lambda expression: {ExpressionBody}", lambda.Body);
                return null;
            }
        }

        private static string? AnalyzeExpression(Expression expression)
        {
            return expression switch
            {
                MethodCallExpression methodCall => AnalyzeMethodCall(methodCall),
                BinaryExpression binary => AnalyzeBinaryExpression(binary),
                UnaryExpression unary => AnalyzeUnaryExpression(unary),
                ParameterExpression => "identity",
                _ => null
            };
        }

        private static string? AnalyzeMethodCall(MethodCallExpression methodCall)
        {
            string methodName = methodCall.Method.Name;

            // Check if this is a string method we support
            if (StringMethodMappings.TryGetValue(methodName, out string? flinkExpression))
            {
                // Check if this is a chained method call
                if (methodCall.Object is MethodCallExpression chainedCall)
                {
                    string? chainedExpression = AnalyzeMethodCall(chainedCall);
                    if (chainedExpression != null)
                    {
                        return $"{chainedExpression},{flinkExpression}";
                    }
                }

                return flinkExpression;
            }

            _logger.Warning("[LambdaExpressionAnalyzer] Unsupported method: {MethodName}", methodName);
            return null;
        }

        private static string? AnalyzeBinaryExpression(BinaryExpression binary)
        {
            string? operation = binary.NodeType switch
            {
                ExpressionType.Multiply => "multiply",
                ExpressionType.Add => "add",
                ExpressionType.Subtract => "subtract",
                ExpressionType.Divide => "divide",
                ExpressionType.Modulo => "modulo",
                ExpressionType.AddChecked => throw new NotImplementedException(),
                ExpressionType.And => throw new NotImplementedException(),
                ExpressionType.AndAlso => throw new NotImplementedException(),
                ExpressionType.ArrayLength => throw new NotImplementedException(),
                ExpressionType.ArrayIndex => throw new NotImplementedException(),
                ExpressionType.Call => throw new NotImplementedException(),
                ExpressionType.Coalesce => throw new NotImplementedException(),
                ExpressionType.Conditional => throw new NotImplementedException(),
                ExpressionType.Constant => throw new NotImplementedException(),
                ExpressionType.Convert => throw new NotImplementedException(),
                ExpressionType.ConvertChecked => throw new NotImplementedException(),
                ExpressionType.Equal => throw new NotImplementedException(),
                ExpressionType.ExclusiveOr => throw new NotImplementedException(),
                ExpressionType.GreaterThan => throw new NotImplementedException(),
                ExpressionType.GreaterThanOrEqual => throw new NotImplementedException(),
                ExpressionType.Invoke => throw new NotImplementedException(),
                ExpressionType.Lambda => throw new NotImplementedException(),
                ExpressionType.LeftShift => throw new NotImplementedException(),
                ExpressionType.LessThan => throw new NotImplementedException(),
                ExpressionType.LessThanOrEqual => throw new NotImplementedException(),
                ExpressionType.ListInit => throw new NotImplementedException(),
                ExpressionType.MemberAccess => throw new NotImplementedException(),
                ExpressionType.MemberInit => throw new NotImplementedException(),
                ExpressionType.MultiplyChecked => throw new NotImplementedException(),
                ExpressionType.Negate => throw new NotImplementedException(),
                ExpressionType.UnaryPlus => throw new NotImplementedException(),
                ExpressionType.NegateChecked => throw new NotImplementedException(),
                ExpressionType.New => throw new NotImplementedException(),
                ExpressionType.NewArrayInit => throw new NotImplementedException(),
                ExpressionType.NewArrayBounds => throw new NotImplementedException(),
                ExpressionType.Not => throw new NotImplementedException(),
                ExpressionType.NotEqual => throw new NotImplementedException(),
                ExpressionType.Or => throw new NotImplementedException(),
                ExpressionType.OrElse => throw new NotImplementedException(),
                ExpressionType.Parameter => throw new NotImplementedException(),
                ExpressionType.Power => throw new NotImplementedException(),
                ExpressionType.Quote => throw new NotImplementedException(),
                ExpressionType.RightShift => throw new NotImplementedException(),
                ExpressionType.SubtractChecked => throw new NotImplementedException(),
                ExpressionType.TypeAs => throw new NotImplementedException(),
                ExpressionType.TypeIs => throw new NotImplementedException(),
                ExpressionType.Assign => throw new NotImplementedException(),
                ExpressionType.Block => throw new NotImplementedException(),
                ExpressionType.DebugInfo => throw new NotImplementedException(),
                ExpressionType.Decrement => throw new NotImplementedException(),
                ExpressionType.Dynamic => throw new NotImplementedException(),
                ExpressionType.Default => throw new NotImplementedException(),
                ExpressionType.Extension => throw new NotImplementedException(),
                ExpressionType.Goto => throw new NotImplementedException(),
                ExpressionType.Increment => throw new NotImplementedException(),
                ExpressionType.Index => throw new NotImplementedException(),
                ExpressionType.Label => throw new NotImplementedException(),
                ExpressionType.RuntimeVariables => throw new NotImplementedException(),
                ExpressionType.Loop => throw new NotImplementedException(),
                ExpressionType.Switch => throw new NotImplementedException(),
                ExpressionType.Throw => throw new NotImplementedException(),
                ExpressionType.Try => throw new NotImplementedException(),
                ExpressionType.Unbox => throw new NotImplementedException(),
                ExpressionType.AddAssign => throw new NotImplementedException(),
                ExpressionType.AndAssign => throw new NotImplementedException(),
                ExpressionType.DivideAssign => throw new NotImplementedException(),
                ExpressionType.ExclusiveOrAssign => throw new NotImplementedException(),
                ExpressionType.LeftShiftAssign => throw new NotImplementedException(),
                ExpressionType.ModuloAssign => throw new NotImplementedException(),
                ExpressionType.MultiplyAssign => throw new NotImplementedException(),
                ExpressionType.OrAssign => throw new NotImplementedException(),
                ExpressionType.PowerAssign => throw new NotImplementedException(),
                ExpressionType.RightShiftAssign => throw new NotImplementedException(),
                ExpressionType.SubtractAssign => throw new NotImplementedException(),
                ExpressionType.AddAssignChecked => throw new NotImplementedException(),
                ExpressionType.MultiplyAssignChecked => throw new NotImplementedException(),
                ExpressionType.SubtractAssignChecked => throw new NotImplementedException(),
                ExpressionType.PreIncrementAssign => throw new NotImplementedException(),
                ExpressionType.PreDecrementAssign => throw new NotImplementedException(),
                ExpressionType.PostIncrementAssign => throw new NotImplementedException(),
                ExpressionType.PostDecrementAssign => throw new NotImplementedException(),
                ExpressionType.TypeEqual => throw new NotImplementedException(),
                ExpressionType.OnesComplement => throw new NotImplementedException(),
                ExpressionType.IsTrue => throw new NotImplementedException(),
                ExpressionType.IsFalse => throw new NotImplementedException(),
                _ => null
            };

            if (operation == null)
            {
                _logger.Warning("[LambdaExpressionAnalyzer] Unsupported binary operation: {NodeType}", binary.NodeType);
                return null;
            }

            // Extract operands
            string? left = ExtractOperand(binary.Left);
            string? right = ExtractOperand(binary.Right);

            if (left != null && right != null)
            {
                return $"{operation}:{left}:{right}";
            }

            return null;
        }

        private static string? AnalyzeUnaryExpression(UnaryExpression unary)
        {
            // Handle unary operations like negation
            if (unary.NodeType == ExpressionType.Negate)
            {
                string? operand = ExtractOperand(unary.Operand);
                if (operand != null)
                {
                    return $"negate:{operand}";
                }
            }

            return AnalyzeExpression(unary.Operand);
        }

        private static string? ExtractOperand(Expression operand)
        {
            return operand switch
            {
                ParameterExpression => "$input",
                ConstantExpression constant => constant.Value?.ToString(),
                BinaryExpression binary when binary.Left is ParameterExpression && binary.Right is ParameterExpression =>
                    "$input", // Special case: i * i becomes multiply:$input:$input
                _ => AnalyzeExpression(operand)
            };
        }
    }
}
