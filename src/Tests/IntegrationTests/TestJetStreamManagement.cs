﻿// Copyright 2021 The NATS Authors
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Text;
using NATS.Client;
using NATS.Client.Internals;
using NATS.Client.JetStream;
using Xunit;
using static IntegrationTests.JetStreamTestBase;
using static UnitTests.TestBase;

namespace IntegrationTests
{
    public class TestJetStreamManagement : TestSuite<JetStreamManagementSuiteContext>
    {
        public TestJetStreamManagement(JetStreamManagementSuiteContext context) : base(context) {}

        [Fact]
        public void TestStreamCreate()
        {
            Context.RunInJsServer(c =>
            {
                IJetStreamManagement jsm = c.CreateJetStreamManagementContext();

                StreamConfiguration sc = StreamConfiguration.Builder()
                    .WithName(STREAM)
                    .WithStorageType(StorageType.Memory)
                    .WithSubjects(Subject(0), Subject(1))
                    .Build();

                StreamInfo si = jsm.AddStream(sc);
                Assert.NotNull(si.Config);
                sc = si.Config;
                Assert.Equal(STREAM, sc.Name);

                Assert.Equal(2, sc.Subjects.Count);
                Assert.Equal(Subject(0), sc.Subjects[0]);
                Assert.Equal(Subject(1), sc.Subjects[1]);

                Assert.Equal(RetentionPolicy.Limits, sc.RetentionPolicy);
                Assert.Equal(DiscardPolicy.Old, sc.DiscardPolicy);
                Assert.Equal(StorageType.Memory, sc.StorageType);

                Assert.NotNull(si.State);
                Assert.Equal(-1, sc.MaxConsumers);
                Assert.Equal(-1, sc.MaxMsgs);
                Assert.Equal(-1, sc.MaxBytes);
                Assert.Equal(-1, sc.MaxMsgSize);
                Assert.Equal(1, sc.Replicas);

                Assert.Equal(Duration.Zero, sc.MaxAge);
                Assert.Equal(Duration.OfSeconds(120), sc.DuplicateWindow);
                Assert.False(sc.NoAck);
                Assert.Empty(sc.TemplateOwner);

                StreamState ss = si.State;
                Assert.Equal(0u, ss.Messages);
                Assert.Equal(0u, ss.Bytes);
                Assert.Equal(0u, ss.FirstSeq);
                Assert.Equal(0u, ss.LastSeq);
                Assert.Equal(0u, ss.ConsumerCount);
            });
        }

        [Fact]
        public void TestStreamCreateWithNoSubject() {
            Context.RunInJsServer(c =>
            {
                IJetStreamManagement jsm = c.CreateJetStreamManagementContext();

                StreamConfiguration sc = StreamConfiguration.Builder()
                    .WithName(STREAM)
                    .WithStorageType(StorageType.Memory)
                    .Build();

                StreamInfo si = jsm.AddStream(sc);
                sc = si.Config;
                Assert.Equal(STREAM, sc.Name);

                Assert.Single(sc.Subjects);
                Assert.Equal(STREAM, sc.Subjects[0]);

                Assert.Equal(RetentionPolicy.Limits, sc.RetentionPolicy);
                Assert.Equal(DiscardPolicy.Old, sc.DiscardPolicy);
                Assert.Equal(StorageType.Memory, sc.StorageType);

                Assert.NotNull(si.Config);
                Assert.NotNull(si.State);
                Assert.Equal(-1, sc.MaxConsumers);
                Assert.Equal(-1, sc.MaxMsgs);
                Assert.Equal(-1, sc.MaxBytes);
                Assert.Equal(-1, sc.MaxMsgSize);
                Assert.Equal(1, sc.Replicas);

                Assert.Equal(Duration.Zero, sc.MaxAge);
                Assert.Equal(Duration.OfSeconds(120), sc.DuplicateWindow);
                Assert.False(sc.NoAck);
                Assert.True(string.IsNullOrEmpty(sc.TemplateOwner));

                StreamState ss = si.State;
                Assert.Equal(0U, ss.Messages);
                Assert.Equal(0U, ss.Bytes);
                Assert.Equal(0U, ss.FirstSeq);
                Assert.Equal(0U, ss.LastSeq);
                Assert.Equal(0, ss.ConsumerCount);
            });
        }

