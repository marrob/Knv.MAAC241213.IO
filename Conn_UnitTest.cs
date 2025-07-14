namespace Knv.MAAC241213.IO
{
    using NUnit.Framework;
    using System;

    [TestFixture]
    internal class Conn_UnitTest
    {
        const string COM_PORT = "COM3";

        [Test]
        public void Name_Version_UnitTest()
        {
            using (var conn = new Connection())
            {
                conn.Open(COM_PORT);
                var name = conn.WhoIs();
                Console.WriteLine(name);
                Assert.AreEqual("MAAC241213.FW", name);

                var version = conn.GetVersion();
                Console.WriteLine(version);
                Assert.AreEqual("250422_1204", version);
            }
        }

        [Test]
        public void BacklighOnOff_UnitTest()
        {
            using (var conn = new Connection())
            {
                conn.Open(COM_PORT);
                var name = conn.WhoIs();
                Console.WriteLine(name);
                Assert.AreEqual("MAAC241213.FW", name);

                conn.BacklightOn();
                Assert.IsTrue(conn.BacklightIsOn());
                conn.BacklightOff();
                Assert.IsFalse(conn.BacklightIsOn());
                conn.BacklightOn();
                Assert.IsTrue(conn.BacklightIsOn());
                conn.BacklightOff();
                Assert.IsFalse(conn.BacklightIsOn());
            }
        }

        [Test]
        public void BaclightIntesity_UnitTest()
        {
            using (var conn = new Connection())
            {
                conn.Open(COM_PORT);
                var name = conn.WhoIs();
                Console.WriteLine(name);
                Assert.AreEqual("MAAC241213.FW", name);

                conn.BacklightOn();

                conn.BacklightIntesity(0);
                Assert.AreEqual(0, conn.BacklightIntesity());

                conn.BacklightIntesity(50);
                Assert.AreEqual(50, conn.BacklightIntesity());

                conn.BacklightIntesity(100);
                Assert.AreEqual(100, conn.BacklightIntesity());

                conn.BacklightOff();
            }
        }

        [Test]
        public void TriClockStatus_UnitTest()
        {
            using (var conn = new Connection())
            {
                conn.Open(COM_PORT);
                var name = conn.WhoIs();
                Console.WriteLine(name);
                Assert.AreEqual("MAAC241213.FW", name);

                Assert.Multiple(() =>
                {
                    var status1 = conn.Ocxo1Status();
        
                    Assert.AreEqual(true, status1.IsLocked);
                    Assert.GreaterOrEqual(status1.Temperature, 10);
                    Assert.GreaterOrEqual(status1.Voltage, 11);
                    Assert.GreaterOrEqual(status1.Current, 0.01);

                    var status2 = conn.Ocxo2Status();
                    //Assert.AreEqual(true, status2.IsLocked);
                    Assert.GreaterOrEqual(status2.Temperature, 10);
                    Assert.GreaterOrEqual(status2.Voltage, 11);
                    //Assert.GreaterOrEqual(status2.Current, 0.01);

                    var status3 = conn.Ocxo3Status();
                    Assert.AreEqual(true, status3.IsLocked);
                    Assert.GreaterOrEqual(status3.Temperature, 10);
                    Assert.GreaterOrEqual(status3.Voltage, 11);
                    Assert.GreaterOrEqual(status3.Current, 0.01);

                    var refocxo = conn.RefOcxoStatus();
                    Assert.AreEqual(false, refocxo.ExtRef);
                    Assert.GreaterOrEqual(refocxo.Temperature, 10);
                    Assert.GreaterOrEqual(refocxo.Voltage, 11);
                    Assert.GreaterOrEqual(refocxo.Current, 0.01);
                });
            }
        }

        [Test]
        public void Log()
        {
            using (var conn = new Connection())
            {

                var myDoc = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

                conn.Open(COM_PORT, $"{myDoc}\\MAAC241213");
                var name = conn.WhoIs();
                Console.WriteLine(name);
                Assert.AreEqual("MAAC241213.FW", name);  

                Assert.Multiple(() =>
                {
                    var status1 = conn.Ocxo1Status();
                    Assert.AreEqual(true, status1.IsLocked);

                    var status2 = conn.Ocxo2Status();
                    Assert.AreEqual(true, !status2.IsLocked);

                    var status3 = conn.Ocxo3Status();
                    Assert.AreEqual(true, status3.IsLocked);

                    var refocxo = conn.RefOcxoStatus();
                    Assert.AreEqual(false, refocxo.ExtRef);
                });
            }
        }

    }
}
