
namespace Knv.MAAC241213.IO
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.IO.Ports;
    using System.Text;
    using System.Web;

    public class OcxoStatus
    {
        /// <summary>
        /// - Az OCXO feszültsége <br/>
        /// - Névleges értéke: 12V <br/>
        /// - I2C Triclock: támogtaja <br/>
        /// - Legacy Triclock: nem támogatja, értéke: 0V <br/>
        /// </summary>
        public float Voltage { get; set; }
        /// <summary>
        /// - Az OCXO árama <br/> 
        /// - Névleges értéke: 0.4A (bekapcsolás pillanatában) <br/>
        /// - I2C Triclock: támogtaja <br/>
        /// - Legacy Triclock: nem támogatja, értéke 0A <br/>
        /// </summary>
        public float Current { get; set; }
        /// <summary>
        /// - Az OCXO hőmérséklete <br/>
        /// - Névleges értéke: 60C <br/>
        /// - I2C Triclock: támogtaja <br/>
        /// - Legacy Triclock: nem támogatja, értéke 0A
        /// </summary>
        public float Temperature { get; set; }
        /// <summary>
        /// - Az OCXO lock stáusza <br/>
        /// - Akár 2 prec is lehet, és kezdtben csak rövid időre lockolódik... <br/>
        /// - I2C Triclock: támogtaja <br/>
        /// - Legacy Triclock: támogatja, és a LegacyLocks-segítségével egyszerübben elérhető  <br/>
        /// </summary>
        public bool IsLocked { get; set; }

    }

    public class RefOcxoStatus
    {
        /// <summary>
        /// - Az OCXO feszültsége <br/>
        /// - Névleges értéke: 12V <br/>
        /// - I2C Triclock: támogtaja <br/>
        /// - Legacy Triclock: nem támogatja, értéke: 0V <br/>
        /// </summary>
        public float Voltage { get; set; }
        /// <summary>
        /// - Az OCXO árama <br/> 
        /// - Névleges értéke: 0.4A (bekapcsolás pillanatában) <br/>
        /// - I2C Triclock: támogtaja <br/>
        /// - Legacy Triclock: nem támogatja, értéke 0A <br/>
        /// </summary>
        public float Current { get; set; }
        /// <summary>
        /// - Az OCXO hőmérséklete <br/>
        /// - Névleges értéke: 60C <br/>
        /// - I2C Triclock: támogajta... <br/>
        /// - Legacy Triclock: nem támogatja, értéke 152.918C ±1%<br/>
        /// </summary>
        public float Temperature { get; set; }

        /// <summary>
        /// I2C Triclock: nem támogatja
        /// Legacy Triclock: támogatja
        /// </summary>
        public float LegacyTemperature { get; set; }

        /// <summary>
        /// true esetén a Refencia órajel külső, false-esteén a belső
        /// </summary>
        public bool ExtRef { get; set; }
    }

    public class Connection : IDisposable
    {
        public Connection()
        {
            TraceLines = 0;
        }

        const string GenericTimestampFormat = "yyyy.MM.dd HH:mm:ss";

        public event EventHandler ConnectionChanged;
        public event EventHandler ErrorHappened;

        public List<string> TraceList = new List<string>();
        public int TraceLines { get; private set; }
        
        SerialPort _sp;
        public bool IsOpen
        {
            get
            {
                if (_sp == null)
                    return false;
                else
                    return _sp.IsOpen;
            }
        }

        public string NewLine { get { return "\r\n"; } }

        public int ReadTimeout 
        {
            get { return _sp.ReadTimeout; }
            set { _sp.ReadTimeout = value; }
        }

        public static string[] GetPortNames()
        {
            return SerialPort.GetPortNames();
        }


        private string _logDirectory = string.Empty;


        /// <summary>
        /// - A Logolás(Tracing) az Open-el kezdődik és a Dispose-al záródik... <br/>
        /// - logDirectory: csak a log könyvtár elérési újtja kell, a fájlnevet naponta generálja, ajánlott a Public/Documents használata...<br/>
        /// - Minden esetben zárd a portot... <br/>
        /// - Ajnlott a using() használata esetén miden zárásos dolog megtörténik... <br/>
        /// </summary>
        /// <param name="port">pl: COM1</param>
        public void Open(string port, string logDirectory)
        { 
            _logDirectory = logDirectory;
            Open(port);
        }
        /// <summary>
        /// - Minden esetben zárd a portot... <br/>
        /// - Ajnlott a using() használata... <br/>
        /// </summary>
        /// <param name="port">pl: COM1</param>
        public void Open(string port)
        {
            try
            {
                _sp = new SerialPort(port)
                {
                    ReadTimeout = 1000,
                    BaudRate = 115200,
                    DtrEnable = true, //RPi pico support
                    NewLine = "\r"
                };
                _sp.Open();
                _sp.DiscardInBuffer();
                Trace("Serial Port: " + port + " is Open.");
                Test();
                OnConnectionChanged();
            }
            catch (Exception ex)
            {
                _sp.Close();
                Trace("IO ERROR Serial Port is: " + port + " Open fail... beacuse:" + ex.Message);
                OnConnectionChanged();
            }
        }

        public void Test()
        {
            if (_sp == null || !_sp.IsOpen)
            {
                Trace("IO ERROR: port is closed.");
            }

            try
            {
                var resp = WriteRead("*OPC?");
                if (resp == null || resp != "*OPC")
                    Trace("Test Failed");
            }
            catch (Exception ex)
            {
                Trace("IO-ERROR:" + ex.Message);
            }
        }

        internal string WriteRead(string request)
        {
            string response = string.Empty;
            Exception exception = null;
            int rxErrors = 0;
            int txErrors = 0;

            do
            {
                if (_sp == null || !_sp.IsOpen)
                {
                    var msg = $"The {_sp.PortName} Serial Port is closed. Please open it.";
                    Trace(msg);
                    OnConnectionChanged();
                    throw new ApplicationException(msg);
                }
                try
                {
                    Trace("Tx: " + request);
                    _sp.WriteLine(request);

                    try
                    {
                        response = _sp.ReadLine().Trim(new char[] { '\0', '\r', '\n' }); ;
                        Trace("Rx: " + response);
                        return response;
                    }
                    catch (Exception ex) //TODO: Nem jol van kezelve a TIMOUT
                    {
                        Trace("Rx ERROR Serial Port is:" + ex.Message);
                        exception = new TimeoutException( $"Last Request: { request}",ex );
                        rxErrors++;
                        OnErrorHappened();
                    }
                }
                catch (Exception ex)
                {
                    Trace("Tx ERROR Serial Port is:" + ex.Message);
                    exception = ex;
                    txErrors++;
                    OnErrorHappened();
                }

            } while (rxErrors < 3 && txErrors < 3);
          
            Trace("There were three consecutive io error. I close the connection.");
            Close();
            throw exception;
        }

        internal string WriteReadWoTracing(string request)
        {
            string response = string.Empty;
            Exception exception = null;
            int rxErrors = 0;
            int txErrors = 0;

            do
            {
                if (_sp == null || !_sp.IsOpen)
                {
                    var msg = $"The {_sp.PortName} Serial Port is closed. Please open it.";
                    Trace(msg);
                    OnConnectionChanged();
                    throw new ApplicationException(msg);
                }
                try
                {
                    _sp.WriteLine(request);
                    try
                    {
                        response = _sp.ReadLine().Trim(new char[] { '\0', '\r', '\n' }); ;
                        return response;
                    }
                    catch (Exception ex)
                    {
                        exception = new TimeoutException($"Last Request: {request}", ex);
                        rxErrors++;
                        OnErrorHappened();
                    }
                }
                catch (Exception ex)
                {
                    Trace("Tx ERROR Serial Port is:" + ex.Message);
                    exception = ex;
                    txErrors++;
                    OnErrorHappened();
                }

            } while (rxErrors < 3 && txErrors < 3);

            Trace("There were three consecutive io error. I close the connection.");
            Close();
            throw exception;
        }

        public void Close()
        {
            TraceList.Add(DateTime.Now.ToString(GenericTimestampFormat) + " " + "Serial Port is: " + "Close");
            _sp.Close();
            OnConnectionChanged();
        }

        internal void TraceError(string errorMsg)
        {
            TraceLines++;
            TraceList.Add(DateTime.Now.ToString(GenericTimestampFormat) + " " + errorMsg);
        }

        internal void Trace(string msg)
        {
            if (!string.IsNullOrEmpty(_logDirectory))
            {
                TraceLines++;
                TraceList.Add(DateTime.Now.ToString(GenericTimestampFormat) + " " + msg);
            }
        }

        public void TraceClear()
        {
            TraceList.Clear();
            TraceLines = 0;
        }
        protected virtual void OnConnectionChanged()
        {
            EventHandler handler = ConnectionChanged;
            handler?.Invoke(this, EventArgs.Empty);
        }
        protected virtual void OnErrorHappened()
        {
            EventHandler handler = ErrorHappened;
            handler?.Invoke(this, EventArgs.Empty);
        }
        /// <summary>
        /// Firmware Verziószáma
        /// </summary>
        /// <returns>pl. 1.0.0.0</returns>
        public string GetVersion()
        {
            var resp = WriteRead("VER?");
            if (resp == null)
                return "n/a";
            else
                return resp;
        }
        /// <summary>
        /// Processzor egyedi azonsítója, hosza nem változik
        /// </summary>
        /// <returns>pl:20001E354D501320383252</returns>
        public string UniqeId()
        {
            var resp = WriteRead("UID?");
            if (resp == null)
                return "n/a";
            else
                return resp;
        }
        /// <summary>
        /// Bekapcsolás óta eltelt idő másodpercben
        /// </summary>
        /// <returns>másodperc</returns>
        public int GetUpTime()
        {
            var resp = WriteRead("UPTIME?");
            if (resp == null)
                return 0;
            else if (int.TryParse(resp, NumberStyles.HexNumber, CultureInfo.GetCultureInfo("en-US"), out int retval))
                return retval;
            else
                return 0;
        }

        /// <summary>
        /// A panel varáció neve pl: MGUI201222V00-PCREF
        /// </summary>
        /// <returns></returns>
        public string WhoIs()
        {
            var resp = WriteRead("*IDN?");
            if (resp == null)
                return "n/a";
            else
                return resp;
        }

        /// <summary>
        /// Beállított háttérfényerő százalékban
        /// </summary>
        /// <returns>0..100</returns>
        public int BacklightIntesity()
        {
            var resp = WriteRead("BLIGHT:PWM?");
            if (resp == null)
                return -1;
            else if (int.TryParse(resp, NumberStyles.Integer, CultureInfo.GetCultureInfo("en-US"), out var retval))
                return retval;
            else
                return -1;
        }

        public void BacklightIntesity(int percent)
        {
            WriteRead($"BLIGHT:PWM {percent:N2}");
        }

        /// <summary>
        /// - A háttérvilágítás bekapcsolása <br/>
        /// - A háttérvilágítás bekacsolásával megáll a frimwarebe "ForceBacklightOn" funkció... <br/>
        /// - A ForceBacklightOn Timeoutja kb: 10sec <br/> 
        /// - A Bekapcsolás uán, vagy közvetlenül előtte állísd be a megfelelő <see cref="void BacklightIntesity(int percent)"/> <br/>
        /// - A PC kikapcsolásával kikapcsol a kijelző mindentől függetlenül.<br/>
        /// </summary>
        public void BacklightOn()
        {
            WriteRead("BLIGHT:ON");
        }

        /// <summary>
        /// - A háttérvilágítás kekapcsolása <br/>
        /// </summary>
        public void BacklightOff()
        {
            WriteRead("BLIGHT:OFF");
        }

        /// <summary>
        /// - A háttérvilágítás akutális állapota <br/>
        /// true ha bekapcsolt...<br/>
        /// </summary>
        public bool BacklightIsOn()
        {
            var resp = WriteRead("BLIGHT?");
            if (resp == "0")
                return false;
            else if (resp == "1")
                return true;
            else
                Trace("IO-ERROR: Invalid Response.");
            return false;
        }

        /// <summary>
        /// Vissza adja azt az időt másodpercben ami után a kijelző biztos bekpacsol, a PC bekapcsolásától számítva. <br/>
        /// Ciklikus írása nem ajánlott, mivel hardveres eeprom írással jár, aminek véges az írási ciklusa
        /// </summary>
        /// <returns></returns>
        public int BacklightTimeoutInSec()
        {
            var resp = WriteRead("BLIGHT:TIMEOUT?");
            if (resp == null)
                return -1;
            else if (int.TryParse(resp, NumberStyles.Integer, CultureInfo.GetCultureInfo("en-US"), out var retval))
                return retval;
            else
                return -1;
        }

        /// <summary>
        /// Beállíthatod azt az időt ami a PC bekapcsolásától számítva a kijelző biztos bekapcsolódik.
        /// </summary>
        /// <param name="seconds"></param>
        public void BacklightTimeoutInSec(int seconds) 
        {
            WriteRead($"BLIGHT:TIMEOUT {seconds:N2}");
        }

        /// <summary>
        /// Az OCXO1-es Státusza <br/>
        /// Frekevenciája: 24MHz <br/>
        /// </summary>
        public OcxoStatus Ocxo1Status()
        {
            return OcxoStatus("TRICLOCK:OCXO1:STAT?");
        }

        /// <summary>
        /// Az OCXO2-es Státusza <br/>
        /// Frekevenciája: 20MHz <br/>
        /// </summary>
        public OcxoStatus Ocxo2Status()
        {
            return OcxoStatus("TRICLOCK:OCXO2:STAT?");
        }

        /// <summary>
        /// Az OCXO3-es Státusza <br/>
        /// Frekevenciája: 25MHz <br/>
        /// </summary>
        public OcxoStatus Ocxo3Status()
        {
            return OcxoStatus("TRICLOCK:OCXO3:STAT?");
        }


        OcxoStatus OcxoStatus(string command)
        {
            var retval = new OcxoStatus();
            var resp = WriteRead(command);
            if (resp == null)
                return null;
            else
            {
                string[] valueArr = resp.Split(new char[]{';'});
                try
                {
                    retval.Voltage = float.Parse(valueArr[0], NumberStyles.Float, CultureInfo.GetCultureInfo("en-US"));
                    retval.Current = float.Parse(valueArr[1], NumberStyles.Float, CultureInfo.GetCultureInfo("en-US"));
                    retval.Temperature = float.Parse(valueArr[2], NumberStyles.Float, CultureInfo.GetCultureInfo("en-US"));
                    retval.IsLocked = valueArr[3] == "L";
                }
                catch (Exception ex)
                {
                    Trace($"IO-ERROR: {ex.Message}");
                }
            }
            return retval;
        }


        /// <summary>
        /// A REFOCXO Státusza <br/>
        /// Frekevenciája: 10MHz <br/>
        /// </summary>
        public RefOcxoStatus RefOcxoStatus()
        {
            var retval = new RefOcxoStatus();
            var resp = WriteRead("TRICLOCK:REFOCXO:STAT?");
            if (resp == null)
                return null;
            else
            {
                string[] valueArr = resp.Split(new char[] { ';' });
                try
                {
                    retval.Voltage = float.Parse(valueArr[0], NumberStyles.Float, CultureInfo.GetCultureInfo("en-US"));
                    retval.Current = float.Parse(valueArr[1], NumberStyles.Float, CultureInfo.GetCultureInfo("en-US"));
                    retval.Temperature = float.Parse(valueArr[2], NumberStyles.Float, CultureInfo.GetCultureInfo("en-US"));
                    retval.LegacyTemperature = float.Parse(valueArr[3], NumberStyles.Float, CultureInfo.GetCultureInfo("en-US"));
                    retval.ExtRef = valueArr[4] == "E";
                }
                catch (Exception ex)
                {
                    Trace($"IO-ERROR: {ex.Message}");
                }
            }
            return retval;
        }

        public void Dispose()
        {
            if (_sp != null)
            {
                _sp.Close();
                _sp.Dispose();
            }

            if(!string.IsNullOrEmpty(_logDirectory))
               TracingToFile( _logDirectory );
        }


        public void TracingToFile(string directory)
        {
            if (!File.Exists(directory))
                Directory.CreateDirectory(directory);

            var dt = DateTime.Now;
            var filePath = $"{directory}\\aac_io_log_{dt:yyyy}{dt:MM}{dt:dd}.txt";

            var fileWrite = new StreamWriter(filePath, true, Encoding.ASCII);
            fileWrite.NewLine = NewLine;

            for (int i = 0; i < TraceList.Count; i++)
            {
                string line = TraceList[i];
                fileWrite.Write(line + NewLine);
            }

            fileWrite.Flush();
            fileWrite.Close();
            TraceList.Clear();
        }
    }
}