        [Fact]
        public void TestUpdateStream()
        {
            Context.RunInJsServer(c =>
            {
                IJetStreamManagement jsm = c.CreateJetStreamManagementContext();
                StreamInfo si = AddTestStream(jsm);
                StreamConfiguration sc = si.Config;
                Assert.NotNull(sc);
                Assert.Equal(STREAM, sc.Name);
                Assert.NotNull(sc.Subjects);
                Assert.Equal(2, sc.Subjects.Count);
                Assert.Equal(Subject(0), sc.Subjects[0]);
                Assert.Equal(Subject(1), sc.Subjects[1]);
                Assert.Equal(-1, sc.MaxBytes);
                Assert.Equal(-1, sc.MaxMsgSize);
                Assert.Equal(Duration.Zero, sc.MaxAge);
                Assert.Equal(StorageType.Memory, sc.StorageType);
                Assert.Equal(DiscardPolicy.Old, sc.DiscardPolicy);
                Assert.Equal(1, sc.Replicas);
                Assert.False(sc.NoAck);
                Assert.Equal(Duration.OfMinutes(2), sc.DuplicateWindow);
                Assert.Empty(sc.TemplateOwner);

                sc = StreamConfiguration.Builder()
                        .WithName(STREAM)
                        .WithStorageType(StorageType.Memory)
                        .WithSubjects(Subject(0), Subject(1), Subject(2))
                        .WithMaxBytes(43)
                        .WithMaxMsgSize(44)
                        .WithMaxAge(Duration.OfDays(100))
                        .WithDiscardPolicy(DiscardPolicy.New)
                        .WithNoAck(true)
                        .WithDuplicateWindow(Duration.OfMinutes(3))
                        .Build();
                si = jsm.UpdateStream(sc);
                Assert.NotNull(si);

                sc = si.Config;
                Assert.NotNull(sc);
                Assert.Equal(STREAM, sc.Name);
                Assert.NotNull(sc.Subjects);
                Assert.Equal(3, sc.Subjects.Count);
                Assert.Equal(Subject(0), sc.Subjects[0]);
                Assert.Equal(Subject(1), sc.Subjects[1]);
                Assert.Equal(Subject(2), sc.Subjects[2]);
                Assert.Equal(43u, sc.MaxBytes);
                Assert.Equal(44, sc.MaxMsgSize);
                Assert.Equal(Duration.OfDays(100), sc.MaxAge);
                Assert.Equal(StorageType.Memory, sc.StorageType);
                Assert.Equal(DiscardPolicy.New, sc.DiscardPolicy);
                Assert.Equal(1, sc.Replicas);
                Assert.True(sc.NoAck);
                Assert.Equal(Duration.OfMinutes(3), sc.DuplicateWindow);
                Assert.Empty(sc.TemplateOwner);
                
                // allowed to change Allow Direct
                jsm.DeleteStream(STREAM);
                jsm.AddStream(GetTestStreamConfigurationBuilder().WithAllowDirect(false).Build());
                jsm.UpdateStream(GetTestStreamConfigurationBuilder().WithAllowDirect(true).Build());
                jsm.UpdateStream(GetTestStreamConfigurationBuilder().WithAllowDirect(false).Build());
            });
        }
        
        [Fact]
        public void TestAddUpdateStreamInvalids()
        {
            Context.RunInJsServer(c =>
            {
                IJetStreamManagement jsm = c.CreateJetStreamManagementContext();

                StreamConfiguration scNoName = StreamConfiguration.Builder().Build();
                Assert.Throws<ArgumentNullException>(() => jsm.AddStream(null));
                Assert.Throws<ArgumentException>(() => jsm.AddStream(scNoName));
                Assert.Throws<ArgumentNullException>(() => jsm.UpdateStream(null));
                Assert.Throws<ArgumentException>(() => jsm.UpdateStream(scNoName));

                
                // cannot update non existent stream
                StreamConfiguration sc = GetTestStreamConfiguration();
                // stream not added yet
                Assert.Throws<NATSJetStreamException>(() => jsm.UpdateStream(sc));

                // add the stream
                jsm.AddStream(sc);

                // cannot change MaxConsumers
                StreamConfiguration scMaxCon = GetTestStreamConfigurationBuilder()
                    .WithMaxConsumers(2)
                    .Build();
                Assert.Throws<NATSJetStreamException>(() => jsm.UpdateStream(scMaxCon));

                // cannot change RetentionPolicy
                StreamConfiguration scRetention = GetTestStreamConfigurationBuilder()
                    .WithRetentionPolicy(RetentionPolicy.Interest)
                    .Build();
                Assert.Throws<NATSJetStreamException>(() => jsm.UpdateStream(scRetention));
            });
        }

        private static StreamInfo AddTestStream(IJetStreamManagement jsm) {
            StreamInfo si = jsm.AddStream(GetTestStreamConfiguration());
            Assert.NotNull(si);
            return si;
        }

        private static StreamConfiguration GetTestStreamConfiguration() {
            return GetTestStreamConfigurationBuilder().Build();
        }

        private static StreamConfiguration.StreamConfigurationBuilder GetTestStreamConfigurationBuilder()
        {
            return StreamConfiguration.Builder()
                .WithName(STREAM)
                .WithStorageType(StorageType.Memory)
                .WithSubjects(Subject(0), Subject(1));
        }

