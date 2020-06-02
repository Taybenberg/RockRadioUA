using System;
using System.Timers;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using ManagedBass;

namespace RockRadioUA
{
    class Radio
    {
        static readonly object Lock = new object();
        readonly Timer _timer;
        int _req; // request number/counter
        int _chan; // stream handle

        private string Url = null;

        public delegate void MessageHandler(string message);
        public event MessageHandler OnStatusUpdate;
        public event MessageHandler OnTitleAndArtistUpdate;


        string _status = string.Empty;
        public string Status
        {
            get { return _status; }
            private set
            {
                _status = value;
                OnStatusUpdate(_status);
            }
        }

        string _titleAndArtist = string.Empty;
        public string TitleAndArtist
        {
            get { return _titleAndArtist; }
            private set
            {
                _titleAndArtist = value;
                OnTitleAndArtistUpdate(_titleAndArtist);
            }
        }

        ~Radio() => Bass.Free();

        public Radio(string url)
        {
            if (!Bass.Init())
                throw new Exception("Критична помилка: не вдалося ініціалізувати відтворення");

            Bass.NetPlaylist = 1; // enable playlist processing
            Bass.NetPreBuffer = 0;  // minimize automatic pre-buffering, so we can do it (and display it) instead

            _timer = new Timer(50);
            _timer.Elapsed += _timer_Tick;

            Url = url;
        }

        public void Stop()
        {
            _timer.Stop();

            Bass.StreamFree(_chan);
        }

        public void Start()
        {
            if (Url == null)
                return;

            Task.Factory.StartNew(() =>
            {
                int r;

                lock (Lock) // make sure only 1 thread at a time can do the following
                    r = ++_req; // increment the request counter for this request

                _timer.Stop(); // stop prebuffer monitoring

                Bass.StreamFree(_chan); // close old stream

                Status = "Підключення…";

                var c = Bass.CreateStream(Url, 0, BassFlags.StreamDownloadBlocks | BassFlags.StreamStatus | BassFlags.AutoFree, StatusProc, new IntPtr(r));

                lock (Lock)
                {
                    if (r != _req)
                    {
                        if (c != 0) // there is a newer request, discard this stream
                            Bass.StreamFree(c);

                        return;
                    }

                    _chan = c; // this is now the current stream
                }

                if (_chan == 0)
                    Status = "Неможливо відтворити аудіопотік"; // failed to open
                else
                    _timer.Start(); // start prebuffer monitoring
            });
        }

        void _timer_Tick(object sender, EventArgs e)
        {
            // percentage of buffer filled
            var progress = Bass.StreamGetFilePosition(_chan, FileStreamPosition.Buffer) * 100 / Bass.StreamGetFilePosition(_chan, FileStreamPosition.End);

            if (progress > 75 || Bass.StreamGetFilePosition(_chan, FileStreamPosition.Connected) == 0)
            {
                // over 75% full (or end of download)
                _timer.Stop(); // finished prebuffering, stop monitoring

                Status = "Відтворення";

                DoMeta();  // get the stream title and set sync for subsequent titles

                Bass.ChannelSetSync(_chan, SyncFlags.MetadataReceived, 0, MetaSync); // Shoutcast
                Bass.ChannelSetSync(_chan, SyncFlags.OggChange, 0, MetaSync); // Icecast/OGG            
                Bass.ChannelSetSync(_chan, SyncFlags.End, 0, EndSync); // set sync for end of stream

                Bass.ChannelPlay(_chan); // play it!
            }
            else
                Status = $"Буферизація… {progress}%";
        }

        void StatusProc(IntPtr buffer, int length, IntPtr user)
        {
            if (buffer != IntPtr.Zero && length == 0 && user.ToInt32() == _req) // got HTTP/ICY tags, and this is still the current request
                Status = Marshal.PtrToStringUTF8(buffer); // display status
        }

        void EndSync(int Handle, int Channel, int Data, IntPtr User) => Status = "Кінець аудіопотоку";

        void MetaSync(int Handle, int Channel, int Data, IntPtr User) => DoMeta();

        void DoMeta()
        {
            var meta = Bass.ChannelGetTags(_chan, TagType.META);

            if (meta != IntPtr.Zero)
            {
                var data = Marshal.PtrToStringUTF8(meta);  // got Shoutcast metadata

                var i = data.IndexOf("StreamTitle='"); // locate the title

                if (i == -1)
                {
                    TitleAndArtist = string.Empty;

                    return;
                }

                var j = data.IndexOf("';", i); // locate the end of it

                if (j != -1)
                    TitleAndArtist = data.Substring(i + 13, j - i - 13);
                else
                    TitleAndArtist = string.Empty;
            }
        }
    }
}
