using System;
using NetMQ.Devices;
using NetMQ.Sockets;
using NUnit.Framework;

namespace NetMQ.Devices.Tests
{
    public abstract class ForwarderDeviceTestBase : DeviceTestBase<ForwarderDevice, SubscriberSocket>
    {

        protected const string Topic = "CanHazTopic";

        protected override void SetupTest()
        {
            CreateDevice = () =>
            {
                var device = new ForwarderDevice(Frontend, Backend);
                device.FrontendSetup.Subscribe(Topic);
                return device;
            };

            CreateClientSocket = () => new PublisherSocket();
        }

        protected override void WorkerSocketAfterConnect(SubscriberSocket socket)
        {
            socket.Subscribe(Topic);
        }

        protected override void DoWork(NetMQSocket socket)
        {
            var received = socket.ReceiveMultipartStrings();
            Console.WriteLine("Worker received: ");

            for (var i = 0; i < received.Count; i++)
            {
                var r = received[i];
                Console.WriteLine("{0}: {1}", i, r);
            }

            Console.WriteLine("------");
        }

        protected override void DoClient(int id, NetMQSocket socket)
        {
            const string value = "Hello World";
            var expected = value + " " + id;
            Console.WriteLine("Client: {0} Publishing: {1}", id, expected);
            socket.SendMoreFrame(Topic);
            socket.SendFrame(expected);
        }

        protected override SubscriberSocket CreateWorkerSocket()
        {
            return new SubscriberSocket();
        }
    }

    [TestFixture]
    public class ForwarderSingleClientTest : ForwarderDeviceTestBase
    {
        [Test]
        public void Run()
        {

            // Allow the subscribers some time to connect
            StartClient(0, 100);

            SleepUntilWorkerReceives(1, TimeSpan.FromMilliseconds(1000));

            StopWorker();
            Device.Stop();

            Assert.AreEqual(1, WorkerReceiveCount);
        }
    }

    [TestFixture]
    public class ForwarderMultiClientTest : ForwarderDeviceTestBase
    {
        [Test]
        public void Run()
        {
            for (var i = 0; i < 10; i++)
            {
                // Allow the subscribers some time to connect
                StartClient(i, 100);
            }

            SleepUntilWorkerReceives(10, TimeSpan.FromMilliseconds(8000));

            StopWorker();
            Device.Stop();

            Assert.AreEqual(10, WorkerReceiveCount);
        }
    }
}