        [Fact]
        public void TestGetStreamInfo()
        {
            Context.RunInJsServer(c =>
            {
                IJetStreamManagement jsm = c.CreateJetStreamManagementContext();
                Assert.Throws<NATSJetStreamException>(() => jsm.GetStreamInfo(STREAM));

                String[] subjects = new String[6];
                for (int x = 0; x < 5; x++) {
                    subjects[x] = Subject(x);
                }
                subjects[5] = "foo.>";
                CreateMemoryStream(jsm, STREAM, subjects);

                IList<PublishAck> packs = new List<PublishAck>();
                IJetStream js = c.CreateJetStreamContext();
                for (int x = 0; x < 5; x++) {
                    JsPublish(js, Subject(x), x + 1);
                    PublishAck pa = JsPublish(js, Subject(x), Data(x + 2));
                    packs.Add(pa);
                    jsm.DeleteMessage(STREAM, pa.Seq);
                }
                JsPublish(js, "foo.bar", 6);

                StreamInfo si = jsm.GetStreamInfo(STREAM);
                Assert.Equal(STREAM, si.Config.Name);
                Assert.Equal(6, si.State.SubjectCount);
                Assert.Null(si.State.Subjects);
                Assert.Equal(5, si.State.DeletedCount);
                Assert.Empty(si.State.Deleted);

                si = jsm.GetStreamInfo(STREAM, 
                    StreamInfoOptions.Builder()
                        .WithAllSubjects()
                        .WithDeletedDetails()
                        .Build());
                Assert.Equal(STREAM, si.Config.Name);
                Assert.Equal(6, si.State.SubjectCount);
                IList<Subject> list = si.State.Subjects;
                Assert.NotNull(list);
                Assert.Equal(6, list.Count);
                Assert.Equal(5, si.State.DeletedCount);
                Assert.Equal(5, si.State.Deleted.Count);
                Dictionary<string, Subject> map = new Dictionary<string, Subject>();
                foreach (Subject su in list) {
                    map[su.Name] = su;
                }
                for (int x = 0; x < 5; x++)
                {
                    Subject subj = map[Subject(x)];
                    Assert.NotNull(subj);
                    Assert.Equal(x + 1, subj.Count);
                }
                Subject s = map["foo.bar"];
                Assert.NotNull(s);
                Assert.Equal(6, s.Count);

                foreach (PublishAck pa in packs) {
                    Assert.True(si.State.Deleted.Contains(pa.Seq));
                }
            });
        }

        [Fact]
        public void TestDeleteStream()
        {
            Context.RunInJsServer(c =>
            {
                IJetStreamManagement jsm = c.CreateJetStreamManagementContext();
                NATSJetStreamException e = 
                    Assert.Throws<NATSJetStreamException>(() => jsm.DeleteStream(STREAM));
                Assert.Equal(10059, e.ApiErrorCode);

                CreateDefaultTestStream(c);
                Assert.NotNull(jsm.GetStreamInfo(STREAM));
                Assert.True(jsm.DeleteStream(STREAM));

                e = Assert.Throws<NATSJetStreamException>(() => jsm.GetStreamInfo(STREAM));
                Assert.Equal(10059, e.ApiErrorCode);                

                e = Assert.Throws<NATSJetStreamException>(() => jsm.DeleteStream(STREAM));
                Assert.Equal(10059, e.ApiErrorCode);                
            });
        }

        [Fact]
        public void TestPurgeStreamAndOptions()
        {
            Context.RunInJsServer(c =>
            {
                Assert.Throws<ArgumentException>(() => PurgeOptions.Builder().WithKeep(1).WithSequence(1).Build());

                IJetStreamManagement jsm = c.CreateJetStreamManagementContext();
                Assert.Throws<NATSJetStreamException>(() => jsm.PurgeStream(STREAM));
                
                CreateMemoryStream(c, STREAM, Subject(1), Subject(2));
                
                StreamInfo si = jsm.GetStreamInfo(STREAM);
                Assert.Equal(0u, si.State.Messages);                

                JsPublish(c, Subject(1), 10);
                si = jsm.GetStreamInfo(STREAM);
                Assert.Equal(10u, si.State.Messages);

                PurgeOptions options = PurgeOptions.Builder().WithKeep(7).Build();
                PurgeResponse pr = jsm.PurgeStream(STREAM, options);
                Assert.True(pr.Success);
                Assert.Equal(3u, pr.Purged);

                options = PurgeOptions.Builder().WithSequence(9).Build();
                pr = jsm.PurgeStream(STREAM, options);
                Assert.True(pr.Success);
                Assert.Equal(5u, pr.Purged);
                si = jsm.GetStreamInfo(STREAM);
                Assert.Equal(2u, si.State.Messages);

                pr = jsm.PurgeStream(STREAM);
                Assert.True(pr.Success);
                Assert.Equal(2u, pr.Purged);
                si = jsm.GetStreamInfo(STREAM);
                Assert.Equal(0u, si.State.Messages);

                JsPublish(c, Subject(1), 10);
                JsPublish(c, Subject(2), 10);
                si = jsm.GetStreamInfo(STREAM);
                Assert.Equal(20u, si.State.Messages);
                jsm.PurgeStream(STREAM, PurgeOptions.WithSubject(Subject(1)));
                si = jsm.GetStreamInfo(STREAM);
                Assert.Equal(10u, si.State.Messages);

                options = PurgeOptions.Builder().WithSubject(Subject(1)).WithSequence(1).Build();
                Assert.Equal(Subject(1), options.Subject);
                Assert.Equal(1u, options.Sequence);

                options = PurgeOptions.Builder().WithSubject(Subject(1)).WithKeep(2).Build();
                Assert.Equal(2u, options.Keep);
            });
        }

