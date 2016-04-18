// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Microbenchmarks.Core;
using Microsoft.EntityFrameworkCore.Microbenchmarks.Core.Models.AdventureWorks;
using Microsoft.EntityFrameworkCore.Microbenchmarks.Core.Models.AdventureWorks.TestHelpers;
using Microsoft.EntityFrameworkCore.Microbenchmarks.Models.AdventureWorks;
using Xunit;

namespace Microsoft.EntityFrameworkCore.Microbenchmarks
{
    public class InitializationTests : IClassFixture<AdventureWorksFixture>
    {
        [Benchmark]
        [BenchmarkVariation("Warm (1000 instances)", false, 1000)]
        public void CreateAndDisposeUnusedContext(IMetricCollector collector, bool cold, int count)
        {
            RunColdStartEnabledTest(cold, c => c.CreateAndDisposeUnusedContext(collector, count));
        }

        [Benchmark]
        [AdventureWorksDatabaseRequired]
        [BenchmarkVariation("Warm (100 instances)", false, 100)]
        public void InitializeAndQuery_AdventureWorks(IMetricCollector collector, bool cold, int count)
        {
            RunColdStartEnabledTest(cold, c => c.InitializeAndQuery_AdventureWorks(collector, count));
        }

        [Benchmark]
        [AdventureWorksDatabaseRequired]
        [BenchmarkVariation("Warm (100 instances)", false, 100)]
        public void InitializeAndSaveChanges_AdventureWorks(IMetricCollector collector, bool cold, int count)
        {
            RunColdStartEnabledTest(cold, t => t.InitializeAndSaveChanges_AdventureWorks(collector, count));
        }

        [Benchmark]
        public void BuildModel_AdventureWorks(IMetricCollector collector)
        {
            collector.StartCollection();

            var builder = new ModelBuilder(SqlServerConventionSetBuilder.Build());
            AdventureWorksContext.ConfigureModel(builder);

            var model = builder.Model;

            collector.StopCollection();

            Assert.Equal(67, model.GetEntityTypes().Count());
        }

        private void RunColdStartEnabledTest(bool cold, Action<ColdStartEnabledTests> test)
        {
            if (cold)
            {
#if NETCOREAPP1_0
                throw new NotSupportedException("ColdStartSandbox can not be used on CoreCLR.");
#else
                using (var sandbox = new ColdStartSandbox())
                {
                    var testClass = sandbox.CreateInstance<ColdStartEnabledTests>();
                    test(testClass);
                }
#endif
            }
            else
            {
                test(new ColdStartEnabledTests());
            }
        }

#if NET451
        private partial class ColdStartEnabledTests : MarshalByRefObject
        {
        }
#endif

        private partial class ColdStartEnabledTests
        {
            public void CreateAndDisposeUnusedContext(IMetricCollector collector, int count)
            {
                using (collector.StartCollection())
                {
                    for (var i = 0; i < count; i++)
                    {
                        using (var context = AdventureWorksFixture.CreateContext())
                        {
                        }
                    }
                }
            }

            public void InitializeAndQuery_AdventureWorks(IMetricCollector collector, int count)
            {
                using (collector.StartCollection())
                {
                    for (var i = 0; i < count; i++)
                    {
                        using (var context = AdventureWorksFixture.CreateContext())
                        {
                            context.Department.First();
                        }
                    }
                }
            }

            public void InitializeAndSaveChanges_AdventureWorks(IMetricCollector collector, int count)
            {
                using (collector.StartCollection())
                {
                    for (var i = 0; i < count; i++)
                    {
                        using (var context = AdventureWorksFixture.CreateContext())
                        {
                            context.Currency.Add(new Currency
                            {
                                CurrencyCode = "TMP",
                                Name = "Temporary"
                            });

                            using (context.Database.BeginTransaction())
                            {
                                context.SaveChanges();

                                // Don't mesure transaction rollback
                                collector.StopCollection();
                            }
                            collector.StartCollection();
                        }
                    }
                }
            }
        }
    }
}
