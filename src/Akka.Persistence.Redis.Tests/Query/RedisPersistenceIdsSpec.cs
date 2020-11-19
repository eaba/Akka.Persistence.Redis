//-----------------------------------------------------------------------
// <copyright file="RedisPersistenceIdsSpec.cs" company="Akka.NET Project">
//     Copyright (C) 2017 Akka.NET Contrib <https://github.com/AkkaNetContrib/Akka.Persistence.Redis>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Reflection;
using System.Threading.Tasks;
using Akka.Configuration;
using Akka.Persistence.Query;
using Akka.Persistence.Redis.Query;
using Akka.Persistence.TCK.Query;
using Akka.Streams.TestKit;
using Akka.Util.Internal;
using Reactive.Streams;
using StackExchange.Redis;
using Xunit;
using Xunit.Abstractions;

namespace Akka.Persistence.Redis.Tests.Query
{
    [Collection("RedisSpec")]
    public sealed class RedisPersistenceIdsSpec : PersistenceIdsSpec
    {
        public const int Database = 1;

        public static Config Config(RedisFixture fixture, int id)
        {
            DbUtils.Initialize(fixture);

            return ConfigurationFactory.ParseString($@"
            akka.loglevel = INFO
            akka.persistence.journal.plugin = ""akka.persistence.journal.redis""
            akka.persistence.journal.redis {{
                class = ""Akka.Persistence.Redis.Journal.RedisJournal, Akka.Persistence.Redis""
                plugin-dispatcher = ""akka.actor.default-dispatcher""
                configuration-string = ""{fixture.ConnectionString}""
                database = {id}
            }}
            akka.test.single-expect-default = 3s")
            .WithFallback(RedisPersistence.DefaultConfig());
        }

        public RedisPersistenceIdsSpec(ITestOutputHelper output, RedisFixture fixture) : base(Config(fixture, Database), nameof(RedisPersistenceIdsSpec), output)
        {
            ReadJournal = Sys.ReadJournalFor<RedisReadJournal>(RedisReadJournal.Identifier);
        }
        [Fact]
        public override Task ReadJournal_should_deallocate_AllPersistenceIds_publisher_when_the_last_subscriber_left()
        {
            if (AllocatesAllPersistenceIDsPublisher)
            {
                var journal = ReadJournal.AsInstanceOf<IPersistenceIdsQuery>();

                Setup("a", 1);
                Setup("b", 1);

                var source = journal.PersistenceIds();
                var probe =
                    source.RunWith(this.SinkProbe<string>(), Materializer);
                var probe2 =
                    source.RunWith(this.SinkProbe<string>(), Materializer);

                var fieldInfo = journal.GetType()
                    .GetField("_persistenceIdsPublisher",
                        BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.True(fieldInfo != null);

                // Assert that publisher is running.
                probe.Within(TimeSpan.FromSeconds(10), () => probe.Request(10)
                    .ExpectNextUnordered("a", "b")
                    .ExpectNoMsg(TimeSpan.FromMilliseconds(200)));

                probe.Cancel();

                // Assert that publisher is still alive when it still have a subscriber
                //Assert.True(fieldInfo.GetValue(journal) is IPublisher<string>);

                probe2.Within(TimeSpan.FromSeconds(10), () => probe2.Request(4)
                    .ExpectNextUnordered("a", "b")
                    .ExpectNoMsg(TimeSpan.FromMilliseconds(200)));

                // Assert that publisher is de-allocated when the last subscriber left
                probe2.Cancel();
                //await Task.Delay(400);
                //Assert.True(fieldInfo.GetValue(journal) is null);
            }

            return Task.CompletedTask;
        }

        [Fact]
        public void ReadJournal_AllPersistenceIds_should_fail_the_stage_on_connection_error()
        {
            // setup redis
            var address = Sys.Settings.Config.GetString("akka.persistence.journal.redis.configuration-string");
            var database = Sys.Settings.Config.GetInt("akka.persistence.journal.redis.database");

            var redis = ConnectionMultiplexer.Connect(address).GetDatabase(database);

            var queries = ReadJournal.AsInstanceOf<IPersistenceIdsQuery>();

            Setup("a", 1);

            var source = queries.PersistenceIds();
            var probe = source.RunWith(this.SinkProbe<string>(), Materializer);

            //// change type of value
            redis.StringSet("journal:persistenceIds", "1");

            probe.Within(TimeSpan.FromSeconds(10), () => probe.Request(1).ExpectError());
        }

        protected override void Dispose(bool disposing)
        {
            DbUtils.Clean(Database);
            base.Dispose(disposing);
        }
    }
}