        [Fact]
        public void TestAddDeleteConsumer()
        {
            Context.RunInJsServer(c =>
            {
                bool atLeast290 = c.ServerInfo.IsSameOrNewerThanVersion("2.9.0");

                IJetStreamManagement jsm = c.CreateJetStreamManagementContext();
                CreateMemoryStream(jsm, STREAM, SubjectDot(">"));
                
                IList<ConsumerInfo> list = jsm.GetConsumers(STREAM);
                Assert.Empty(list);

                ConsumerConfiguration cc = ConsumerConfiguration.Builder().Build();
                
                ArgumentException e = Assert.Throws<ArgumentException>(
                    () => jsm.AddOrUpdateConsumer(null, cc));
                Assert.Contains("Stream cannot be null or empty", e.Message);
                
                e = Assert.Throws<ArgumentNullException>(
                    () => jsm.AddOrUpdateConsumer(STREAM, null));
                Assert.Contains("Value cannot be null", e.Message);
                
                e = Assert.Throws<ArgumentNullException>(
                    () => jsm.AddOrUpdateConsumer(STREAM, cc));
                Assert.Contains("Value cannot be null", e.Message);

                // with and w/o deliver subject for push/pull
                AddConsumer(jsm, atLeast290, 1, false, null, ConsumerConfiguration.Builder()
                    .WithDurable(Durable(1))
                    .Build());

                AddConsumer(jsm, atLeast290, 2, true, null, ConsumerConfiguration.Builder()
                    .WithDurable(Durable(2))
                    .WithDeliverSubject(Deliver(2))
                    .Build());

                // test delete here
                IList<string> consumers = jsm.GetConsumerNames(STREAM);
                Assert.Equal(2, consumers.Count);
                Assert.True(jsm.DeleteConsumer(STREAM, Durable(1)));
                consumers = jsm.GetConsumerNames(STREAM);
                Assert.Equal(1, consumers.Count);
                Assert.Throws<NATSJetStreamException>(() => jsm.DeleteConsumer(STREAM, Durable(1)));

                // some testing of new name
                if (atLeast290) {
                    AddConsumer(jsm, atLeast290, 3, false, null, ConsumerConfiguration.Builder()
                        .WithDurable(Durable(3))
                        .WithName(Durable(3))
                        .Build());

                    AddConsumer(jsm, atLeast290, 4, true, null, ConsumerConfiguration.Builder()
                        .WithDurable(Durable(4))
                        .WithName(Durable(4))
                        .WithDeliverSubject(Deliver(4))
                        .Build());

                    AddConsumer(jsm, atLeast290, 5, false, ">", ConsumerConfiguration.Builder()
                        .WithDurable(Durable(5))
                        .WithFilterSubject(">")
                        .Build());

                    AddConsumer(jsm, atLeast290, 6, false, SubjectDot(">"), ConsumerConfiguration.Builder()
                        .WithDurable(Durable(6))
                        .WithFilterSubject(SubjectDot(">"))
                        .Build());

                    AddConsumer(jsm, atLeast290, 7, false, SubjectDot("foo"), ConsumerConfiguration.Builder()
                        .WithDurable(Durable(7))
                        .WithFilterSubject(SubjectDot("foo"))
                        .Build());
                }
            });
        }

        private static void AddConsumer(IJetStreamManagement jsm, bool atLeast290, int id, bool deliver, string fs, ConsumerConfiguration cc) {
            ConsumerInfo ci = jsm.AddOrUpdateConsumer(STREAM, cc);
            Assert.Equal(Durable(id), ci.Name);
            if (atLeast290) {
                Assert.Equal(Durable(id), ci.ConsumerConfiguration.Name);
            }
            Assert.Equal(Durable(id), ci.ConsumerConfiguration.Durable);
            if (fs == null) {
                Assert.Empty(ci.ConsumerConfiguration.FilterSubject);
            }
            if (deliver) {
                Assert.Equal(Deliver(id), ci.ConsumerConfiguration.DeliverSubject);
            }
        }

