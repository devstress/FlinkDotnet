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

using FlinkDotNet.JobManager.Workflows;

namespace FlinkDotNet.JobManager.Tests.Workflows;

/// <summary>
/// Base class for FlinkJobWorkflow tests providing common setup for fast test execution.
/// Optimizes workflow delays to 1ms for rapid test execution (following JobGateway pattern).
/// </summary>
public abstract class FlinkJobWorkflowTestBase
{
    public FlinkJobWorkflowTestBase()
    {
        // Set workflow delays to 1ms for fast test execution
        // This reduces test execution time from 5+ seconds per test to ~100ms
        // Following the same optimization pattern as JobGateway tests
        FlinkJobWorkflow.TaskMonitoringDelay = TimeSpan.FromMilliseconds(1);
    }
}
