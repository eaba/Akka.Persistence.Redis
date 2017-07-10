﻿//-----------------------------------------------------------------------
// <copyright file="RedisCurrentEventsByTagSpec.cs" company="Akka.NET Project">
//     Copyright (C) 2017 Akka.NET Contrib <https://github.com/AkkaNetContrib/Akka.Persistence.Redis>
// </copyright>
//-----------------------------------------------------------------------

using Akka.Configuration;
using Akka.Persistence.Query;
using Akka.Persistence.Redis.Query;
using Akka.Persistence.TCK.Query;
using Xunit;
using Xunit.Abstractions;

namespace Akka.Persistence.Redis.Tests.Query
{
    [Collection("RedisSpec")]
    public sealed class RedisCurrentEventsByTagSpec : CurrentEventsByTagSpec
    {
        public const int Database = 1;

        public static Config Config(int id) => ConfigurationFactory.ParseString($@"
            akka.loglevel = INFO
            akka.persistence.journal.plugin = ""akka.persistence.journal.redis""
            akka.persistence.journal.redis {{
                event-adapters {{
                  color-tagger  = ""Akka.Persistence.TCK.Query.ColorFruitTagger, Akka.Persistence.TCK""
                }}
                event-adapter-bindings = {{
                  ""System.String"" = color-tagger
                }}
                class = ""Akka.Persistence.Redis.Journal.RedisJournal, Akka.Persistence.Redis""
                plugin-dispatcher = ""akka.actor.default-dispatcher""
                configuration-string = ""127.0.0.1:6379""
                database = {id}
                key-prefix = ""sbtech:""
            }}
            akka.test.single-expect-default = 3s")
            .WithFallback(RedisReadJournal.DefaultConfiguration());

        public RedisCurrentEventsByTagSpec(ITestOutputHelper output) : base(Config(Database), nameof(RedisCurrentEventsByTagSpec), output)
        {
            ReadJournal = Sys.ReadJournalFor<RedisReadJournal>(RedisReadJournal.Identifier);
        }

        [Fact(Skip = "Not implemented yet")]
        public override void ReadJournal_query_CurrentEventsByTag_should_find_events_from_offset()
        {
        }

        [Fact(Skip = "Not implemented yet")]
        public override void ReadJournal_query_CurrentEventsByTag_should_not_see_new_events_after_complete()
        {
        }

        protected override void Dispose(bool disposing)
        {
            DbUtils.Clean(Database);
            base.Dispose(disposing);
        }
    }
}