        [Fact]
        public void TestValidConsumerUpdates()
        {
            Context.RunInJsServer(c =>
            {
                IJetStreamManagement jsm = c.CreateJetStreamManagementContext();
                CreateMemoryStream(jsm, STREAM, SUBJECT_GT);

                ConsumerConfiguration cc = PrepForUpdateTest(jsm);
                cc = ConsumerConfiguration.Builder(cc).WithDeliverSubject(Deliver(2)).Build();
                AssertValidAddOrUpdate(jsm, cc);

                cc = PrepForUpdateTest(jsm);
                cc = ConsumerConfiguration.Builder(cc).WithAckWait(Duration.OfSeconds(5)).Build();
                AssertValidAddOrUpdate(jsm, cc);

                cc = PrepForUpdateTest(jsm);
                cc = ConsumerConfiguration.Builder(cc).WithRateLimitBps(100).Build();
                AssertValidAddOrUpdate(jsm, cc);

                cc = PrepForUpdateTest(jsm);
                cc = ConsumerConfiguration.Builder(cc).WithMaxAckPending(100).Build();
                AssertValidAddOrUpdate(jsm, cc);

                cc = PrepForUpdateTest(jsm);
                cc = ConsumerConfiguration.Builder(cc).WithMaxDeliver(4).Build();
                AssertValidAddOrUpdate(jsm, cc);

                if (c.ServerInfo.IsNewerVersionThan("2.8.4"))
                {
                    cc = PrepForUpdateTest(jsm);
                    cc = ConsumerConfiguration.Builder(cc).WithFilterSubject(SUBJECT_STAR).Build();
                    AssertValidAddOrUpdate(jsm, cc);
                }
            });
        }

        [Fact]
        public void TestInvalidConsumerUpdates()
        {
            Context.RunInJsServer(c =>
            {
                IJetStreamManagement jsm = c.CreateJetStreamManagementContext();
                CreateMemoryStream(jsm, STREAM, SUBJECT_GT);

                ConsumerConfiguration cc = PrepForUpdateTest(jsm);
                cc = ConsumerConfiguration.Builder(cc).WithDeliverPolicy(DeliverPolicy.New).Build();
                AssertInvalidConsumerUpdate(jsm, cc);

                if (c.ServerInfo.IsSameOrOlderThanVersion("2.8.4"))
                {
                    cc = PrepForUpdateTest(jsm);
                    cc = ConsumerConfiguration.Builder(cc).WithFilterSubject(SUBJECT_STAR).Build();
                    AssertInvalidConsumerUpdate(jsm, cc);
                }

                cc = PrepForUpdateTest(jsm);
                cc = ConsumerConfiguration.Builder(cc).WithIdleHeartbeat(Duration.OfMillis(111)).Build();
                AssertInvalidConsumerUpdate(jsm, cc);
            });
        }
        
        private ConsumerConfiguration PrepForUpdateTest(IJetStreamManagement jsm)
        {
            try {
                jsm.DeleteConsumer(STREAM, Durable(1));
            }
            catch (Exception) { /* ignore */ }

            ConsumerConfiguration cc = ConsumerConfiguration.Builder()
                .WithDurable(Durable(1))
                .WithAckPolicy(AckPolicy.Explicit)
                .WithDeliverSubject(Deliver(1))
                .WithMaxDeliver(3)
                .WithFilterSubject(SUBJECT_GT)
                .Build();
            AssertValidAddOrUpdate(jsm, cc);
            return cc;
        }

        private void AssertInvalidConsumerUpdate(IJetStreamManagement jsm, ConsumerConfiguration cc) {
            NATSJetStreamException e = Assert.Throws<NATSJetStreamException>(() => jsm.AddOrUpdateConsumer(STREAM, cc));
            Assert.Equal(10012, e.ApiErrorCode);
            Assert.Equal(500, e.ErrorCode);
        }

        private void AssertValidAddOrUpdate(IJetStreamManagement jsm, ConsumerConfiguration cc) {
            ConsumerInfo ci = jsm.AddOrUpdateConsumer(STREAM, cc);
            ConsumerConfiguration ciCc = ci.ConsumerConfiguration;
            Assert.Equal(cc.Durable, ci.Name);
            Assert.Equal(cc.Durable, ciCc.Durable);
            Assert.Equal(cc.DeliverSubject, ciCc.DeliverSubject);
            Assert.Equal(cc.MaxDeliver, ciCc.MaxDeliver);
            Assert.Equal(cc.DeliverPolicy, ciCc.DeliverPolicy);

            IList<string> consumers = jsm.GetConsumerNames(STREAM);
            Assert.Single(consumers);
            Assert.Equal(cc.Durable, consumers[0]);
        }

