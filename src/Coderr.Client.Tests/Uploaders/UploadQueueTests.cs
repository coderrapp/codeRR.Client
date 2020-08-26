﻿using System;
using System.Threading.Tasks;
using Coderr.Client.Uploaders;
using FluentAssertions;
using Xunit;

namespace Coderr.Client.NetStd.Tests.Uploaders
{
    public class UploadQueueTests
    {
        [Fact]
        public void should_invoke_Background_job()
        {
            var uploadIsInvoked = false;
            var sut = new UploadQueue<string>(x => uploadIsInvoked = true) {ActivateSync = true};

            sut.Enqueue("hello");

            sut.Wait(100);
            sut.TaskWasInvoked.Should().BeTrue();
            uploadIsInvoked.Should().BeTrue();
        }

        [Fact]
        public void should_invoke_directly_if_queue_is_empty()
        {
            var uploadIsInvoked = false;
            var directly = false;
            var sut = new UploadQueue<string>(x => uploadIsInvoked = true) {ActivateSync = true};

            sut.EnqueueIfNotEmpty("hello", () => directly = true);

            sut.Wait(100);
            directly.Should().BeTrue("because queue is empty");
            uploadIsInvoked.Should()
                .BeFalse(
                    "because it should not be invoked if a task is supplied as an argument and the queue was empty.");
        }

        [Fact]
        public void should_not_invoke_directly_if_queue_got_items()
        {
            var uploadIsInvoked = false;
            var directly = false;
            var sut = new UploadQueue<string>(x =>
            {
                uploadIsInvoked = true;
                Task.Delay(50).Wait();
            });
            sut.Enqueue("precondition");
            sut.ActivateSync = true;

            sut.EnqueueIfNotEmpty("hello", () => directly = true);

            sut.Wait(10000);
            directly.Should().BeFalse("because there is an active worker");
            uploadIsInvoked.Should()
                .BeTrue(
                    "because it should be invoked even if a task is supplied as an argument when the queue is not empty");
        }


        [Fact]
        public void should_throw_new_entries_if_queue_is_full()
        {
            var failed = false;
            var sut = new UploadQueue<string>(x => Task.Delay(1000).Wait())
            {
                MaxQueueSize = 1,
                ActivateSync = true
            };
            sut.Enqueue("hello");

            sut.UploadFailed += (o, e) => failed = true;
            sut.Enqueue("hello");

            failed.Should().BeTrue();
        }

        [Fact]
        public void should_retry_if_upload_fails()
        {
            int attempts = 0;
            var sut = new UploadQueue<string>(x =>
            {
                attempts++;
                throw new NotSupportedException("we need more power");
            })
            {
                ActivateSync = true,
                RetryInterval = TimeSpan.FromMilliseconds(10)
            };

            sut.Enqueue("hello");

            sut.Wait(1000);
            attempts.Should().BeGreaterThan(1);
        }

        [Fact]
        public void should_throw_report_after_max_attempts()
        {
            int attempts = 0;
            var sut = new UploadQueue<string>(x =>
            {
                attempts++;
                throw new NotSupportedException("we need more power");
            })
            {
                ActivateSync = true,
                RetryInterval = TimeSpan.FromMilliseconds(10),
                MaxAttempts = 3
            };
            bool failed = false;
            sut.UploadFailed += (sender, args) => failed = true;

            sut.Enqueue("hello");

            sut.Wait(1000);
            failed.Should().BeTrue();
        }

        [Fact]
        public void should_not_invoke_Failed_report_if_upload_is_successful()
        {
            var sut = new UploadQueue<string>(x => { })
            {
                ActivateSync = true,
                RetryInterval = TimeSpan.FromMilliseconds(10),
                MaxAttempts = 3
            };
            var failed = false;
            sut.UploadFailed += (sender, args) => failed = true;

            sut.Enqueue("hello");

            sut.Wait(1000);
            failed.Should().BeFalse();
        }
    }
}