        [Fact]
        public void TestCreateConsumersWithFilters()
        {
            Context.RunInJsServer(c => {
                IJetStreamManagement jsm = c.CreateJetStreamManagementContext();

                // plain subject
                CreateDefaultTestStream(jsm);
                
                ConsumerConfiguration.ConsumerConfigurationBuilder builder = ConsumerConfiguration.Builder().WithDurable(DURABLE);
                jsm.AddOrUpdateConsumer(STREAM, builder.WithFilterSubject(SUBJECT).Build());
                
                Assert.Throws<NATSJetStreamException>(() => jsm.AddOrUpdateConsumer(STREAM,
                    builder.WithFilterSubject(SubjectDot("not-match")).Build()));

                // wildcard subject
                jsm.DeleteStream(STREAM);
                CreateMemoryStream(jsm, STREAM, SUBJECT_STAR);

                jsm.AddOrUpdateConsumer(STREAM, builder.WithFilterSubject(SubjectDot("A")).Build());

                if (c.ServerInfo.IsSameOrOlderThanVersion("2.8.4"))
                {
                    Assert.Throws<NATSJetStreamException>(() => jsm.AddOrUpdateConsumer(STREAM,
                        builder.WithFilterSubject(SubjectDot("not-match")).Build()));
                }

                // gt subject
                jsm.DeleteStream(STREAM);
                CreateMemoryStream(jsm, STREAM, SUBJECT_GT);

                jsm.AddOrUpdateConsumer(STREAM, builder.WithFilterSubject(SubjectDot("A")).Build());

                jsm.AddOrUpdateConsumer(STREAM, ConsumerConfiguration.Builder()
                    .WithDurable(Durable(42))
                    .WithFilterSubject(SubjectDot("F"))
                    .Build()
                );
            });
        }
        

        [Fact]
        public void TestGetConsumerInfo() 
        {
            Context.RunInJsServer(c => {
                IJetStreamManagement jsm = c.CreateJetStreamManagementContext();
                CreateDefaultTestStream(jsm);
                Assert.Throws<NATSJetStreamException>(() => jsm.DeleteConsumer(STREAM, DURABLE));
                ConsumerConfiguration cc = ConsumerConfiguration.Builder().WithDurable(DURABLE).Build();
                ConsumerInfo ci = jsm.AddOrUpdateConsumer(STREAM, cc);
                Assert.Equal(STREAM, ci.Stream);
                Assert.Equal(DURABLE, ci.Name);
                ci = jsm.GetConsumerInfo(STREAM, DURABLE);
                Assert.Equal(STREAM, ci.Stream);
                Assert.Equal(DURABLE, ci.Name);
                Assert.Throws<NATSJetStreamException>(() => jsm.GetConsumerInfo(STREAM, Durable(999)));
            });
        }

        [Fact]
        public void TestGetConsumers() 
        {
            Context.RunInJsServer(c => {
                IJetStreamManagement jsm = c.CreateJetStreamManagementContext();
                CreateMemoryStream(jsm, STREAM, Subject(0), Subject(1));

                AddConsumers(jsm, STREAM, 600, "A", null); // getConsumers pages at 256

                IList<ConsumerInfo> list = jsm.GetConsumers(STREAM);
                Assert.Equal(600, list.Count);

                AddConsumers(jsm, STREAM, 500, "B", null); // getConsumerNames pages at 1024
                IList<string> names = jsm.GetConsumerNames(STREAM);
                Assert.Equal(1100, names.Count);
            });
        }

        private void AddConsumers(IJetStreamManagement jsm, string stream, int count, string durableVary, string filterSubject)
        {
            for (int x = 0; x < count; x++) {
                String dur = Durable(durableVary, x + 1);
                ConsumerConfiguration cc = ConsumerConfiguration.Builder()
                        .WithDurable(dur)
                        .WithFilterSubject(filterSubject)
                        .Build();
                ConsumerInfo ci = jsm.AddOrUpdateConsumer(stream, cc);
                Assert.Equal(dur, ci.Name);
                Assert.Equal(dur, ci.ConsumerConfiguration.Durable);
                Assert.Empty(ci.ConsumerConfiguration.DeliverSubject);
            }
        }

        [Fact]
        public void TestGetStreams()
        {
            Context.RunInJsServer(c => {
                IJetStreamManagement jsm = c.CreateJetStreamManagementContext();

                AddStreams(jsm, 600, 0); // getStreams pages at 256

                IList<StreamInfo> list = jsm.GetStreams();
                Assert.Equal(600, list.Count);

                AddStreams(jsm, 500, 600); // getStreamNames pages at 1024
                IList<string> names = jsm.GetStreamNames();
                Assert.Equal(1100, names.Count);
            });
        }

        private void AddStreams(IJetStreamManagement jsm, int count, int adj) {
            for (int x = 0; x < count; x++) {
                CreateMemoryStream(jsm, Stream(x + adj), Subject(x + adj));
            }
        }

        [Fact]
        public void TestDeleteMessage() {
            MessageDeleteRequest mdr = new MessageDeleteRequest(1, true);
            Assert.Equal("{\"seq\":1}", Encoding.UTF8.GetString(mdr.Serialize()));
            Assert.Equal(1U, mdr.Sequence);
            Assert.True(mdr.Erase);
            Assert.False(mdr.NoErase);
            
            mdr = new MessageDeleteRequest(1, false);
            Assert.Equal("{\"seq\":1,\"no_erase\":true}", Encoding.UTF8.GetString(mdr.Serialize()));
            Assert.Equal(1U, mdr.Sequence);
            Assert.False(mdr.Erase);
            Assert.True(mdr.NoErase);

            Context.RunInJsServer(c => {
                CreateDefaultTestStream(c);
                IJetStream js = c.CreateJetStreamContext();

                MsgHeader h = new MsgHeader { { "foo", "bar" } };

                js.Publish(new Msg(SUBJECT, null, h, DataBytes(1)));
                js.Publish(new Msg(SUBJECT, null));

                IJetStreamManagement jsm = c.CreateJetStreamManagementContext();

                MessageInfo mi = jsm.GetMessage(STREAM, 1);
                Assert.Equal(SUBJECT, mi.Subject);
                Assert.Equal(Data(1), Encoding.ASCII.GetString(mi.Data));
                Assert.Equal(1U, mi.Sequence);
                Assert.NotNull(mi.Headers);
                Assert.Equal("bar", mi.Headers["foo"]);

                mi = jsm.GetMessage(STREAM, 2);
                Assert.Equal(SUBJECT, mi.Subject);
                Assert.Null(mi.Data);
                Assert.Equal(2U, mi.Sequence);
                Assert.Null(mi.Headers);

                Assert.True(jsm.DeleteMessage(STREAM, 1, false)); // added coverage for use of erase (no_erase) flag.
                Assert.Throws<NATSJetStreamException>(() => jsm.DeleteMessage(STREAM, 1));
                Assert.Throws<NATSJetStreamException>(() => jsm.GetMessage(STREAM, 1));
                Assert.Throws<NATSJetStreamException>(() => jsm.GetMessage(STREAM, 3));
                Assert.Throws<NATSJetStreamException>(() => jsm.DeleteMessage(Stream(999), 1));
                Assert.Throws<NATSJetStreamException>(() => jsm.GetMessage(Stream(999), 1));
            });
        }

        [Fact]
        public void TestGetStreamNamesBySubjectFilter()
        {
            Context.RunInJsServer(c => {
                IJetStreamManagement jsm = c.CreateJetStreamManagementContext();
                
                CreateMemoryStream(jsm, Stream(1), "foo");
                CreateMemoryStream(jsm, Stream(2), "bar");
                CreateMemoryStream(jsm, Stream(3), "a.a");
                CreateMemoryStream(jsm, Stream(4), "a.b");

                IList<string> list = jsm.GetStreamNamesBySubjectFilter("*");
                AssertStreamNameList(list, 1, 2);

                list = jsm.GetStreamNamesBySubjectFilter(">");
                AssertStreamNameList(list, 1, 2, 3, 4);

                list = jsm.GetStreamNamesBySubjectFilter("*.*");
                AssertStreamNameList(list, 3, 4);

                list = jsm.GetStreamNamesBySubjectFilter("a.>");
                AssertStreamNameList(list, 3, 4);

                list = jsm.GetStreamNamesBySubjectFilter("a.*");
                AssertStreamNameList(list, 3, 4);

                list = jsm.GetStreamNamesBySubjectFilter("foo");
                AssertStreamNameList(list, 1);

                list = jsm.GetStreamNamesBySubjectFilter("a.a");
                AssertStreamNameList(list, 3);

                list = jsm.GetStreamNamesBySubjectFilter("nomatch");
                AssertStreamNameList(list);
            });
        }

        private void AssertStreamNameList(IList<string> list, params int[] ids) {
            Assert.NotNull(list);
            Assert.Equal(ids.Length, list.Count);
            foreach (int id in ids) {
                Assert.Contains(Stream(id), list);
            }
        }

        [Fact]
        public void TestConsumerReplica()
        {
            Context.RunInJsServer(c => {
                IJetStreamManagement jsm = c.CreateJetStreamManagementContext();
                CreateMemoryStream(jsm, STREAM, Subject(0), Subject(1));

                ConsumerConfiguration cc0 = ConsumerConfiguration.Builder()
                    .WithDurable(Durable(0))
                    .Build();
                ConsumerInfo ci = jsm.AddOrUpdateConsumer(STREAM, cc0);

                // server returns 0 when value is not set
                Assert.Equal(0, ci.ConsumerConfiguration.NumReplicas);
                
                ConsumerConfiguration cc1 = ConsumerConfiguration.Builder()
                    .WithDurable(Durable(0))
                    .WithNumReplicas(1)
                    .Build();
                ci = jsm.AddOrUpdateConsumer(STREAM, cc1);
                Assert.Equal(1, ci.ConsumerConfiguration.NumReplicas);
            });
        }

        [Fact]
        public void TestGetMessage()
        {
            Context.RunInJsServer(c => {
                IJetStreamManagement jsm = c.CreateJetStreamManagementContext();
                IJetStream js = c.CreateJetStreamContext();

                StreamConfiguration sc = StreamConfiguration.Builder()
                    .WithName(STREAM)
                    .WithStorageType(StorageType.Memory)
                    .WithSubjects(Subject(1), Subject(2))
                    .Build();
                StreamInfo si = jsm.AddStream(sc);
                Assert.False(si.Config.AllowDirect);
                
                js.Publish(Subject(1), Encoding.UTF8.GetBytes("s1-q1"));
                js.Publish(Subject(2), Encoding.UTF8.GetBytes("s2-q2"));
                js.Publish(Subject(1), Encoding.UTF8.GetBytes("s1-q3"));
                js.Publish(Subject(2), Encoding.UTF8.GetBytes("s2-q4"));
                js.Publish(Subject(1), Encoding.UTF8.GetBytes("s1-q5"));
                js.Publish(Subject(2), Encoding.UTF8.GetBytes("s2-q6"));

                ValidateGetMessage(jsm);

                sc = StreamConfiguration.Builder(si.Config).WithAllowDirect(true).Build();
                si = jsm.UpdateStream(sc);
                Assert.True(si.Config.AllowDirect);
                ValidateGetMessage(jsm);

                // error case stream doesn't exist
                Assert.Throws<NATSJetStreamException>(() => jsm.GetMessage(Stream(999), 1));
            });
        }
        
        private void ValidateGetMessage(IJetStreamManagement jsm) {

            AssertMessageInfo(1, 1, jsm.GetMessage(STREAM, 1));
            AssertMessageInfo(1, 5, jsm.GetLastMessage(STREAM, Subject(1)));
            AssertMessageInfo(2, 6, jsm.GetLastMessage(STREAM, Subject(2)));

            AssertMessageInfo(1, 1, jsm.GetNextMessage(STREAM, 0, Subject(1)));
            AssertMessageInfo(2, 2, jsm.GetNextMessage(STREAM, 0, Subject(2)));
            AssertMessageInfo(1, 1, jsm.GetFirstMessage(STREAM, Subject(1)));
            AssertMessageInfo(2, 2, jsm.GetFirstMessage(STREAM, Subject(2)));

            AssertMessageInfo(1, 1, jsm.GetNextMessage(STREAM, 1, Subject(1)));
            AssertMessageInfo(2, 2, jsm.GetNextMessage(STREAM, 1, Subject(2)));

            AssertMessageInfo(1, 3, jsm.GetNextMessage(STREAM, 2, Subject(1)));
            AssertMessageInfo(2, 2, jsm.GetNextMessage(STREAM, 2, Subject(2)));

            AssertMessageInfo(1, 5, jsm.GetNextMessage(STREAM, 5, Subject(1)));
            AssertMessageInfo(2, 6, jsm.GetNextMessage(STREAM, 5, Subject(2)));

            AssertStatus(10003, Assert.Throws<NATSJetStreamException>(() => jsm.GetMessage(STREAM, 0)));
            AssertStatus(10037, Assert.Throws<NATSJetStreamException>(() => jsm.GetMessage(STREAM, 9)));
            AssertStatus(10037, Assert.Throws<NATSJetStreamException>(() => jsm.GetLastMessage(STREAM, "not-a-subject")));
            AssertStatus(10037, Assert.Throws<NATSJetStreamException>(() => jsm.GetFirstMessage(STREAM, "not-a-subject")));
            AssertStatus(10037, Assert.Throws<NATSJetStreamException>(() => jsm.GetNextMessage(STREAM, 9, Subject(1))));
            AssertStatus(10037, Assert.Throws<NATSJetStreamException>(() => jsm.GetNextMessage(STREAM, 1, "not-a-subject")));
        }

        private void AssertStatus(int apiErrorCode, NATSJetStreamException e) {
            Assert.Equal(apiErrorCode, e.ApiErrorCode);
        }
        
        private void AssertMessageInfo(int subj, ulong seq, MessageInfo mi) {
            Assert.Equal(STREAM, mi.Stream);
            Assert.Equal(Subject(subj), mi.Subject);
            Assert.Equal(seq, mi.Sequence);
            Assert.Equal("s" + subj + "-q" + seq, Encoding.UTF8.GetString(mi.Data));
        }

        [Fact]
        public void TestMessageGetRequest()
        {
            ValidateMgr(1, null, null, MessageGetRequest.ForSequence(1));
            ValidateMgr(0, "last", null, MessageGetRequest.LastForSubject("last"));
            ValidateMgr(0, null, "first", MessageGetRequest.FirstForSubject("first"));
            ValidateMgr(1, null, "first", MessageGetRequest.NextForSubject(1, "first"));
        }
        
        private void ValidateMgr(ulong seq, String lastBySubject, String nextBySubject, MessageGetRequest mgr) {
            Assert.Equal(seq, mgr.Sequence);
            Assert.Equal(lastBySubject, mgr.LastBySubject);
            Assert.Equal(nextBySubject, mgr.NextBySubject);
            Assert.Equal(seq > 0 && nextBySubject == null, mgr.IsSequenceOnly);
            Assert.Equal(lastBySubject != null, mgr.IsLastBySubject);
            Assert.Equal(nextBySubject != null, mgr.IsNextBySubject);
        }
    }
}
